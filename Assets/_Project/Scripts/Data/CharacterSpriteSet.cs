using UnityEngine;

namespace FarmFuryStampede.Data
{
    /// <summary>
    /// Real art for a character, as separate right- and left-facing frames (the art is not a mirror image, so
    /// the sprites are swapped instead of flipped). Characters without a set keep their single placeholder sprite
    /// and are flipped in code. Frames are picked by CharacterSpriteAnimator from the live controller state. Any
    /// frame may be missing: jump and the ability pose fall back to idle, defeat to idle, idle to the first run
    /// frame, and a one-frame run simply holds that frame.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterSprites", menuName = "Farm Fury Stampede/Character Sprite Set")]
    public class CharacterSpriteSet : ScriptableObject
    {
        [Header("Facing right")]
        public Sprite idleRight;
        public Sprite[] runRight;
        public Sprite jumpRight;

        [Header("Facing left")]
        public Sprite idleLeft;
        public Sprite[] runLeft;
        public Sprite jumpLeft;

        [Header("Any facing")]
        [Tooltip("Shown while the character is defeated, just before the respawn.")]
        public Sprite defeat;

        [Header("Ability pose (optional)")]
        [Tooltip("Shown while the character's ability is active (Billy's ram, Percy's roll). Null = the normal frames.")]
        public Sprite abilityRight;
        public Sprite abilityLeft;

        [Header("Timing")]
        [Tooltip("Run-cycle frames per second at full move speed.")]
        public float runFramesPerSecond = 8f;
    }
}
