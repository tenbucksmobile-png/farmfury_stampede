using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Harvest Fairground's hay-bale trampoline (LevelBuilder.Trampoline): a solid bale on the Ground layer. A character
    /// that lands (or stands) on it is bounced straight up to <see cref="launchHeight"/> above its top
    /// (CharacterController2D), and can steer sideways in the air onto a ledge. The art squashes on each bounce.
    /// </summary>
    public class Trampoline : MonoBehaviour
    {
        [Tooltip("How high above the trampoline's top the bounce peaks, in units.")]
        public float launchHeight = 5f;
        [Tooltip("The bale art (optional), squashed briefly on each bounce.")]
        public Transform art;

        private Vector3 _artScale;
        private float _squashUntil;
        private const float SquashSeconds = 0.18f;

        private void Awake()
        {
            if (art != null) { _artScale = art.localScale; }
        }

        /// <summary>Called by the character on each bounce.</summary>
        public void Bounced() => _squashUntil = Time.time + SquashSeconds;

        private void Update()
        {
            if (art == null) { return; }
            float left = _squashUntil - Time.time;
            float s = left > 0f ? Mathf.Sin(left / SquashSeconds * Mathf.PI) * 0.18f : 0f;
            art.localScale = new Vector3(_artScale.x * (1f + s * 0.5f), _artScale.y * (1f - s), _artScale.z);
        }
    }
}
