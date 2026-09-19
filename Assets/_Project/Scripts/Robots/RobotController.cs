using System.Collections;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Robots
{
    /// <summary>
    /// Base for pooled robots. Moves a kinematic Rigidbody2D via MovePosition and resolves contact with the
    /// player: stomped from above (player falling, feet above the robot's centre) or hit by an ability it takes a
    /// hit (see <see cref="TakeHit"/>); any other contact through the hurt zone costs the player a life.
    /// Subclasses implement <see cref="Tick"/> to move <see cref="Position"/> and may override
    /// <see cref="TakeHit"/> (bosses need several) and <see cref="CanHurtPlayer"/> (staggered bosses are harmless).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(PooledObject))]
    public abstract class RobotController : MonoBehaviour
    {
        [SerializeField] private RobotType robotType;
        [SerializeField] protected SpriteRenderer visual;
        [Tooltip("The StompDetector and HurtDetector trigger colliders; disabled while the defeat effect plays.")]
        [SerializeField] private Collider2D[] contactColliders;
        [SerializeField] private float fallbackSpeed = 2f;
        [SerializeField] private float stompBounceVelocity = 14f;
        [SerializeField] private float defeatEffectSeconds = 0.25f;

        protected Rigidbody2D Body { get; private set; }
        protected Vector2 Position;
        protected float Speed { get; private set; }
        protected float PatrolDistance { get; private set; }
        protected bool IsDefeated { get; private set; }
        public RobotType Type => robotType;

        private Vector3 _visualScale = Vector3.one;

        protected static bool IsPlaying =>
            GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Body.bodyType = RigidbodyType2D.Kinematic;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            if (visual != null)
            {
                _visualScale = visual.transform.localScale;
            }
        }

        protected virtual void OnEnable()
        {
            ResetState();
        }

        /// <summary>Called by LevelLoader right after the robot is taken from the pool and positioned.</summary>
        public void Initialize(float patrolDistance)
        {
            ResetState();

            PatrolDistance = Mathf.Max(0.1f, patrolDistance);

            var data = DataManager.Instance != null ? DataManager.Instance.GetRobotData(robotType) : null;
            Speed = data != null ? data.moveSpeed : fallbackSpeed;

            Position = transform.position;
            Body.position = Position;
            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }

        private void ResetState()
        {
            IsDefeated = false;
            StopAllCoroutines();

            if (contactColliders != null)
            {
                foreach (var c in contactColliders)
                {
                    if (c != null)
                    {
                        c.enabled = true;
                    }
                }
            }

            if (visual != null)
            {
                visual.transform.localScale = _visualScale;
                visual.color = Color.white;
            }

            OnResetState();
        }

        protected virtual void OnResetState()
        {
        }

        private void FixedUpdate()
        {
            if (IsDefeated || !IsPlaying)
            {
                return;
            }

            Tick(Time.fixedDeltaTime);
            Body.MovePosition(Position);
        }

        protected abstract void Tick(float dt);

        /// <summary>Whether touching this robot's body costs the player a life right now.</summary>
        protected virtual bool CanHurtPlayer => true;

        /// <summary>
        /// A stomp or ability hit lands. Return true if it registered. The default robot is simply defeated;
        /// bosses count hits and refuse hits while staggered.
        /// </summary>
        protected virtual bool TakeHit()
        {
            Defeat();
            return true;
        }

        /// <summary>Called by the robot's StompDetector / HurtDetector trigger zones.</summary>
        public void HandlePlayerContact(Collider2D playerCollider, bool fromStompZone)
        {
            if (IsDefeated || !IsPlaying)
            {
                return;
            }

            var player = playerCollider.GetComponentInParent<CharacterController2D>();
            if (player == null)
            {
                return;
            }

            // Percy's dash, Gerald's inflated glide and Bessie's pound hit whatever they touch.
            if (player.ContactAttacking)
            {
                TakeHit();
                return;
            }

            bool stomped = player.Velocity.y <= 0f && playerCollider.bounds.min.y >= transform.position.y;
            if (stomped)
            {
                TakeHit();
                player.Bounce(stompBounceVelocity);
            }
            else if (!fromStompZone && !player.IsInvulnerable && CanHurtPlayer)
            {
                GameManager.Instance.RespawnPlayer();
            }
        }

        /// <summary>A hit on behalf of an ability (pound shockwave, horseshoe). No bounce. False if it did not register.</summary>
        public bool DefeatByAbility()
        {
            if (IsDefeated || !gameObject.activeInHierarchy)
            {
                return false;
            }

            return TakeHit();
        }

        protected void Defeat()
        {
            IsDefeated = true;
            GameManager.Instance.RunState.DefeatRobot();

            foreach (var c in contactColliders)
            {
                if (c != null)
                {
                    c.enabled = false;
                }
            }

            StartCoroutine(DefeatEffect());
        }

        // Placeholder defeat effect: squash and fade, then return to the pool.
        private IEnumerator DefeatEffect()
        {
            float elapsed = 0f;
            while (elapsed < defeatEffectSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / defeatEffectSeconds);
                if (visual != null)
                {
                    visual.transform.localScale = new Vector3(_visualScale.x * (1f + t * 0.4f), _visualScale.y * (1f - t), _visualScale.z);
                    visual.color = new Color(1f, 1f, 1f, 1f - t);
                }
                yield return null;
            }

            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.Release(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
