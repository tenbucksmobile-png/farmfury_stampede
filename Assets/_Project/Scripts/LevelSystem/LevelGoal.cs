using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>End-of-level trigger. Completing scores the stars (StarCalculator) via GameManager.CompleteLevel.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelGoal : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentState != GameState.Playing
                || other.GetComponentInParent<CharacterController2D>() == null)
            {
                return;
            }

            gm.CompleteLevel();
        }
    }
}
