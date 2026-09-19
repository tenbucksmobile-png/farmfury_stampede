using FarmFuryStampede.Characters;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;

namespace FarmFuryStampede.Movement
{
    /// <summary>
    /// Shared kinematic platformer controller for all eight characters. All motion is integrated by hand in
    /// FixedUpdate, resolved against the Ground layer with BoxCasts, and applied via Rigidbody2D.MovePosition
    /// (never transform writes). Every character gets identical base movement (CharacterData.moveSpeed /
    /// jumpHeight); only the plugged-in <see cref="CharacterAbility"/> differs. The ability button is gated by
    /// a per-level use count, not a cooldown.
    ///
    /// Abilities bend motion through the public modifier API (Velocity, GravityScale, InputLocked,
    /// MaxFallSpeedOverride, ContactAttacking, WaterImmune, Launch). The per-step modifiers are reset at the
    /// start of every fixed step and re-applied by the active ability.
    /// The root transform must keep scale 1; sprite flipping is done on the visual's SpriteRenderer.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlayerInputReader))]
    public class CharacterController2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer visual;
        [Tooltip("Character used until the first StartLevel picks one.")]
        [SerializeField] private CharacterType defaultCharacter = CharacterType.Cluck;
        [SerializeField] private AbilityPrefabs abilityPrefabs = new AbilityPrefabs();

