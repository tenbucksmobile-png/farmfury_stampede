using UnityEngine;

namespace FarmFuryStampede.Data
{
    /// <summary>Defines one level: its world, par target, prefab, and any ability-gated secret area.</summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "Farm Fury Stampede/Level Data")]
    public class LevelData : ScriptableObject
    {
        public string levelId;
        public string displayName;
        public WorldType worldType;

        [Tooltip("The world's boss level: unlocks after the world's regular levels, and clearing it unlocks the next world.")]
        public bool isBossLevel;

        [Tooltip("Prefab holding this level's tilemaps and spawn/checkpoint/goal markers. Instantiated by LevelLoader.")]
        public GameObject levelPrefab;

        [Header("Character-Gated Secret (GDD Section 6)")]
        public bool hasCharacterGatedSecret;

        [Tooltip("The character whose ability the level's secret is designed around.")]
        public CharacterType characterGatedSecretCharacter;

        [Tooltip("Other characters whose abilities also open the same secret (e.g. two ways to gain height).")]
        public CharacterType[] alsoOpensSecret = new CharacterType[0];

        [TextArea]
        public string secretDescription;

        public int parStars = 3;

        /// <summary>True if the given character's ability opens this level's secret.</summary>
        public bool CharacterOpensSecret(CharacterType type)
        {
            if (!hasCharacterGatedSecret)
            {
                return false;
            }

            if (type == characterGatedSecretCharacter)
            {
                return true;
            }

            foreach (var other in alsoOpensSecret)
            {
                if (other == type)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
