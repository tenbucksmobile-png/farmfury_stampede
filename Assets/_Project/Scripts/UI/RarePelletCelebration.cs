using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Movement;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// What happens when a rare pellet is eaten (RarePelletPickup.Collected). Every pellet unlocks the next locked
    /// character, ability included (SaveManager.CollectRarePellet). The game freezes and the screen dims; the crystal
    /// apple flies from where it was eaten to the middle over a turning sunburst and "RARE PELLET!" pops in. The new
    /// character's card appears behind it as a dark silhouette, the apple sinks into it, and the card flashes white and
    /// is revealed in full colour with a bounce and confetti: "NEW CHARACTER!", "&lt;name&gt; unlocked!" and their
    /// ability. "PLAY AS &lt;name&gt;" swaps to them on the spot (GameManager.SwapCharacter); a tap anywhere else (after
    /// a short beat), Esc, or waiting closes it, and play resumes where it froze. With every character already
    /// unlocked it just says so. Everything runs on unscaled time.
    /// </summary>
    public class RarePelletCelebration : MonoBehaviour
    {
        public bool IsOpen => _root != null && _root.activeSelf;

        private const float AppleSize = 300f, CardSize = 380f;
        private static readonly Vector2 AppleCentre = new(0f, 40f), CardSpot = new(0f, 20f);
        private static readonly Color Silhouette = new(0.08f, 0.07f, 0.12f, 1f);
        private static readonly Color Gold = new(1f, 0.84f, 0.25f, 1f);
        private static readonly Color OutlineColor = new(0.24f, 0.11f, 0.03f, 1f);
        private const float MinShowSeconds = 1.2f;      // taps before this are ignored, so the moment isn't skipped by accident
        private const float AutoCloseSeconds = 6f;      // after the reveal

        private GameObject _root;
        private RectTransform _rect;
        private Image _dim, _rays, _apple, _card, _flash;
        private Text _title, _subtitle, _ability, _hint;
        private Button _play;
        private Text _playLabel;
        private ConfettiBurst _confetti;
        private Coroutine _routine;
        private bool _tapped, _playChosen;
        private float _shownAt;
        private CharacterType? _revealed;

        /// <summary>Builds the (hidden) overlay under the canvas and starts listening for pellets.</summary>
        public static RarePelletCelebration Create(Transform canvas, MenuArt art)
        {
            var rect = UIKit.NewRect("RarePelletCelebration", canvas);
            UIKit.Stretch(rect);
            var celebration = rect.gameObject.AddComponent<RarePelletCelebration>();
            celebration.Build(rect, art);
            return celebration;
        }

        private static readonly Vector2 Centre = new(0.5f, 0.5f);

        private void Build(RectTransform rect, MenuArt art)
        {
            _root = rect.gameObject;
            _rect = rect;

            _dim = _root.AddComponent<Image>();
            _dim.color = new Color(0f, 0f, 0f, 0f);
            var tap = _root.AddComponent<Button>();
            tap.targetGraphic = _dim;
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => _tapped = true);

            _rays = UIKit.Picture(rect, "Rays", SunburstSprite());
            _rays.gameObject.SetActive(true);
            UIKit.Place(_rays.rectTransform, Centre, Centre, AppleCentre, Vector2.one * 950f);

            _card = UIKit.Picture(rect, "Card", null);
            UIKit.Place(_card.rectTransform, Centre, Centre, CardSpot, Vector2.one * CardSize);
            _flash = UIKit.Picture(_card.transform, "Flash", null);
            UIKit.Stretch(_flash.rectTransform);

            _apple = UIKit.Picture(rect, "Apple", art != null ? art.rarePellet : null);
            _apple.gameObject.SetActive(true);
            _apple.rectTransform.sizeDelta = Vector2.one * AppleSize;

            _title = MakeText(rect, "Title", 110, new Vector2(0f, 360f), new Vector2(1600f, 150f));
            _subtitle = MakeText(rect, "Subtitle", 64, new Vector2(0f, -235f), new Vector2(1600f, 90f));
            _ability = MakeText(rect, "Ability", 44, new Vector2(0f, -305f), new Vector2(1600f, 64f));
            _ability.color = Color.white;
            _hint = MakeText(rect, "TapHint", 34, new Vector2(0f, -480f), new Vector2(900f, 56f));
            _hint.color = new Color(1f, 1f, 1f, 0.8f);
            _hint.text = "Tap to continue";

            _play = UIKit.MakeButton(rect, "PlayAsButton", "", Gold, () => { _playChosen = true; _tapped = true; }, 46);
            _playLabel = _play.GetComponentInChildren<Text>();
            _playLabel.color = new Color(0.24f, 0.11f, 0.03f, 1f);
            _playLabel.fontStyle = FontStyle.Bold;
            UIKit.Place(_play.image.rectTransform, Centre, Centre, new Vector2(0f, -395f), new Vector2(560f, 96f));

            var particles = UIKit.NewRect("Confetti", rect);
            UIKit.Stretch(particles);
            _confetti = particles.gameObject.AddComponent<ConfettiBurst>();
            _confetti.particlesRoot = particles;

            _root.SetActive(false);
            RarePelletPickup.Collected += OnCollected;
        }

        private Text MakeText(Transform parent, string textName, int size, Vector2 position, Vector2 box)
        {
            var text = UIKit.Label(parent, textName, "", size, TextAnchor.MiddleCenter, Gold);
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(4f, -4f);
            outline.effectColor = OutlineColor;
            UIKit.Place(text.rectTransform, Centre, Centre, position, box);
            return text;
        }

        private void OnDestroy()
        {
            RarePelletPickup.Collected -= OnCollected;
        }

        private void OnCollected(Vector3 worldPosition, List<CharacterType> unlocked)
        {
            if (_routine != null) { StopCoroutine(_routine); }
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            PlayerInputReader.ReleaseTouch();   // the HUD buttons are covered; nothing stays held through the freeze
            Time.timeScale = 0f;
            _routine = StartCoroutine(Play(ScreenPoint(worldPosition), unlocked));
        }

        /// <summary>Hurries it to the end (Esc): the same as a tap.</summary>
        public void Skip() => _tapped = true;

        /// <summary>DEBUG / tests: presses "PLAY AS" once it is showing.</summary>
        public void ChoosePlayAs() => _play.onClick.Invoke();

        /// <summary>The character the reveal showed (null before a reveal, or when none was left to unlock).</summary>
        public CharacterType? Revealed => _revealed;

        /// <summary>Hides it at once without touching the time scale (the game left the level; the new state owns it).</summary>
        public void Close()
        {
            if (!IsOpen) { return; }
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            ResetAlpha();
            _root.SetActive(false);
        }

        // Where the pellet was, in this overlay's local coordinates.
        private Vector2 ScreenPoint(Vector3 world)
        {
            var cam = Camera.main;
            if (cam == null) { return AppleCentre; }
            Vector2 screen = cam.WorldToScreenPoint(world);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, null, out var local) ? local : AppleCentre;
        }

        private IEnumerator Play(Vector2 from, List<CharacterType> unlocked)
        {
            _tapped = false;
            _playChosen = false;
            _revealed = null;
            _shownAt = Time.unscaledTime;

            // Reset.
            ResetAlpha();
            _title.text = "RARE PELLET!";
            _subtitle.text = "";
            _ability.text = "";
            SetAlpha(_title, 0f); SetAlpha(_subtitle, 0f); SetAlpha(_ability, 0f); SetAlpha(_hint, 0f);
            _card.gameObject.SetActive(false);
            _play.gameObject.SetActive(false);
            _flash.color = new Color(1f, 1f, 1f, 0f);
            _rays.color = new Color(1f, 0.9f, 0.5f, 0f);
            _rays.rectTransform.anchoredPosition = AppleCentre;
            _apple.rectTransform.anchoredPosition = from;
            _apple.rectTransform.localScale = Vector3.one * 0.25f;
            _apple.color = Color.white;
            _apple.rectTransform.localRotation = Quaternion.identity;

            // 1. Dim, and the apple flies from where it was eaten to the middle, growing, over the turning rays.
            yield return Animate(0.7f, t =>
            {
                _dim.color = new Color(0f, 0f, 0f, 0.72f * Mathf.Clamp01(t * 2f));
                _apple.rectTransform.anchoredPosition = Vector2.LerpUnclamped(from, AppleCentre, EaseOut(t));
                _apple.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0.25f, 1f, EaseOutBack(t));
                _apple.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 3f) * 12f * (1f - t));
                _rays.color = new Color(1f, 0.9f, 0.5f, 0.55f * t);
            });

            // 2. Title pops in.
            yield return Animate(0.35f, t =>
            {
                SetAlpha(_title, Mathf.Clamp01(t * 2f));
                _title.rectTransform.localScale = Vector3.one * EaseOutBack(t);
            });
            yield return Wait(0.3f);

            // 3. The character it unlocked (normally exactly one), revealed in turn.
            var reveals = new List<CharacterData>();
            foreach (var type in unlocked)
            {
                var data = DataManager.Instance != null ? DataManager.Instance.GetCharacterData(type) : null;
                if (data != null) { reveals.Add(data); }
            }

            if (reveals.Count == 0)
            {
                _subtitle.text = "Every character is unlocked!";
                yield return Animate(0.3f, t => SetAlpha(_subtitle, t));
            }
            for (int i = 0; i < reveals.Count; i++)
            {
                yield return Reveal(reveals[i]);
                if (i < reveals.Count - 1) { yield return WaitForTapOrTime(2.5f); }
            }

            // 4. Tap to continue, "PLAY AS", or it closes itself.
            yield return Animate(0.3f, t => SetAlpha(_hint, t * 0.8f));
            yield return WaitForTapOrTime(AutoCloseSeconds);

            // 5. Fade out and resume, as the new character if "PLAY AS" was chosen.
            yield return Animate(0.25f, t =>
            {
                float a = 1f - t;
                _dim.color = new Color(0f, 0f, 0f, 0.72f * a);
                foreach (var g in _root.GetComponentsInChildren<Graphic>())
                {
                    if (g != _dim) { g.canvasRenderer.SetAlpha(a); }
                }
            });
            ResetAlpha();
            _root.SetActive(false);
            _routine = null;

            var gm = GameManager.Instance;
            if (gm != null && gm.CurrentState == GameState.Playing && !gm.ReviveDecisionPending)
            {
                if (_playChosen && _revealed.HasValue) { gm.SwapCharacter(_revealed.Value); }
                Time.timeScale = 1f;
            }
        }

        // The card appears as a silhouette behind the apple, the apple sinks into it, and it is revealed in colour.
        private IEnumerator Reveal(CharacterData data)
        {
            _card.sprite = data.selectCard != null ? data.selectCard : data.placeholderSprite;
            _card.color = Silhouette;
            _card.gameObject.SetActive(true);
            _card.transform.SetSiblingIndex(_apple.transform.GetSiblingIndex());   // behind the apple
            _flash.sprite = _card.sprite;
            _flash.preserveAspect = true;
            _flash.gameObject.SetActive(_card.sprite != null);
            _flash.color = new Color(1f, 1f, 1f, 0f);
            _apple.color = Color.white;
            _apple.rectTransform.localScale = Vector3.one;
            _apple.rectTransform.anchoredPosition = AppleCentre;

            yield return Animate(0.4f, t =>
            {
                _card.canvasRenderer.SetAlpha(t);
                _card.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, EaseOutBack(t));
            });
            yield return Wait(0.25f);

            // The apple sinks into the card...
            yield return Animate(0.35f, t =>
            {
                _apple.rectTransform.anchoredPosition = Vector2.Lerp(AppleCentre, CardSpot, t);
                _apple.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.1f, t * t);
            });
            _apple.color = new Color(1f, 1f, 1f, 0f);

            // ...which flashes white and is revealed with a bounce and confetti.
            _revealed = data.characterType;
            _flash.color = Color.white;
            _card.color = Color.white;
            _title.text = "NEW CHARACTER!";
            _subtitle.text = $"{ShortName(data)} unlocked!";
            _ability.text = $"Ability: {Spaced(data.abilityType.ToString())}";
            _playLabel.text = $"PLAY AS {ShortName(data).ToUpperInvariant()}";
            _play.gameObject.SetActive(true);
            _confetti.transform.SetAsLastSibling();
            _confetti.Burst(110, 3f);
            yield return Animate(0.55f, t =>
            {
                _flash.color = new Color(1f, 1f, 1f, 1f - t);
                _card.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, EaseOutBack(t));
                _title.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, t);
                SetAlpha(_subtitle, t);
                SetAlpha(_ability, t);
                _play.image.canvasRenderer.SetAlpha(t);
                _playLabel.canvasRenderer.SetAlpha(t);
            });
        }

        // "Percy the Pig" -> "Percy".
        private static string ShortName(CharacterData data)
        {
            string name = string.IsNullOrEmpty(data.displayName) ? data.characterType.ToString() : data.displayName;
            int space = name.IndexOf(' ');
            return space > 0 ? name.Substring(0, space) : name;
        }

        // "RollDash" -> "Roll Dash".
        private static string Spaced(string pascal) => Regex.Replace(pascal, "(?<=[a-z])(?=[A-Z])", " ");

        private static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }

        private void ResetAlpha()
        {
            foreach (var g in _root.GetComponentsInChildren<Graphic>(true)) { g.canvasRenderer.SetAlpha(1f); }
        }

        private void LateUpdate()
        {
            if (IsOpen)
            {
                _rays.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -Time.unscaledTime * 25f);
            }
        }

        private IEnumerator Animate(float seconds, System.Action<float> step)
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
            {
                step(t);
                yield return null;
            }
            step(1f);
        }

        private IEnumerator Wait(float seconds)
        {
            for (float end = Time.unscaledTime + seconds; Time.unscaledTime < end;) { yield return null; }
        }

        private IEnumerator WaitForTapOrTime(float seconds)
        {
            _tapped = false;
            for (float end = Time.unscaledTime + seconds; Time.unscaledTime < end;)
            {
                if (_tapped && (_playChosen || Time.unscaledTime - _shownAt >= MinShowSeconds)) { break; }
                _tapped = false;
                yield return null;
            }
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        // Soft golden rays turning behind the apple, made once.
        private static Sprite _sunburst;

        private static Sprite SunburstSprite()
        {
            if (_sunburst != null) { return _sunburst; }
            const int size = 256, rays = 14;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - size * 0.5f, dy = y + 0.5f - size * 0.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) / (size * 0.5f);
                    float angle = Mathf.Atan2(dy, dx);
                    float ray = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * rays * 0.5f)), 6f);
                    float fade = Mathf.Clamp01(1f - r);
                    float glow = Mathf.Clamp01(1f - r * 2.2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(ray * fade * 0.9f + glow * 0.8f)));
                }
            }
            tex.Apply();
            _sunburst = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _sunburst;
        }
    }
}