        [Header("Horizontal (overwritten from CharacterData; identical for all characters)")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float groundAcceleration = 60f;
        [SerializeField] private float groundDeceleration = 70f;
        [Range(0f, 1f)]
        [SerializeField] private float airControl = 0.7f;

        [Header("Jump (jumpHeight overwritten from CharacterData)")]
        [Tooltip("Full-hold jump apex height in units.")]
        [SerializeField] private float jumpHeight = 3.5f;
        [Tooltip("Seconds to reach the apex. Lower = snappier, higher gravity.")]
        [SerializeField] private float timeToApex = 0.4f;
        [Tooltip("Apex height of a tap (button released immediately).")]
        [SerializeField] private float minJumpHeight = 1f;
        [SerializeField] private float fallGravityMultiplier = 1.7f;
        [SerializeField] private float maxFallSpeed = 25f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Water (applies to everyone except water-immune characters)")]
        [SerializeField] private float waterSpeedMultiplier = 0.4f;
        [SerializeField] private float waterDrownSeconds = 1.2f;

        [Header("Collision")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float skinWidth = 0.03f;
        [SerializeField] private float groundCheckDistance = 0.06f;

        private const float MoveEpsilon = 0.0001f;

        private Rigidbody2D _body;
        private BoxCollider2D _collider;
        private PlayerInputReader _input;

        private Vector2 _position;
        private Vector2 _velocity;
        private bool _grounded;
        private bool _isJumping;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private int _facing = 1;

        private float _gravity;
        private float _jumpVelocity;
        private float _minJumpVelocity;

        private float _invulnerableUntil;

        private CharacterAbility _ability;
        private bool _abilityQueued;
        private bool _characterAssigned;

        private bool _inWaterFlag;
        private float _waterTime;

        // ---- public state

        public CharacterType Character { get; private set; }
        public CharacterData Data { get; private set; }
        public int UsesRemaining { get; private set; }
        public int UsesPerLevel { get; private set; }

        public bool IsGrounded => _grounded;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;
        public int Facing => _facing;
        public float CoyoteTimeRemaining => _coyoteTimer;
        public float JumpBufferRemaining => _jumpBufferTimer;
        public bool CoyoteAvailable => _coyoteTimer > 0f;

        public float MoveSpeed => moveSpeed;
        public float JumpHeight => jumpHeight;
        public float JumpVelocity => _jumpVelocity;
        public float GravityAcceleration => _gravity;

        public Vector2 Position => _position;
        public Vector2 FeetPosition => _position + _collider.offset + Vector2.down * (_collider.size.y * 0.5f);
        public Bounds ColliderBounds => _collider.bounds;
        public LayerMask GroundMask => groundMask;
        public AbilityPrefabs Prefabs => abilityPrefabs;

        public CharacterAbility Ability => _ability;
        public bool AbilityActive => _ability != null && _ability.IsActive;

        // ---- modifier API for abilities (reset each fixed step, re-applied by the ability's Tick)

        public Vector2 Velocity
        {
            get => _velocity;
            set => _velocity = value;
        }

        /// <summary>Multiplier on gravity this step (0 = weightless).</summary>
        public float GravityScale { get; set; } = 1f;

        /// <summary>When true this step, player steering is ignored so the ability fully controls horizontal velocity.</summary>
        public bool InputLocked { get; set; }

        /// <summary>Overrides the terminal fall speed this step; negative means no override.</summary>
        public float MaxFallSpeedOverride { get; set; } = -1f;

        /// <summary>While true, robot contact defeats the robot instead of hurting the player.</summary>
        public bool ContactAttacking { get; set; }

        /// <summary>While true, Water tiles neither slow nor drown this character.</summary>
        public bool WaterImmune { get; set; }

        private bool IsPlaying =>
            GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing;

        // ------------------------------------------------------------ lifecycle

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();
            _input = GetComponent<PlayerInputReader>();

            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.useFullKinematicContacts = false;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            if (groundMask.value == 0)
            {
                groundMask = LayerMask.GetMask("Ground");
            }

            if (transform.lossyScale != Vector3.one)
            {
                Debug.LogWarning($"[CharacterController2D] '{name}' must have scale 1; collision sizes assume it.");
            }

            _position = _body.position;
            RecalculateJump();
        }

        private void Start()
        {
            if (!_characterAssigned)
            {
                SetCharacter(defaultCharacter);
            }
        }

        private void OnValidate()
        {
            timeToApex = Mathf.Max(0.05f, timeToApex);
            jumpHeight = Mathf.Max(0.1f, jumpHeight);
            minJumpHeight = Mathf.Clamp(minJumpHeight, 0.05f, jumpHeight);
            RecalculateJump();
        }

        private void RecalculateJump()
        {
            _gravity = 2f * jumpHeight / (timeToApex * timeToApex);
            _jumpVelocity = _gravity * timeToApex;
            _minJumpVelocity = Mathf.Sqrt(2f * _gravity * Mathf.Min(minJumpHeight, jumpHeight));
        }

        /// <summary>
        /// Turns this player into the given character: base stats and placeholder art from its CharacterData,
        /// and its ability (via <see cref="AbilityFactory"/>) with a fresh per-level use count.
        /// </summary>
        public void SetCharacter(CharacterType type)
        {
            _characterAssigned = true;
            Character = type;
            Data = DataManager.Instance != null ? DataManager.Instance.GetCharacterData(type) : null;

            if (Data != null)
            {
                moveSpeed = Data.moveSpeed;
                jumpHeight = Data.jumpHeight;
                UsesPerLevel = Data.abilityUsesPerLevel;
                _ability = AbilityFactory.Create(Data.abilityType);
                if (visual != null && Data.placeholderSprite != null)
                {
                    visual.sprite = Data.placeholderSprite;
                }
            }
            else
            {
                Debug.LogWarning($"[CharacterController2D] No CharacterData for {type}; the character has no ability.");
                _ability = null;
                UsesPerLevel = 0;
            }

            RecalculateJump();
            UsesRemaining = UsesPerLevel;
            _abilityQueued = false;
            _waterTime = 0f;
            _ability?.Reset(this);
        }

        /// <summary>Instantly moves the player (level start, respawn), clearing all motion state.</summary>
        public void Teleport(Vector2 position)
        {
            _position = position;
            _velocity = Vector2.zero;
            _grounded = false;
            _isJumping = false;
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
            _abilityQueued = false;
            _waterTime = 0f;
            _ability?.Reset(this);
            transform.position = position;
            _body.position = position;
            Physics2D.SyncTransforms();
        }

        /// <summary>Launches the player upward with no variable-height cut (stomp bounce, flutter, vault).</summary>
        public void Launch(float upwardVelocity)
        {
            _velocity.y = upwardVelocity;
            _isJumping = false;
            _grounded = false;
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
        }

        /// <summary>Launches so the jump peaks at the given height above the take-off point.</summary>
        public void LaunchToHeight(float height)
        {
            Launch(Mathf.Sqrt(2f * _gravity * height));
        }

        /// <summary>Kept for robot stomps; same as <see cref="Launch"/>.</summary>
        public void Bounce(float upwardVelocity)
        {
            Launch(upwardVelocity);
        }

        /// <summary>Makes the player immune to robot contact for a short time (e.g. right after respawning).</summary>
        public void GrantInvulnerability(float seconds)
        {
            _invulnerableUntil = Time.time + seconds;
        }

        /// <summary>Called every physics step by WaterZone while the player overlaps it.</summary>
        public void NotifyInWater()
        {
            _inWaterFlag = true;
        }

        // ------------------------------------------------------------ per-frame

        private void Update()
        {
            if (visual != null)
            {
                // Blink while invulnerable.
                visual.enabled = !IsInvulnerable || ((int)(Time.time * 12f) % 2 == 0);
                visual.color = _ability != null ? _ability.Tint : Color.white;
                visual.transform.localScale = _ability != null ? (Vector3)_ability.VisualScale : Vector3.one;
            }

            // Presses are captured per rendered frame; buffers are consumed and aged in FixedUpdate.
            if (IsPlaying)
            {
                if (_input.JumpPressedThisFrame)
                {
                    _jumpBufferTimer = jumpBufferTime;
                }
                if (_input.AbilityPressedThisFrame)
                {
                    _abilityQueued = true;
                }
            }
        }

        private void FixedUpdate()
        {
            if (!IsPlaying)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            float moveInput = _input.Move;
            bool groundedAtStart = _grounded;

            // Per-step modifiers start neutral; the active ability re-applies its own in Tick.
            GravityScale = 1f;
            InputLocked = false;
            MaxFallSpeedOverride = -1f;
            ContactAttacking = false;
            WaterImmune = false;

            // Ability activation (button press) then the ability's ongoing effect.
            if (_abilityQueued)
            {
                _abilityQueued = false;
                TryActivateAbility();
            }
            _ability?.Tick(this, dt);

            // Water: slow down and, if lingering, drown (respawn) unless immune.
            bool inWater = _inWaterFlag && !WaterImmune;
            _inWaterFlag = false;
            _waterTime = inWater ? _waterTime + dt : Mathf.Max(0f, _waterTime - dt * 2f);
            if (_waterTime >= waterDrownSeconds)
            {
                _waterTime = 0f;
                Debug.Log($"[CharacterController2D] {Character} drowned in water.");
                GameManager.Instance?.RespawnPlayer();
                return;
            }

            if (!InputLocked)
            {
                if (Mathf.Abs(moveInput) > 0.01f)
                {
                    _facing = moveInput > 0f ? 1 : -1;
                    if (visual != null)
                    {
                        visual.flipX = _facing < 0;
                    }
                }

                // Horizontal: accelerate toward target speed; decelerate to rest or when reversing.
                float targetSpeed = moveInput * moveSpeed * (inWater ? waterSpeedMultiplier : 1f);
                bool reversingOrStopping = Mathf.Approximately(targetSpeed, 0f)
                    || (_velocity.x != 0f && Mathf.Sign(targetSpeed) != Mathf.Sign(_velocity.x));
                float rate = reversingOrStopping ? groundDeceleration : groundAcceleration;
                if (!_grounded)
                {
                    rate *= airControl;
                }
                _velocity.x = Mathf.MoveTowards(_velocity.x, targetSpeed, rate * dt);
            }

            // Jump timers and start.
            _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - dt);
            _coyoteTimer = _grounded ? coyoteTime : Mathf.Max(0f, _coyoteTimer - dt);

            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
            {
                _velocity.y = _jumpVelocity;
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                _isJumping = true;
                _grounded = false;
            }

            // Variable jump height: releasing early caps the rise at the tap velocity.
            if (_isJumping)
            {
                if (!_input.JumpHeld && _velocity.y > _minJumpVelocity)
                {
                    _velocity.y = _minJumpVelocity;
                }
                if (_velocity.y <= 0f)
                {
                    _isJumping = false;
                }
            }

            // Manual gravity. Averaged velocity keeps the apex height exact regardless of dt.
            float g = (_velocity.y > 0f ? _gravity : _gravity * fallGravityMultiplier) * GravityScale;
            float fallCap = MaxFallSpeedOverride >= 0f ? MaxFallSpeedOverride : maxFallSpeed;
            float previousVy = _velocity.y;
            _velocity.y = Mathf.Max(previousVy - g * dt, -fallCap);
            float dy = (previousVy + _velocity.y) * 0.5f * dt;

            MoveAndCollide(new Vector2(_velocity.x * dt, dy), out bool hitX, out bool hitY);

            if (hitX)
            {
                _velocity.x = 0f;
            }

            bool landed = hitY && dy < 0f;
            if (hitY)
            {
                _velocity.y = 0f;
            }

            _grounded = landed || (_velocity.y <= 0f && GroundCheck());
            if (_grounded)
            {
                _isJumping = false;
            }

            _body.MovePosition(_position);

            if (!groundedAtStart && _grounded)
            {
                _ability?.OnLanded(this);
            }
        }

