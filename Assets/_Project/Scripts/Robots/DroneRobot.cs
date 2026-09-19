using UnityEngine;

namespace FarmFuryStampede.Robots
{
    /// <summary>
    /// Flying robot. Hovers at its spawn height, bobbing slightly, and sweeps back and forth within
    /// patrolDistance of its spawn point. Ignores ground geometry entirely.
    /// </summary>
    public class DroneRobot : RobotController
    {
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobSpeed = 3f;

        private float _originX;
        private float _originY;
        private float _travel;
        private float _time;
        private float _lastX;

        protected override void OnInitialize()
        {
            _originX = Position.x;
            _originY = Position.y;
            _travel = PatrolDistance; // start at the centre of the sweep, heading right
            _time = 0f;
            _lastX = Position.x;
        }

        protected override void Tick(float dt)
        {
            _time += dt;
            _travel += Speed * dt;

            Position.x = _originX + Mathf.PingPong(_travel, PatrolDistance * 2f) - PatrolDistance;
            Position.y = _originY + Mathf.Sin(_time * bobSpeed) * bobAmplitude;

            float dx = Position.x - _lastX;
            if (visual != null && Mathf.Abs(dx) > 0.0001f)
            {
                visual.flipX = dx < 0f;
            }
            _lastX = Position.x;
        }
    }
}
