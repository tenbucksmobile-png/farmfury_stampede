using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// The "Water" trigger-tile type. Put on a trigger collider (typically a Water tilemap layer). While a
    /// character stands in it, the controller slows them and drowns them (a respawn) if they stay too long,
    /// unless they are water-immune (Ducky). Meadow Ruins has no water; Worlds 3 and 5 will.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WaterZone : MonoBehaviour
    {
        private void OnTriggerStay2D(Collider2D other)
        {
            var character = other.GetComponentInParent<CharacterController2D>();
            if (character != null)
            {
                character.NotifyInWater();
            }
        }
    }
}
