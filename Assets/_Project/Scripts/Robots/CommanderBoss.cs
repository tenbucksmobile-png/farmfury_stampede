using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using UnityEngine;

namespace FarmFuryStampede.Robots
{
    /// <summary>
    /// ===== BOSS ENCOUNTER PATTERN (template for every world's boss; Worlds 2-6 vary it, they do not rebuild it) =====
    ///
    /// A boss level is a normal level prefab (built with LevelBuilder: a run-in, an arena, cover, a checkpoint) that
    /// has NO goal marker. Its "goal" is this component: the Commander spawns from a RobotSpawnPoint of type
    /// Commander, and defeating it calls GameManager.BossDefeated(): every other robot falls, the Commander explodes in
    /// slow motion, and after a short beat GameManager.CompleteLevel() scores stars, saves progress, unlocks the next
    /// world (LevelData.isBossLevel) and calls EndLevel(completed: true); Level Complete then opens with the World
    /// Cleared celebration (WorldClearedScreen).
    ///
    /// The fight:
    ///  * The Commander patrols the arena like a big ground robot (GroundPatrolRobot: turns at walls/ledges),
    ///    drawn hovering slightly above the ground (Robot_right / Robot_left art, swapped as it turns). Defeated, it
    ///    drops to the ground as Commander_Defeated.png and the wreck stays there (HoldDefeatPose) until Level Complete.
    ///  * It needs <see cref="hitsToDefeat"/> hits (stomps, or Percy/Bessie ability contact, or an egg, horseshoe, water spout or feather gust).
    ///  * Each hit that lands staggers it for <see cref="staggerSeconds"/>: it stops, flashes, cannot be hit again
    ///    and cannot hurt the player, so it can't be chain-stomped and the player can safely reposition.
    ///  * After every hit it speeds up (<see cref="speedUpPerHit"/>) and reinforcement waves spawn: every
    ///    RobotSpawnPoint with wave = N spawns when the Nth hit lands (wave 0 is there from the start).
    ///    Waves use the world's ordinary robots (Harvester/Scout/Drone), so the boss is never a bare 1-on-1.
    ///  * Hit counter only, no health bar: <see cref="Active"/> exposes the counts to the HUD.
    ///  * It fights back with this world's <see cref="Attacks"/> (BossAttack flags, set per spawn from the level's
    ///    RobotSpawnPoint, since 2026-10-25): every <see cref="AttackInterval"/> seconds while the player is in the
    ///    arena it stops, warns (shakes and glows) and then charges, hop-slams (a shockwave each way along the floor)
    ///    or lobs an orb at the player. It opens with the first attack it has and adds the next one with each hit,
    ///    and attacks more often after each hit. A hit interrupts whatever it was doing. Every attack is dodged with
    ///    running and jumping alone, so any character can win. Meadow Ruins' boss has none and only patrols.
    ///
    /// To reskin for another world: duplicate the Commander prefab and RobotData (new sprite, hits, stagger, speed),
    /// author a new arena prefab with that world's hazards, mark it isBossLevel, and set the world's bossLevelId.
    /// </summary>
    public class CommanderBoss : GroundPatrolRobot
    {
        [SerializeField] private int hitsToDefeat = 3;
        [SerializeField] private float staggerSeconds = 1.5f;
        [SerializeField] private float speedUpPerHit = 0.35f;

        /// <summary>The boss currently in the level (for the HUD hit counter). Null when there is none.</summary>
        public static CommanderBoss Active { get; private set; }

        public int Hits { get; private set; }
        public int HitsToDefeat => hitsToDefeat;
        public bool IsStaggered => _staggerLeft > 0f;

        [Header("Hover (visual only: the body and its contact zones stay on the ground line)")]
        [SerializeField] private float hoverHeight = 0.35f;
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobsPerSecond = 0.6f;

        private float _staggerLeft;
        private Vector3 _visualRest;

        /// <summary>This world's attacks (None = only patrols). Set by LevelLoader before Initialize.</summary>
        public BossAttack Attacks { get; set; }

