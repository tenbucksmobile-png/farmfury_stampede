using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// One end of a secret underground passage (LevelBuilder.SecretPassage), marked by the SecretSign art: the
    /// player walking into it is taken there automatically (a short fade, see LevelLoader.TravelThroughPassage).
    /// The entrance sign on the level sends the player down into the passage's room and limits the camera to it;
    /// the exit sign at the room's far end brings them back up on the entrance sign itself (since 2026-10-03, so no
    /// part of the level is skipped) and restores the level's camera limits. The entrance then ignores the player
    /// until they step off it, so they don't drop straight back down. Baked into the level prefab by LevelBuilder.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SecretPassageDoor : MonoBehaviour
    {
        [Tooltip("Where the player's feet-centre lands (the room's entry spot, or the level ground past the exit).")]
        public Vector2 destination;
        [Tooltip("True for the room's exit: restores the level's camera limits instead of using the ones below.")]
        public bool returnsToLevel;
        [Tooltip("Camera X limits while in the room (entrances only).")]
        public float cameraMinX, cameraMaxX;
        [Tooltip("Exits only: the level's entrance sign the player comes back up on (held until they step off it).")]
        public SecretPassageDoor entrance;

        private bool _held;

        /// <summary>Ignores the player until they leave this sign (they've just come back up onto it).</summary>
        public void HoldUntilLeft() => _held = true;

        private void OnEnable() => _held = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_held && other.GetComponentInParent<CharacterController2D>() != null && LevelLoader.Instance != null)
            {
                LevelLoader.Instance.TravelThroughPassage(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<CharacterController2D>() != null) { _held = false; }
        }
    }
}
