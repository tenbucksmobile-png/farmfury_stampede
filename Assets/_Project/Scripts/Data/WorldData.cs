using UnityEngine;

namespace FarmFuryStampede.Data
{
    /// <summary>Defines one themed world and its unlock/progression footprint.</summary>
    [CreateAssetMenu(fileName = "WorldData", menuName = "Farm Fury Stampede/World Data")]
    public class WorldData : ScriptableObject
    {
        public WorldType worldType;
        public string displayName;
        public int levelCount;

        [Header("Unlock")]
        [Tooltip("Star threshold required to unlock this world. Purchase-gated worlds use purchaseRequired instead.")]
        public int unlockStarThreshold;
        public bool purchaseRequired;
    }
}
