using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// The beaten boss's dazed stars: a few small stars (DazedStar.png) circling over its head like a cartoon
    /// character that has been knocked silly. They orbit on a flat ellipse just above the top of the boss's art,
    /// drawn in front of it and a little bigger on the near half of the circle, behind it and smaller on the far
    /// half, each turning as it goes. They follow the art (the wreck drops to the ground) and go back to the pool
    /// when the boss does (the level unloads) or after <see cref="maxSeconds"/>. Runs on unscaled time, so the
    /// victory slow motion doesn't slow the whirl.
    /// </summary>
    [RequireComponent(typeof(PooledObject))]
    public class DazedStars : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer star;            // the first star; the others are copies of it
        [SerializeField] private int count = 5;
        [SerializeField] private float lapSeconds = 1.2f;
        [SerializeField] private float spinDegreesPerSecond = 200f;
        [SerializeField] private float aboveHead = 0.15f;
        [SerializeField] private float maxSeconds = 12f;

        private const float FadeInSeconds = 0.25f;
        private const float MinRadius = 0.7f, MaxRadius = 1.3f;
        private const float Flatten = 0.28f;                      // ellipse height / width

        private SpriteRenderer[] _stars;
        private SpriteRenderer _target;
        private Vector3 _fallbackCentre;
        private float _age;

        /// <summary>Starts circling over <paramref name="target"/>'s art (or <paramref name="fallback"/> without it).</summary>
        public void Play(SpriteRenderer target, Vector3 fallback)
        {
            BuildStars();
            _target = target;
            _fallbackCentre = fallback + Vector3.up * 2f;
            _age = 0f;
            Apply();
        }

        private void BuildStars()
        {
            if (_stars != null || star == null)
            {
                return;
            }
            _stars = new SpriteRenderer[Mathf.Max(1, count)];
            _stars[0] = star;
            for (int i = 1; i < _stars.Length; i++)
            {
                _stars[i] = Instantiate(star.gameObject, transform).GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            bool targetGone = _target != null && !_target.gameObject.activeInHierarchy;
            if (_age >= maxSeconds || targetGone)
            {
                ObjectPool.Instance.Release(gameObject);
                return;
            }
            Apply();
        }

        private void Apply()
        {
            if (_stars == null)
            {
                return;
            }

            Vector3 centre;
            float radius;
            int order = 10;
            if (_target != null)
            {
                Bounds b = _target.bounds;
                centre = new Vector3(b.center.x, b.max.y + aboveHead, 0f);
                radius = Mathf.Clamp(b.size.x * 0.35f, MinRadius, MaxRadius);
                order = _target.sortingOrder;
            }
            else
            {
                centre = _fallbackCentre;
                radius = MinRadius;
            }
            transform.position = centre;

            float alpha = Mathf.Clamp01(_age / FadeInSeconds);
            float turn = _age / lapSeconds * Mathf.PI * 2f;
            for (int i = 0; i < _stars.Length; i++)
            {
                float angle = turn + i * Mathf.PI * 2f / _stars.Length;
                float depth = -Mathf.Sin(angle);                     // +1 = the near side (lowest point of the ellipse)
                var t = _stars[i].transform;
                t.localPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * Flatten, 0f);
                t.localScale = Vector3.one * (0.85f + 0.2f * depth);
                t.localRotation = Quaternion.Euler(0f, 0f, _age * spinDegreesPerSecond + i * 40f);
                _stars[i].sortingOrder = order + (depth >= 0f ? 1 : -1);
                var c = _stars[i].color;
                c.a = alpha;
                _stars[i].color = c;
            }
        }
    }
}
