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
        /// <summary>Grass-topped variants for every exposed top cell of the ground; empty keeps groundTile everywhere.</summary>
        public Tile[] groundSurfaceTiles = Array.Empty<Tile>();
        public int groundLayer;
        /// <summary>Optional decorative backdrop filling every Chamber() interior; left blank draws nothing (transparent).</summary>
        public Sprite chamberBackdrop;
        /// <summary>Art for ground obstacles (feet-pivoted); each obstacle picks one at random. Empty places none.</summary>
        public Sprite[] obstacleSprites = Array.Empty<Sprite>();
        /// <summary>Obstacles per non-boss level, at seeded-random valid spots.</summary>
        public int obstaclesPerLevel;
        /// <summary>Stone slab art for SecretLedge() surfaces; null draws them with platformTile like any platform.</summary>
        public Sprite ledgeSprite;
        /// <summary>Collision-only tile (no sprite) under the stone ledge slabs.</summary>
        public Tile invisibleTile;
        /// <summary>Which obstacle art gets a barn behind it (see PlaceScenery).</summary>
        public Sprite haybaleSprite;
        /// <summary>Background scenery (no collider): a barn behind each haybale, a windmill behind each barrel pyramid.</summary>
        public Sprite barnSprite;
        public Sprite windmillSprite;
        /// <summary>Art for BarrelPyramid() barrels (feet-pivoted); null skips the pyramids' art but keeps them solid.</summary>
        public Sprite barrelSprite;
        /// <summary>Art for the hand-authored farm backdrop (CornField / Fence / Wildflowers / Backdrop / Biplane).</summary>
        public FarmBackdropArt farmArt = new();
        /// <summary>Square block art for StoneBlocks() bonus platforms; null draws them with platformTile.</summary>
        public Sprite stoneBlockSprite;
        /// <summary>Art for BonusCoin() crops (CropSpawnPoint.visualOverride).</summary>
        public Sprite coinSprite;
    }

    /// <summary>Background-only farm art (no colliders). Any missing piece is skipped where a level asks for it.</summary>
    internal sealed class FarmBackdropArt
    {
        public Sprite[] cornStalks = Array.Empty<Sprite>();
        public Sprite barn;
        public Sprite windmill;
        public Sprite silo;
        public Sprite gnarledTree;
        public Sprite oak;
        public Sprite waterWheel;
        public Sprite cart;
        public Sprite fence;
        public Sprite wildflowers;
        public Sprite plane;
    }

    /// <summary>Single background props a level can place with <see cref="LevelBuilder.Backdrop"/>.</summary>
    internal enum FarmProp { Silo, Oak, WaterWheel, Cart, Barn, Windmill, GnarledTree }

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
            public bool stone;   // drawn with LevelAssets.ledgeSprite slabs over invisible collision tiles
            public bool blocks;  // StoneBlocks(): drawn with LevelAssets.stoneBlockSprite squares instead
            public bool bonus;   // optional platform validated against the double jump, not the base jump
            public override string ToString() => $"{(secret ? "Secret" : "")}{kind}[{x0},{x1}) top={top}";
        }

        private struct CropRec { public float x, y; public bool secret, coin; }
        private struct RobotRec { public RobotType type; public float x, y, patrol; public int wave; }
        private struct BreakableRec { public int x, length, top; }
        private struct ChamberRec { public int x0, floorTop, interiorWidth; public bool barrierSeal; }

        private const float MaxRise = 2.5f;
        // Rise a bonus (StoneBlocks) platform may need: every character has a full-height double jump (3.5 + 3.5).
        private const float MaxBonusRise = 5f;
        private const float MaxFlatGap = 5f;
        private const float RiseGapPenalty = 0.7f;
        private const int GroundDepth = -6;
        // Crop centre above whatever it rests on: the 0.6-unit icon then clears the 0.46-unit grass overhang of the
        // surface tiles, while still overlapping the 0.95-tall player so walking past collects it.
        private const float CropRestHeight = 0.9f;
        // Closest two crops may sit (centre to centre) without their icons overlapping: the widest icon is the
        // 1.2-tall corn cob at ~1.09 wide, and the pool is picked at random per crop, so every pair allows for it.
        private const float MinCropSpacing = 1.25f;

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
        private readonly List<Vector2> _pyramids = new();
        private readonly List<Vector2> _haystacks = new();
        private bool _manualScenery;
        private readonly List<(FarmProp prop, float x, bool flip)> _props = new();
        private readonly List<Vector2> _cornFields = new();   // (x0, x1)
        private readonly List<Vector2> _fences = new();
        private float? _biplaneHeight;
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

        /// <summary>
        /// Optional floating stone-block platform (bonus crops, not the critical path), drawn one square block per
        /// tile. Validated against the double jump every character has, so it may sit up to MaxBonusRise above what
        /// it is reached from. Keep bonus blocks well away from a gated secret, or the validator will (rightly)
        /// report the secret as reachable through them.
        /// </summary>
        public LevelBuilder StoneBlocks(int x, int length, int top)
        {
            _surfaces.Add(new Surface { x0 = x, x1 = x + length, top = top, baseTop = top - 1, kind = Kind.Floating, stone = true, blocks = true, bonus = true });
            return this;
        }

        /// <summary>A floating ledge the base jump cannot reach: a gated secret needing extra height or range.</summary>
        public LevelBuilder SecretLedge(int x, int length, int top)
        {
            _surfaces.Add(new Surface { x0 = x, x1 = x + length, top = top, baseTop = top - 1, kind = Kind.Floating, secret = true, stone = true });
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
        public LevelBuilder Crop(float x, float above = CropRestHeight, bool secret = false)
        {
            AddCrop(x, GroundTop(x) + above, secret);
            return this;
        }

        /// <summary>
        /// Crops above the ground from x0 to x1 inclusive, doubled up along the row: they're placed every step/2
        /// (never closer than MinCropSpacing, so icons never overlap), so a row authored at step 4 gets corn every 2 units.
        /// </summary>
        public LevelBuilder CropRow(float x0, float x1, float step, float above = CropRestHeight)
        {
            float spacing = Mathf.Max(step * 0.5f, MinCropSpacing);
            for (float x = x0; x <= x1 + 0.001f; x += spacing)
            {
                Crop(x, above);
            }
            return this;
        }

        /// <summary>Crop at an absolute world position (over gaps, on floating platforms, in alcoves).</summary>
        public LevelBuilder CropAt(float x, float y, bool secret = false)
        {
            AddCrop(x, y, secret);
            return this;
        }

        /// <summary>A normal crop drawn as the gold coin (LevelAssets.coinSprite), resting on the surface whose top is given.</summary>
        public LevelBuilder BonusCoin(float x, int surfaceTop)
        {
            _crops.Add(new CropRec { x = x, y = surfaceTop + CropRestHeight, coin = true });
            return this;
        }

        private void AddCrop(float x, float y, bool secret) => _crops.Add(new CropRec { x = x, y = y, secret = secret });

        private static bool CropOverlaps(float x, float y, Surface s)
        {
            float bottom = s.kind == Kind.Floating ? s.baseTop : GroundDepth;
            return x > s.x0 - 0.3f && x < s.x1 + 0.3f && y - 0.4f < s.top && y + 0.4f > bottom;
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

        /// <summary>
        /// Six-barrel pyramid (rows of 3-2-1) centred at x on flat ground, 4.5 units high, climbed in 1.5 steps.
        /// The barrels are to the shared world scale. Ground corn under it is cleared and one corn sits on the top barrel.
        /// </summary>
        public LevelBuilder BarrelPyramid(float x)
        {
            int top = GroundTopAt(x, out bool found);
            float reach = BarrelRows * BarrelWidth * 0.5f + 0.5f;
            for (float dx = -reach; found && dx <= reach; dx += 0.25f)
            {
                if (GroundTopAt(x + dx, out bool f) != top || !f)
                {
                    found = false;
                }
            }
            if (!found)
            {
                Error($"Barrel pyramid at x={x} needs flat ground {reach} units either side.");
                return this;
            }

            _pyramids.Add(new Vector2(x, top));
            return this;
        }

        // ------------------------------------------------------------ farm backdrop (visual only, no colliders)

        /// <summary>A two-row corn field behind the play area over [x0, x1); stalks over a gap are skipped.</summary>
        public LevelBuilder CornField(float x0, float x1) { _cornFields.Add(new Vector2(x0, x1)); return this; }

        /// <summary>
        /// A wooden fence run over [x0, x1), in front of the corn, with a bunch of wildflowers at each end; sections
        /// over a gap or step are skipped.
        /// </summary>
        public LevelBuilder Fence(float x0, float x1) { _fences.Add(new Vector2(x0, x1)); return this; }

        /// <summary>
        /// This level's scenery is fully hand-placed: no random rock/haybale obstacles and no automatic barn/windmill
        /// behind them (use Backdrop(FarmProp.Barn/Windmill) and HayStack instead).
        /// </summary>
        public LevelBuilder ManualScenery() { _manualScenery = true; return this; }

        /// <summary>One background prop standing on the ground at x (skipped if it would hang over a gap or cover a barn/windmill).</summary>
        public LevelBuilder Backdrop(FarmProp prop, float x, bool flip = false) { _props.Add((prop, x, flip)); return this; }

        /// <summary>A biplane flying across the sky this high above y=0, looping over the whole level.</summary>
        public LevelBuilder Biplane(float height) { _biplaneHeight = height; return this; }

        /// <summary>
        /// Three to-scale hay bales stacked (two side by side, one on top) at x on flat ground: a solid, climbable
        /// 3-high step (two 1.5 steps). Ground corn under it is cleared.
        /// </summary>
        public LevelBuilder HayStack(float x)
        {
            int top = GroundTopAt(x, out bool found);
            for (float dx = -(BaleWidth + 0.5f); found && dx <= BaleWidth + 0.5f; dx += 0.5f)
            {
                if (GroundTopAt(x + dx, out bool f) != top || !f)
                {
                    found = false;
                }
            }
            if (!found)
            {
                Error($"Hay stack at x={x} needs flat ground {BaleWidth + 0.5f} units either side.");
                return this;
            }

            _haystacks.Add(new Vector2(x, top));
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
            if (to.bonus)
            {
                return rise <= MaxBonusRise && dx <= MaxFlatGap;
            }
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
                    if (CropOverlaps(c.x, c.y, s))
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
                        Sprite stoneSprite = !s.stone ? null : s.blocks ? assets.stoneBlockSprite : assets.ledgeSprite;
                        bool stoneArt = stoneSprite != null && assets.invisibleTile != null;
                        Fill(platforms, stoneArt ? assets.invisibleTile : assets.platformTile, s.x0, s.x1 - 1, s.top - 1, s.top - 1);
                        if (stoneArt)
                        {
                            AddStoneSlabs(root.transform, stoneSprite, s);
                        }
                        break;
                }
            }

            foreach (var c in _chambers)
            {
                int right = c.x0 + c.interiorWidth + 1;
                Fill(ground, assets.groundTile, c.x0, right, c.floorTop + 3, c.floorTop + 3);   // ceiling
                Fill(ground, assets.groundTile, right, right, c.floorTop, c.floorTop + 3);      // right wall

                if (assets.chamberBackdrop != null)
                {
                    var backdrop = new GameObject($"ChamberBackdrop_{c.x0}");
                    backdrop.transform.SetParent(root.transform, false);
                    backdrop.transform.position = new Vector3(c.x0 + 1f + c.interiorWidth / 2f, c.floorTop + 1.5f, 0f);
                    var backdropRenderer = backdrop.AddComponent<SpriteRenderer>();
                    backdropRenderer.sprite = assets.chamberBackdrop;
                    backdropRenderer.sortingOrder = -2; // behind the ground/platform tilemaps (0/1), in front of the parallax background
                    Vector2 native = assets.chamberBackdrop.bounds.size;
                    backdrop.transform.localScale = new Vector3(
                        c.interiorWidth / Mathf.Max(native.x, 0.01f), 3f / Mathf.Max(native.y, 0.01f), 1f);
                }
            }

            Fill(ground, assets.groundTile, _startX - 1, _startX - 1, GroundDepth, maxTop + 8); // left wall
            Fill(ground, assets.groundTile, endX, endX, GroundDepth, maxTop + 8);               // right wall

            PaintSurface(ground, assets);
            Finish(ground);
            BuildBarrelPyramids(root.transform, assets);
            BuildHayStacks(root.transform, assets);
            var haybales = _manualScenery ? new List<Vector2>() : PlaceObstacles(root.transform, assets);
            var scenery = _manualScenery ? new List<(float x, float halfWidth)>() : PlaceScenery(root.transform, assets, haybales);
            PlaceFarmBackdrop(root.transform, assets.farmArt, scenery, endX);
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

            RemoveOverlappingCrops();

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
                var crop = AddMarker<CropSpawnPoint>(markers.transform, _crops[i].coin ? $"Crop_{i:00}_Coin" : $"Crop_{i:00}", new Vector2(_crops[i].x, _crops[i].y));
                crop.secretCluster = _crops[i].secret;
                if (_crops[i].coin) { crop.visualOverride = assets.coinSprite; }
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

        // Drops any crop whose icon would overlap one already kept (hand-placed groups, arcs and secret rows are
        // authored 1 unit apart). Secret-cluster crops are kept first so no secret loses its reward.
        private void RemoveOverlappingCrops()
        {
            var kept = new List<CropRec>();
            foreach (var c in _crops.Where(c => c.secret).Concat(_crops.Where(c => !c.secret)))
            {
                if (!kept.Any(k => Mathf.Abs(k.x - c.x) < MinCropSpacing - 0.001f && Mathf.Abs(k.y - c.y) < MinCropSpacing))
                {
                    kept.Add(c);
                }
            }
            _crops.Clear();
            _crops.AddRange(kept);
        }

        // ------------------------------------------------------------ obstacles

        private const float ObstacleWidth = 2.1f;
        private const float ObstacleHeight = 1.95f;  // under the base jump's 3.5 apex; a step, not a wall
        private const float ObstacleSpacing = 10f;
        private const float ObstacleLiftRange = ObstacleWidth * 0.5f + 0.4f; // corn this close to an obstacle's centre sits on top of it

        // Drops solid rock/haybale obstacles on the main-path ground at random spots, seeded by the level id so a
        // re-run of setup places them identically. Only spots that keep the level fair are candidates: flat ground
        // at least 2 units either side (never at a gap's take-off or landing), clear of the start/goal/checkpoints,
        // robot patrols, breakable floors, water, low platforms and the secret areas. Corn resting on the ground
        // where an obstacle lands is lifted to sit on top of it.
        // Returns the ground position of every haybale placed (PlaceScenery puts a barn behind each).
        private List<Vector2> PlaceObstacles(Transform root, LevelAssets assets)
        {
            var haybales = new List<Vector2>();
            if (IsBoss || assets.obstaclesPerLevel <= 0 || assets.obstacleSprites == null || assets.obstacleSprites.Length == 0)
            {
                return haybales;
            }

            var candidates = new List<(float x, int top)>();
            foreach (var s in _surfaces.Where(s => s.kind == Kind.Ground && !s.secret))
            {
                for (int cx = s.x0 + 2; cx <= s.x1 - 3; cx++)
                {
                    float x = cx + 0.5f;
                    if (IsObstacleSpotClear(x, s.top))
                    {
                        candidates.Add((x, s.top));
                    }
                }
            }

            var random = new System.Random(StableSeed(Id));
            var placed = new List<float>();
            while (placed.Count < assets.obstaclesPerLevel)
            {
                var open = candidates.Where(c => placed.All(p => Mathf.Abs(p - c.x) >= ObstacleSpacing)).ToList();
                if (open.Count == 0)
                {
                    Debug.LogWarning($"[LevelBuilder] {Id}: only room for {placed.Count} of {assets.obstaclesPerLevel} obstacles.");
                    break;
                }

                var (x, top) = open[random.Next(open.Count)];
                placed.Add(x);
                Sprite art = assets.obstacleSprites[random.Next(assets.obstacleSprites.Length)];

                float height = ObstacleHeight;
                if (art == assets.haybaleSprite)
                {
                    // One to-scale bale (1.9 x 1.5) is obstacle-sized on its own.
                    AddBale(root, assets, $"Obstacle_HayBale_{placed.Count}", new Vector2(x, top), 3);
                    haybales.Add(new Vector2(x, top));
                    height = BaleHeight;
                }
                else
                {
                    var obstacle = new GameObject($"Obstacle_{art.name}_{placed.Count}") { layer = assets.groundLayer };
                    obstacle.transform.SetParent(root, false);
                    obstacle.transform.position = new Vector3(x, top, 0f);
                    var box = obstacle.AddComponent<BoxCollider2D>();
                    box.size = new Vector2(ObstacleWidth, ObstacleHeight);
                    box.offset = new Vector2(0f, ObstacleHeight * 0.5f);
                    var renderer = obstacle.AddComponent<SpriteRenderer>();
                    renderer.sprite = art;
                    renderer.sortingOrder = 3; // in front of the ground tiles, behind robots (8) and the player (10)
                }

                for (int i = 0; i < _crops.Count; i++)
                {
                    var c = _crops[i];
                    if (Mathf.Abs(c.x - x) < ObstacleLiftRange && c.y < top + height + CropRestHeight)
                    {
                        c.y = top + height + CropRestHeight;
                        _crops[i] = c;
                    }
                }
            }
            return haybales;
        }

        // Background scenery: purely visual (no collider), so the player runs straight past it. A damaged barn
        // stands behind each haybale (the bale at its front corner) and a windmill behind each barrel pyramid,
        // leaning left or right by a seeded coin flip. Drawn behind the ground tiles so the grass covers the base,
        // and slightly dimmed so the gameplay layer in front reads first. A piece that would overlap scenery
        // already placed tries the other side, then is skipped.
        private const float BarnOffset = 2.4f;
        private const float WindmillOffset = 3.2f;
        private static readonly Color SceneryTint = new Color(0.86f, 0.86f, 0.9f);

        // Returns the footprint (centre x, half width) of every piece placed.
        private List<(float x, float halfWidth)> PlaceScenery(Transform root, LevelAssets assets, List<Vector2> haybales)
        {
            var random = new System.Random(StableSeed(Id) ^ 0x5CE9E);
            var placed = new List<(float x, float halfWidth)>();

            void Place(Sprite art, Vector2 anchor, float offset, string label)
            {
                if (art == null) { return; }
                float half = art.bounds.size.x * 0.5f;
                int first = random.Next(2) == 0 ? -1 : 1;
                foreach (int side in new[] { first, -first })
                {
                    float x = anchor.x + side * offset;
                    int top = (int)anchor.y;
                    bool grounded = true;
                    for (float dx = -half * 0.7f; dx <= half * 0.7f; dx += 0.5f)
                    {
                        if (GroundTopAt(x + dx, out bool found) != top || !found) { grounded = false; break; }
                    }
                    if (!grounded) { continue; }   // never hang a building out over a gap or a step
                    if (placed.Any(p => Mathf.Abs(p.x - x) < p.halfWidth + half)) { continue; }

                    var go = new GameObject($"Scenery_{label}_{placed.Count + 1}");
                    go.transform.SetParent(root, false);
                    go.transform.position = new Vector3(x, top, 0f);
                    var renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sprite = art;
                    renderer.color = SceneryTint;
                    renderer.sortingOrder = -5; // in front of the parallax (-30..-10), behind the ground tiles (0)
                    placed.Add((x, half));
                    return;
                }
            }

            foreach (var p in _pyramids) { Place(assets.windmillSprite, p, WindmillOffset, "Windmill"); }
            foreach (var h in haybales) { Place(assets.barnSprite, h, BarnOffset, "Barn"); }
            return placed;
        }

        // Hand-authored farm backdrop (see CornField/Fence/Wildflowers/Backdrop/Biplane), laid out and sized after
        // the Level 1 mockups. Everything is visual only and drawn behind the ground tiles (0), back to front:
        //   -9 biplane, -8 silo/oak/gnarled tree/water wheel, -7 barn/windmill (the silo tucks behind the barn),
        //   -6/-5 back/front corn rows (in front of the trees' trunks), -4 cart and fence, -3 wildflowers.
        // Crops stay readable because the pickup is the smiling kernel (no cobs) and most sit up on stone blocks.
        // Seeded from the level id, so a re-run of setup lays it out identically.
        private static readonly Color BackdropTint = new Color(0.9f, 0.9f, 0.94f);
        // The surface tiles' grass overhangs the cell above by ~0.46, so pieces stood at the ground top look buried;
        // lifting them this much stands them on the grass line like the mockups.
        private const float BackdropLift = 0.3f;
        private static readonly Color CornBackTint = new Color(0.78f, 0.82f, 0.78f);
        private static readonly Color CornFrontTint = new Color(0.92f, 0.94f, 0.92f);

        private void PlaceFarmBackdrop(Transform root, FarmBackdropArt art, List<(float x, float halfWidth)> scenery, int endX)
        {
            if (art == null || (_props.Count == 0 && _cornFields.Count == 0 && _fences.Count == 0
                && !_biplaneHeight.HasValue)) { return; }
            var random = new System.Random(StableSeed(Id) ^ 0xFA23);
            float Range(float a, float b) => a + (float)random.NextDouble() * (b - a);
            var parent = new GameObject("FarmBackdrop").transform;
            parent.SetParent(root, false);

            // Ground top under the whole span [x - half, x + half], or null over a gap or a change of height.
            int? FlatTop(float x, float half)
            {
                int top = GroundTopAt(x, out bool found);
                if (!found) { return null; }
                for (float dx = -half; dx <= half; dx += 0.25f)
                {
                    if (GroundTopAt(x + dx, out bool f) != top || !f) { return null; }
                }
                return top;
            }

            GameObject Spawn(string label, Sprite sprite, Vector2 at, float scale, bool flip, Color tint, int order)
            {
                var go = new GameObject(label);
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(at.x, at.y + BackdropLift, 0f);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.flipX = flip;
                renderer.color = tint;
                renderer.sortingOrder = order;
                return go;
            }

            foreach (var (prop, x, flip) in _props)
            {
                // Every prop is imported to the shared world scale (StampedeUIArt.UnitsPerMetre), so none is rescaled here.
                const float scale = 1f;
                (Sprite sprite, int order) = prop switch
                {
                    FarmProp.Silo => (art.silo, -8),
                    FarmProp.Oak => (art.oak, -8),
                    FarmProp.GnarledTree => (art.gnarledTree, -8),
                    FarmProp.WaterWheel => (art.waterWheel, -8),
                    FarmProp.Barn => (art.barn, -7),
                    FarmProp.Windmill => (art.windmill, -7),
                    _ => (art.cart, -4),
                };
                if (sprite == null) { continue; }
                float half = sprite.bounds.size.x * 0.5f * scale;
                int? top = FlatTop(x, half * 0.6f);
                if (top == null || scenery.Any(p => Mathf.Abs(p.x - x) < p.halfWidth + half * 0.8f))
                {
                    Debug.LogWarning($"[LevelBuilder] {Id}: skipped backdrop {prop} at x={x} (over a gap/step or covering a barn/windmill).");
                    continue;
                }
                Spawn($"{prop}_{x}", sprite, new Vector2(x, top.Value), scale, flip, BackdropTint, order);
            }

            if (art.cornStalks.Length > 0)
            {
                // Back row: shorter, darker, tighter; front row offset so the gaps interleave.
                var rows = new[]
                {
                    (step: 0.5f, minScale: 0.85f, maxScale: 0.95f, tint: CornBackTint, order: -6, offset: 0f),
                    (step: 0.65f, minScale: 0.95f, maxScale: 1.1f, tint: CornFrontTint, order: -5, offset: 0.3f),
                };
                foreach (var field in _cornFields)
                {
                    foreach (var row in rows)
                    {
                        for (float x = field.x + row.offset; x < field.y; x += row.step)
                        {
                            float sx = x + Range(-0.12f, 0.12f);
                            int? top = FlatTop(sx, 0.3f);
                            if (top == null) { continue; }
                            Sprite stalk = art.cornStalks[random.Next(art.cornStalks.Length)];
                            Spawn($"CornStalk_{sx:0.00}", stalk, new Vector2(sx, top.Value), Range(row.minScale, row.maxScale),
                                random.Next(2) == 0, row.tint, row.order);
                        }
                    }
                }
            }

            if (art.fence != null)
            {
                float width = art.fence.bounds.size.x;
                foreach (var run in _fences)
                {
                    // Sections overlap slightly so the rails join without a seam.
                    float? first = null, last = null;
                    for (float x = run.x + width * 0.5f; x + width * 0.5f <= run.y + 0.01f; x += width * 0.97f)
                    {
                        int? top = FlatTop(x, width * 0.5f);
                        if (top == null) { continue; }
                        Spawn($"Fence_{x:0.00}", art.fence, new Vector2(x, top.Value), 1f, false, BackdropTint, -4);
                        first ??= x - width * 0.5f;
                        last = x + width * 0.5f;
                    }

                    // A bunch of wildflowers (a big clump with a smaller one tucked beside it) at each end of the run.
                    if (art.wildflowers == null || first == null) { continue; }
                    foreach (var (end, outward) in new[] { (first.Value, -1f), (last.Value, 1f) })
                    {
                        int? top = FlatTop(end, 0.3f);
                        if (top == null) { continue; }
                        Spawn($"Wildflowers_{end:0.00}", art.wildflowers, new Vector2(end, top.Value), Range(1.05f, 1.2f),
                            random.Next(2) == 0, Color.white, -3);
                        Spawn($"Wildflowers_{end:0.00}_b", art.wildflowers, new Vector2(end + outward * 0.45f, top.Value),
                            Range(0.75f, 0.9f), random.Next(2) == 0, Color.white, -3);
                    }
                }
            }

            if (art.plane != null && _biplaneHeight.HasValue)
            {
                var plane = Spawn("Biplane", art.plane, new Vector2(_startX + 6f, _biplaneHeight.Value), 1f, false, Color.white, -9);
                plane.AddComponent<SkyDrifter>().Configure(_startX - 12f, endX + 12f, 2.2f);
            }
        }

        // To the shared world scale (StampedeUIArt.UnitsPerMetre): barrels and bales are both 1.5 units tall.
        private const float BarrelWidth = 1.1f;      // the barrel art's 0.73 aspect
        private const float BarrelHeight = 1.5f;
        private const int BarrelRows = 3;            // 3-2-1 pyramid, 4.5 high in 1.5 steps
        private const float BaleWidth = 1.9f;        // the bale body's 1.28 aspect (lying on its side)
        private const float BaleHeight = 1.5f;

        private void BuildHayStacks(Transform root, LevelAssets assets)
        {
            for (int h = 0; h < _haystacks.Count; h++)
            {
                BuildHayStack(root, assets, $"HayStack_{h + 1}", _haystacks[h]);
                Vector2 at = _haystacks[h];
                _crops.RemoveAll(c => !c.secret && Mathf.Abs(c.x - at.x) < BaleWidth + 0.6f && c.y < at.y + 2f * BaleHeight);
            }
        }

        private static void BuildHayStack(Transform root, LevelAssets assets, string stackName, Vector2 at)
        {
            AddBale(root, assets, $"{stackName}_Left", new Vector2(at.x - BaleWidth * 0.5f, at.y), 3);
            AddBale(root, assets, $"{stackName}_Right", new Vector2(at.x + BaleWidth * 0.5f, at.y), 3);
            AddBale(root, assets, $"{stackName}_Top", new Vector2(at.x, at.y + BaleHeight), 4);
        }

        // One bale: solid box on the Ground layer. The art is imported so the bale body matches the box, its straw
        // skirt and wisps spill past the edges; drawn 6% bigger so neighbouring bales touch with no seam.
        private static void AddBale(Transform root, LevelAssets assets, string baleName, Vector2 feet, int sortingOrder)
        {
            var bale = new GameObject(baleName) { layer = assets.groundLayer };
            bale.transform.SetParent(root, false);
            bale.transform.position = new Vector3(feet.x, feet.y, 0f);
            var box = bale.AddComponent<BoxCollider2D>();
            box.size = new Vector2(BaleWidth, BaleHeight);
            box.offset = new Vector2(0f, BaleHeight * 0.5f);
            if (assets.haybaleSprite != null)
            {
                var art = new GameObject("Art");
                art.transform.SetParent(bale.transform, false);
                art.transform.localScale = new Vector3(1.06f, 1.06f, 1f);
                var renderer = art.AddComponent<SpriteRenderer>();
                renderer.sprite = assets.haybaleSprite;
                renderer.sortingOrder = sortingOrder; // the top bale draws over the two below
            }
        }

        private void BuildBarrelPyramids(Transform root, LevelAssets assets)
        {
            for (int p = 0; p < _pyramids.Count; p++)
            {
                Vector2 at = _pyramids[p];
                for (int row = 0; row < BarrelRows; row++)
                {
                    int count = BarrelRows - row;
                    for (int i = 0; i < count; i++)
                    {
                        float x = at.x + (i - (count - 1) * 0.5f) * BarrelWidth;
                        AddBarrel(root, assets, $"BarrelPyramid_{p + 1}_R{row}_{i}", new Vector2(x, at.y + row * BarrelHeight), 3 + row);
                    }
                }

                float halfBase = BarrelRows * BarrelWidth * 0.5f;
                _crops.RemoveAll(c => !c.secret && Mathf.Abs(c.x - at.x) < halfBase + 0.6f && c.y < at.y + BarrelRows * BarrelHeight);
                _crops.Add(new CropRec { x = at.x, y = at.y + BarrelRows * BarrelHeight + CropRestHeight });
            }
        }

        // Lays whole stone slabs across a one-tile-thick ledge: as many as fit at close to the art's own aspect,
        // each stretched a little so they exactly span the ledge (no half slab at the end).
        private static void AddStoneSlabs(Transform root, Sprite slab, Surface s)
        {
            Vector2 native = slab.bounds.size;
            float aspect = native.x / Mathf.Max(native.y, 0.01f);
            int length = s.x1 - s.x0;
            int count = Mathf.Max(1, Mathf.RoundToInt(length / aspect));
            float width = (float)length / count;

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"StoneLedge_{s.x0}_{i}");
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(s.x0 + width * (i + 0.5f), s.top - 0.5f, 0f);
                go.transform.localScale = new Vector3(width / native.x, 1f / native.y, 1f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = slab;
                renderer.sortingOrder = 1; // same layer as the Platforms tilemap
            }
        }

        private static void AddBarrel(Transform root, LevelAssets assets, string barrelName, Vector2 feet, int sortingOrder)
        {
            var barrel = new GameObject(barrelName) { layer = assets.groundLayer };
            barrel.transform.SetParent(root, false);
            barrel.transform.position = new Vector3(feet.x, feet.y, 0f);
            var box = barrel.AddComponent<BoxCollider2D>();
            box.size = new Vector2(BarrelWidth, BarrelHeight);
            box.offset = new Vector2(0f, BarrelHeight * 0.5f);
            if (assets.barrelSprite != null)
            {
                // Drawn 12% bigger than the to-scale collider so each lid tucks under the barrel stacked on it
                // (higher rows sort in front) instead of leaving a gap between the rounded rims.
                var art = new GameObject("Art");
                art.transform.SetParent(barrel.transform, false);
                art.transform.localScale = new Vector3(1.12f, 1.12f, 1f);
                var renderer = art.AddComponent<SpriteRenderer>();
                renderer.sprite = assets.barrelSprite;
                renderer.sortingOrder = sortingOrder;
            }
        }

        private bool IsObstacleSpotClear(float x, int top)
        {
            if (_pyramids.Any(p => Mathf.Abs(p.x - x) < BarrelRows * BarrelWidth * 0.5f + ObstacleWidth * 0.5f + 3f)) { return false; }
            if (_haystacks.Any(p => Mathf.Abs(p.x - x) < BaleWidth + ObstacleWidth * 0.5f + 3f)) { return false; }
            // Flat ground past both sides, so it never sits at a gap edge or against a step.
            float flat = ObstacleWidth * 0.5f + 1f;
            for (float dx = -flat; dx <= flat; dx += 0.5f)
            {
                int t = GroundTopAt(x + dx, out bool found);
                if (!found || t != top) { return false; }
            }

            bool Near(Vector2? p, float range) => p.HasValue && Mathf.Abs(p.Value.x - x) < range;
            if (Near(_playerStart, 6f) || Near(_goal, 4f)) { return false; }
            if (_checkpoints.Any(c => Mathf.Abs(c.x - x) < 3f)) { return false; }
            if (_robots.Any(r => Mathf.Abs(r.x - x) < r.patrol + ObstacleWidth * 0.5f + 1.2f)) { return false; }
            if (_breakables.Any(b => x > b.x - 2f && x < b.x + b.length + 2f)) { return false; }
            if (_water.Any(w => x > w.xMin - 2f && x < w.xMax + 2f)) { return false; }
            if (_chambers.Any(c => x > c.x0 - 3f && x < c.x0 + c.interiorWidth + 5f)) { return false; }
            // Keep off anything overhead (low platforms, secret ledges): an extra step up must not open a secret.
            if (_surfaces.Any(s => s.kind == Kind.Floating && x > s.x0 - 3f && x < s.x1 + 3f)) { return false; }
            if (_surfaces.Any(s => s.secret && x > s.x0 - 3f && x < s.x1 + 3f)) { return false; }
            return true;
        }

        // string.GetHashCode isn't guaranteed stable between runs, so hash the id by hand (FNV-1a).
        private static int StableSeed(string text)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in text)
                {
                    hash = (hash ^ c) * 16777619;
                }
                return (int)hash;
            }
        }

        // Swaps every ground cell with nothing above it (flat tops, mound tops, chamber ceilings, hollow floors)
        // for a grass-topped variant, cycling variants by column so neighbours differ.
        private static void PaintSurface(Tilemap ground, LevelAssets assets)
        {
            if (assets.groundSurfaceTiles == null || assets.groundSurfaceTiles.Length == 0)
            {
                return;
            }

            ground.CompressBounds();
            BoundsInt bounds = ground.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (ground.GetTile(cell) != null && ground.GetTile(cell + Vector3Int.up) == null)
                    {
                        int variant = ((x % assets.groundSurfaceTiles.Length) + assets.groundSurfaceTiles.Length) % assets.groundSurfaceTiles.Length;
                        ground.SetTile(cell, assets.groundSurfaceTiles[variant]);
                    }
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
