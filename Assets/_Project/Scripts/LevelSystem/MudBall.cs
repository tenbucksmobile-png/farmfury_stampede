using FarmFuryStampede.Robots;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Billy's slung mud (Mud Sling): a glob lobbed forward in an arc, like Cluck's egg. It defeats the first robot it
    /// touches with a splat; landing on the ground it leaves a mud pile (MudPile) that knocks out the next robot to
    /// walk into it. Moved with MovePosition on a kinematic body, like the egg and the horseshoe.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(PooledObject))]
    public class MudBall : MonoBehaviour
    {
        [SerializeField] private float forwardSpeed = 11f;
        [SerializeField] private float upwardSpeed = 6f;
        [SerializeField] private float gravity = 20f;
        [SerializeField] private float lifetime = 1.8f;
        [SerializeField] private float spinDegreesPerSecond = 300f;
        [SerializeField] private LayerMask groundMask;
        [Tooltip("Left where the glob lands on the ground (MudPile, pooled).")]
        [SerializeField] private GameObject pile;
        [Tooltip("The splat where it hits a robot or a wall (FadeEffect, pooled).")]
        [SerializeField] private GameObject splat;

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
            Vector2 from = _position;
            _position += _velocity * dt;
            _body.MovePosition(_position);

            if (Physics2D.OverlapPoint(_position, groundMask) != null)
            {
                Land(from);
            }
            else if (_timeLeft <= 0f)
            {
                Finish(false);
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
                Finish(true);
            }
        }

        // Hit something solid: on a floor (falling onto its top) it leaves a pile there; against a wall it just splats.
        private void Land(Vector2 from)
        {
            var down = Physics2D.Raycast(from + Vector2.up * 0.2f, Vector2.down, 2f, groundMask);
            if (_velocity.y < 0f && down.collider != null && down.normal.y > 0.7f && pile != null && ObjectPool.Instance != null)
            {
                ObjectPool.Instance.Get(pile, down.point);
                _spent = true;
                ObjectPool.Instance.Release(gameObject);
                return;
            }
            Finish(true);
        }

        private void Finish(bool splatter)
        {
            _spent = true;
            if (splatter && splat != null && ObjectPool.Instance != null)
            {
                var effect = ObjectPool.Instance.Get(splat, _position);
                if (effect.TryGetComponent(out FadeEffect fade)) { fade.Play(0.8f); }
            }
            ObjectPool.Instance.Release(gameObject);
        }
    }
}
