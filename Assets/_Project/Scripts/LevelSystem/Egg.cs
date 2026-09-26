using FarmFuryStampede.Robots;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Cluck's launched egg: lobbed forward in an arc, it defeats the first robot it touches (then vanishes), and
    /// breaks on the ground or when its lifetime runs out. Moved with MovePosition on a kinematic body, like the
    /// horseshoe.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(PooledObject))]
    public class Egg : MonoBehaviour
    {
        [SerializeField] private float forwardSpeed = 13f;
        [SerializeField] private float upwardSpeed = 5f;
        [SerializeField] private float gravity = 22f;
        [SerializeField] private float lifetime = 1.6f;
        [SerializeField] private float spinDegreesPerSecond = 540f;
        [SerializeField] private LayerMask groundMask;

        private Rigidbody2D _body;
        private Vector2 _position;
        private Vector2 _velocity;
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

        /// <summary>Starts the arc from the current position, heading left (-1) or right (+1).</summary>
        public void Launch(int direction)
        {
            _direction = direction;
            _velocity = new Vector2(direction * forwardSpeed, upwardSpeed);
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

            float dt = Time.fixedDeltaTime;
            _timeLeft -= dt;
            _velocity.y -= gravity * dt;
            _position += _velocity * dt;
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
