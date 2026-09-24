using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>Marks where LevelLoader spawns a pooled crop.</summary>
    public class CropSpawnPoint : MonoBehaviour
    {
        [Tooltip("Part of an out-of-the-way cluster reserved for a character-gated secret (gating arrives in Phase 4).")]
        public bool secretCluster;
        [Tooltip("Optional art for this one crop instead of the random pool pick (e.g. the bonus coin on a tower top).")]
        public Sprite visualOverride;

        private void OnDrawGizmos()
        {
            MarkerGizmos.Draw(transform, secretCluster ? Color.magenta : Color.yellow, Vector3.one * 0.5f);
        }
    }
}
