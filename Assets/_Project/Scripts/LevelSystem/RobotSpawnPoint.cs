using FarmFuryStampede.Data;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>Marks where LevelLoader spawns a pooled robot of the given type.</summary>
    public class RobotSpawnPoint : MonoBehaviour
    {
        public RobotType robotType;

        [Tooltip("Half-range of the patrol, in units, either side of the spawn position.")]
        public float patrolDistance = 3f;

        [Tooltip("0 = present from the start. N > 0 = a reinforcement wave that spawns when the boss takes its Nth hit.")]
        public int wave;

        private void OnDrawGizmos()
        {
            MarkerGizmos.Draw(transform, Color.red, Vector3.one);
            Gizmos.color = Color.red;
            Vector3 p = transform.position;
            Gizmos.DrawLine(p + Vector3.left * patrolDistance, p + Vector3.right * patrolDistance);
        }
    }
}