        private void TryActivateAbility()
        {
            if (_ability == null || UsesRemaining <= 0 || !_ability.CanActivate(this))
            {
                return;
            }

            UsesRemaining--;
            _ability.Activate(this);
            Debug.Log($"[CharacterController2D] {Character} used {_ability.Type} ({UsesRemaining}/{UsesPerLevel} left).");
        }

        // ------------------------------------------------------------ collision

        private void MoveAndCollide(Vector2 delta, out bool hitX, out bool hitY)
        {
            hitX = false;
            hitY = false;

            if (Mathf.Abs(delta.x) > MoveEpsilon)
            {
                float dir = Mathf.Sign(delta.x);
                float dist = Mathf.Abs(delta.x);
                var hit = Cast(_position, new Vector2(dir, 0f), dist + skinWidth);
                if (hit.collider != null)
                {
                    dist = Mathf.Max(0f, hit.distance - skinWidth);
                    hitX = true;
                }
                _position.x += dir * dist;
            }

            if (Mathf.Abs(delta.y) > MoveEpsilon)
            {
                float dir = Mathf.Sign(delta.y);
                float dist = Mathf.Abs(delta.y);
                var hit = Cast(_position, new Vector2(0f, dir), dist + skinWidth);
                if (hit.collider != null)
                {
                    dist = Mathf.Max(0f, hit.distance - skinWidth);
                    hitY = true;
                }
                _position.y += dir * dist;
            }
        }

        private bool GroundCheck()
        {
            return Cast(_position, Vector2.down, groundCheckDistance + skinWidth).collider != null;
        }

        // The cast box is the collider inset by skinWidth so a body resting on a surface never
        // starts the cast inside it.
        private RaycastHit2D Cast(Vector2 position, Vector2 direction, float distance)
        {
            Vector2 size = _collider.size - Vector2.one * (2f * skinWidth);
            return Physics2D.BoxCast(position + _collider.offset, size, 0f, direction, distance, groundMask);
        }
    }
}
