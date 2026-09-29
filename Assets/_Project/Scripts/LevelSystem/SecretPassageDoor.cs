using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// One end of a secret underground passage (LevelBuilder.SecretPassage), marked by the SecretSign art: the
    /// player walking into it is taken there automatically (a short fade, see LevelLoader.TravelThroughPassage).
    /// The entrance sign on the level sends the player down into the passage's room and limits the camera to it;
    /// the exit sign at the room's far end brings them back up further along the level and restores the level's
    /// camera limits. Baked into the level prefab by LevelBuilder.
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<CharacterController2D>() != null && LevelLoader.Instance != null)
            {
                LevelLoader.Instance.TravelThroughPassage(this);
            }
        }
    }
}
