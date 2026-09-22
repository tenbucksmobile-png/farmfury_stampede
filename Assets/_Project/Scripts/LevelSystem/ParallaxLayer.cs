using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// A single parallax background layer. <see cref="Configure"/> stretches the layer so it fully
    /// covers a level's camera-visible span at every point along the horizontal pan, then each frame
    /// offsets it by a fraction of how far the camera has moved from the level's centre. A small
    /// <c>parallaxFactor</c> reads as "distant" (barely moves); a factor near 1 reads as "close"
    /// (moves almost with the camera). Orthographic 2D has no depth-based parallax of its own, so this
    /// is done by hand rather than by placing layers at different Z depths.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("0 = fixed relative to the world (barely moves, reads as distant); 1 = moves exactly with the camera.")]
        [SerializeField, Range(0f, 1f)] private float parallaxFactor = 0.2f;

        private SpriteRenderer _renderer;
        private Transform _camera;
        private float _levelCenterX;
        private float _baseY;
        private bool _configured;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Sizes and centres this layer so it stays edge-to-edge behind the camera across the level's
        /// full [minX, maxX] span, then anchors its parallax motion around the level's horizontal centre.
        /// </summary>
        public void Configure(Camera targetCamera, float minX, float maxX, float baseY)
        {
            _camera = targetCamera.transform;
            _baseY = baseY;
            _levelCenterX = (minX + maxX) * 0.5f;

            float halfCameraWidth = targetCamera.orthographicSize * targetCamera.aspect;
            float levelWidth = Mathf.Max(maxX - minX, 0.01f);

            // The layer moves only `parallaxFactor` as far as the camera, so relative to the camera it
            // "slides" by levelWidth * (1 - parallaxFactor) over a full pan from minX to maxX. The layer
            // needs to be that wide plus one full screen width so its edges never show; a 5% margin
            // covers float slop at the extremes.
            float requiredWidth = (halfCameraWidth * 2f + levelWidth * (1f - parallaxFactor)) * 1.05f;

            Vector2 nativeSize = _renderer.sprite.bounds.size;
            float scale = requiredWidth / Mathf.Max(nativeSize.x, 0.01f);
            transform.localScale = new Vector3(scale, scale, 1f); // uniform, so the art never looks squashed

            _configured = true;
            transform.position = new Vector3(_levelCenterX, _baseY, transform.position.z);
        }

        private void LateUpdate()
        {
            if (!_configured || _camera == null)
            {
                return;
            }

            float delta = (_camera.position.x - _levelCenterX) * parallaxFactor;
            transform.position = new Vector3(_levelCenterX + delta, _baseY, transform.position.z);
        }
    }
}
