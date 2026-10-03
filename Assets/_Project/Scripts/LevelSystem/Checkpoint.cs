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
        [Tooltip("Real art swap for the idle/active states; if either is unassigned, falls back to tinting the placeholder square.")]
        [SerializeField] private Sprite inactiveSprite;
        [SerializeField] private Sprite activeSprite;
        [SerializeField] private Color inactiveColor = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private Color activeColor = new Color(0.3f, 0.9f, 0.35f);
        [Tooltip("Height above the pole base where the player reappears.")]
        [SerializeField] private float respawnHeight = 0.6f;

        private static readonly Color ReachedGlow = new(1f, 0.82f, 0.35f);

        private bool _active;
        private Sprite _worldArt;   // a world's own single-frame art (SetWorldArt); null = the signpost pair

        private void OnEnable()
        {
            _active = false;
            _worldArt = null;
            SetFlagState(inactiveSprite, inactiveColor);
        }

        /// <summary>
        /// Gives this spawn a world's own checkpoint art (one frame: shown plain, tinted gold once reached); null keeps
        /// the signpost. Call right after spawning (the pool resets it on every spawn).
        /// </summary>
        public void SetWorldArt(Sprite art)
        {
            _worldArt = art;
            if (art != null && flag != null)
            {
                flag.sprite = art;
                flag.color = Color.white;
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
            if (_worldArt != null && flag != null) { flag.color = ReachedGlow; }
            else { SetFlagState(activeSprite, activeColor); }

            Vector2 respawn = (Vector2)transform.position + Vector2.up * respawnHeight;
            gm.RunState.SetCheckpoint(respawn);
            Debug.Log($"[Checkpoint] Activated at {respawn}.");
        }

        private void SetFlagState(Sprite sprite, Color fallbackColor)
        {
            if (flag == null)
            {
                return;
            }

            if (sprite != null)
            {
                flag.sprite = sprite;
                flag.color = Color.white;
            }
            else
            {
                flag.color = fallbackColor;
            }
        }
    }
}
