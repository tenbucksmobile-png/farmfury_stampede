using UnityEngine;

namespace FarmFuryStampede.Data
{
    /// <summary>Defines one level: its world, par target, and any ability-gated secret area.</summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "Farm Fury Stampede/Level Data")]
    public class LevelData : ScriptableObject
    {
        public string levelId;
        public WorldType worldType;

        [Tooltip("Which character's ability the level's bonus/secret area requires, if any (GDD Section 6).")]
        public CharacterType? characterGatedSecretCharacter;

        public int parStars = 3;
    }
}
