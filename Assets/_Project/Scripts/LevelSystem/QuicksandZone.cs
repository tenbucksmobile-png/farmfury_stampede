using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Dustbowl Canyon's quicksand (LevelBuilder.Quicksand): a trigger over a stretch of ground. While a character is
    /// in it they wade at under half speed (CharacterController2D.NotifyInQuicksand); jumping still works as normal,
    /// so it never traps anyone. The sand art (optional) churns gently.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class QuicksandZone : MonoBehaviour
    {
        [Tooltip("The tiled sand art (optional), drifted slowly sideways so the surface churns.")]
        public SpriteRenderer art;

        private Vector2 _size;

        private void Awake()
        {
            if (art != null) { _size = art.size; }
        }

        private void Update()
        {
            if (art == null) { return; }
            // A slow swell: the tiled strip widens and narrows by a few percent, so the swirls drift.
            art.size = new Vector2(_size.x * (1f + 0.03f * Mathf.Sin(Time.time * 0.9f)), _size.y);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            var character = other.GetComponentInParent<CharacterController2D>();
            if (character != null)
            {
                character.NotifyInQuicksand();
            }
        }
    }
}
