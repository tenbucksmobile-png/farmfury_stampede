using UnityEngine;

namespace FarmFuryStampede.Robots
{
    /// <summary>
    /// The river piranha (Watermill Village's WaterRobot): waits under a River()'s surface, hidden behind the water
    /// art, then leaps out in an arc to patrolDistance above where it waits and splashes back down, drifting a little
    /// sideways and alternating direction each leap. Harmless while submerged; stompable in the air like any robot.
    /// Leaps are staggered by spawn position so neighbours don't jump together.
    /// </summary>
    public class PiranhaRobot : RobotController
    {
        [SerializeField] private float restSeconds = 1.6f;
        [SerializeField] private float leapSeconds = 1.3f;
        [SerializeField] private float drift = 1.5f;      // sideways travel per leap
        [SerializeField] private float maxTilt = 40f;      // nose up on the way up, down on the way down

        private Vector2 _origin;
        private float _timer;
        private bool _leaping;
        private int _direction = 1;

        protected override bool CanHurtPlayer => _leaping;

        protected override void OnInitialize()
        {
            _origin = Position;
            _leaping = false;
            _direction = 1;
            // Spread the first leap over the rest time by position, so a row of piranhas leaps in turn.
            _timer = Mathf.Repeat(_origin.x * 0.37f, restSeconds);
            ApplyTilt(0f);
        }

        protected override void Tick(float dt)
        {
            _timer += dt;
            if (!_leaping)
            {
                Position = _origin;
                if (_timer >= restSeconds)
                {
                    _timer = 0f;
                    _leaping = true;
                    SetFacing(_direction > 0);
                }
                return;
            }

            float t = Mathf.Clamp01(_timer / leapSeconds);
            Position.x = _origin.x + _direction * drift * (t - 0.5f);
            Position.y = _origin.y + PatrolDistance * 4f * t * (1f - t);
            ApplyTilt(1f - 2f * t);

            if (t >= 1f)
            {
                _leaping = false;
                _timer = 0f;
                _direction = -_direction;
                ApplyTilt(0f);
            }
        }

        // Tilts the art along the arc (the art faces right; a left-facing flip mirrors the tilt).
        private void ApplyTilt(float upness)
        {
            if (visual == null) { return; }
            float angle = upness * maxTilt * (visual.flipX ? -1f : 1f);
            visual.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
