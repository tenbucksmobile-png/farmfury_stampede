using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>Marks where the player starts. LevelLoader moves the player here on load.</summary>
    public class PlayerStartPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            MarkerGizmos.Draw(transform, Color.cyan, new Vector3(0.8f, 0.95f, 0f));
        }
    }
}
