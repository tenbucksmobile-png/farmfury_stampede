using FarmFuryStampede.Core;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>Collectible crop. On touch it counts toward the run and returns to the object pool.</summary>
    [RequireComponent(typeof(Collider2D), typeof(PooledObject))]
    public class CropPickup : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [Tooltip("Picked at random for a normal crop when real art is assigned; falls back to the placeholder sprite if empty.")]
        [SerializeField] private Sprite[] normalSprites;
        [Tooltip("Picked at random for a secret-cluster crop when real art is assigned.")]
        [SerializeField] private Sprite[] secretSprites;

        private bool _isSecretCluster;
        private bool _collected;

        /// <summary>Set by LevelLoader on each spawn: this crop belongs to the level's secret cluster.</summary>
        public bool isSecretCluster
        {
            get => _isSecretCluster;
            set
            {
                _isSecretCluster = value;
                ApplyVisual();
            }
        }

        private void OnEnable()
        {
            _collected = false;
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (visual == null)
            {
                return;
            }

            Sprite[] pool = _isSecretCluster && secretSprites != null && secretSprites.Length > 0 ? secretSprites : normalSprites;
            if (pool != null && pool.Length > 0)
            {
                visual.sprite = pool[Random.Range(0, pool.Length)];
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || other.GetComponentInParent<CharacterController2D>() == null)
            {
                return;
            }

            _collected = true;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.RunState.CollectCrop(isSecretCluster);

                // Finding a character-gated secret is remembered for the Level Select "undiscovered secret" icon.
                if (isSecretCluster && gm.CurrentLevel != null && gm.CurrentLevel.hasCharacterGatedSecret && SaveManager.Instance != null)
                {
                    SaveManager.Instance.MarkSecretFound(gm.CurrentLevel.levelId);
                }
            }

            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.Release(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
