using System;
using System.Collections.Generic;
using System.Linq;
using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>Assets a level prefab is built from.</summary>
    internal sealed class LevelAssets
    {
        public Tile groundTile;
        public Tile platformTile;
        public Tile breakableTile;
        public Tile waterTile;
        public int groundLayer;
    }

    /// <summary>
    /// Authoring DSL for a level. Lay out geometry left to right (Flat / Gap / Mound / Floating / Secret*),
    /// THEN add objects (crops, robots, checkpoints, start, goal, gated secrets); relative placement reads the
    /// ground already laid. Build() validates the design against the shared base jump numbers and produces the
    /// level prefab: tilemaps + composite colliders + pit trigger + empty marker objects. Gameplay objects are
    /// not in the prefab; LevelLoader spawns them from pools at the markers.
    ///
    /// Base jump numbers (moveSpeed 8, jumpHeight 3.5): a flat gap is comfortably crossable up to ~5 units and a
    /// rise up to ~2.5. Anything the base jump cannot reach may only exist as a *secret* surface, and the
    /// validator insists secrets really are unreachable without an ability. See <see cref="CanReach"/>.
    /// </summary>
    internal sealed class LevelBuilder
    {
        private enum Kind { Ground, Mound, Floating }

        private sealed class Surface
        {
            public int x0, x1, top, baseTop;
            public Kind kind;
            public bool secret;
            public override string ToString() => $"{(secret ? "Secret" : "")}{kind}[{x0},{x1}) top={top}";
        }

        private struct CropRec { public float x, y; public bool secret; }
        private struct RobotRec { public RobotType type; public float x, y, patrol; public int wave; }
        private struct BreakableRec { public int x, length, top; }
        private struct ChamberRec { public int x0, floorTop, interiorWidth; public bool barrierSeal; }

        private const float MaxRise = 2.5f;
        private const float MaxFlatGap = 5f;
        private const float RiseGapPenalty = 0.7f;
        private const int GroundDepth = -6;

        public readonly string Id;
        public readonly string Title;

        private readonly int _startX;
        private int _x;
        private int _top;
        private readonly List<Surface> _surfaces = new();
        private readonly List<CropRec> _crops = new();
        private readonly List<RobotRec> _robots = new();
        private readonly List<Vector2> _checkpoints = new();
        private readonly List<BreakableRec> _breakables = new();
        private readonly List<ChamberRec> _chambers = new();
        private readonly List<RectInt> _water = new();
        private Vector2? _playerStart;
        private Vector2? _goal;
        private readonly List<float> _noPatrolCheck = new();
        private readonly List<string> _errors = new();

        public bool IsBoss { get; private set; }
        private bool _noGoal;

        public bool HasGate { get; private set; }
        public CharacterType GatePrimary { get; private set; }
        public CharacterType[] GateAlso { get; private set; } = new CharacterType[0];
        public string GateDescription { get; private set; } = "";

        public int X => _x;
        public int CropCount => _crops.Count;
        public int RobotCount => _robots.Count;
        public int CheckpointCount => _checkpoints.Count;
        public int SecretCropCount => _crops.Count(c => c.secret);

        public LevelBuilder(string id, string title, int startX = -4)
        {
            Id = id;
            Title = title;
            _startX = startX;
            _x = startX;
        }

        // ------------------------------------------------------------ layout

        /// <summary>Solid ground of the given length from the cursor. Pass a top to change the ground height.</summary>
        public LevelBuilder Flat(int length, int? top = null)
        {
            return AddFlat(length, top, false);
        }

        /// <summary>Ground that only exists for a secret (e.g. an island across a chasm). Excluded from the main path.</summary>
        public LevelBuilder SecretFlat(int length, int? top = null)
        {
            return AddFlat(length, top, true);
        }

        private LevelBuilder AddFlat(int length, int? top, bool secret)
        {
            if (top.HasValue)
            {
                _top = top.Value;
            }
            _surfaces.Add(new Surface { x0 = _x, x1 = _x + length, top = _top, baseTop = _top, kind = Kind.Ground, secret = secret });
            _x += length;
            return this;
        }

        /// <summary>A pit: no ground for this many units. Falling in respawns the player.</summary>
        public LevelBuilder Gap(int width)
        {
            _x += width;
            return this;
        }

        /// <summary>Solid block standing on the ground (a step / stepping stone).</summary>
        public LevelBuilder Mound(int x, int length, int height)
        {
            int baseTop = GroundTopAt(x, out bool found);
            if (!found || Enumerable.Range(x, length).Any(cx => GroundTopAt(cx + 0.5f, out _) != baseTop))
            {
                Error($"Mound at x={x} needs level ground of one height under it.");
                return this;
            }

            _surfaces.Add(new Surface { x0 = x, x1 = x + length, top = baseTop + height, baseTop = baseTop, kind = Kind.Mound });
            return this;
        }

        /// <summary>One-tile-thick platform floating with its top surface at the given height.</summary>
        public LevelBuilder Floating(int x, int length, int top)
        {
            _surfaces.Add(new Surface { x0 = x, x1 = x + length, top = top, baseTop = top - 1, kind = Kind.Floating });
            return this;
        }

        /// <summary>A floating ledge the base jump cannot reach: a gated secret needing extra height or range.</summary>
        public LevelBuilder SecretLedge(int x, int length, int top)
        {
            _surfaces.Add(new Surface { x0 = x, x1 = x + length, top = top, baseTop = top - 1, kind = Kind.Floating, secret = true });
            return this;
        }

        /// <summary>
        /// Breakable Floor (Bessie): the top tile row of the ground over [x, x+length) becomes breakable tiles,
        /// with a two-tall hollow chamber beneath. Put secret crops in the hollow (y = top - 2.5).
        /// </summary>
        public LevelBuilder BreakableFloor(int x, int length)
        {
            int top = GroundTopAt(x, out bool found);
            if (!found || Enumerable.Range(x, length).Any(cx => GroundTopAt(cx + 0.5f, out _) != top))
            {
                Error($"Breakable floor at x={x} needs level ground of one height under it.");
                return this;
            }

            _breakables.Add(new BreakableRec { x = x, length = length, top = top });
            return this;
        }

        /// <summary>
        /// Sealed chamber (Billy) standing on a floating platform whose top is floorTop. The chamber occupies
        /// cells x0 .. x0+interiorWidth+1: a Breakable Wall at x0 (3 tall), the hollow, a solid right wall and
        /// ceiling. The supporting platform must already cover that span. Put secret crops inside.
        /// </summary>
        public LevelBuilder Chamber(int x0, int floorTop, int interiorWidth, bool sealWithBarrierUnit = false)
        {
            _chambers.Add(new ChamberRec { x0 = x0, floorTop = floorTop, interiorWidth = interiorWidth, barrierSeal = sealWithBarrierUnit });
            return this;
        }

        /// <summary>Marks this as the world's boss level: no goal marker (defeating the Commander completes it).</summary>
        public LevelBuilder Boss()
        {
            IsBoss = true;
            _noGoal = true;
            return this;
        }

        /// <summary>Water tiles over the cell rectangle [x0,x1) x [y0,y1). A trigger tile type: no World 1 level uses it.</summary>
        public LevelBuilder Water(int x0, int x1, int y0, int y1)
        {
            _water.Add(new RectInt(x0, y0, x1 - x0, y1 - y0));
            return this;
        }

        /// <summary>Records which character(s) the level's secret is designed around (written to LevelData).</summary>
        public LevelBuilder Gate(CharacterType primary, string description, params CharacterType[] alsoOpens)
        {
            HasGate = true;
            GatePrimary = primary;
            GateAlso = alsoOpens;
            GateDescription = description;
            return this;
        }

        // ------------------------------------------------------------ markers and objects

        public LevelBuilder Start(int x)
        {
            _playerStart = new Vector2(x, GroundTop(x) + 0.6f);
            return this;
        }

        public LevelBuilder Goal(int x)
        {
            _goal = new Vector2(x, GroundTop(x));
            return this;
        }

        public LevelBuilder Checkpoint(int x)
        {
            _checkpoints.Add(new Vector2(x, GroundTop(x)));
            return this;
        }

        /// <summary>Crop resting above the ground (or mound) at x.</summary>
        public LevelBuilder Crop(float x, float above = 0.5f, bool secret = false)
        {
            _crops.Add(new CropRec { x = x, y = GroundTop(x) + above, secret = secret });
            return this;
        }

        /// <summary>Evenly spaced crops above the ground from x0 to x1 inclusive.</summary>
        public LevelBuilder CropRow(float x0, float x1, float step, float above = 0.5f)
        {
            for (float x = x0; x <= x1 + 0.001f; x += step)
            {
                Crop(x, above);
            }
            return this;
        }

        /// <summary>Crop at an absolute world position (over gaps, on floating platforms, in alcoves).</summary>
        public LevelBuilder CropAt(float x, float y, bool secret = false)
        {
            _crops.Add(new CropRec { x = x, y = y, secret = secret });
            return this;
        }

        /// <summary>Row of secret-cluster crops at an absolute height.</summary>
        public LevelBuilder SecretRow(float x0, float x1, float step, float y)
        {
            for (float x = x0; x <= x1 + 0.001f; x += step)
            {
                CropAt(x, y, true);
            }
            return this;
        }

        /// <summary>Arc of n crops from x0 to x1, rising from baseY by peak in the middle (a jump-path hint).</summary>
        public LevelBuilder CropArc(float x0, float x1, int n, float baseY, float peak)
        {
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0.5f : i / (float)(n - 1);
                CropAt(Mathf.Lerp(x0, x1, t), baseY + peak * Mathf.Sin(Mathf.PI * t));
            }
            return this;
        }

        /// <summary>Ground patrol robot at x, sweeping patrol units either side (ledges stop it early).</summary>
        public LevelBuilder Harvester(float x, float patrol, int wave = 0)
        {
            return AddGroundRobot(RobotType.Harvester, x, patrol, 0.5f, wave);
        }

        /// <summary>Fast patrol robot that turns to face a player who slips behind it.</summary>
        public LevelBuilder Scout(float x, float patrol, int wave = 0)
        {
            return AddGroundRobot(RobotType.Scout, x, patrol, 0.5f, wave);
        }

        /// <summary>Pursuer: chases the player within aggroRange along its flat platform (stops at ledges and walls).</summary>
        public LevelBuilder Chaser(float x, float aggroRange)
        {
            return AddGroundRobot(RobotType.Chaser, x, aggroRange, 0.5f, 0, checkPatrolFlat: false);
        }

        /// <summary>The world boss (2 wide, 2 tall). Patrols +/- patrol; needs several hits.</summary>
        public LevelBuilder Commander(float x, float patrol)
        {
            return AddGroundRobot(RobotType.Commander, x, patrol, 1.0f, 0);
        }

        /// <summary>
        /// Barrier Unit (1x3) standing on the ground at x. Only Billy's Charge Break clears it. It can be jumped
        /// over on open ground, so seal a chamber with it (Chamber(..., sealWithBarrierUnit: true)) instead.
        /// </summary>
        public LevelBuilder Barrier(float x)
        {
            return AddGroundRobot(RobotType.BarrierUnit, x, 0.1f, 1.5f, 0, checkPatrolFlat: false);
        }

        private LevelBuilder AddGroundRobot(RobotType type, float x, float patrol, float centreAboveGround, int wave, bool checkPatrolFlat = true)
        {
            _robots.Add(new RobotRec { type = type, x = x, y = GroundTop(x) + centreAboveGround, patrol = patrol, wave = wave });
            if (!checkPatrolFlat)
            {
                _noPatrolCheck.Add(x);
            }
            return this;
        }

        /// <summary>Flying robot hovering hoverAboveGround units above the ground at x, sweeping patrol either side.</summary>
        public LevelBuilder Drone(float x, float hoverAboveGround, float patrol, int wave = 0)
        {
            _robots.Add(new RobotRec { type = RobotType.Drone, x = x, y = GroundTop(x) + hoverAboveGround, patrol = patrol, wave = wave });
            return this;
        }

        // ------------------------------------------------------------ queries

        private float GroundTop(float x)
        {
            float top = GroundTopAt(x, out bool found);
            if (!found)
            {
                Error($"No ground at x={x} for a ground-relative object.");
            }
            return top;
        }

        private int GroundTopAt(float x, out bool found)
        {
            int best = int.MinValue;
            foreach (var s in _surfaces)
            {
                if (s.kind != Kind.Floating && x >= s.x0 && x < s.x1)
                {
                    best = Mathf.Max(best, s.top);
                }
            }
            found = best != int.MinValue;
            return found ? best : 0;
        }

        private void Error(string message)
        {
            _errors.Add($"[{Id}] {message}");
        }

        // World-space rectangles of the hollows under breakable floors and inside chambers (crops belong there).
        private IEnumerable<Rect> Hollows()
        {
            foreach (var b in _breakables)
            {
                yield return new Rect(b.x, b.top - 3, b.length, 2);
            }
            foreach (var c in _chambers)
            {
                yield return new Rect(c.x0 + 1, c.floorTop, c.interiorWidth, 3);
            }
        }

        // ------------------------------------------------------------ validation

        /// <summary>
        /// Can a player standing on 'from' reach 'to' with the shared base jump (no ability)? Rise is capped at
        /// MaxRise; the horizontal gap allowed shrinks as the rise grows.
        /// </summary>
        private static bool CanReach(Surface from, Surface to)
        {
            if (ReferenceEquals(from, to))
            {
                return false;
            }

            int dx = Mathf.Max(0, Mathf.Max(to.x0 - from.x1, from.x0 - to.x1));
            float rise = to.top - from.top;
            if (rise > MaxRise)
            {
                return false;
            }

            float maxDx = rise > 0f ? MaxFlatGap - RiseGapPenalty * rise : MaxFlatGap;
            return dx <= maxDx;
        }

        private void Validate()
        {
            if (_playerStart == null) { Error("No player start."); }
            if (_goal == null && !_noGoal) { Error("No goal."); }
            if (_surfaces.Count == 0) { return; }

            if (_playerStart.HasValue)
            {
                var startSurface = _surfaces
                    .Where(s => !s.secret && s.kind != Kind.Floating && _playerStart.Value.x >= s.x0 && _playerStart.Value.x < s.x1)
                    .OrderByDescending(s => s.top).FirstOrDefault();
                if (startSurface == null)
                {
                    Error("Player start is not over ground.");
                }
                else
                {
                    var reached = new HashSet<Surface> { startSurface };
                    var queue = new Queue<Surface>();
                    queue.Enqueue(startSurface);
                    while (queue.Count > 0)
                    {
                        var current = queue.Dequeue();
                        foreach (var next in _surfaces)
                        {
                            if (!reached.Contains(next) && CanReach(current, next))
                            {
                                reached.Add(next);
                                queue.Enqueue(next);
                            }
                        }
                    }

                    foreach (var s in _surfaces)
                    {
                        if (!s.secret && !reached.Contains(s))
                        {
                            Error($"Unreachable from start with the base jump: {s}.");
                        }
                        else if (s.secret && reached.Contains(s))
                        {
                            Error($"{s} is not actually gated: the base jump can reach it.");
                        }
                    }
                }
            }

            foreach (var f in _surfaces.Where(s => s.kind == Kind.Floating))
            {
                foreach (var g in _surfaces.Where(s => s.kind != Kind.Floating && f.x0 < s.x1 && s.x0 < f.x1))
                {
                    if (f.baseTop - g.top < 2)
                    {
                        Error($"Floating {f} is too low over {g}: needs at least 2 units of clearance.");
                    }
                }
            }

            foreach (var r in _robots.Where(r => (r.type == RobotType.Harvester || r.type == RobotType.Scout || r.type == RobotType.Commander)))
            {
                float baseTop = GroundTopAt(r.x, out _);
                for (float px = r.x - r.patrol; px <= r.x + r.patrol; px += 0.5f)
                {
                    int top = GroundTopAt(px, out bool found);
                    if (!found || top != baseTop)
                    {
                        Error($"Harvester at x={r.x} patrol {r.patrol} leaves its flat platform at x={px}.");
                        break;
                    }
                }

                if (_playerStart.HasValue && Mathf.Abs(_playerStart.Value.x - r.x) < r.patrol + 4f)
                {
                    Error($"Harvester at x={r.x} is too close to the player start.");
                }
                foreach (var c in _checkpoints.Where(c => Mathf.Abs(c.x - r.x) < r.patrol + 2f))
                {
                    Error($"Harvester at x={r.x} patrols over checkpoint at x={c.x}.");
                }
                foreach (var b in _breakables.Where(b => r.x + r.patrol > b.x - 1 && r.x - r.patrol < b.x + b.length + 1))
                {
                    Error($"Harvester at x={r.x} patrols over the breakable floor at x={b.x}.");
                }
            }

            foreach (var chamber in _chambers)
            {
                bool supported = _surfaces.Any(s => s.kind == Kind.Floating && s.top == chamber.floorTop
                    && s.x0 <= chamber.x0 && s.x1 >= chamber.x0 + chamber.interiorWidth + 2);
                if (!supported)
                {
                    Error($"Chamber at x={chamber.x0} needs a floating platform with top {chamber.floorTop} under all of it.");
                }
            }

            var hollows = Hollows().ToList();
            foreach (var c in _crops)
            {
                if (hollows.Any(h => h.Contains(new Vector2(c.x, c.y))))
                {
                    continue;
                }

                foreach (var s in _surfaces)
                {
                    float bottom = s.kind == Kind.Floating ? s.baseTop : GroundDepth;
                    if (c.x > s.x0 - 0.3f && c.x < s.x1 + 0.3f && c.y - 0.4f < s.top && c.y + 0.4f > bottom)
                    {
                        Error($"Crop at ({c.x},{c.y}) overlaps {s}.");
                    }
                }
            }
        }

        // ------------------------------------------------------------ prefab construction

        /// <summary>Validates and builds the level prefab asset. Returns null (with errors) if the design is invalid.</summary>
        public GameObject BuildPrefab(LevelAssets assets, string prefabPath, List<string> errorsOut)
        {
            Validate();
            if (_errors.Count > 0)
            {
                errorsOut.AddRange(_errors);
                return null;
            }

            int endX = _x;
            int maxTop = Mathf.Max(_surfaces.Max(s => s.top), _chambers.Count > 0 ? _chambers.Max(c => c.floorTop + 4) : 0);

            var root = new GameObject(Id);
            var rootComponent = root.AddComponent<LevelPrefabRoot>();
            rootComponent.cameraMinX = _startX;
            rootComponent.cameraMaxX = endX;

            var grid = new GameObject("Grid");
            grid.transform.SetParent(root.transform, false);
            grid.AddComponent<Grid>();

            Tilemap ground = CreateTilemap(grid.transform, "Ground", assets.groundLayer, 0);
            Tilemap platforms = CreateTilemap(grid.transform, "Platforms", assets.groundLayer, 1);

            // Cells of ground carved out for breakable-floor hollows (and the breakable row itself).
            bool IsCarved(int x, int y) => _breakables.Any(b => x >= b.x && x < b.x + b.length && y >= b.top - 3 && y <= b.top - 1);

            foreach (var s in _surfaces)
            {
                switch (s.kind)
                {
                    case Kind.Ground:
                        Fill(ground, assets.groundTile, s.x0, s.x1 - 1, GroundDepth, s.top - 1, IsCarved);
                        break;
                    case Kind.Mound:
                        Fill(ground, assets.groundTile, s.x0, s.x1 - 1, s.baseTop, s.top - 1);
                        break;
                    case Kind.Floating:
                        Fill(platforms, assets.platformTile, s.x0, s.x1 - 1, s.top - 1, s.top - 1);
                        break;
                }
            }

            foreach (var c in _chambers)
            {
                int right = c.x0 + c.interiorWidth + 1;
                Fill(ground, assets.groundTile, c.x0, right, c.floorTop + 3, c.floorTop + 3);   // ceiling
                Fill(ground, assets.groundTile, right, right, c.floorTop, c.floorTop + 3);      // right wall
            }

            Fill(ground, assets.groundTile, _startX - 1, _startX - 1, GroundDepth, maxTop + 8); // left wall
            Fill(ground, assets.groundTile, endX, endX, GroundDepth, maxTop + 8);               // right wall

            Finish(ground);
            Finish(platforms);

            if (_breakables.Count > 0)
            {
                Tilemap breakable = CreateTilemap(grid.transform, "BreakableFloor", assets.groundLayer, 2);
                foreach (var b in _breakables)
                {
                    Fill(breakable, assets.breakableTile, b.x, b.x + b.length - 1, b.top - 1, b.top - 1);
                }
                Finish(breakable);
                breakable.gameObject.AddComponent<BreakableFloorLayer>();
            }

            if (_water.Count > 0)
            {
                Tilemap water = CreateTilemap(grid.transform, "Water", 0, 4);
                foreach (var w in _water)
                {
                    Fill(water, assets.waterTile, w.xMin, w.xMax - 1, w.yMin, w.yMax - 1);
                }
                water.CompressBounds();
                water.gameObject.AddComponent<TilemapCollider2D>().isTrigger = true;
                water.gameObject.AddComponent<WaterZone>();
            }

            var pit = new GameObject("PitDeathZone");
            pit.transform.SetParent(root.transform, false);
            pit.transform.position = new Vector3((_startX + endX) * 0.5f, -10f, 0f);
            var pitBox = pit.AddComponent<BoxCollider2D>();
            pitBox.isTrigger = true;
            pitBox.size = new Vector2(endX - _startX + 60f, 4f);
            pit.AddComponent<PitDeathZone>();

            var markers = new GameObject("Markers");
            markers.transform.SetParent(root.transform, false);

            AddMarker<PlayerStartPoint>(markers.transform, "PlayerStart", _playerStart.Value);
            if (_goal.HasValue)
            {
                AddMarker<GoalMarker>(markers.transform, "Goal", _goal.Value);
            }

            for (int i = 0; i < _checkpoints.Count; i++)
            {
                AddMarker<CheckpointMarker>(markers.transform, $"Checkpoint_{i + 1}", _checkpoints[i]);
            }

            for (int i = 0; i < _chambers.Count; i++)
            {
                var c = _chambers[i];
                if (c.barrierSeal)
                {
                    var seal = AddMarker<RobotSpawnPoint>(markers.transform, $"BarrierUnit_{i + 1}", new Vector2(c.x0 + 0.5f, c.floorTop + 1.5f));
                    seal.robotType = RobotType.BarrierUnit;
                    seal.patrolDistance = 0.1f;
                }
                else
                {
                    AddMarker<BreakableWallMarker>(markers.transform, $"BreakableWall_{i + 1}", new Vector2(c.x0 + 0.5f, c.floorTop));
                }
            }

            for (int i = 0; i < _crops.Count; i++)
            {
                var crop = AddMarker<CropSpawnPoint>(markers.transform, $"Crop_{i:00}", new Vector2(_crops[i].x, _crops[i].y));
                crop.secretCluster = _crops[i].secret;
            }

            for (int i = 0; i < _robots.Count; i++)
            {
                var robot = AddMarker<RobotSpawnPoint>(markers.transform, $"{_robots[i].type}_{i + 1}", new Vector2(_robots[i].x, _robots[i].y));
                robot.robotType = _robots[i].type;
                robot.patrolDistance = _robots[i].patrol;
                robot.wave = _robots[i].wave;
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static T AddMarker<T>(Transform parent, string markerName, Vector2 position) where T : Component
        {
            var go = new GameObject(markerName);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            return go.AddComponent<T>();
        }

        private static Tilemap CreateTilemap(Transform parent, string tilemapName, int layer, int sortingOrder)
        {
            var go = new GameObject(tilemapName) { layer = layer };
            go.transform.SetParent(parent, false);
            var tilemap = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return tilemap;
        }

        private static void Fill(Tilemap tilemap, Tile tile, int x0, int x1, int y0, int y1, Func<int, int, bool> skip = null)
        {
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    if (skip != null && skip(x, y))
                    {
                        continue;
                    }
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        private static void Finish(Tilemap tilemap)
        {
            tilemap.CompressBounds();

            var tilemapCollider = tilemap.gameObject.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            var composite = tilemap.gameObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

            var body = tilemap.gameObject.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = tilemap.gameObject.AddComponent<Rigidbody2D>();
            }
            body.bodyType = RigidbodyType2D.Static;
        }
    }
}
