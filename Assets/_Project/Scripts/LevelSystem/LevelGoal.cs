using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>End-of-level trigger. Stars are a placeholder (always 1) until Phase 5 builds scoring.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelGoal : MonoBehaviour
    {
        private const int PlaceholderStars = 1;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentState != GameState.Playing
                || other.GetComponentInParent<CharacterController2D>() == null)
            {
                return;
            }

            gm.EndLevel(completed: true, stars: PlaceholderStars);
        }
    }
}
