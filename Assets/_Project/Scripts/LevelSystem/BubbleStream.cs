using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Purely decorative rising bubbles (Sunken City): bubbles leave the object's position, rise to 'height' with a
    /// wobble, swell a little and fade out near the top, then start again at the bottom. Used for the air vents' columns
    /// (over an UpdraftZone, which does the lifting) and for thin ambient trails from the seabed. No collider. The
    /// bubbles are made once in Awake, as children, so a level prefab only stores the settings.
    /// </summary>
    public class BubbleStream : MonoBehaviour
    {
        public Sprite bubble;
        public int count = 12;
        public float height = 8f;
        [Tooltip("Spread of the starting x, centred on the object.")]
        public float width = 1.5f;
        public float minSpeed = 2.5f;
        public float maxSpeed = 4.5f;
        [Tooltip("Bubble diameter range in world units.")]
        public float minSize = 0.2f;
        public float maxSize = 0.45f;
        public float alpha = 0.85f;
        public int sortingOrder = 6;

        private Transform[] _bubbles;
        private SpriteRenderer[] _renderers;
        private float[] _speed, _size, _x, _phase, _y;

        private void Awake()
        {
            if (bubble == null || count <= 0)
            {
                enabled = false;
                return;
            }

            _bubbles = new Transform[count];
            _renderers = new SpriteRenderer[count];
            _speed = new float[count];
            _size = new float[count];
            _x = new float[count];
            _phase = new float[count];
            _y = new float[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Bubble");
                go.transform.SetParent(transform, false);
                _bubbles[i] = go.transform;
                _renderers[i] = go.AddComponent<SpriteRenderer>();
                _renderers[i].sprite = bubble;
                _renderers[i].sortingOrder = sortingOrder;
                Respawn(i);
                _y[i] = Random.Range(0f, height);   // start spread over the column, not all at the bottom
            }
        }

        private void Respawn(int i)
        {
            _y[i] = 0f;
            _x[i] = Random.Range(-width * 0.5f, width * 0.5f);
            _speed[i] = Random.Range(minSpeed, maxSpeed);
            _size[i] = Random.Range(minSize, maxSize);
            _phase[i] = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            float native = Mathf.Max(bubble.bounds.size.x, 0.01f);
            for (int i = 0; i < count; i++)
            {
                _y[i] += _speed[i] * Time.deltaTime;
                if (_y[i] >= height)
                {
                    Respawn(i);
                }

                float t = _y[i] / height;
                float wobble = Mathf.Sin(_phase[i] + _y[i] * 2.2f) * 0.18f;
                _bubbles[i].localPosition = new Vector3(_x[i] + wobble, _y[i], 0f);
                float size = _size[i] * Mathf.Lerp(0.7f, 1.15f, t);
                _bubbles[i].localScale = Vector3.one * (size / native);
                var c = _renderers[i].color;
                c.a = alpha * Mathf.Clamp01(Mathf.Min(t * 8f, (1f - t) * 5f));   // fade in at the vent, out near the top
                _renderers[i].color = c;
            }
        }
    }
}
