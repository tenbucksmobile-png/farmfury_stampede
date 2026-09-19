using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;

namespace FarmFuryStampede.Movement
{
    /// <summary>
    /// Custom kinematic platformer controller. All motion is integrated by hand in FixedUpdate,
    /// resolved against the Ground layer with BoxCasts, and applied via Rigidbody2D.MovePosition
    /// (never transform writes). Gravity is applied manually so the rising arc (jumpHeight /
    /// timeToApex) is tunable independently of fall speed (fallGravityMultiplier).
    /// The root transform must keep scale 1; sprite flipping is done on the visual's SpriteRenderer.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlayerInputReader))]
    public class CharacterController2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private CharacterType characterType = CharacterType.Cluck;
        [Tooltip("Pull moveSpeed and jumpHeight from the character's CharacterData at Start.")]
        [SerializeField] private bool applyCharacterData = true;

        [Header("Horizontal")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float groundAcceleration = 60f;
        [SerializeField] private float groundDeceleration = 70f;
        [Range(0f, 1f)]
        [SerializeField] private float airControl = 0.7f;

        [Header("Jump")]
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

        public bool IsGrounded => _grounded;
        public Vector2 Velocity => _velocity;
        public int Facing => _facing;
        public float CoyoteTimeRemaining => _coyoteTimer;
        public float JumpBufferRemaining => _jumpBufferTimer;

        private bool IsPlaying =>
            GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing;

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
            if (!applyCharacterData || DataManager.Instance == null)
            {
                return;
            }

            var data = DataManager.Instance.GetCharacterData(characterType);
            if (data != null)
            {
                moveSpeed = data.moveSpeed;
                jumpHeight = data.jumpHeight;
                RecalculateJump();
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

        private void Update()
        {
            // Presses are captured per rendered frame; the buffer is consumed and aged in FixedUpdate.
            if (IsPlaying && _input.JumpPressedThisFrame)
            {
                _jumpBufferTimer = jumpBufferTime;
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

            if (Mathf.Abs(moveInput) > 0.01f)
            {
                _facing = moveInput > 0f ? 1 : -1;
                if (visual != null)
                {
                    visual.flipX = _facing < 0;
                }
            }

            // Horizontal: accelerate toward target speed; decelerate to rest or when reversing.
            float targetSpeed = moveInput * moveSpeed;
            bool reversingOrStopping = Mathf.Approximately(targetSpeed, 0f)
                || (_velocity.x != 0f && Mathf.Sign(targetSpeed) != Mathf.Sign(_velocity.x));
            float rate = reversingOrStopping ? groundDeceleration : groundAcceleration;
            if (!_grounded)
            {
                rate *= airControl;
            }
            _velocity.x = Mathf.MoveTowards(_velocity.x, targetSpeed, rate * dt);

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
            float g = _velocity.y > 0f ? _gravity : _gravity * fallGravityMultiplier;
            float previousVy = _velocity.y;
            _velocity.y = Mathf.Max(previousVy - g * dt, -maxFallSpeed);
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
        }

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
