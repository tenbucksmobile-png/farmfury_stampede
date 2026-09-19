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

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RunState.CollectCrop();
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
