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

    /// <summary>Shape of a secret passage room (LevelBuilder.SecretPassage); each world uses each at most once.</summary>
    public enum PassageLayout { Steps, Staircase, Pyramid, Ledges, Pillars, Tunnel, Zigzag }
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
        /// <summary>Rope bridge art for Bridge() spans (UI/Bridge.png, deck along its lower third), repeated across the span; null draws them with platformTile.</summary>
        public Sprite bridgeSprite;
        /// <summary>Square block art for BrickBlocks() steps (Frozen Tundra: WoodBlock.png); null uses stoneBlockSprite.</summary>
        public Sprite brickBlockSprite;
        /// <summary>Hung in the chasm under every main-path Bridge() (Frozen Tundra: FrozenWaterfall.png, feet-pivoted); null places none.</summary>
        public Sprite chasmWaterfallSprite;
        /// <summary>The world's everyday crop art (Frozen Tundra: the blueberry, Watermill Village: the golden acorn) for every normal, non-coin crop; null keeps the pickup's corn kernel.</summary>
        public Sprite cropSprite;
        /// <summary>Art for BonusCoin() crops (CropSpawnPoint.visualOverride).</summary>
        public Sprite coinSprite;
        /// <summary>The rare pellet (crystal apple) placed by RarePellet(); null places none.</summary>
        public Sprite rarePelletSprite;
        /// <summary>SecretSign.png (feet-pivoted): marks both ends of a SecretPassage(); null leaves the doors invisible.</summary>
        public Sprite secretSignSprite;

        // ---- per world (Meadow Ruins leaves these empty) ----
        /// <summary>Tile for IceFlat() ground (the slippery Ice tilemap); null draws ice with groundTile.</summary>
        public Tile iceTile;
        /// <summary>Warning sign stood at the start of every IceFlat() stretch (scenery); null places none.</summary>
        public Sprite iceSignSprite;
        /// <summary>This world's parallax art, far to near (LevelPrefabRoot.parallaxLayers); empty keeps Meadow Ruins' layers.</summary>
        public Sprite[] parallaxLayers = Array.Empty<Sprite>();
        /// <summary>This world's look per robot type (right, left, defeat), written onto every robot marker; missing types keep the prefab art.</summary>
        public Dictionary<RobotType, (Sprite right, Sprite left, Sprite defeat)> robotArt = new();
        /// <summary>How much bigger than the prefab the robotArt robots are drawn (feet on the ground, colliders unchanged).</summary>
        public float robotArtScale = 1f;
        /// <summary>This world's checkpoint art (one frame, feet-pivoted), written onto every checkpoint marker; null keeps the signpost.</summary>
        public Sprite checkpointSprite;
        /// <summary>River() water (Watermill: Props/River.png, top-pivoted, full-rect mesh so it tiles sideways); null draws no water.</summary>
        public Sprite riverSprite;
        /// <summary>Boat() art (Watermill: FishingBoat.png, centred, gunwale at its middle row); null draws a boat as a plain ledge.</summary>
        public Sprite boatSprite;
        /// <summary>Mooring post stood in the water at each end of a River() (feet-pivoted); null places none.</summary>
        public Sprite mooringPostSprite;
        /// <summary>Reeds stood on each bank of a River() (feet-pivoted); null places none.</summary>
        public Sprite reedSprite;
        /// <summary>Balloon() art (Sky Islands: AirBalloon.png); its basket rim is the deck. Null draws a balloon as a plain ledge.</summary>
        public Sprite balloonSprite;
        /// <summary>Updraft() column art (Sky Islands: UpdraftSpiral.png), stretched over the column; null leaves the wind invisible.</summary>
        public Sprite updraftSprite;

        /// <summary>A copy to override per world (the dictionary and arrays are shared until replaced).</summary>
        public LevelAssets Clone() => (LevelAssets)MemberwiseClone();
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
            public bool ice;     // IceFlat(): main-path ground on the slippery Ice tilemap
            public bool bridge;  // Bridge(): a rope bridge, drawn with LevelAssets.bridgeSprite over invisible collision tiles
            public bool brick;   // BrickBlocks(): StoneBlocks drawn with LevelAssets.brickBlockSprite
            public Surface partner;   // MovingLedge(): the same ledge at the other end of its trip (validation only)
            public override string ToString() => $"{(secret ? "Secret" : "")}{kind}[{x0},{x1}) top={top}";
        }

        private struct CropRec { public float x, y; public bool secret, coin, path, passage; }
        private struct RobotRec { public RobotType type; public float x, y, patrol; public int wave; }
        private struct BreakableRec { public int x, length, top; }
        private struct ChamberRec { public int x0, floorTop, interiorWidth; public bool barrierSeal; }
        private sealed class MoverRec { public Surface start, end; public float period, phase; public bool boat, balloon; }
        private struct UpdraftRec { public float x; public int baseTop; public float height; }
        private struct RiverRec { public int x0, x1; public float surface; }

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
        private readonly List<MoverRec> _movers = new();
        private readonly List<RectInt> _water = new();
        private readonly List<RiverRec> _rivers = new();
        private readonly List<UpdraftRec> _updrafts = new();
        private readonly List<Vector2> _pyramids = new();
        private readonly List<(Vector2 at, int rows)> _haystacks = new();   // bale stacks: 2 rows = HayStack, 3 = HayPyramid
        private bool _manualScenery;
        private bool _pathCorn;
        private int _haybalesAsBarrels;
        private bool _standardFarm;
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

        /// <summary>
        /// Frozen ground (Frozen Tundra): like Flat, but on the Ice tilemap (IceSurface), so characters slide - slow to
        /// get going, slower to stop or turn. A THIN ICE sign stands where each stretch begins.
        /// </summary>
        public LevelBuilder IceFlat(int length, int? top = null)
        {
            AddFlat(length, top, false);
            _surfaces[_surfaces.Count - 1].ice = true;
            return this;
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

        /// <summary>One-tile-thick platform floating with its top surface at the given height (drawn with stone slabs).</summary>
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

        /// <summary>
        /// A rope bridge: a walkable span [x, x+length) with its deck at 'top' (one tile of collision, drawn with
        /// LevelAssets.bridgeSprite). Laid across a Gap() at the ground's height it carries the main path over a chasm
        /// too wide to jump (path corn runs along its deck, and in Frozen Tundra a frozen waterfall hangs in the chasm
        /// below); pass bonus = true for an optional raised span (validated against the double jump, like StoneBlocks).
        /// </summary>
        public LevelBuilder Bridge(int x, int length, int top, bool bonus = false)
        {
            _surfaces.Add(new Surface { x0 = x, x1 = x + length, top = top, baseTop = top - 1, kind = Kind.Floating, bridge = true, bonus = bonus });
            return this;
        }

        /// <summary>
        /// Mario-style moving ledge (MovingPlatform at runtime): a one-tile-thick ledge [x, x+length) with its top at
        /// 'top' that glides to (x+dx, top+dy) and back, one round trip every 'period' seconds, starting 'phase' (0-1)
        /// of the way round (0.5 = at the far end, so two ledges can move in turn). The player rides it. Validated as
        /// its two end positions, joined (riding from one to the other); the whole sweep must stay clear of the ground.
        /// Put them over a chasm too wide to jump to make the player hop across from one to the next.
        /// </summary>
        public LevelBuilder MovingLedge(int x, int length, int top, int dx, int dy, float period = 4f, float phase = 0f)
        {
            var start = new Surface { x0 = x, x1 = x + length, top = top, baseTop = top - 1, kind = Kind.Floating };
            var end = new Surface { x0 = x + dx, x1 = x + dx + length, top = top + dy, baseTop = top + dy - 1, kind = Kind.Floating };
            start.partner = end;
            end.partner = start;
            _surfaces.Add(start);
            _surfaces.Add(end);
            _movers.Add(new MoverRec { start = start, end = end, period = period, phase = phase });
            return this;
        }

        /// <summary>
        /// A rowing boat across the last River(): a MovingLedge 'length' wide at the banks' height (deck level with the
        /// grass), drawn with LevelAssets.boatSprite, docked flush against the near bank and rowing to the far bank and
        /// back once per 'period' (phase as MovingLedge), so the player simply walks aboard and off again.
        /// </summary>
        public LevelBuilder Boat(int length = 3, float period = 4f, float phase = 0f)
        {
            if (_rivers.Count == 0)
            {
                Error("Boat() needs a River() declared first.");
                return this;
            }
            var river = _rivers[_rivers.Count - 1];
            MovingLedge(river.x0, length, Mathf.RoundToInt(river.surface + RiverSurfaceDrop), river.x1 - river.x0 - length, 0, period, phase);
            _movers[_movers.Count - 1].boat = true;
            return this;
        }

        /// <summary>
        /// A hot-air balloon lift (Sky Islands): a two-wide MovingLedge whose deck is the basket rim, rising 'rise'
        /// units from 'top' and back once per 'period' (phase as MovingLedge). Drawn with LevelAssets.balloonSprite.
        /// </summary>
        public LevelBuilder Balloon(int x, int top, int rise, float period = 4.5f, float phase = 0f)
        {
            MovingLedge(x, BalloonDeck, top, 0, rise, period, phase);
            _movers[_movers.Count - 1].balloon = true;
            return this;
        }

        private const int BalloonDeck = 2;
        // AirBalloon.png: the basket rim is 22.7% of the art up from its bottom and spans 60% of its width.
        private const float BalloonRimFromBottom = 0.227f, BalloonBasketWidth = 0.6f;

        /// <summary>
        /// A rising wind (Sky Islands, UpdraftZone at runtime): a 2-wide column on the ground at x, 'height' units tall.
        /// Inside it the player floats up at 9 u/s and can drift out sideways at any height; leaving the top they coast
        /// ~0.9 higher. Validation counts it as a ride from the ground under it to any surface beside the column
        /// whose top is no higher than the column's top + 0.5 (so a gated secret must stay out of its reach).
        /// </summary>
        public LevelBuilder Updraft(float x, float height)
        {
            int baseTop = GroundTopAt(x, out bool found);
            if (!found)
            {
                Error($"Updraft at x={x} needs ground under it.");
                return this;
            }
            _updrafts.Add(new UpdraftRec { x = x, baseTop = baseTop, height = height });
            return this;
        }

        private const float UpdraftHalfWidth = 1f;
        private const float UpdraftReach = 2.5f;   // sideways from the column's edge to a surface the rider steps onto

        // The water's surface sits this far under the lower bank, so a boat's hull and a bridge's ropes dip into it.
        private const float RiverSurfaceDrop = 0.7f;

        /// <summary>
        /// Fills the Gap() [x0, x1) with a river (LevelAssets.riverSprite): its surface RiverSurfaceDrop under the lower
        /// bank, a splash line just under the surface that costs a life (as a pit does), a mooring post in the water at
        /// each end and reeds on both banks. Cross it by Bridge(), Boat() or moving ledges; Piranha() robots leap out.
        /// </summary>
        public LevelBuilder River(int x0, int x1)
        {
            int left = GroundTopAt(x0 - 0.5f, out bool foundLeft), right = GroundTopAt(x1 + 0.5f, out bool foundRight);
            bool open = !_surfaces.Any(g => g.kind != Kind.Floating && g.x0 < x1 && x0 < g.x1);
            if (!foundLeft || !foundRight || !open)
            {
                Error($"River [{x0},{x1}) needs a Gap() with ground on both sides.");
                return this;
            }
            _rivers.Add(new RiverRec { x0 = x0, x1 = x1, surface = Mathf.Min(left, right) - RiverSurfaceDrop });
            return this;
        }

        /// <summary>A piranha robot waiting under a River()'s surface at x, leaping leapAbove units over the banks.</summary>
        public LevelBuilder Piranha(float x, float leapAbove = 2f, int wave = 0)
        {
            var river = _rivers.FirstOrDefault(r => x > r.x0 + 0.5f && x < r.x1 - 0.5f);
            if (river.x1 == river.x0)
            {
                Error($"Piranha at x={x} needs a River() under it (declare the river first).");
                return this;
            }
            float wait = river.surface - 1.2f;   // fully under the surface art
            _robots.Add(new RobotRec { type = RobotType.Piranha, x = x, y = wait, patrol = river.surface + RiverSurfaceDrop + leapAbove - wait, wave = wave });
            _noPatrolCheck.Add(x);
            return this;
        }

        /// <summary>An optional floating ledge (ledge slab art, like Floating) validated against the double jump.</summary>
        public LevelBuilder BonusLedge(int x, int length, int top)
        {
            _surfaces.Add(new Surface { x0 = x, x1 = x + length, top = top, baseTop = top - 1, kind = Kind.Floating, bonus = true });
            return this;
        }

        /// <summary>StoneBlocks drawn with the world's brick block art (Frozen Tundra: the snow-capped WoodBlock).</summary>
        public LevelBuilder BrickBlocks(int x, int length, int top)
        {
            StoneBlocks(x, length, top);
            _surfaces[_surfaces.Count - 1].brick = true;
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
        /// One unbroken line of kernels along the main path from the start to the goal (the boss arena's far wall):
        /// resting on the ground, climbing over hay stacks, barrel pyramids and the random obstacles, and arcing over
        /// every jumpable pit. Hand-placed crops on the path line are replaced by it; crops up on platforms, stone
        /// blocks, coins and secrets stay. Laid out at build time, after all the geometry is known.
        /// </summary>
        public LevelBuilder PathCorn() { _pathCorn = true; return this; }

        /// <summary>
        /// The farm scenery every level shares, laid out automatically for any layout: the entrance group (water wheel,
        /// windmill, gnarled tree) after the start, the farmyard (broken barn and cart, the oak behind them) mid-level,
        /// the silo later among the corn, fenced corn fields along the rest and the biplane. Each group goes on the
        /// flat ground nearest its preferred spot, never behind a platform or ledge. Replaces the automatic
        /// barn/windmill scenery (the random obstacles stay).
        /// </summary>
        public LevelBuilder StandardFarm() { _standardFarm = true; return this; }

        /// <summary>
        /// The leftmost <paramref name="count"/> random hay-bale obstacles are built as single wooden barrels instead.
        /// The random layout is chosen exactly as before (same spots, same rocks), only the art/solid changes.
        /// </summary>
        public LevelBuilder HaybalesAsBarrels(int count = 1) { _haybalesAsBarrels = count; return this; }

        /// <summary>
        /// Three to-scale hay bales stacked (two side by side, one on top) at x on flat ground: a solid, climbable
        /// 3-high step (two 1.5 steps). Ground corn under it is cleared.
        /// </summary>
        public LevelBuilder HayStack(float x) => BaleStack(x, 2, "Hay stack");

        /// <summary>
        /// Six to-scale hay bales in a 3-2-1 pyramid at x on flat ground: a solid, climbable 4.5-high obstacle in
        /// three 1.5 steps, 5.7 wide at the base. Ground corn under it is cleared (the path corn climbs over it).
        /// </summary>
        public LevelBuilder HayPyramid(float x) => BaleStack(x, 3, "Hay pyramid");

        private LevelBuilder BaleStack(float x, int rows, string label)
        {
            int top = GroundTopAt(x, out bool found);
            float reach = rows * BaleWidth * 0.5f + 0.5f;
            for (float dx = -reach; found && dx <= reach; dx += 0.5f)
            {
                if (GroundTopAt(x + dx, out bool f) != top || !f)
                {
                    found = false;
                }
            }
            if (!found)
            {
                Error($"{label} at x={x} needs flat ground {reach} units either side.");
                return this;
            }

            _haystacks.Add((new Vector2(x, top), rows));
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
            ValidateRarePellet();
            ValidatePassage();
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
                        if (current.partner != null && reached.Add(current.partner))
                        {
                            queue.Enqueue(current.partner);   // ride a moving ledge to the other end of its trip
                        }
                        foreach (var u in _updrafts.Where(u => u.baseTop == current.top && u.x >= current.x0 && u.x < current.x1))
                        {
                            foreach (var next in _surfaces.Where(n => n.top > u.baseTop && n.top <= u.baseTop + u.height + 0.5f
                                && n.x1 > u.x - UpdraftHalfWidth - UpdraftReach && n.x0 < u.x + UpdraftHalfWidth + UpdraftReach))
                            {
                                if (reached.Add(next)) { queue.Enqueue(next); }   // ride the updraft up beside it
                            }
                        }
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

            foreach (var m in _movers)
            {
                int sx0 = Mathf.Min(m.start.x0, m.end.x0), sx1 = Mathf.Max(m.start.x1, m.end.x1);
                int lowBase = Mathf.Min(m.start.baseTop, m.end.baseTop);
                foreach (var g in _surfaces.Where(s => s.kind != Kind.Floating && s.x0 < sx1 && sx0 < s.x1))
                {
                    if (lowBase < g.top)
                    {
                        Error($"Moving ledge from {m.start} to {m.end} sweeps through {g}.");
                    }
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
            if (_pathCorn) { AddPathCorn(); }
            if (_standardFarm) { PlanStandardFarm(assets.farmArt); }
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
            rootComponent.parallaxLayers = assets.parallaxLayers ?? Array.Empty<Sprite>();

            var grid = new GameObject("Grid");
            grid.transform.SetParent(root.transform, false);
            grid.AddComponent<Grid>();

            Tilemap ground = CreateTilemap(grid.transform, "Ground", assets.groundLayer, 0);
            Tilemap platforms = CreateTilemap(grid.transform, "Platforms", assets.groundLayer, 1);
            // Frozen ground (IceFlat) on its own tilemap, so its collider carries the IceSurface traction. Named "Ice":
            // the camera treats it as floor like "Ground".
            Tilemap ice = _surfaces.Any(s => s.ice) ? CreateTilemap(grid.transform, "Ice", assets.groundLayer, 0) : null;

            // Cells of ground carved out for breakable-floor hollows (and the breakable row itself).
            bool IsCarved(int x, int y) => _breakables.Any(b => x >= b.x && x < b.x + b.length && y >= b.top - 3 && y <= b.top - 1);

            foreach (var s in _surfaces)
            {
                switch (s.kind)
                {
                    case Kind.Ground when s.ice:
                        Fill(ice, assets.iceTile != null ? assets.iceTile : assets.groundTile, s.x0, s.x1 - 1, GroundDepth, s.top - 1, IsCarved);
                        break;
                    case Kind.Ground:
                        Fill(ground, assets.groundTile, s.x0, s.x1 - 1, GroundDepth, s.top - 1, IsCarved);
                        break;
                    case Kind.Mound:
                        Fill(ground, assets.groundTile, s.x0, s.x1 - 1, s.baseTop, s.top - 1);
                        break;
                    case Kind.Floating when s.partner != null:
                        break;   // a moving ledge: built as its own object below
                    case Kind.Floating:
                        // Every floating platform (ordinary ones too, not just secret ledges) is drawn with the
                        // Stone_Block.png slabs; StoneBlocks() bonus platforms keep their square blocks.
                        Sprite stoneSprite = s.bridge ? assets.bridgeSprite
                            : s.brick && assets.brickBlockSprite != null ? assets.brickBlockSprite
                            : s.blocks ? assets.stoneBlockSprite : assets.ledgeSprite;
                        bool stoneArt = stoneSprite != null && assets.invisibleTile != null;
                        Fill(platforms, stoneArt ? assets.invisibleTile : assets.platformTile, s.x0, s.x1 - 1, s.top - 1, s.top - 1);
                        if (stoneArt && s.bridge)
                        {
                            AddBridge(root.transform, stoneSprite, s);
                        }
                        else if (stoneArt)
                        {
                            AddStoneSlabs(root.transform, stoneSprite, s);
                        }
                        break;
                }
            }

            for (int i = 0; i < _movers.Count; i++)
            {
                BuildMovingLedge(root.transform, assets, _movers[i], i);
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
            if (_passage.HasValue)
            {
                BuildPassage(root.transform, grid.transform, ground, platforms, assets, endX);   // after PaintSurface: the cellar stays plain dirt
            }
            Finish(ground);
            BuildBarrelPyramids(root.transform, assets);
            BuildHayStacks(root.transform, assets);
            var haybales = _manualScenery ? new List<Vector2>() : PlaceObstacles(root.transform, assets);
            var scenery = _manualScenery || _standardFarm ? new List<(float x, float halfWidth)>() : PlaceScenery(root.transform, assets, haybales);
            PlaceFarmBackdrop(root.transform, assets.farmArt, scenery, endX);
            Finish(platforms);
            if (ice != null)
            {
                Finish(ice);
                ice.gameObject.AddComponent<IceSurface>();
                PlaceIceSigns(root.transform, assets);
            }
            PlaceChasmWaterfalls(root.transform, assets);
            BuildRivers(root.transform, assets);
            BuildUpdrafts(root.transform, assets);

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
            PlaceRarePellet(root.transform, assets);

            var markers = new GameObject("Markers");
            markers.transform.SetParent(root.transform, false);

            AddMarker<PlayerStartPoint>(markers.transform, "PlayerStart", _playerStart.Value);
            if (_goal.HasValue)
            {
                AddMarker<GoalMarker>(markers.transform, "Goal", _goal.Value);
            }

            for (int i = 0; i < _checkpoints.Count; i++)
            {
                AddMarker<CheckpointMarker>(markers.transform, $"Checkpoint_{i + 1}", _checkpoints[i]).worldArt = assets.checkpointSprite;
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
                if (_crops[i].coin)
                {
                    crop.visualOverride = assets.coinSprite;
                    crop.coinValue = 1;
                    crop.passageCoin = _crops[i].passage;
                }
                else if (!_crops[i].secret && assets.cropSprite != null)
                {
                    crop.visualOverride = assets.cropSprite;
                }
            }

            for (int i = 0; i < _robots.Count; i++)
            {
                var robot = AddMarker<RobotSpawnPoint>(markers.transform, $"{_robots[i].type}_{i + 1}", new Vector2(_robots[i].x, _robots[i].y));
                robot.robotType = _robots[i].type;
                robot.patrolDistance = _robots[i].patrol;
                robot.wave = _robots[i].wave;
                if (assets.robotArt != null && assets.robotArt.TryGetValue(_robots[i].type, out var look))
                {
                    robot.artRight = look.right;
                    robot.artLeft = look.left;
                    robot.artDefeat = look.defeat;
                    robot.artScale = assets.robotArtScale;
                }
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
        // Rare pellet (see RarePelletPickup): at most one per level, where RarePellet() put it - always somewhere every
        // character can reach (a normal or double jump from the path or an open stair), never behind an ability gate,
        // since any character may find it and it unlocks the next character. Baked into the prefab, which is
        // instantiated fresh on every load, so it needs no marker or pool.
        private const float RarePelletRadius = 0.6f;
        private const int RarePelletSortingOrder = 6;   // over the crops (5)
        private Vector2? _rarePellet;

        /// <summary>The level's rare pellet, centred at (x, y). Keep it reachable by every character (see PlaceRarePellet).</summary>
        public LevelBuilder RarePellet(float x, float y)
        {
            if (_rarePellet.HasValue)
            {
                Error("Only one rare pellet per level.");
            }
            _rarePellet = new Vector2(x, y);
            return this;
        }

        // The pellet must float clear of every solid surface (checked in Validate, before anything is built).
        private void ValidateRarePellet()
        {
            if (!_rarePellet.HasValue)
            {
                return;
            }

            var spot = _rarePellet.Value;
            foreach (var s in _surfaces)
            {
                float bottom = s.kind == Kind.Floating ? s.baseTop : GroundDepth;
                if (spot.x > s.x0 - RarePelletRadius && spot.x < s.x1 + RarePelletRadius
                    && spot.y - RarePelletRadius < s.top && spot.y + RarePelletRadius > bottom)
                {
                    Error($"Rare pellet at {spot} overlaps solid ground/platform [{s.x0},{s.x1}) top {s.top}.");
                }
            }
        }

        private void PlaceRarePellet(Transform root, LevelAssets assets)
        {
            if (!_rarePellet.HasValue || assets.rarePelletSprite == null)
            {
                return;
            }

            var spot = _rarePellet.Value;
            // No crop hidden behind the apple.
            _crops.RemoveAll(c => Vector2.Distance(new Vector2(c.x, c.y), spot) < RarePelletRadius * 2f);

            var pellet = new GameObject("RarePellet");
            pellet.transform.SetParent(root, false);
            pellet.transform.position = new Vector3(spot.x, spot.y, 0f);
            var trigger = pellet.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = RarePelletRadius;
            var art = new GameObject("Art");
            art.transform.SetParent(pellet.transform, false);
            var renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = assets.rarePelletSprite;
            renderer.sortingOrder = RarePelletSortingOrder;
            var pickup = pellet.AddComponent<RarePelletPickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("visual").objectReferenceValue = renderer;
            so.ApplyModifiedPropertiesWithoutUndo();
            RarePelletPosition = new Vector2(spot.x, spot.y);
        }

        /// <summary>Where this level's rare pellet was placed (after Build), or null when it has none.</summary>
        public Vector2? RarePelletPosition { get; private set; }

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

        // ------------------------------------------------------------ ice

        // A THIN ICE sign (scenery, no collider) where each IceFlat stretch begins: on the ground just before it when
        // that is flat and one height, otherwise on the ice itself. A stretch that carries straight on from another
        // (same height) gets none.
        private void PlaceIceSigns(Transform root, LevelAssets assets)
        {
            if (assets.iceSignSprite == null) { return; }
            foreach (var s in _surfaces.Where(s => s.ice))
            {
                if (_surfaces.Any(o => o.ice && o != s && o.x1 == s.x0 && o.top == s.top)) { continue; }
                float x = s.x0 - 1.2f;
                int top = GroundTopAt(x, out bool found);
                if (!found || top != s.top) { x = s.x0 + 1f; top = s.top; }

                var go = new GameObject($"IceSign_{s.x0}");
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(x, top + BackdropLift, 0f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = assets.iceSignSprite;
                renderer.sortingOrder = -4;   // with the fences: behind the ground tiles' grass, in front of the scenery
            }
        }

        // ------------------------------------------------------------ secret passages
        //
        // Mario-style secret passage (at most one per level): a SecretSign post standing on some optional surface every
        // character can reach. Walking into it drops the player (SecretPassageDoor, automatic, a short fade) into an
        // underground cellar built far below and beyond the level (clear of the pit trigger and the level's camera
        // span): a dirt room with three stone-block steps, PassageCoins coins (1 coin each, not crops) and the level's
        // rare pellet on the middle, tallest step. A second sign at the room's far end brings the player back up where
        // they went down, on the entrance sign (which ignores them until they step off it), so no part of the level is
        // skipped (since 2026-10-03; was: further along the level at an authored exitX). The room is generated, not
        // authored, and is laid out so every coin and the pellet are reachable with the ordinary jump and double jump;
        // the author only picks the entrance.

        private struct PassageRec { public float x; public int top; public PassageLayout layout; }
        private PassageRec? _passage;

        private const int RoomWidth = 34;        // interior cells
        private const int RoomHeight = 7;        // interior cells, floor to ceiling
        private const int RoomFloorTop = -40;    // far below the pit trigger at y=-10
        private const int RoomOffsetX = 20;      // gap between the level's right wall and the room
        private const float DoorWidth = 1.2f, DoorHeight = 2.4f;
        private const int PassageSignSortingOrder = 3;   // over the farm backdrop, under crops (5) and actors
        private static readonly Color BackwallTint = new(0.3f, 0.24f, 0.2f, 1f);
        private const float RoomEntryX = 2f, RoomExitX = 32f;

        // ---- room layouts: every passage names one, and no world repeats one, so each secret room plays differently.
        // Blocks are in room cells: dx from the room's left wall, width, top above the floor, and bottom (0 = a solid
        // column down to the floor; otherwise a one-block-thick floating slab, bottom = top - 1). Keep dx within
        // [5, 28] so the entry (x 2) and exit sign (x 32) stay on open floor, and tops at most MaxRoomTop so a
        // character standing there clears the 7-high ceiling. The pellet floats over the layout's high point.
        private readonly struct RoomBlock
        {
            public readonly int dx, width, top, bottom;
            public RoomBlock(int dx, int width, int top, int bottom) { this.dx = dx; this.width = width; this.top = top; this.bottom = bottom; }
        }

        private static RoomBlock Col(int dx, int width, int top) => new(dx, width, top, 0);
        private static RoomBlock Slab(int dx, int width, int top) => new(dx, width, top, top - 1);

        private static readonly Dictionary<PassageLayout, (RoomBlock[] blocks, Vector2 pellet)> RoomLayouts = new()
        {
            // Three steps, 2 / 3 / 2 high; the pellet over the tall middle one (the original room).
            { PassageLayout.Steps, (new[] { Col(8, 3, 2), Col(15, 3, 3), Col(22, 3, 2) }, new Vector2(16.5f, 4.2f)) },
            // A staircase climbing one block at a time to a 4-high landing, then a drop and a last low step.
            { PassageLayout.Staircase, (new[] { Col(7, 3, 1), Col(10, 3, 2), Col(13, 3, 3), Col(16, 3, 4), Col(24, 2, 1) }, new Vector2(17.5f, 5.2f)) },
            // A stepped pyramid up to a 4-high summit and down the other side.
            { PassageLayout.Pyramid, (new[] { Col(9, 2, 1), Col(11, 2, 2), Col(13, 2, 3), Col(15, 3, 4), Col(18, 2, 3), Col(20, 2, 2), Col(22, 2, 1) }, new Vector2(16.5f, 5.2f)) },
            // Floating ledges over an open floor (the floor runs underneath); the pellet on the highest.
            { PassageLayout.Ledges, (new[] { Slab(7, 3, 3), Slab(13, 3, 4), Slab(20, 3, 3), Col(26, 2, 1) }, new Vector2(14.5f, 5.2f)) },
            // Thin pillars rising and falling; the pellet hangs between the two tallest, a jump off either.
            { PassageLayout.Pillars, (new[] { Col(7, 1, 1), Col(10, 1, 2), Col(13, 1, 3), Col(17, 1, 3), Col(21, 1, 2), Col(25, 1, 1) }, new Vector2(15.5f, 4.6f)) },
            // A long roof over a low tunnel: up a block onto it, the pellet at its far end; the coins run below.
            { PassageLayout.Tunnel, (new[] { Col(5, 2, 2), Slab(8, 18, 4) }, new Vector2(24f, 5.2f)) },
            // Low and high slabs in turn: hop the low ones, run under or over the high ones; the pellet on the last high one.
            { PassageLayout.Zigzag, (new[] { Slab(6, 3, 2), Slab(11, 3, 4), Slab(16, 3, 2), Slab(21, 3, 4), Slab(26, 2, 2) }, new Vector2(22.5f, 5.2f)) },
        };

        private const int MaxRoomTop = 5;              // a character standing here still clears the ceiling
        private const float RoomJumpRise = 3f;         // reachable with the single base jump (3.5) and some margin
        private const float RoomJumpReach = 3.5f;      // horizontal gap a jump comfortably crosses between surfaces
        private const float PassableClearance = 2f;    // a slab this high off the floor can be walked under
        private const int PassageCoinCount = 16;
        private const float PassageCoinFirstX = 3f, PassageCoinLastX = 29.5f, CoinRest = 0.9f, CoinHalf = 0.45f;

        /// <summary>
        /// The level's secret passage: its entrance sign stands at x on a surface whose top is 'top' (ground, mound,
        /// platform, ledge or a Chamber roof); the room's exit brings the player back up on the same spot. The
        /// passage room holds the level's rare pellet, so don't also call RarePellet(). Put the sign somewhere
        /// optional (entering is automatic) that every character can reach. 'layout' picks the room's shape (see
        /// RoomLayouts); give each passage in a world a different one.
        /// </summary>
        public LevelBuilder SecretPassage(float x, int top, PassageLayout layout)
        {
            if (_passage.HasValue)
            {
                Error("Only one secret passage per level.");
            }
            _passage = new PassageRec { x = x, top = top, layout = layout };
            return this;
        }

        /// <summary>The passage room's layout (after Build), or null when the level has no passage.</summary>
        public PassageLayout? PassageLayoutUsed => _passage?.layout;

        private void ValidatePassage()
        {
            if (!_passage.HasValue)
            {
                return;
            }

            var p = _passage.Value;
            if (_rarePellet.HasValue)
            {
                Error("A level with a secret passage keeps its rare pellet in the passage room: remove RarePellet().");
            }
            ValidateRoomLayout(p.layout);

            bool onSurface = _surfaces.Any(s => s.top == p.top && p.x >= s.x0 + 0.5f && p.x <= s.x1 - 0.5f);
            bool onChamberRoof = _chambers.Any(c => c.floorTop + 4 == p.top && p.x >= c.x0 + 0.5f && p.x <= c.x0 + c.interiorWidth + 1.5f);
            if (!onSurface && !onChamberRoof)
            {
                Error($"Secret passage sign at x={p.x} has no surface with top {p.top} under it.");
            }

        }

        private void BuildPassage(Transform root, Transform grid, Tilemap ground, Tilemap platforms, LevelAssets assets, int endX)
        {
            var p = _passage.Value;
            int r0 = endX + RoomOffsetX, r1 = r0 + RoomWidth, f = RoomFloorTop;

            // Cellar shell on the Ground tilemap (so the camera frames its floor): floor, walls, ceiling.
            Fill(ground, assets.groundTile, r0 - 1, r1, f - 4, f - 1);
            Fill(ground, assets.groundTile, r0 - 1, r0 - 1, f, f + RoomHeight);
            Fill(ground, assets.groundTile, r1, r1, f, f + RoomHeight);
            Fill(ground, assets.groundTile, r0 - 1, r1, f + RoomHeight, f + RoomHeight + 1);

            // Dark dirt back wall around the room, well past what the zoomed-out camera can see (no collider).
            Tilemap backwall = CreateTilemap(grid, "PassageBackwall", 0, -2);
            Fill(backwall, assets.groundTile, r0 - 30, r1 + 30, f - 16, f + RoomHeight + 18);
            backwall.color = BackwallTint;

            // The layout's stone blocks: solid columns down to the floor, or one-block floating slabs.
            var (blocks, pellet) = RoomLayouts[p.layout];
            bool stoneArt = assets.stoneBlockSprite != null && assets.invisibleTile != null;
            foreach (var block in blocks)
            {
                for (int row = block.bottom + 1; row <= block.top; row++)
                {
                    var cell = new Surface { x0 = r0 + block.dx, x1 = r0 + block.dx + block.width, top = f + row, baseTop = f + row - 1, kind = Kind.Floating, stone = true, blocks = true };
                    Fill(platforms, stoneArt ? assets.invisibleTile : assets.platformTile, cell.x0, cell.x1 - 1, cell.top - 1, cell.top - 1);
                    if (stoneArt)
                    {
                        AddStoneSlabs(root, assets.stoneBlockSprite, cell);
                    }
                }
            }

            foreach (var coin in RoomCoins(blocks, pellet))
            {
                _crops.Add(new CropRec { x = r0 + coin.x, y = f + coin.y, coin = true, passage = true });
            }
            _rarePellet = new Vector2(r0 + pellet.x, f + pellet.y);

            // No crop left inside the entrance sign (the ledge's secret row, path corn).
            _crops.RemoveAll(c => !c.passage && Mathf.Abs(c.x - p.x) < 1f && c.y > p.top && c.y < p.top + DoorHeight + 0.5f);

            var entrance = AddDoor(root, "SecretPassage_Entrance", new Vector2(p.x, p.top), assets.secretSignSprite, false);
            entrance.destination = new Vector2(r0 + RoomEntryX, f + 0.6f);
            entrance.cameraMinX = r0 - 1;
            entrance.cameraMaxX = r1 + 1;

            // Back up where they went down: on the entrance sign, which then waits for them to step off it.
            var exit = AddDoor(root, "SecretPassage_Exit", new Vector2(r0 + RoomExitX, f), assets.secretSignSprite, true);
            exit.destination = new Vector2(p.x, p.top + 0.6f);
            exit.returnsToLevel = true;
            exit.entrance = entrance;
            PassageRoom = new RectInt(r0, f, RoomWidth, RoomHeight);
        }

        /// <summary>The passage room's interior cells (after Build), or null when the level has no passage.</summary>
        public RectInt? PassageRoom { get; private set; }

        // Blocks that stand in the way at floor level (solid columns, and slabs too low to walk under).
        private static bool Obstructs(RoomBlock b) => b.bottom < PassableClearance;

        // PassageCoinCount coins spread evenly across the room, each resting on whatever it's over: the top of a
        // block in the way, else the floor (also under a slab high enough to walk beneath). None on the pellet.
        private static IEnumerable<Vector2> RoomCoins(RoomBlock[] blocks, Vector2 pellet)
        {
            float step = (PassageCoinLastX - PassageCoinFirstX) / (PassageCoinCount - 1);
            for (int i = 0; i < PassageCoinCount; i++)
            {
                float x = PassageCoinFirstX + i * step;
                float rest = 0f;
                foreach (var b in blocks)
                {
                    if (Obstructs(b) && x + CoinHalf > b.dx && x - CoinHalf < b.dx + b.width) { rest = Mathf.Max(rest, b.top); }
                }
                var coin = new Vector2(x, rest + CoinRest);
                if (Vector2.Distance(coin, pellet) < 1.3f) { coin.y += 1.3f; }
                yield return coin;
            }
        }

        // The room must be crossable from the entry to the exit and its pellet reachable with the single base jump
        // (every character, no ability), standing surfaces must clear the ceiling, and nothing may sit on the entry,
        // the exit sign or the pellet.
        private void ValidateRoomLayout(PassageLayout layout)
        {
            if (!RoomLayouts.TryGetValue(layout, out var def))
            {
                Error($"Unknown passage layout {layout}.");
                return;
            }
            var (blocks, pellet) = def;

            foreach (var b in blocks)
            {
                if (b.dx < 5 || b.dx + b.width > 29) { Error($"Passage layout {layout}: block at {b.dx} crowds the entry or exit."); }
                if (b.top > MaxRoomTop) { Error($"Passage layout {layout}: block top {b.top} leaves no headroom under the ceiling."); }
                if (pellet.x + RarePelletRadius > b.dx && pellet.x - RarePelletRadius < b.dx + b.width
                    && pellet.y - RarePelletRadius < b.top && pellet.y + RarePelletRadius > b.bottom)
                {
                    Error($"Passage layout {layout}: the pellet overlaps the block at {b.dx}.");
                }
            }
            if (pellet.y + RarePelletRadius > RoomHeight) { Error($"Passage layout {layout}: the pellet is in the ceiling."); }

            // Standing surfaces: every block top, and the floor between the blocks in the way.
            var surfaces = new List<(float x0, float x1, float top)>();
            foreach (var b in blocks) { surfaces.Add((b.dx, b.dx + b.width, b.top)); }
            var cuts = blocks.Where(Obstructs).OrderBy(b => b.dx).ToList();
            float floorStart = 0f;
            foreach (var b in cuts)
            {
                if (b.dx > floorStart) { surfaces.Add((floorStart, b.dx, 0f)); }
                floorStart = Mathf.Max(floorStart, b.dx + b.width);
            }
            if (floorStart < RoomWidth) { surfaces.Add((floorStart, RoomWidth, 0f)); }

            // Reachable from the entry floor: a surface is reached from another within a jump's rise and reach
            // (dropping down any height is fine).
            int start = surfaces.FindIndex(s => s.top == 0f && RoomEntryX >= s.x0 && RoomEntryX <= s.x1);
            var reached = new bool[surfaces.Count];
            var queue = new Queue<int>();
            if (start >= 0) { reached[start] = true; queue.Enqueue(start); }
            while (queue.Count > 0)
            {
                var a = surfaces[queue.Dequeue()];
                for (int i = 0; i < surfaces.Count; i++)
                {
                    if (reached[i]) { continue; }
                    var s = surfaces[i];
                    float gap = Mathf.Max(0f, Mathf.Max(s.x0 - a.x1, a.x0 - s.x1));
                    if (gap <= RoomJumpReach && s.top - a.top <= RoomJumpRise)
                    {
                        reached[i] = true;
                        queue.Enqueue(i);
                    }
                }
            }

            if (!surfaces.Where((s, i) => reached[i]).Any(s => s.top == 0f && RoomExitX >= s.x0 && RoomExitX <= s.x1))
            {
                Error($"Passage layout {layout}: the exit sign can't be reached from the entry with a single jump.");
            }
            bool pelletReached = surfaces.Where((s, i) => reached[i]).Any(s =>
                pellet.x >= s.x0 - 1.5f && pellet.x <= s.x1 + 1.5f && pellet.y - RarePelletRadius - s.top <= RoomJumpRise + 1.5f);
            if (!pelletReached)
            {
                Error($"Passage layout {layout}: the pellet can't be reached with a single jump.");
            }
        }

        // A sign standing at feet with a trigger over its post; the exit's sign faces back the other way.
        private static SecretPassageDoor AddDoor(Transform root, string doorName, Vector2 feet, Sprite sign, bool flip)
        {
            var go = new GameObject(doorName);
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(feet.x, feet.y, 0f);
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(DoorWidth, DoorHeight);
            trigger.offset = new Vector2(0f, DoorHeight * 0.5f);

            if (sign != null)
            {
                var art = new GameObject("Sign");
                art.transform.SetParent(go.transform, false);
                var renderer = art.AddComponent<SpriteRenderer>();
                renderer.sprite = sign;
                renderer.flipX = flip;
                renderer.sortingOrder = PassageSignSortingOrder;
            }
            return go.AddComponent<SecretPassageDoor>();
        }

        // ------------------------------------------------------------ path corn

        // Kernel centres along the path are this far apart: every second kernel of the tightest line icons allow
        // (MinCropSpacing), so the line is an even dotted trail rather than a solid row.
        private const float PathCornSpacing = MinCropSpacing * 2f;
        // Jump-hint arc over a pit: starts this far above the higher edge and bulges this much more mid-pit.
        private const float PitArcLift = 1.2f, PitArcPeak = 1.4f;

        // Main-path range: start to goal, or to the far wall in a boss arena (no goal).
        private (float x0, float x1) PathRange() =>
            (_playerStart?.x ?? _startX, _goal.HasValue ? _goal.Value.x : _x - 1.5f);

        // Height of what a kernel on the path rests on at x: the ground or mound, raised by a hay stack or barrel
        // pyramid standing there (their stepped profiles). found = false over a pit.
        private float PathSurface(float x, out bool found)
        {
            int ground = GroundTopAt(x, out found);
            if (!found) { return 0f; }
            float top = ground;
            foreach (var p in _pyramids)
            {
                float dx = Mathf.Abs(x - p.x);
                int rows = 0;
                for (int row = 0; row < BarrelRows; row++)
                {
                    if (dx < (BarrelRows - row) * BarrelWidth * 0.5f) { rows++; }
                }
                if (rows > 0 && Mathf.Approximately(p.y, ground)) { top = Mathf.Max(top, ground + rows * BarrelHeight); }
            }
            foreach (var (h, stackRows) in _haystacks)
            {
                float dx = Mathf.Abs(x - h.x);
                int rows = 0;
                for (int row = 0; row < stackRows; row++)
                {
                    if (dx < (stackRows - row) * BaleWidth * 0.5f) { rows++; }
                }
                if (rows > 0 && Mathf.Approximately(h.y, ground)) { top = Mathf.Max(top, ground + rows * BaleHeight); }
            }
            return top;
        }

        // The highest path surface within a kernel's half-width of x, so a kernel beside a step sits on the step
        // rather than inside it.
        private float PathRestTop(float x)
        {
            float best = float.MinValue;
            for (float dx = -0.45f; dx <= 0.45f + 0.001f; dx += 0.15f)
            {
                float top = PathSurface(x + dx, out bool found);
                if (found) { best = Mathf.Max(best, top); }
            }
            return best;
        }

        private bool BlockedForCrop(float x, float y) =>
            _surfaces.Any(s => CropOverlaps(x, y, s)) || Hollows().Any(h => h.Contains(new Vector2(x, y)));

        private bool HasGround(float x)
        {
            GroundTopAt(x, out bool found);
            return found;
        }

        // The main-path bridge deck at x, if any (bonus bridges aren't on the path line).
        private Surface PathBridgeAt(float x) => _surfaces.FirstOrDefault(s => s.bridge && !s.bonus && x >= s.x0 && x < s.x1);

        // Replaces the hand-placed crops on the path line with one evenly spaced line of kernels (see PathCorn).
        private void AddPathCorn()
        {
            var (x0, x1) = PathRange();

            // Pits the main path crosses, as [a, b) with ground either side; only jumpable ones get an arc.
            var pits = new List<(int a, int b, float baseTop)>();
            for (int cx = Mathf.FloorToInt(x0); cx < Mathf.CeilToInt(x1); cx++)
            {
                if (HasGround(cx + 0.5f) || PathBridgeAt(cx + 0.5f) != null) { continue; }
                int a = cx;
                while (cx < x1 && !HasGround(cx + 0.5f) && PathBridgeAt(cx + 0.5f) == null) { cx++; }
                int b = cx;
                if (b - a > MaxFlatGap || !HasGround(a - 0.5f) || !HasGround(b + 0.5f)) { continue; }
                float baseTop = Mathf.Max(PathSurface(a - 0.5f, out _), PathSurface(b + 0.5f, out _));
                pits.Add((a, b, baseTop));
            }

            bool OnPathLine(CropRec c)
            {
                if (c.x < x0 - 1f || c.x > x1 + 1f) { return false; }
                if (HasGround(c.x)) { return Mathf.Abs(c.y - (PathRestTop(c.x) + CropRestHeight)) < 0.6f; }
                if (PathBridgeAt(c.x) is { } deck) { return Mathf.Abs(c.y - (deck.top + CropRestHeight)) < 0.6f; }
                return pits.Any(p => c.x > p.a - 0.5f && c.x < p.b + 0.5f && c.y < p.baseTop + PitArcLift + PitArcPeak + 1f);
            }
            _crops.RemoveAll(c => !c.secret && !c.coin && OnPathLine(c));

            for (float x = x0 + 1f; x <= x1 - 0.5f; x += PathCornSpacing)
            {
                float y;
                if (HasGround(x)) { y = PathRestTop(x) + CropRestHeight; }
                else if (PathBridgeAt(x) is { } deck) { y = deck.top + CropRestHeight; }   // along a bridge's deck
                else { continue; }   // pits get their arc below
                if (!BlockedForCrop(x, y)) { _crops.Add(new CropRec { x = x, y = y, path = true }); }
            }

            // Over a pit the line keeps the same spacing (so no arc kernel crowds a ground one and gets thinned out),
            // lifted into an arc; a pit too narrow to hold a grid point gets one kernel at its middle.
            foreach (var (a, b, baseTop) in pits)
            {
                bool any = false;
                for (float x = x0 + 1f; x <= x1 - 0.5f; x += PathCornSpacing)
                {
                    if (x < a || x >= b) { continue; }   // x == a has no ground under it, so the arc owns it
                    any = true;
                    AddArcKernel(x, a, b, baseTop);
                }
                if (!any) { AddArcKernel((a + b) * 0.5f, a, b, baseTop); }
            }
        }

        private void AddArcKernel(float x, int a, int b, float baseTop)
        {
            float t = (x - a) / (b - a);
            float y = baseTop + PitArcLift + PitArcPeak * Mathf.Sin(Mathf.PI * t);
            if (!BlockedForCrop(x, y)) { _crops.Add(new CropRec { x = x, y = y, path = true }); }
        }

        // ------------------------------------------------------------ standard farm scenery

        // The farm backdrop every level shares (StandardFarm), as three landmark groups, each prop an offset from the
        // group's anchor:
        //   Entrance, just after the start: water wheel, windmill and gnarled tree together.
        //   Farmyard, mid-level: the broken barn with the wooden cart beside it and the oak behind them.
        //   Silo, later on, standing among the corn fields (the corn grows in front of it).
        private static readonly (FarmProp prop, float dx)[] EntranceGroup =
        {
            (FarmProp.WaterWheel, 0f), (FarmProp.Windmill, 3.5f), (FarmProp.GnarledTree, 7.5f),
        };
        private static readonly (FarmProp prop, float dx)[] FarmyardGroup =
        {
            (FarmProp.Oak, -1.5f), (FarmProp.Barn, 0f), (FarmProp.Cart, 4.5f),
        };
        private static readonly (FarmProp prop, float dx)[] SiloGroup = { (FarmProp.Silo, 0f) };
        private const float EntranceFraction = 0.04f, FarmyardFraction = 0.5f, SiloFraction = 0.8f;
        private const float FieldMargin = 1f;         // clear ground kept between a landmark group and the corn fields
        private const float LedgeClearance = 1.5f;    // a landmark keeps this far from any platform/ledge in front of it
        private const float TightLedgeClearance = 0.5f; // fallback for a crowded level: still never behind a ledge

        private static Sprite FarmSprite(FarmProp prop, FarmBackdropArt art) => art == null ? null : prop switch
        {
            FarmProp.Silo => art.silo,
            FarmProp.Oak => art.oak,
            FarmProp.GnarledTree => art.gnarledTree,
            FarmProp.WaterWheel => art.waterWheel,
            FarmProp.Barn => art.barn,
            FarmProp.Windmill => art.windmill,
            _ => art.cart,
        };

        // Lays out StandardFarm(): the three landmark groups (each on the flat ground nearest its preferred fraction of
        // the level, never behind a platform, ledge, stone block, chamber or stacked obstacle, so they stay in view),
        // then fenced corn over the ground left between the entrance and farmyard groups (it runs past the silo).
        private void PlanStandardFarm(FarmBackdropArt art)
        {
            var (x0, x1) = PathRange();
            float length = x1 - x0;
            var occupied = new List<(float a, float b)>();

            // Foreground things that would hide a landmark standing behind them.
            var blockers = new List<(float a, float b)>();
            blockers.AddRange(_surfaces.Where(sf => sf.kind == Kind.Floating).Select(sf => ((float)sf.x0, (float)sf.x1)));
            blockers.AddRange(_chambers.Select(c => ((float)c.x0 - 1f, (float)(c.x0 + c.interiorWidth + 1))));
            blockers.AddRange(_pyramids.Select(pyr => (pyr.x - 2.5f, pyr.x + 2.5f)));
            blockers.AddRange(_haystacks.Select(h => (h.at.x - 2.5f, h.at.x + 2.5f)));

            // Nearest x to 'preferred' (scanning outward) where [x - left, x + right] is flat ground of one height,
            // inside the level, clear of the groups already placed and of every blocker; null if there is none.
            float? FindSpot(float preferred, float left, float right, bool fieldMargin, float clearance)
            {
                float margin = fieldMargin ? FieldMargin : 0f;
                for (float d = 0f; d < length; d += 0.5f)
                {
                    foreach (float spot in new[] { preferred + d, preferred - d })
                    {
                        float a = spot - left, b = spot + right;
                        if (a < x0 - 1f || b > x1 - 1f) { continue; }
                        if (occupied.Any(o => b > o.a - margin && a < o.b + margin)) { continue; }
                        if (blockers.Any(k => b > k.a - clearance && a < k.b + clearance)) { continue; }
                        if (FlatSpan(a, b)) { return spot; }
                    }
                }
                return null;
            }

            // Places a group near preferredLeft (its left edge); returns its extent, or null. When the whole group
            // finds no room, each fallback (a smaller version of it) is tried in turn.
            (float a, float b)? PlaceGroup(string name, (FarmProp prop, float dx)[] group, float preferredLeft, bool reserve,
                params (FarmProp prop, float dx)[][] fallbacks)
            {
                float left = 0f, right = 0f;
                foreach (var (prop, dx) in group)
                {
                    var sprite = FarmSprite(prop, art);
                    // The whole width (the art is cropped to its opaque pixels), so no prop overhangs a pit or step.
                    float half = sprite != null ? sprite.bounds.size.x * 0.5f : 1f;
                    left = Mathf.Max(left, half - dx);
                    right = Mathf.Max(right, dx + half);
                }

                // A crowded level (pits, platforms, stairs) may have no stretch that long with the usual clearance:
                // try again tighter, without the corn margin, before leaving the group out.
                float? at = FindSpot(preferredLeft + left, left, right, reserve, LedgeClearance)
                    ?? FindSpot(preferredLeft + left, left, right, false, TightLedgeClearance);
                if (!at.HasValue && fallbacks.Length > 0)
                {
                    return PlaceGroup(name, fallbacks[0], preferredLeft, reserve, fallbacks.Skip(1).ToArray());
                }
                if (!at.HasValue)
                {
                    Debug.LogWarning($"[LevelBuilder] {Id}: no clear flat stretch for the {name}; left out.");
                    return null;
                }

                foreach (var (prop, dx) in group) { Backdrop(prop, at.Value + dx); }
                var extent = (at.Value - left, at.Value + right);
                if (reserve) { occupied.Add(extent); }
                return extent;
            }

            PlaceGroup("entrance (water wheel, windmill, gnarled tree)", EntranceGroup, x0 + length * EntranceFraction, true);
            // A crowded level that can't fit the whole farmyard on flat ground drops the cart, then the oak.
            PlaceGroup("farmyard (barn, cart, oak)", FarmyardGroup, x0 + length * FarmyardFraction - 4f, true,
                FarmyardGroup.Where(g => g.prop != FarmProp.Cart).ToArray(),
                FarmyardGroup.Where(g => g.prop == FarmProp.Barn).ToArray());
            var silo = PlaceGroup("silo", SiloGroup, x0 + length * SiloFraction, false);
            if (silo.HasValue) { occupied.Add(silo.Value); }   // later groups keep off it; the corn doesn't

            // Fenced corn over what's left between the entrance and farmyard groups (CornField/Fence skip gaps and
            // steps themselves); the silo stands behind it.
            var groups = occupied.Where(o => !silo.HasValue || o != silo.Value).OrderBy(o => o.a).ToList();
            float from = x0 + 3f;
            foreach (var (a, b) in groups.Append((x1 - 1f, x1 - 1f)))
            {
                float to = a - FieldMargin;
                if (to - from >= 4f) { CornField(from, to).Fence(from, to); }
                from = Mathf.Max(from, b + FieldMargin);
            }

            _biplaneHeight ??= 8.5f;
        }

        // True when [a, b] is ground of one height with no pit.
        private bool FlatSpan(float a, float b)
        {
            int top = GroundTopAt(a, out bool found);
            if (!found) { return false; }
            for (float x = a; x <= b; x += 0.25f)
            {
                if (GroundTopAt(x, out bool f) != top || !f) { return false; }
            }
            return true;
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
            var picks = new List<(float x, int top, Sprite art)>();
            while (placed.Count < assets.obstaclesPerLevel)
            {
                var open = candidates.Where(c => placed.All(p => Mathf.Abs(p - c.x) >= ObstacleSpacing)).ToList();
                if (open.Count == 0)
                {
                    Debug.LogWarning($"[LevelBuilder] {Id}: only room for {placed.Count} of {assets.obstaclesPerLevel} obstacles.");
                    break;
                }

                var (px, ptop) = open[random.Next(open.Count)];
                placed.Add(px);
                picks.Add((px, ptop, assets.obstacleSprites[random.Next(assets.obstacleSprites.Length)]));
            }

            // HaybalesAsBarrels(): the leftmost hay bales become barrels (chosen after the random picks, so the
            // layout itself is unchanged).
            var barrelSpots = new HashSet<float>(picks.Where(p => p.art == assets.haybaleSprite).OrderBy(p => p.x)
                .Take(_haybalesAsBarrels).Select(p => p.x));

            for (int n = 0; n < picks.Count; n++)
            {
                var (x, top, art) = picks[n];
                float height = ObstacleHeight;
                if (barrelSpots.Contains(x))
                {
                    // One to-scale barrel (1.1 x 1.5): a shorter, narrower step than the bale it replaces.
                    AddBarrel(root, assets, $"Obstacle_Barrel_{n + 1}", new Vector2(x, top), 3);
                    height = BarrelHeight;
                }
                else if (art == assets.haybaleSprite)
                {
                    // One to-scale bale (1.9 x 1.5) is obstacle-sized on its own.
                    AddBale(root, assets, $"Obstacle_HayBale_{n + 1}", new Vector2(x, top), 3);
                    haybales.Add(new Vector2(x, top));
                    height = BaleHeight;
                }
                else
                {
                    var obstacle = new GameObject($"Obstacle_{art.name}_{n + 1}") { layer = assets.groundLayer };
                    obstacle.transform.SetParent(root, false);
                    obstacle.transform.position = new Vector3(x, top, 0f);
                    // The box follows the art's own top and width (a log is lower than a crate), so nothing stands
                    // on air above it; the fixed 2.1 x 1.95 is only the fallback when the art can't be read.
                    Vector2 fit = StampedeUIArt.ObstacleTopAndWidth(art);
                    height = fit.x > 0f ? Mathf.Clamp(fit.x, 0.6f, 2.3f) : ObstacleHeight;
                    float width = fit.y > 0f ? Mathf.Clamp(fit.y, 1f, ObstacleWidth) : ObstacleWidth;
                    var box = obstacle.AddComponent<BoxCollider2D>();
                    box.size = new Vector2(width, height);
                    box.offset = new Vector2(0f, height * 0.5f);
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
                    for (float dx = -half; dx <= half + 0.001f; dx += 0.25f)
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
                int? top = FlatTop(x, half);   // the whole width: never out over a pit or a step
                if (top == null || scenery.Any(p => Mathf.Abs(p.x - x) < p.halfWidth + half * 0.8f))
                {
                    Debug.LogWarning($"[LevelBuilder] {Id}: skipped backdrop {prop} at x={x} (over a gap/step or covering a barn/windmill).");
                    continue;
                }
                Spawn($"{prop}_{x}", sprite, new Vector2(x, top.Value), scale, flip, BackdropTint, order);
            }

            // Background corn, fences and wildflowers only go on the level's base ground (the height at the start):
            // none on mounds, terraces or other raised ground, which read as hills and stay clean.
            int baseTop = GroundTopAt(_playerStart?.x ?? _startX, out _);
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
                            if (top == null || top.Value > baseTop) { continue; }
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
                        if (top == null || top.Value > baseTop) { continue; }
                        Spawn($"Fence_{x:0.00}", art.fence, new Vector2(x, top.Value), 1f, false, BackdropTint, -4);
                        first ??= x - width * 0.5f;
                        last = x + width * 0.5f;
                    }

                    // A bunch of wildflowers (a big clump with a smaller one tucked beside it) at each end of the run.
                    if (art.wildflowers == null || first == null) { continue; }
                    foreach (var (end, outward) in new[] { (first.Value, -1f), (last.Value, 1f) })
                    {
                        int? top = FlatTop(end, 0.3f);
                        if (top == null || top.Value > baseTop) { continue; }
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
                var (at, rows) = _haystacks[h];
                BuildHayStack(root, assets, $"{(rows > 2 ? "HayPyramid" : "HayStack")}_{h + 1}", at, rows);
                _crops.RemoveAll(c => !c.secret && !c.path && Mathf.Abs(c.x - at.x) < rows * BaleWidth * 0.5f + 0.6f
                    && c.y < at.y + rows * BaleHeight);
            }
        }

        // Rows of bales centred on at.x: the bottom row has 'rows' bales, each row above one fewer, each drawn over
        // the row below it.
        private static void BuildHayStack(Transform root, LevelAssets assets, string stackName, Vector2 at, int rows)
        {
            for (int row = 0; row < rows; row++)
            {
                int count = rows - row;
                for (int i = 0; i < count; i++)
                {
                    float x = at.x + (i - (count - 1) * 0.5f) * BaleWidth;
                    AddBale(root, assets, $"{stackName}_R{row}_{i}", new Vector2(x, at.y + row * BaleHeight), 3 + row);
                }
            }
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
                _crops.RemoveAll(c => !c.secret && !c.path && Mathf.Abs(c.x - at.x) < halfBase + 0.6f && c.y < at.y + BarrelRows * BarrelHeight);
                if (!_pathCorn)   // the path line already runs over the top
                {
                    _crops.Add(new CropRec { x = at.x, y = at.y + BarrelRows * BarrelHeight + CropRestHeight });
                }
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

        // Bridge.png: the plank deck spans the art's rows 31-41 of 46 (top down), rails above, post stubs below.
        private const float BridgeDeckTop = 15f / 46f;   // the deck's top edge, as a fraction of the art from its bottom
        private const float BridgeArtHeight = 2f;        // units: a ~0.5 deck and rails ~1.35 high (the actors are 1.5)

        // Repeats the bridge art across the span (whole copies at close to its own aspect, stretched to fit exactly),
        // the deck's top on the surface top.
        private static void AddBridge(Transform root, Sprite art, Surface s)
        {
            Vector2 native = art.bounds.size;
            float aspect = native.x / Mathf.Max(native.y, 0.01f);
            int length = s.x1 - s.x0;
            int count = Mathf.Max(1, Mathf.RoundToInt(length / (BridgeArtHeight * aspect)));
            float width = (float)length / count;
            float centreY = s.top - BridgeDeckTop * BridgeArtHeight + BridgeArtHeight * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Bridge_{s.x0}_{i}");
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(s.x0 + width * (i + 0.5f), centreY, 0f);
                go.transform.localScale = new Vector3(width / native.x, BridgeArtHeight / native.y, 1f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = art;
                renderer.sortingOrder = 1; // with the Platforms tilemap: behind robots and the player
            }
        }

        // A moving ledge: its own solid object on the Ground layer (a kinematic body with a MovingPlatform), drawn with
        // the world's ledge slabs (or the plain platform tile's sprite), placed at the start of its trip.
        private static void BuildMovingLedge(Transform root, LevelAssets assets, MoverRec m, int index)
        {
            var s = m.start;
            int length = s.x1 - s.x0;
            var go = new GameObject($"MovingLedge_{index + 1}") { layer = assets.groundLayer };
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(s.x0 + length * 0.5f, s.top - 0.5f, 0f);
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(length, 1f);
            var mover = go.AddComponent<MovingPlatform>();
            mover.travel = new Vector2(m.end.x0 - s.x0, m.end.top - s.top);
            mover.period = m.period;
            mover.phase = m.phase;

            if (m.balloon && assets.balloonSprite != null)
            {
                // Sized so the basket matches the deck, its rim 0.1 over the deck; the envelope rises above, behind the
                // player (sorting 7). Works whatever the sprite's pivot (it doubles as feet-pivoted scenery).
                var art = new GameObject("Balloon");
                art.transform.SetParent(go.transform, false);
                Sprite sprite = assets.balloonSprite;
                float k = (length + 0.4f) / BalloonBasketWidth / sprite.bounds.size.x;
                float height = sprite.bounds.size.y * k;
                float pivotFromBottom = sprite.pivot.y / sprite.rect.height;
                art.transform.localScale = new Vector3(k, k, 1f);
                art.transform.localPosition = new Vector3(0f, 0.5f + 0.1f - (BalloonRimFromBottom - pivotFromBottom) * height, 0f);
                var renderer = art.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 7;
            }
            else if (m.boat && assets.boatSprite != null)
            {
                // The boat a little longer than its deck, gunwale (the art's middle row) just over the deck so the
                // rider stands inside it; the hull below the water line hides behind the river (sorting 9).
                var art = new GameObject("Boat");
                art.transform.SetParent(go.transform, false);
                Vector2 native = assets.boatSprite.bounds.size;
                float k = (length + 0.6f) / native.x;
                art.transform.localScale = new Vector3(k, k, 1f);
                art.transform.localPosition = new Vector3(0f, 0.5f + 0.25f, 0f);
                var renderer = art.AddComponent<SpriteRenderer>();
                renderer.sprite = assets.boatSprite;
                renderer.sortingOrder = 7;
            }
            else if (assets.ledgeSprite != null)
            {
                AddStoneSlabs(go.transform, assets.ledgeSprite, s);   // world positions: the ledge is at its start
            }
            else if (assets.platformTile != null && assets.platformTile.sprite != null)
            {
                var art = new GameObject("Art");
                art.transform.SetParent(go.transform, false);
                Vector2 native = assets.platformTile.sprite.bounds.size;
                art.transform.localScale = new Vector3(length / native.x, 1f / native.y, 1f);
                var renderer = art.AddComponent<SpriteRenderer>();
                renderer.sprite = assets.platformTile.sprite;
                renderer.sortingOrder = 1;
            }
        }

        // Updrafts: a trigger column (UpdraftZone) with the spiral art stretched over it, behind the player.
        private void BuildUpdrafts(Transform root, LevelAssets assets)
        {
            for (int i = 0; i < _updrafts.Count; i++)
            {
                var u = _updrafts[i];
                var go = new GameObject($"Updraft_{i + 1}");
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(u.x, u.baseTop, 0f);
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(UpdraftHalfWidth * 2f, u.height);
                box.offset = new Vector2(0f, u.height * 0.5f);
                var zone = go.AddComponent<UpdraftZone>();

                if (assets.updraftSprite != null)
                {
                    var art = new GameObject("Spiral");
                    art.transform.SetParent(go.transform, false);
                    Vector2 native = assets.updraftSprite.bounds.size;
                    art.transform.localPosition = new Vector3(0f, u.height * 0.5f, 0f);   // centre-pivoted art
                    art.transform.localScale = new Vector3(2.6f / native.x, u.height / native.y, 1f);
                    var renderer = art.AddComponent<SpriteRenderer>();
                    renderer.sprite = assets.updraftSprite;
                    renderer.color = new Color(1f, 1f, 1f, 0.8f);
                    renderer.sortingOrder = 5;
                    zone.art = art.transform;
                }
            }
        }

        // Rivers: the water art tiled across each gap (in front of robots so a waiting piranha and a boat's hull hide
        // under it, behind the player), a splash line under the surface, mooring posts in the water and reeds on the banks.
        private void BuildRivers(Transform root, LevelAssets assets)
        {
            foreach (var r in _rivers)
            {
                float width = r.x1 - r.x0;
                if (assets.riverSprite != null)
                {
                    var water = new GameObject($"River_{r.x0}");
                    water.transform.SetParent(root, false);
                    water.transform.position = new Vector3((r.x0 + r.x1) * 0.5f, r.surface, 0f);
                    var renderer = water.AddComponent<SpriteRenderer>();
                    renderer.sprite = assets.riverSprite;
                    renderer.drawMode = SpriteDrawMode.Tiled;
                    renderer.tileMode = SpriteTileMode.Continuous;
                    renderer.size = new Vector2(width, assets.riverSprite.bounds.size.y);
                    renderer.sortingOrder = 9;
                }

                var splash = new GameObject($"RiverSplash_{r.x0}");
                splash.transform.SetParent(root, false);
                splash.transform.position = new Vector3((r.x0 + r.x1) * 0.5f, r.surface - 0.9f, 0f);
                var box = splash.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(width, 0.6f);
                splash.AddComponent<PitDeathZone>();

                if (assets.mooringPostSprite != null)
                {
                    foreach (float px in new[] { r.x0 + 0.35f, r.x1 - 0.35f })
                    {
                        var post = new GameObject($"MooringPost_{px:0.#}");
                        post.transform.SetParent(root, false);
                        post.transform.position = new Vector3(px, r.surface - 0.8f, 0f);
                        var renderer = post.AddComponent<SpriteRenderer>();
                        renderer.sprite = assets.mooringPostSprite;
                        renderer.sortingOrder = 6;   // behind a boat (7) and the water (9)
                    }
                }

                if (assets.reedSprite != null)
                {
                    foreach (float px in new[] { r.x0 - 0.9f, r.x1 + 0.9f })
                    {
                        var reed = new GameObject($"RiverReed_{px:0.#}");
                        reed.transform.SetParent(root, false);
                        reed.transform.position = new Vector3(px, GroundTopAt(px, out _) - 0.1f, 0f);
                        var renderer = reed.AddComponent<SpriteRenderer>();
                        renderer.sprite = assets.reedSprite;
                        renderer.flipX = px > r.x1;
                        renderer.sortingOrder = 2;   // in front of the ground tiles, behind robots and the player
                    }
                }
            }
        }

        // A frozen waterfall hung in the chasm under every main-path bridge (scenery, behind the ground tiles), its
        // top just under the deck and narrowed to fit between the cliffs if needed.
        private void PlaceChasmWaterfalls(Transform root, LevelAssets assets)
        {
            if (assets.chasmWaterfallSprite == null) { return; }
            foreach (var s in _surfaces.Where(s => s.bridge && !s.bonus))
            {
                Vector2 native = assets.chasmWaterfallSprite.bounds.size;
                float scale = Mathf.Min(1f, (s.x1 - s.x0 - 0.6f) / native.x);
                var go = new GameObject($"ChasmWaterfall_{s.x0}");
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3((s.x0 + s.x1) * 0.5f, s.top - 0.4f - native.y * scale, 0f);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = assets.chasmWaterfallSprite;
                renderer.color = SceneryTint;
                renderer.sortingOrder = -5;
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
            if (_haystacks.Any(p => Mathf.Abs(p.at.x - x) < p.rows * BaleWidth * 0.5f + ObstacleWidth * 0.5f + 3f)) { return false; }
            // Flat ground past both sides, so it never sits at a gap edge or against a step.
            float flat = ObstacleWidth * 0.5f + 1f;
            for (float dx = -flat; dx <= flat; dx += 0.5f)
            {
                int t = GroundTopAt(x + dx, out bool found);
                if (!found || t != top) { return false; }
            }

            bool Near(Vector2? p, float range) => p.HasValue && Mathf.Abs(p.Value.x - x) < range;
            if (Near(_playerStart, 6f) || Near(_goal, 4f)) { return false; }
            if (Near(_rarePellet, ObstacleWidth * 0.5f + 2f)) { return false; }   // the pellet floats over open ground
            if (_passage.HasValue && Mathf.Abs(_passage.Value.x - x) < ObstacleWidth * 0.5f + 2f) { return false; }   // passage sign
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
