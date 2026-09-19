using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>Root component of every level prefab: per-level data that is not a spawn marker.</summary>
    public class LevelPrefabRoot : MonoBehaviour
    {
        [Header("Camera")]
        public float cameraMinX;
        public float cameraMaxX;
    }
}
