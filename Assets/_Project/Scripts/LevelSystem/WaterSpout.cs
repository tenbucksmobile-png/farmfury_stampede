using FarmFuryStampede.Robots;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Ducky's water spout: a ring of water (WaterSpout.png) that rolls out along the ground the way she faces,
    /// spinning as it goes and leaving fading ripples behind. It drops off ledges and climbs small steps, and it
    /// bursts in a splash on the first robot it reaches (defeating it), on a wall, or at the end of its range.
    /// Thrown in the air it falls to the ground first. Moved with MovePosition on a kinematic body, like the egg.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(PooledObject))]
    public class WaterSpout : MonoBehaviour
    {
        public const float Radius = 0.65f;          // half the ring's drawn height; it rolls on its rim

        [SerializeField] private float rollSpeed = 10f;
        [SerializeField] private float range = 12f;
        [SerializeField] private float gravity = 30f;
        [SerializeField] private float stepUp = 0.6f;          // climbs steps up to this high; anything taller stops it
        [SerializeField] private float growSeconds = 0.15f;    // rolls out from a small ring to full size
        [SerializeField] private float fadeDistance = 1.5f;    // shrinks away over the last stretch of its range
        [SerializeField] private float rippleSpacing = 0.9f;
        [SerializeField] private float rippleSize = 0.4f;
        [SerializeField] private float splashSize = 1.3f;
        [SerializeField] private LayerMask groundMask;

        private Rigidbody2D _body;
        private Transform _visual;
        private GameObject _splashPrefab;
        private Vector2 _position;
        private float _fallSpeed;
        private int _direction = 1;
        private float _travelled;
        private float _toNextRipple;
        private float _age;
        private bool _spent;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            var renderer = GetComponentInChildren<SpriteRenderer>();
            _visual = renderer != null ? renderer.transform : transform;
            if (groundMask.value == 0)
            {
                groundMask = LayerMask.GetMask("Ground");
            }
        }

        /// <summary>
        /// Starts rolling from the current position (the ring's centre), heading left (-1) or right (+1).
        /// 'splash' is the FadeEffect drawn for the ripples and the final burst (may be null).
        /// </summary>
        public void Launch(int direction, GameObject splash)
        {
            _direction = direction >= 0 ? 1 : -1;
            _splashPrefab = splash;
            _position = transform.position;
            _body.position = _position;
            _fallSpeed = 0f;
            _travelled = 0f;
            _toNextRipple = rippleSpacing;
            _age = 0f;
            _spent = false;
            _visual.localRotation = Quaternion.identity;
            ApplyScale();
        }

        private void FixedUpdate()
        {
            if (_spent)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            float step = rollSpeed * dt;

            // A wall ahead (solid above step height) stops it.
            var wall = Physics2D.Raycast(_position + Vector2.down * (Radius - stepUp), Vector2.right * _direction,
                Radius + step, groundMask);
            if (wall.collider != null)
            {
                Burst();
                return;
            }

            _position.x += _direction * step;
            _travelled += step;

            // Rest the rim on the ground below (climbing small steps); with nothing there, fall.
            var ground = Physics2D.Raycast(_position + Vector2.up * stepUp, Vector2.down, stepUp + Radius + 0.05f + _fallSpeed * dt, groundMask);
            if (ground.collider != null && _fallSpeed >= 0f)
            {
                _position.y = ground.point.y + Radius;
                _fallSpeed = 0f;
            }
            else
            {
                _fallSpeed += gravity * dt;
                _position.y -= _fallSpeed * dt;
            }

            _body.MovePosition(_position);

            _toNextRipple -= step;
            if (_toNextRipple <= 0f && ground.collider != null)
            {
                _toNextRipple += rippleSpacing;
                Effect(new Vector2(_position.x - _direction * Radius * 0.5f, _position.y - Radius * 0.4f), rippleSize);
            }

            if (_travelled >= range)
            {
                Finish();
            }
        }

        private void Update()
        {
            if (_spent)
            {
                return;
            }

            // Rolling: the ring turns one radian per radius travelled, plus a little wobble so it reads as water.
            _age += Time.deltaTime;
            float degrees = rollSpeed / Radius * Mathf.Rad2Deg * Time.deltaTime;
            _visual.Rotate(0f, 0f, -_direction * degrees);
            ApplyScale();
        }

        private void ApplyScale()
        {
            float grow = growSeconds > 0f ? Mathf.Clamp01(_age / growSeconds) : 1f;
            float fade = Mathf.Clamp01((range - _travelled) / fadeDistance);
            float size = Mathf.Lerp(0.4f, 1f, grow) * Mathf.Lerp(0.5f, 1f, fade);
            float wobble = 1f + 0.06f * Mathf.Sin(_age * 25f);
            _visual.localScale = new Vector3(size * wobble, size / wobble, 1f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_spent)
            {
                return;
            }

            var robot = other.GetComponentInParent<RobotController>();
            if (robot != null && robot.DefeatByAbility())
            {
                Burst();
            }
        }

        private void Burst()
        {
            Effect(_position, splashSize);
            Finish();
        }

        private void Effect(Vector2 at, float size)
        {
            if (_splashPrefab == null || ObjectPool.Instance == null)
            {
                return;
            }

            var go = ObjectPool.Instance.Get(_splashPrefab, at);
            if (go.TryGetComponent(out FadeEffect effect))
            {
                effect.Play(size);
            }
        }

        private void Finish()
        {
            _spent = true;
            ObjectPool.Instance.Release(gameObject);
        }
    }
}
