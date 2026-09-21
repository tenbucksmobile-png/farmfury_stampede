using UnityEngine;

namespace FarmFuryStampede.Robots
{
    /// <summary>
    /// Shared ground-robot movement: walks back and forth within patrolDistance of its spawn point and turns
    /// around at a wall or at a ledge edge, so it never walks off a platform. Stays at its spawn height.
    /// Harvester uses it as-is; Scout, Chaser and the Commander boss build on it.
    /// </summary>
    public class GroundPatrolRobot : RobotController
    {
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float halfWidth = 0.45f;
        [SerializeField] private float halfHeight = 0.5f;

        private float _originX;

        /// <summary>+1 facing right, -1 facing left.</summary>
        protected int Direction { get; private set; } = 1;

        /// <summary>Multiplies the data speed (bosses speed up as they take hits).</summary>
        protected float SpeedScale { get; set; } = 1f;

        protected float OriginX => _originX;
        protected LayerMask GroundMask => groundMask;

        protected override void Awake()
        {
            base.Awake();
            if (groundMask.value == 0)
            {
                groundMask = LayerMask.GetMask("Ground");
            }
        }

        protected override void OnInitialize()
        {
            _originX = Position.x;
            Direction = 1;
            SpeedScale = 1f;
            UpdateFacing();
        }

        protected override void Tick(float dt)
        {
            float nextX = Position.x + Direction * Speed * SpeedScale * dt;

            if (nextX > _originX + PatrolDistance)
            {
                nextX = _originX + PatrolDistance;
                Reverse();
            }
            else if (nextX < _originX - PatrolDistance)
            {
                nextX = _originX - PatrolDistance;
                Reverse();
            }
            else if (WallAhead(Direction) || !GroundAhead(Direction))
            {
                nextX = Position.x;
                Reverse();
            }

            Position.x = nextX;
        }

        protected void Reverse()
        {
            Face(-Direction);
        }

        protected void Face(int direction)
        {
            Direction = direction;
            UpdateFacing();
        }

        private void UpdateFacing()
        {
            SetFacing(Direction >= 0);
        }

        protected bool WallAhead(int direction)
        {
            return Physics2D.Raycast(Position, new Vector2(direction, 0f), halfWidth + 0.1f, groundMask).collider != null;
        }

        // Ledge check: probe for ground just beyond the front foot.
        protected bool GroundAhead(int direction)
        {
            Vector2 origin = new Vector2(Position.x + direction * (halfWidth + 0.05f), Position.y - halfHeight + 0.1f);
            return Physics2D.Raycast(origin, Vector2.down, 0.5f, groundMask).collider != null;
        }
    }
}
