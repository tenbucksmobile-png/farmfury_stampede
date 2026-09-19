using FarmFuryStampede.Robots;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Horace's thrown horseshoe: flies straight, defeats the first robot it touches (then vanishes), and is
    /// stopped by ground or by running out its short lifetime. Moved with MovePosition on a kinematic body.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(PooledObject))]
    public class Horseshoe : MonoBehaviour
    {
        [SerializeField] private float speed = 16f;
        [SerializeField] private float lifetime = 1.2f;
        [SerializeField] private float spinDegreesPerSecond = 720f;
        [SerializeField] private LayerMask groundMask;

        private Rigidbody2D _body;
        private Vector2 _position;
        private int _direction = 1;
        private float _timeLeft;
        private bool _spent;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            if (groundMask.value == 0)
            {
                groundMask = LayerMask.GetMask("Ground");
            }
        }

        /// <summary>Starts the flight from the current position, heading left (-1) or right (+1).</summary>
        public void Launch(int direction)
        {
            _direction = direction;
            _timeLeft = lifetime;
            _spent = false;
            _position = transform.position;
            _body.position = _position;
        }

        private void FixedUpdate()
        {
            if (_spent)
            {
                return;
            }

            _timeLeft -= Time.fixedDeltaTime;
            _position.x += _direction * speed * Time.fixedDeltaTime;
            _body.MovePosition(_position);

            if (_timeLeft <= 0f || Physics2D.OverlapPoint(_position, groundMask) != null)
            {
                Finish();
            }
        }

        private void Update()
        {
            transform.Rotate(0f, 0f, -_direction * spinDegreesPerSecond * Time.deltaTime);
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
                Finish();
            }
        }

        private void Finish()
        {
            _spent = true;
            ObjectPool.Instance.Release(gameObject);
        }
    }
}
