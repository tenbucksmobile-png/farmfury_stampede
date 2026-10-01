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

        [Header("Art (optional; without it the visual is flipped)")]
        [SerializeField] private Sprite spriteRight;
        [SerializeField] private Sprite spriteLeft;
        [SerializeField] private Sprite defeatSprite;

        private Vector3 _visualScale = Vector3.one;
        private Vector3 _prefabVisualScale = Vector3.one;
        private Sprite _prefabRight, _prefabLeft, _prefabDefeat;

        /// <summary>
        /// Gives this (pooled) robot a world's own art for this spawn, or its prefab art back when all are null
        /// (LevelLoader calls it on every spawn, so a reused robot never keeps another world's look).
        /// </summary>
        /// <param name="scale">The art drawn this many times its prefab size, grown up from its feet (colliders unchanged).</param>
        public void SetWorldArt(Sprite right, Sprite left, Sprite defeat, float scale = 1f)
        {
            _visualScale = _prefabVisualScale * (scale > 0f ? scale : 1f);
            bool custom = right != null;
            spriteRight = custom ? right : _prefabRight;
            spriteLeft = custom ? left : _prefabLeft;
            defeatSprite = custom ? (defeat != null ? defeat : _prefabDefeat) : _prefabDefeat;
            if (visual != null)
            {
                visual.flipX = false;
                visual.transform.localScale = _visualScale;
                if (spriteRight != null) { visual.sprite = spriteRight; }
            }
        }

        /// <summary>
        /// Points the visual left or right. Robots with separate left/right art swap sprites (the art is not a
        /// mirror image); the rest flip.
        /// </summary>
        protected void SetFacing(bool faceRight)
        {
            if (visual == null)
            {
                return;
            }

            if (spriteRight != null && spriteLeft != null)
            {
                visual.flipX = false;
                visual.sprite = faceRight ? spriteRight : spriteLeft;
            }
            else
            {
                visual.flipX = !faceRight;
            }
        }

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
                _prefabVisualScale = _visualScale;
            }
            _prefabRight = spriteRight;
            _prefabLeft = spriteLeft;
            _prefabDefeat = defeatSprite;
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
                if (spriteRight != null)
                {
                    visual.sprite = spriteRight;
                }
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

        /// <summary>Defeats it outright, whatever it is (the boss's fall takes every robot with it). False if already down.</summary>
        public bool ForceDefeat()
        {
            if (IsDefeated || !gameObject.activeInHierarchy)
            {
                return false;
            }

            Defeat();
            return true;
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

            if (visual != null && defeatSprite != null)
            {
                visual.flipX = false;
                visual.sprite = defeatSprite;
            }

            if (!HoldDefeatPose)
            {
                StartCoroutine(DefeatEffect());
            }
        }

        /// <summary>
        /// True: the defeat sprite stays where it fell until the level unloads (the boss's wreck) instead of the
        /// usual squash-and-fade back into the pool.
        /// </summary>
        protected virtual bool HoldDefeatPose => false;

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
