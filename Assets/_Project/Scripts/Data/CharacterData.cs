using System;
using UnityEngine;

namespace FarmFuryStampede.Data
{
    /// <summary>Where a hat sits on one art frame: the top of the head and its width, in the Visual's local units.</summary>
    [Serializable]
    public struct HatPlacement
    {
        public Sprite frame;
        public Vector2 anchor;
        public float width;
    }

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
        [Tooltip("Where a hat sits on each art frame (set by setup from StampedeCosmetics.HeadPoints, hand-measured per frame).")]
        public HatPlacement[] hatPlacements = new HatPlacement[0];
        [Tooltip("Fallback for a frame not in hatPlacements: where a hat sits facing right / left, relative to the Visual's feet pivot.")]
        public Vector2 hatAnchor = new(0f, 1.35f);
        public Vector2 hatAnchorLeft = new(0f, 1.35f);
        [Tooltip("Fallback head width in world units; a hat's CosmeticData.hatScale is a fraction of the head width.")]
        public float hatWidth = 0.7f;

        /// <summary>The top of the head and its width on this body frame (in the Visual's local units).</summary>
        public void GetHatPlacement(Sprite body, bool facingRight, out Vector2 anchor, out float width)
        {
            if (body != null && hatPlacements != null)
            {
                foreach (var placement in hatPlacements)
                {
                    if (placement.frame == body)
                    {
                        anchor = placement.anchor;
                        width = placement.width;
                        return;
                    }
                }
            }

            anchor = facingRight ? hatAnchor : hatAnchorLeft;
            width = hatWidth;
        }
    }
}
