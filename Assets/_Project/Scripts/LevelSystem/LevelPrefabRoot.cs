using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>Root component of every level prefab: per-level data that is not a spawn marker.</summary>
    public class LevelPrefabRoot : MonoBehaviour
    {
        [Header("Camera")]
        public float cameraMinX;
        public float cameraMaxX;

        [Header("Background")]
        [Tooltip("This world's parallax art, far to near, replacing the background's own layers (Meadow Ruins' Layer1-3) " +
                 "one for one; a layer past the end of the list is hidden. Empty keeps the default layers.")]
        public Sprite[] parallaxLayers = new Sprite[0];

        [Header("Physics")]
        [Tooltip("Sunken City: the whole level is underwater - jumps reach the same height but float (UnderwaterHangTime " +
                 "times as long in the air) and falls sink slower (CharacterController2D.SetUnderwater).")]
        public bool underwater;
    }
}
