using FarmFuryStampede.Core;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Crop Factory's rhythm crusher (LevelBuilder.Crusher): a heavy press hanging over the path. Each cycle it waits up
    /// high, shudders as a warning, slams down onto the floor, holds there and winds back up. The head is solid (a
    /// kinematic body on the Ground layer: the player bumps their head on it, and can't walk through it while it is
    /// down); being under it while it slams or holds costs a life. Time the run between slams. Scaled time, so it waits
    /// while the game is paused or frozen.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class Crusher : MonoBehaviour
    {
        [Tooltip("The press head (kinematic Rigidbody2D + BoxCollider2D); its resting (down) position is where it starts.")]
        public Rigidbody2D head;
        [Tooltip("How far above the floor the head's bottom waits between slams.")]
        public float raise = 5f;
        [Tooltip("Seconds for one whole cycle.")]
        public float period = 3.2f;
        [Tooltip("Where in the cycle it starts, 0-1.")]
        [Range(0f, 1f)] public float phase;
        [Tooltip("The art (optional), shuddered before each slam.")]
        public Transform art;

        // Fractions of the cycle: waiting up, the warning shudder, the slam, holding down, then rising.
        private const float WarnAt = 0.42f, SlamAt = 0.55f, DownAt = 0.6f, RiseAt = 0.72f;

        private Vector2 _down;
        private Vector3 _artRest;
        private BoxCollider2D _box;
        private float _time;
        private int _playerMask;

        private void Awake()
        {
            _down = head.position;
            _box = head.GetComponent<BoxCollider2D>();
            _time = phase * period;
            _playerMask = LayerMask.GetMask("Player");
            if (art != null) { _artRest = art.localPosition; }
            head.position = _down + Vector2.up * raise * Height(Fraction);
            head.transform.position = head.position;
        }

        private float Fraction => period > 0f ? Mathf.Repeat(_time / period, 1f) : 0f;

        // How high the head is (1 = up, 0 = on the floor) at a point in the cycle.
        private static float Height(float f)
        {
            if (f < SlamAt) { return 1f; }
            if (f < DownAt) { float s = (f - SlamAt) / (DownAt - SlamAt); return 1f - s * s; }   // accelerating slam
            if (f < RiseAt) { return 0f; }
            float r = (f - RiseAt) / (1f - RiseAt);
            return r * r * (3f - 2f * r);                                                        // eased wind-up
        }

        private void FixedUpdate()
        {
            _time += Time.fixedDeltaTime;
            float f = Fraction;
            head.MovePosition(_down + Vector2.up * raise * Height(f));

            // Lethal while slamming and holding: anyone under the head's footprint (a little narrower than it, reaching
            // a little below its bottom) is crushed. Standing beside it is safe.
            if (f >= SlamAt && f < RiseAt && _box != null && GameManager.Instance != null)
            {
                Vector2 bottom = head.position + _box.offset + Vector2.down * (_box.size.y * 0.5f);
                var hit = Physics2D.OverlapBox(bottom + Vector2.down * 0.2f, new Vector2(_box.size.x - 0.25f, 0.9f), 0f, _playerMask);
                if (hit != null && hit.GetComponentInParent<CharacterController2D>() != null)
                {
                    GameManager.Instance.RespawnPlayer();
                }
            }
        }

        private void Update()
        {
            if (art == null) { return; }
            float f = Fraction;
            bool warning = f >= WarnAt && f < SlamAt;
            art.localPosition = _artRest + (warning ? new Vector3(0.05f * Mathf.Sin(Time.time * 70f), 0f, 0f) : Vector3.zero);
        }
    }
}
