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

        [Tooltip("levelId of this world's boss level. Clearing it unlocks the next world.")]
        public string bossLevelId;

        [TextArea]
        public string blurb;

        [Header("Menu art")]
        [Tooltip("World Select card (world name baked in, centred). Null = plain coloured card with the name as text.")]
        public Sprite selectCardArt;
        [Tooltip("Full-screen Level Select backdrop (world name baked in along the top). Null = plain dark screen with the name as text.")]
        public Sprite levelSelectBackground;

        [Header("Unlock")]
        [Tooltip("Star threshold required to unlock this world. Purchase-gated worlds use purchaseRequired instead.")]
        public int unlockStarThreshold;
        public bool purchaseRequired;
    }
}
