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

        [Tooltip("Seconds after each use before the ability can be used again (Arcade's cooldown model). The HUD lets the player skip it for coins or a rewarded ad.")]
        public float abilityCooldown = 5f;

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

        [Tooltip("Optional real art (directional frames). When set, sprites are swapped by state instead of flipped.")]
        public CharacterSpriteSet spriteSet;
        public Color uiColor = Color.white;

        [Tooltip("Optional framed character card (name baked in) shown on Character Select; without it the slot is a uiColor panel with the portrait.")]
        public Sprite selectCard;

        [Tooltip("This character giving a thumbs up: one per life on the HUD. Null = the shared MenuArt.lifeIcon.")]
        public Sprite lifeIcon;
        [Tooltip("Draw size relative to the standard 1.5-unit animal (Bessie the cow is bigger). The collider is unchanged.")]
        public float visualScale = 1f;

        [Header("Cosmetics")]
        [Tooltip("Where a hat sits, relative to the Visual's feet pivot, facing right (x is mirrored facing left). Measured from the idle art by Phase 6 setup.")]
        public Vector2 hatAnchor = new(0f, 1.35f);
        [Tooltip("Head width in world units; a hat's CosmeticData.hatScale is a fraction of this.")]
        public float hatWidth = 0.7f;
    }
}
