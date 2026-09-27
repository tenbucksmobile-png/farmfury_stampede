using UnityEngine;

namespace FarmFuryStampede.Data
{
    /// <summary>
    /// One cosmetic, ported from Farm Fury: Arcade's CosmeticData and reshaped for a side-view platformer: art per
    /// facing (right / left) instead of Arcade's four maze directions. Ids match Arcade's so the franchise catalog
    /// and store products stay the same (sombrero_hat, chef_hat, crown, baseball_cap_&lt;character&gt;,
    /// cowboy_hat_&lt;character&gt;, trail_*, machine_*). Created by Phase 6 setup (StampedeCosmetics).
    /// </summary>
    [CreateAssetMenu(fileName = "CosmeticData_XX", menuName = "Farm Fury Stampede/Cosmetic Data")]
    public class CosmeticData : ScriptableObject
    {
        [Tooltip("Persistence key (owned/equipped). Never rename once players may own it.")]
        public string cosmeticId;
        public string displayName;
        public CosmeticType cosmeticType;
        [Tooltip("Hat/Skin made for one character (baseball caps, cowboy hats, machines). Ignored when anyCharacter.")]
        public CharacterType character;
        [Tooltip("Hat for every character (sombrero, chef hat, crown) rather than one character's variant.")]
        public bool anyCharacter;
        [Tooltip("Locker tile / suggestion icon.")]
        public Sprite previewSprite;

        [Header("Hat")]
        [Tooltip("Worn facing right / left. A missing side mirrors the other.")]
        public Sprite hatRight;
        public Sprite hatLeft;
        [Tooltip("Optional alternates (Sombrero_1..4): one is picked per level, as in Arcade.")]
        public Sprite[] hatVariants = new Sprite[0];
        [Tooltip("Hat width as a fraction of the character's head width (CharacterData.hatWidth).")]
        public float hatScale = 1f;

        [Header("Skin (machine)")]
        [Tooltip("Replaces the character's art entirely while equipped (every pose).")]
        public Sprite skinRight;
        public Sprite skinLeft;
        [Tooltip("Puffs exhaust smoke behind the character while moving (tractor, truck, baler).")]
        public bool spawnsMovementSmoke;

        [Header("Trail")]
        [Tooltip("Stamped behind the moving character and faded out (Arcade's ghost trail).")]
        public Sprite trailSprite;

        public bool FitsCharacter(CharacterType type) => anyCharacter || character == type;

        public Sprite Hat(bool facingRight, int levelIndex)
        {
            if (hatVariants != null && hatVariants.Length > 1)
            {
                return hatVariants[Mathf.Abs(levelIndex) % hatVariants.Length];
            }
            return facingRight ? (hatRight != null ? hatRight : hatLeft) : (hatLeft != null ? hatLeft : hatRight);
        }

        /// <summary>True when the chosen hat art only exists for the other side and must be mirrored.</summary>
        public bool HatNeedsMirror(bool facingRight) =>
            (hatVariants == null || hatVariants.Length <= 1) && (facingRight ? hatRight == null : hatLeft == null);

        public Sprite Skin(bool facingRight) =>
            facingRight ? (skinRight != null ? skinRight : skinLeft) : (skinLeft != null ? skinLeft : skinRight);
    }
}
