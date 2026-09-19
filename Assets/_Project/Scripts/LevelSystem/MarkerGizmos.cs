using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>Scene-view drawing for the empty marker objects inside level prefabs.</summary>
    internal static class MarkerGizmos
    {
        public static void Draw(Transform t, Color color, Vector3 size)
        {
            Gizmos.color = color;
            Gizmos.DrawWireCube(t.position, size);
            Gizmos.DrawLine(t.position + Vector3.left * 0.2f, t.position + Vector3.right * 0.2f);
            Gizmos.DrawLine(t.position + Vector3.down * 0.2f, t.position + Vector3.up * 0.2f);
        }
    }
}
