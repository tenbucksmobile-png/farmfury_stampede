using FarmFuryStampede.Core;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Trigger below the level: falling into it respawns the player at the last checkpoint (or the level
    /// start). Supersedes Phase 2's EndLevel(false); there is no lives/retry limit until Phase 5/6.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PitDeathZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (GameManager.Instance == null || other.GetComponentInParent<CharacterController2D>() == null)
            {
                return;
            }

            GameManager.Instance.RespawnPlayer();
        }
    }
}
