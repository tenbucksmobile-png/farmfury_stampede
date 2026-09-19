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
        /// <summary>Set by LevelLoader on each spawn: this crop belongs to the level's secret cluster.</summary>
        public bool isSecretCluster;

        private bool _collected;

        private void OnEnable()
        {
            _collected = false;
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
