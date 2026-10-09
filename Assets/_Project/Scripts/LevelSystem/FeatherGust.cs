using FarmFuryStampede.Robots;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Gerald's blown feathers, and (with the wool art, drawn smaller) Woolly's tuft of wool: a flurry of feathers
    /// (FeatherBurst.png) blown straight ahead at chest height,
    /// spreading out as it flies (growing, swaying and tumbling), shedding loose feathers behind it, and slowing
    /// as it loses breath. It defeats the first robot it reaches - a Drone at that height too - scattering into a
    /// burst of feathers, and also scatters on a wall or when it runs out. Moved with MovePosition on a kinematic
    /// body, like the egg.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(PooledObject))]
    public class FeatherGust : MonoBehaviour
    {
        [SerializeField] private float startSpeed = 13f;
        [SerializeField] private float endSpeed = 6f;
        [SerializeField] private float lifetime = 0.9f;
        [SerializeField] private float startScale = 0.45f;
        [SerializeField] private float endScale = 1.1f;
        [SerializeField] private float swayHeight = 0.25f;
        [SerializeField] private float swayDegrees = 14f;
        [SerializeField] private float shedSpacing = 1.1f;     // a loose feather left behind this often
        [SerializeField] private float shedSize = 0.3f;
        [SerializeField] private float scatterSize = 1.2f;
        [SerializeField] private float artScale = 1f;          // the art's drawn size at full spread (Woolly's tuft is smaller)
        [SerializeField] private LayerMask groundMask;

        private Rigidbody2D _body;
        private SpriteRenderer _renderer;
        private Transform _visual;
        private GameObject _scatterPrefab;
        private Vector2 _origin;
        private Vector2 _position;
        private int _direction = 1;
        private float _age;
        private float _travelled;
        private float _toNextShed;
        private bool _spent;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _visual = _renderer != null ? _renderer.transform : transform;
            if (groundMask.value == 0)
            {
                groundMask = LayerMask.GetMask("Ground");
            }
        }

        /// <summary>
        /// Blows the feathers from the current position, heading left (-1) or right (+1). 'scatter' is the
        /// FadeEffect drawn for shed feathers and the final burst (may be null).
        /// </summary>
        public void Launch(int direction, GameObject scatter)
        {
            _direction = direction >= 0 ? 1 : -1;
            _scatterPrefab = scatter;
            _origin = transform.position;
            _position = _origin;
            _body.position = _position;
            _age = 0f;
            _travelled = 0f;
            _toNextShed = shedSpacing * 0.5f;
            _spent = false;
            if (_renderer != null)
            {
                _renderer.flipX = _direction < 0;
            }
            Apply();
        }

        private void FixedUpdate()
        {
            if (_spent)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            _age += dt;
            float t = Mathf.Clamp01(_age / lifetime);
            float step = Mathf.Lerp(startSpeed, endSpeed, t) * dt;
            _travelled += step;
            _position = _origin + new Vector2(_direction * _travelled, swayHeight * Mathf.Sin(_age * 14f));
            _body.MovePosition(_position);

            if (Physics2D.OverlapPoint(_position, groundMask) != null)
            {
                Scatter();
                return;
            }

            _toNextShed -= step;
            if (_toNextShed <= 0f)
            {
                _toNextShed += shedSpacing;
                Effect(_position - new Vector2(_direction * 0.6f, Random.Range(-0.3f, 0.3f)), shedSize);
            }

            if (t >= 1f)
            {
                Scatter();
            }
        }

        private void Update()
        {
            if (!_spent)
            {
                Apply();
            }
        }

        // Spreads out as it flies, swaying back and forth, and fades over its last third.
        private void Apply()
        {
            float t = Mathf.Clamp01(_age / lifetime);
            float size = Mathf.Lerp(startScale, endScale, 1f - (1f - t) * (1f - t));
            _visual.localScale = Vector3.one * (size * artScale);
            _visual.localRotation = Quaternion.Euler(0f, 0f, swayDegrees * Mathf.Sin(_age * 11f) * _direction);
            if (_renderer != null)
            {
                var c = _renderer.color;
                c.a = t < 0.67f ? 1f : Mathf.Lerp(1f, 0.3f, (t - 0.67f) / 0.33f);
                _renderer.color = c;
            }
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
                Scatter();
            }
        }

        private void Scatter()
        {
            Effect(_position, scatterSize);
            _spent = true;
            ObjectPool.Instance.Release(gameObject);
        }

        private void Effect(Vector2 at, float size)
        {
            if (_scatterPrefab == null || ObjectPool.Instance == null)
            {
                return;
            }

            var go = ObjectPool.Instance.Get(_scatterPrefab, at);
            if (go.TryGetComponent(out FadeEffect effect))
            {
                effect.Play(size);
            }
        }
    }
}
