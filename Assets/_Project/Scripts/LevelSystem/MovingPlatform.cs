using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// A Mario-style moving ledge (LevelBuilder.MovingLedge): a solid one-tile-thick platform on the Ground layer that
    /// glides back and forth between its start and start + travel, easing in and out at each end, one round trip per
    /// period. Kinematic Rigidbody2D moved by MovePosition in FixedUpdate (scaled time: it stops while the game is
    /// paused or frozen). Runs before the player (DefaultExecutionOrder) and exposes this step's movement as
    /// <see cref="Delta"/>; CharacterController2D adds it while standing on the platform, so the player rides along.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [Tooltip("Offset from the start position to the far end of the trip (world units).")]
        public Vector2 travel = new(6f, 0f);
        [Tooltip("Seconds for one round trip (there and back).")]
        public float period = 5f;
        [Tooltip("Where in the round trip it starts, 0-1 (0 = at the start, 0.5 = at the far end).")]
        [Range(0f, 1f)] public float phase;

        /// <summary>How far the platform moves during the current physics step.</summary>
        public Vector2 Delta { get; private set; }

        private Rigidbody2D _body;
        private Vector2 _origin;
        private float _time;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _origin = transform.position;
            _time = phase * period;
            _body.position = PositionAt(_time);
            transform.position = _body.position;
        }

        private void FixedUpdate()
        {
            Vector2 from = PositionAt(_time);
            _time += Time.fixedDeltaTime;
            Vector2 to = PositionAt(_time);
            Delta = to - from;
            _body.MovePosition(to);
        }

        // Smooth ping-pong: 0 at the start, 1 at the far end, easing at both.
        private Vector2 PositionAt(float time)
        {
            float t = period > 0f ? 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * time / period) : 0f;
            return _origin + travel * t;
        }
    }
}
