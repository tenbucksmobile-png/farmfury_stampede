using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>Trigger below the level: falling into it fails the level. Lives/respawn arrive in Phase 5.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class PitDeathZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentState != GameState.Playing
                || other.GetComponentInParent<CharacterController2D>() == null)
            {
                return;
            }

            gm.EndLevel(completed: false, stars: 0);
        }
    }
}
