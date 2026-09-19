using FarmFuryStampede.LevelSystem;
using UnityEngine;

namespace FarmFuryStampede.Robots
{
    /// <summary>
    /// Fast patrol robot (RobotData speed is well above the Harvester's). Unlike a Harvester it is not blind:
    /// when the player comes within sight range, at about its own height, with a clear line of sight, and is
    /// BEHIND it, it reverses direction to face them. A short cooldown stops it flip-flopping. It still stays
    /// inside its patrol range and turns at walls and ledges.
    /// </summary>
    public class ScoutRobot : GroundPatrolRobot
    {
        [SerializeField] private float sightRange = 7f;
        [SerializeField] private float sightHeight = 1.6f;
        [SerializeField] private float turnCooldownSeconds = 1f;

        private float _cooldown;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _cooldown = 0f;
        }

        protected override void Tick(float dt)
        {
            _cooldown -= dt;
            if (_cooldown <= 0f && PlayerBehindInSight())
            {
                Reverse();
                _cooldown = turnCooldownSeconds;
            }

            base.Tick(dt);
        }

        private bool PlayerBehindInSight()
        {
            var loader = LevelLoader.Instance;
            if (loader == null || loader.Player == null)
            {
                return false;
            }

            Vector2 player = loader.Player.Position;
            float dx = player.x - Position.x;
            bool behind = dx * Direction < 0f;
            if (!behind || Mathf.Abs(dx) > sightRange || Mathf.Abs(player.y - Position.y) > sightHeight)
            {
                return false;
            }

            return Physics2D.Linecast(Position, player, GroundMask).collider == null;
        }
    }
}
