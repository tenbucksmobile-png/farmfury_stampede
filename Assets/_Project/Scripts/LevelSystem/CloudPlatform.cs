using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Woolly's temporary wool platform. Sits on the Ground layer so the controller treats it as floor; expires
    /// after a few seconds (blinking near the end) and returns to the pool.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(PooledObject))]
    public class CloudPlatform : MonoBehaviour
    {
        public const float Thickness = 0.3f;

        [SerializeField] private float lifetime = 3f;
        [SerializeField] private SpriteRenderer visual;

        private float _timeLeft;

        /// <summary>Starts the expiry countdown. Called right after the cloud is taken from the pool.</summary>
        public void Begin()
        {
            _timeLeft = lifetime;
            if (visual != null)
            {
                visual.enabled = true;
            }
            Physics2D.SyncTransforms();
        }

        private void Update()
        {
            _timeLeft -= Time.deltaTime;
            if (visual != null && _timeLeft < 0.6f)
            {
                visual.enabled = ((int)(Time.time * 14f) % 2) == 0;
            }

            if (_timeLeft <= 0f)
            {
                ObjectPool.Instance.Release(gameObject);
            }
        }
    }
}
