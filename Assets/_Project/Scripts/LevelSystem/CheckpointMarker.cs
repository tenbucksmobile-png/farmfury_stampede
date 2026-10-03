using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>Marks where LevelLoader spawns a pooled checkpoint. Position is the base of the flag pole.</summary>
    public class CheckpointMarker : MonoBehaviour
    {
        [Tooltip("This world's checkpoint art (Frozen Tundra: the THIN ICE sign), feet-pivoted; it glows gold once reached. Null = the prefab's signpost.")]
        public Sprite worldArt;

        private void OnDrawGizmos()
        {
            MarkerGizmos.Draw(transform, Color.green, new Vector3(0.6f, 2f, 0f));
        }
    }
}