        /// <summary>Seconds between attacks at the start of the fight (shortened by each hit). Set by LevelLoader.</summary>
        public float AttackInterval { get; set; } = 4.5f;

        /// <summary>This world's orb and shockwave art (null = a glow in hazardColor). Set by LevelLoader.</summary>
        public Sprite ShotArt { get; set; }
        public Sprite ShockwaveArt { get; set; }

        [Header("Attacks (see the class comment)")]
        [Tooltip("Only attacks while the player is within this many units sideways.")]
        [SerializeField] private float attackRange = 16f;
        [SerializeField] private float chargeWindup = 0.75f;
        [SerializeField] private float chargeSpeed = 10.5f;
        [SerializeField] private float chargeDistance = 16f;
        [SerializeField] private float slamWindup = 0.5f;
        [SerializeField] private float slamHopSeconds = 0.6f;
        [SerializeField] private float slamHopHeight = 1.8f;
        [SerializeField] private float shockwaveSpeed = 8.5f;
        [SerializeField] private float shockwaveRange = 11f;
        [SerializeField] private float shotWindup = 0.55f;
        [SerializeField] private float shotFlightSeconds = 1.15f;
        [SerializeField] private float shotGravity = 16f;
        [SerializeField] private Color hazardColor = new Color(1f, 0.45f, 0.2f);

        private enum Phase { Patrol, Windup, Charging, Hopping, Recover }

        private const float IntervalCutPerHit = 0.15f, MinInterval = 1.8f;

        private Phase _phase;
        private BossAttack _attack, _lastAttack;
        private float _phaseTime, _cooldown, _travelled, _groundY;
        private Vector3 _attackOffset;

        /// <summary>False for a boss that walks on the ground (set per spawn by LevelLoader from the marker).</summary>
        public bool Hovers { get; set; } = true;

        private Vector2[] _zoneSize, _zoneOffset;

        protected override void Awake()
        {
            base.Awake();
            if (visual != null)
            {
                _visualRest = visual.transform.localPosition;
            }
        }

