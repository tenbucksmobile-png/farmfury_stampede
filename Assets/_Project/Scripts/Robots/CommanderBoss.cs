using FarmFuryStampede.Core;
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
    ///  * It needs <see cref="hitsToDefeat"/> hits (stomps, or Percy/Gerald/Bessie ability contact, or a horseshoe).
    ///  * Each hit that lands staggers it for <see cref="staggerSeconds"/>: it stops, flashes, cannot be hit again
    ///    and cannot hurt the player, so it can't be chain-stomped and the player can safely reposition.
    ///  * After every hit it speeds up (<see cref="speedUpPerHit"/>) and reinforcement waves spawn: every
    ///    RobotSpawnPoint with wave = N spawns when the Nth hit lands (wave 0 is there from the start).
    ///    Waves use the world's ordinary robots (Harvester/Scout/Drone), so the boss is never a bare 1-on-1.
    ///  * Hit counter only, no health bar: <see cref="Active"/> exposes the counts to the HUD.
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

        protected override void Awake()
        {
            base.Awake();
            if (visual != null)
            {
                _visualRest = visual.transform.localPosition;
            }
        }

        // Its wreck (Commander_Defeated.png) stays on the field among the victory explosions.
        protected override bool HoldDefeatPose => true;

        // Floats just above the ground, bobbing gently, as it patrols left and right; the wreck lies on the ground.
        private void Update()
        {
            if (visual != null && IsDefeated)
            {
                visual.transform.localPosition = _visualRest;
                visual.color = Color.white;
            }
            else if (visual != null)
            {
                float bob = Mathf.Sin(Time.time * bobsPerSecond * 2f * Mathf.PI) * bobAmplitude;
                visual.transform.localPosition = _visualRest + Vector3.up * (hoverHeight + bob);
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
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Hits = 0;
            _staggerLeft = 0f;
            Active = this;
        }

        protected override void OnResetState()
        {
            Hits = 0;
            _staggerLeft = 0f;
            SpeedScale = 1f;
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

            if (Hits >= hitsToDefeat)
            {
                Defeat();
                GameManager.Instance.BossDefeated(transform.position);   // the victory moment, then Level Complete
                return true;
            }

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

            base.Tick(dt);
        }
    }
}
