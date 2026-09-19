using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>Marks where LevelLoader spawns a pooled Breakable Wall. Position is the wall's bottom centre.</summary>
    public class BreakableWallMarker : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            MarkerGizmos.Draw(transform, new Color(1f, 0.5f, 0f), new Vector3(1f, 3f, 0f));
        }
    }
}
