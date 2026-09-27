using System.Collections;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>Arcade's machine exhaust: a grey puff that drifts up, grows and fades. Procedural circle until smoke art exists.</summary>
    public class MachineSmokePuff : MonoBehaviour
    {
        private static readonly Color SmokeColor = new(0.55f, 0.55f, 0.55f, 0.6f);
        private const float StartScale = 0.35f;
        private const float EndScaleMultiplier = 1.9f;
        private const float DriftUpDistance = 0.3f;
        private static Sprite _circle;

        public void Configure(int sortingLayerID, int sortingOrder, float lifetimeSeconds)
        {
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Circle();
            sr.color = SmokeColor;
            sr.sortingLayerID = sortingLayerID;
            sr.sortingOrder = sortingOrder;
            transform.localScale = Vector3.one * StartScale;
            StartCoroutine(DriftFadeAndDestroy(sr, lifetimeSeconds));
        }

        private static Sprite Circle()
        {
            if (_circle != null)
            {
                return _circle;
            }

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
                }
            }
            texture.Apply();
            _circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _circle;
        }

        private IEnumerator DriftFadeAndDestroy(SpriteRenderer sr, float lifetimeSeconds)
        {
            Vector3 startScale = transform.localScale;
            Vector3 startPos = transform.position;
            float t = 0f;
            while (t < lifetimeSeconds)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / lifetimeSeconds);
                transform.position = startPos + Vector3.up * (DriftUpDistance * progress);
                transform.localScale = Vector3.Lerp(startScale, startScale * EndScaleMultiplier, progress);
                Color c = sr.color;
                c.a = Mathf.Lerp(SmokeColor.a, 0f, progress);
                sr.color = c;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
