using System;
using System.Collections;
using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// The level's rare pellet (the crystal apple, RarePellets_apple.png): at most one per level, somewhere every
    /// character can reach (LevelBuilder.RarePellet), baked into the level prefab. It bobs, pulses a soft glow and
    /// twinkles so it reads as "take me, something will happen". Any character can eat it: that saves it
    /// (SaveManager.CollectRarePellet, once per level), unlocks the next locked character, and raises
    /// <see cref="Collected"/>, which the UI turns into the celebration (RarePelletCelebration). A pellet already
    /// found shows as a faint ghost that does nothing, so the player can see this level's pellet is done.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class RarePelletPickup : MonoBehaviour
    {
        /// <summary>Raised when a new pellet is eaten: where it was (world), and the characters it unlocked (may be empty).</summary>
        public static event Action<Vector3, List<CharacterType>> Collected;

        [SerializeField] private SpriteRenderer visual;

        private const float BobHeight = 0.18f, BobSpeed = 2.2f;
        private const float PulseAmount = 0.06f, PulseSpeed = 3.1f;
        private const float GlowSize = 2.4f;   // world units across the halo
        private const int SparkleCount = 4;
        private const float GhostAlpha = 0.3f;

        private static Sprite _glowSprite, _sparkleSprite;

        private Vector3 _home, _baseScale;
        private SpriteRenderer _glow;
        private readonly List<SpriteRenderer> _sparkles = new();
        private bool _collected, _ghost;
        private float _phase;

        private void Start()
        {
            if (visual == null) { visual = GetComponentInChildren<SpriteRenderer>(); }
            _home = visual.transform.localPosition;
            _baseScale = visual.transform.localScale;
            _phase = UnityEngine.Random.value * 10f;

            string levelId = GameManager.Instance != null && GameManager.Instance.CurrentLevel != null
                ? GameManager.Instance.CurrentLevel.levelId : null;
            _ghost = levelId != null && SaveManager.Instance != null && SaveManager.Instance.IsRarePelletFound(levelId);
            if (_ghost)
            {
                visual.color = new Color(1f, 1f, 1f, GhostAlpha);
                return;
            }

            _glow = MakeChild("Glow", GlowSprite(), visual.sortingOrder - 1, GlowSize);
            for (int i = 0; i < SparkleCount; i++)
            {
                _sparkles.Add(MakeChild($"Sparkle{i}", SparkleSprite(), visual.sortingOrder + 1, 0.45f));
            }
        }

        private void Update()
        {
            if (_collected) { return; }

            float t = Time.time + _phase;
            visual.transform.localPosition = _home + Vector3.up * (Mathf.Sin(t * BobSpeed) * BobHeight);
            if (_ghost) { return; }

            float pulse = 1f + Mathf.Sin(t * PulseSpeed) * PulseAmount;
            visual.transform.localScale = _baseScale * pulse;
            _glow.transform.localPosition = visual.transform.localPosition;
            _glow.color = new Color(1f, 0.92f, 0.65f, 0.45f + 0.2f * Mathf.Sin(t * PulseSpeed));

            // Sparkles circle the apple, each twinkling on its own beat, in rainbow tints like the crystal.
            for (int i = 0; i < _sparkles.Count; i++)
            {
                float a = t * 1.3f + i * Mathf.PI * 2f / _sparkles.Count;
                var s = _sparkles[i];
                s.transform.localPosition = visual.transform.localPosition + new Vector3(Mathf.Cos(a) * 0.75f, Mathf.Sin(a) * 0.6f, 0f);
                float twinkle = Mathf.Clamp01(Mathf.Sin(t * 4f + i * 1.7f));
                s.transform.localScale = Vector3.one * (0.15f + 0.3f * twinkle);
                s.transform.localRotation = Quaternion.Euler(0f, 0f, t * 90f);
                s.color = Color.HSVToRGB(Mathf.Repeat(t * 0.15f + i * 0.25f, 1f), 0.35f, 1f) * new Color(1f, 1f, 1f, twinkle);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || _ghost || other.GetComponentInParent<CharacterController2D>() == null)
            {
                return;
            }

            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentState != GameState.Playing || gm.CurrentLevel == null || SaveManager.Instance == null)
            {
                return;
            }

            _collected = true;
            var unlocked = SaveManager.Instance.CollectRarePellet(gm.CurrentLevel.levelId);
            Collected?.Invoke(visual.transform.position, unlocked);
            StartCoroutine(Pop());
        }

        // Swells and fades out; unscaled, since the celebration freezes the game while this plays behind it.
        private IEnumerator Pop()
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            var start = visual.transform.localScale;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                visual.transform.localScale = start * (1f + t * 0.8f);
                foreach (var r in renderers)
                {
                    var c = r.color;
                    c.a = Mathf.Min(c.a, 1f - t);
                    r.color = c;
                }
                yield return null;
            }
            gameObject.SetActive(false);
        }

        private SpriteRenderer MakeChild(string childName, Sprite sprite, int order, float worldSize)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            go.transform.localScale = Vector3.one * worldSize;
            return r;
        }

        // A soft round halo, 1 unit across, made once.
        private static Sprite GlowSprite()
        {
            if (_glowSprite != null) { return _glowSprite; }
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) / (size * 0.5f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            tex.Apply();
            _glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _glowSprite;
        }

        // A four-point twinkle star, 1 unit across, made once.
        internal static Sprite SparkleSprite()
        {
            if (_sparkleSprite != null) { return _sparkleSprite; }
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - size * 0.5f) / (size * 0.5f);
                    float dy = Mathf.Abs(y + 0.5f - size * 0.5f) / (size * 0.5f);
                    // Two thin crossed rays plus a bright core.
                    float ray = Mathf.Max(Mathf.Clamp01(1f - dx * 8f) * (1f - dy), Mathf.Clamp01(1f - dy * 8f) * (1f - dx));
                    float core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 3f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(ray + core)));
                }
            }
            tex.Apply();
            _sparkleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _sparkleSprite;
        }
    }
}
