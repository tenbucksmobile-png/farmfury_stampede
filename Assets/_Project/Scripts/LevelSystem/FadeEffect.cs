using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// A one-shot visual (Bessie's ground-pound impact): grows from startScale to endScale while fading out over
    /// duration, then returns to the pool. Purely visual; no collider.
    /// </summary>
    [RequireComponent(typeof(PooledObject))]
    public class FadeEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private float duration = 0.35f;
        [SerializeField] private float startScale = 0.6f;
        [SerializeField] private float endScale = 1.1f;

        private float _time;
        private float _size = 1f;

        private void OnEnable()
        {
            _time = 0f;
            _size = 1f;
            Apply(0f);
        }

        /// <summary>Restarts the effect drawn 'size' times its normal size (call right after taking it from the pool).</summary>
        public void Play(float size)
        {
            _time = 0f;
            _size = size;
            Apply(0f);
        }

        private void Update()
        {
            _time += Time.deltaTime;
            float t = Mathf.Clamp01(_time / duration);
            Apply(t);
            if (t >= 1f)
            {
                ObjectPool.Instance.Release(gameObject);
            }
        }

        private void Apply(float t)
        {
            transform.localScale = Vector3.one * (Mathf.Lerp(startScale, endScale, t) * _size);
            if (visual != null)
            {
                var c = visual.color;
                c.a = 1f - t * t;
                visual.color = c;
            }
        }
    }
}
