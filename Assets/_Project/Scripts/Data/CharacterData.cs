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

        [Header("Unlock")]
        [Tooltip("Number of levels the player must complete before this character unlocks.")]
        public int unlockLevelsRequired;

        [Header("Movement (placeholder, tuned in Phase 2)")]
        public float moveSpeed = 5f;
        public float jumpHeight = 3f;
    }
}
