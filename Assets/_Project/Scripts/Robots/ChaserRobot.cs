using FarmFuryStampede.LevelSystem;
using UnityEngine;

namespace FarmFuryStampede.Robots
{
    /// <summary>
    /// Pursuer: once the player is within the aggro range (the spawn marker's patrolDistance) and on roughly
    /// the same level, it walks straight at them at a FIXED speed. RobotData gives it a speed slightly under
    /// the player's base speed, so it is pressure rather than an unavoidable threat: keep running and it falls
    /// behind, stop and it catches you. It stops at walls and ledges (it does not jump) and can be stomped.
    /// </summary>
    public class ChaserRobot : GroundPatrolRobot
    {
        [SerializeField] private float verticalTolerance = 3f;
        [SerializeField] private float stopDistance = 0.3f;

        protected override void Tick(float dt)
        {
            var loader = LevelLoader.Instance;
            if (loader == null || loader.Player == null)
            {
                return;
            }

            Vector2 player = loader.Player.Position;
            float dx = player.x - Position.x;
            if (Mathf.Abs(dx) > PatrolDistance || Mathf.Abs(player.y - Position.y) > verticalTolerance || Mathf.Abs(dx) < stopDistance)
            {
                return;
            }

            int direction = dx > 0f ? 1 : -1;
            Face(direction);
            if (WallAhead(direction) || !GroundAhead(direction))
            {
                return;
            }

            Position.x += direction * Speed * dt;
        }
    }
}
