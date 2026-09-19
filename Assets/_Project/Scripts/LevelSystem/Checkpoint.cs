using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Pooled checkpoint. Touching it makes it the respawn point (the most recently touched checkpoint wins).
    /// Each checkpoint activates once per spawn.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(PooledObject))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer flag;
        [SerializeField] private Color inactiveColor = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private Color activeColor = new Color(0.3f, 0.9f, 0.35f);
        [Tooltip("Height above the pole base where the player reappears.")]
        [SerializeField] private float respawnHeight = 0.6f;

        private bool _active;

        private void OnEnable()
        {
            _active = false;
            if (flag != null)
            {
                flag.color = inactiveColor;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var gm = GameManager.Instance;
            if (_active || gm == null || gm.CurrentState != GameState.Playing
                || other.GetComponentInParent<CharacterController2D>() == null)
            {
                return;
            }

            _active = true;
            if (flag != null)
            {
                flag.color = activeColor;
            }

            Vector2 respawn = (Vector2)transform.position + Vector2.up * respawnHeight;
            gm.RunState.SetCheckpoint(respawn);
            Debug.Log($"[Checkpoint] Activated at {respawn}.");
        }
    }
}
