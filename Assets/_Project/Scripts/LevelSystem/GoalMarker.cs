using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>Marks where LevelLoader spawns the pooled level goal. Position is the base of the flag pole.</summary>
    public class GoalMarker : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            MarkerGizmos.Draw(transform, Color.white, new Vector3(0.8f, 4f, 0f));
        }
    }
}
