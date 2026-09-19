using UnityEngine;

namespace FarmFuryStampede.Data
{
    /// <summary>Defines one playable character: its ability, unlock requirement, and base movement stats.</summary>
    [CreateAssetMenu(fileName = "CharacterData", menuName = "Farm Fury Stampede/Character Data")]
    public class CharacterData : ScriptableObject
    {
        public CharacterType characterType;
        public string displayName;

        [Header("Ability")]
        public AbilityType abilityType;
        [TextArea]
        public string abilityDescription;

        [Tooltip("PLAYTEST-TUNING PLACEHOLDER, not a balance decision: how many times the ability can be used per level attempt.")]
        public int abilityUsesPerLevel = 3;

        [Header("Unlock")]
        [Tooltip("Number of distinct levels the player must have completed before this character unlocks (GDD ladder: 0, 0, 5, 10, 15, 20, 30, 40).")]
        public int unlockLevelsRequired;

        [Header("Movement (identical for every character by design)")]
        [Tooltip("Shared by all eight characters. Per-character speed variance 'read as arbitrary' in Arcade; only abilities differ.")]
        public float moveSpeed = 8f;
        [Tooltip("Shared by all eight characters. Horace's taller jump is his ability (Rear Vault), not a base stat.")]
        public float jumpHeight = 3.5f;

        [Header("Placeholder Art")]
        public Sprite placeholderSprite;
        public Color uiColor = Color.white;
    }
}