        // A world boss drawn bigger (Frozen Tundra's Glacier Harvester) gets stomp/hurt zones grown to match, from the
        // feet up, so a stomp lands on top of the art rather than somewhere inside it. Scale 1 restores the prefab's.
        protected override void OnWorldArtScale(float scale)
        {
            var zones = ContactColliders;
            if (zones == null)
            {
                return;
            }
            if (_zoneSize == null)
            {
                _zoneSize = new Vector2[zones.Length];
                _zoneOffset = new Vector2[zones.Length];
                for (int i = 0; i < zones.Length; i++)
                {
                    if (zones[i] is BoxCollider2D box)
                    {
                        _zoneSize[i] = box.size;
                        _zoneOffset[i] = box.offset;
                    }
                }
            }

            float feet = _visualRest.y;   // the art's feet, in the body's space
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] is BoxCollider2D box)
                {
                    box.size = new Vector2(_zoneSize[i].x * scale, _zoneSize[i].y * scale);
                    box.offset = new Vector2(_zoneOffset[i].x * scale, feet + (_zoneOffset[i].y - feet) * scale);
                }
            }
        }

        // Its wreck (Commander_Defeated.png) stays on the field among the victory explosions.
        protected override bool HoldDefeatPose => true;

        // Floats just above the ground, bobbing gently, as it patrols left and right; the wreck lies on the ground.
        private void Update()
        {
            if (visual != null && (IsDefeated || !Hovers))
            {
                visual.transform.localPosition = _visualRest + (IsDefeated ? Vector3.zero : _attackOffset);
                if (IsDefeated)
                {
                    visual.color = Color.white;
                }
            }
            else if (visual != null)
            {
                float bob = Mathf.Sin(Time.time * bobsPerSecond * 2f * Mathf.PI) * bobAmplitude;
                visual.transform.localPosition = _visualRest + Vector3.up * (hoverHeight + bob) + _attackOffset;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Active = this;
        }

        private void OnDisable()
        {
            if (Active == this)
            {
                Active = null;
            }
            BossHazard.ClearAll();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Hits = 0;
            _staggerLeft = 0f;
            Active = this;
            _groundY = Position.y;
            EndAttack();
            _cooldown = AttackInterval * 0.5f;   // the first attack comes soon after the player walks in
        }

        protected override void OnResetState()
        {
            Hits = 0;
            _staggerLeft = 0f;
            SpeedScale = 1f;
            _phase = Phase.Patrol;
            _attackOffset = Vector3.zero;
            _lastAttack = BossAttack.None;
        }

        protected override bool CanHurtPlayer => !IsStaggered;

        protected override bool TakeHit()
        {
            if (IsStaggered)
            {
                return false;
            }

            Hits++;
            Debug.Log($"[CommanderBoss] Hit {Hits}/{hitsToDefeat}.");
            EndAttack();   // a hit interrupts the attack (and lands it back on the ground mid-hop)
            Body.position = Position;
            _cooldown = CurrentInterval;

            if (Hits >= hitsToDefeat)
            {
                Defeat();
                GameManager.Instance.BossDefeated(transform.position, visual);   // the victory moment, then Level Complete
                return true;
            }

            AudioManager.Play(a => a.bossHit);
            _staggerLeft = staggerSeconds;
            SpeedScale = 1f + speedUpPerHit * Hits;
            if (LevelLoader.Instance != null)
            {
                LevelLoader.Instance.SpawnWave(Hits);
            }
            return true;
        }

        protected override void Tick(float dt)
        {
            if (_staggerLeft > 0f)
            {
                _staggerLeft -= dt;
                if (visual != null)
                {
                    visual.color = ((int)(Time.time * 16f) % 2 == 0) ? new Color(1f, 0.6f, 0.6f) : Color.white;
                }
                if (_staggerLeft <= 0f && visual != null)
                {
                    visual.color = Color.white;
                }
                return;
            }

            if (Attacks == BossAttack.None)
            {
                base.Tick(dt);
                return;
            }

            switch (_phase)
            {
                case Phase.Patrol:
                    base.Tick(dt);
                    if (PlayerInArena(out float dx))
                    {
                        _cooldown -= dt;
                        if (_cooldown <= 0f) { BeginAttack(dx); }
                    }
                    break;

                case Phase.Windup:
                    _phaseTime -= dt;
                    _attackOffset = new Vector3(Mathf.Sin(Time.time * 70f) * 0.09f, 0f, 0f);
                    if (visual != null)
                    {
                        visual.color = ((int)(Time.time * 12f) % 2 == 0) ? new Color(1f, 0.75f, 0.45f) : Color.white;
                    }
                    if (_phaseTime <= 0f) { Strike(); }
                    break;

                case Phase.Charging:
                    StepCharge(dt);
                    break;

                case Phase.Hopping:
                    _phaseTime += dt;
                    float t = Mathf.Clamp01(_phaseTime / slamHopSeconds);
                    Position.y = _groundY + 4f * slamHopHeight * t * (1f - t);
                    if (t >= 1f) { Land(); }
                    break;

                case Phase.Recover:
                    _phaseTime -= dt;
                    if (_phaseTime <= 0f)
                    {
                        _phase = Phase.Patrol;
                        _cooldown = CurrentInterval;
                    }
                    break;
            }
        }

        // ------------------------------------------------------------ attacks

        private float CurrentInterval => Mathf.Max(MinInterval, AttackInterval * (1f - IntervalCutPerHit * Hits));

        // The player is near enough sideways, in the arena and not far above or below.
        private bool PlayerInArena(out float dx)
        {
            dx = 0f;
            var loader = LevelLoader.Instance;
            if (loader == null || loader.Player == null || loader.IsRespawning) { return false; }
            Vector2 p = loader.Player.Position;
            dx = p.x - Position.x;
            return Mathf.Abs(dx) < attackRange && Mathf.Abs(p.x - OriginX) < PatrolDistance + 4f && Mathf.Abs(p.y - _groundY) < 7f;
        }

        // Picks one of the attacks it has unlocked (the first, plus one more per hit), not the same twice running.
        private void BeginAttack(float dx)
        {
            var unlocked = new List<BossAttack>();
            foreach (var a in new[] { BossAttack.Charge, BossAttack.Slam, BossAttack.Shot })
            {
                if ((Attacks & a) != 0 && unlocked.Count < 1 + Hits) { unlocked.Add(a); }
            }
            if (unlocked.Count == 0) { return; }
            if (unlocked.Count > 1) { unlocked.Remove(_lastAttack); }
            _attack = unlocked[Random.Range(0, unlocked.Count)];
            _lastAttack = _attack;

            Face(dx >= 0f ? 1 : -1);
            _phase = Phase.Windup;
            _phaseTime = _attack == BossAttack.Charge ? chargeWindup : _attack == BossAttack.Slam ? slamWindup : shotWindup;
        }

        private void Strike()
        {
            _attackOffset = Vector3.zero;
            if (visual != null) { visual.color = Color.white; }
            switch (_attack)
            {
                case BossAttack.Charge:
                    _phase = Phase.Charging;
                    _travelled = 0f;
                    break;
                case BossAttack.Slam:
                    _phase = Phase.Hopping;
                    _phaseTime = 0f;
                    break;
                default:
                    FireOrbs();
                    Recover(0.4f);
                    break;
            }
        }

        // Dashes the way it faces until a wall, a ledge edge, the arena's end or its distance.
        private void StepCharge(float dt)
        {
            float step = chargeSpeed * (1f + 0.1f * Hits) * dt;
            float nextX = Position.x + Direction * step;
            bool stop = WallAhead(Direction) || !GroundAhead(Direction)
                || nextX > OriginX + PatrolDistance || nextX < OriginX - PatrolDistance;
            if (!stop)
            {
                Position.x = nextX;
                _travelled += step;
            }
            if (stop || _travelled >= chargeDistance) { Recover(0.6f); }
        }

        // Down again: a shockwave each way along the floor.
        private void Land()
        {
            Position.y = _groundY;
            Vector2 feet = new Vector2(Position.x, _groundY + _visualRest.y);
            float speed = shockwaveSpeed * (1f + 0.08f * Hits);
            BossHazard.Shockwave(feet, 1, speed, shockwaveRange, hazardColor, ShockwaveArt);
            BossHazard.Shockwave(feet, -1, speed, shockwaveRange, hazardColor, ShockwaveArt);
            AudioManager.Play(a => a.bossHit);
            Recover(0.5f);
        }

        // An orb lobbed to land where the player stands; after the second hit a second, slower one follows it there.
        private void FireOrbs()
        {
            var loader = LevelLoader.Instance;
            if (loader == null || loader.Player == null) { return; }
            Vector2 from = visual != null ? (Vector2)visual.bounds.center + Vector2.up * visual.bounds.extents.y * 0.4f : Position + Vector2.up;
            Vector2 target = loader.Player.Position;
            FireOrb(from, target, shotFlightSeconds);
            if (Hits >= 2) { FireOrb(from, target, shotFlightSeconds * 1.4f); }
        }

        private void FireOrb(Vector2 from, Vector2 target, float flight)
        {
            Vector2 d = target - from;
            var velocity = new Vector2(d.x / flight, (d.y + 0.5f * shotGravity * flight * flight) / flight);
            BossHazard.Orb(from, velocity, shotGravity, hazardColor, ShotArt);
        }

        private void Recover(float seconds)
        {
            _phase = Phase.Recover;
            _phaseTime = seconds;
        }

        // Back to patrolling on the ground, no warning shake.
        private void EndAttack()
        {
            _phase = Phase.Patrol;
            _attackOffset = Vector3.zero;
            Position.y = _groundY;
        }
    }
}
