using System.Collections;
using System.Collections.Generic;
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
    /// character, ability included (SaveManager.CollectRarePellet). The game freezes and the New Character page
    /// (NewCharacterPage, the 2026-09-29 mockup: sunset backdrop, "New Character" lettering, the framed card) fades in;
    /// the card appears as a dark silhouette, flashes white and is revealed in colour with a bounce, golden rays
    /// turning behind it and confetti. There is no other text or button: the page stays until the player taps (or
    /// presses Esc), after a short beat so it isn't skipped by accident, and play resumes where it froze. Several
    /// unlocks are shown one per tap. With every character already unlocked it says so. Unscaled time throughout.
    /// </summary>
    public class RarePelletCelebration : MonoBehaviour
    {
        public bool IsOpen => _root != null && _root.activeSelf;

        private static readonly Color Silhouette = new(0.08f, 0.07f, 0.12f, 1f);
        private const float MinShowSeconds = 1.2f;      // taps before this are ignored, so the moment isn't skipped by accident
        private const float VeilAlpha = 0.85f;          // without the backdrop art

        private GameObject _root;
        private Image _veil;
        private CanvasGroup _group;
        private NewCharacterPage _page;
        private Coroutine _routine;
        private bool _tapped;
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

        private void Build(RectTransform rect, MenuArt art)
        {
            _root = rect.gameObject;
            _group = _root.AddComponent<CanvasGroup>();
            _veil = _root.AddComponent<Image>();
            var tap = _root.AddComponent<Button>();
            tap.targetGraphic = _veil;
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => _tapped = true);

            _page = new NewCharacterPage(rect, art);
            // Behind the (letterboxed) backdrop: black, so the page reads as its own screen; without art, a dark veil.
            _veil.color = _page.HasBackdrop ? Color.black : new Color(0f, 0f, 0f, VeilAlpha);

            _root.SetActive(false);
            RarePelletPickup.Collected += OnCollected;
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
            _routine = StartCoroutine(Play(unlocked));
        }

        /// <summary>Esc: the same as a tap.</summary>
        public void Skip() => _tapped = true;

        /// <summary>The character the reveal showed (null before a reveal, or when none was left to unlock).</summary>
        public CharacterType? Revealed => _revealed;

        /// <summary>Hides it at once without touching the time scale (the game left the level; the new state owns it).</summary>
        public void Close()
        {
            if (!IsOpen) { return; }
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            _root.SetActive(false);
        }

        private IEnumerator Play(List<CharacterType> unlocked)
        {
            _tapped = false;
            _revealed = null;
            _shownAt = Time.unscaledTime;

            _group.alpha = 0f;
            _page.Card.gameObject.SetActive(false);
            _page.Message.text = "";
            _page.Rays.canvasRenderer.SetAlpha(0f);
            _page.Title.rectTransform.localScale = Vector3.zero;

            // 1. The page fades in and the lettering pops.
            yield return Animate(0.3f, t => _group.alpha = t);
            yield return Animate(0.35f, t => _page.Title.rectTransform.localScale = Vector3.one * EaseOutBack(t));

            // 2. The character it unlocked (normally exactly one), revealed in turn, one per tap.
            var reveals = new List<CharacterData>();
            foreach (var type in unlocked)
            {
                var data = DataManager.Instance != null ? DataManager.Instance.GetCharacterData(type) : null;
                if (data != null) { reveals.Add(data); }
            }

            if (reveals.Count == 0)
            {
                _page.Message.text = "Every character is unlocked!";
            }
            for (int i = 0; i < reveals.Count; i++)
            {
                if (i > 0) { yield return WaitForTap(); }
                yield return Reveal(reveals[i]);
            }

            // 3. Stays until tapped, then fades out and play resumes.
            yield return WaitForTap();
            yield return Animate(0.25f, t => _group.alpha = 1f - t);
            _root.SetActive(false);
            _routine = null;

            var gm = GameManager.Instance;
            if (gm != null && gm.CurrentState == GameState.Playing && !gm.ReviveDecisionPending)
            {
                Time.timeScale = 1f;
            }
        }

        // The card appears as a silhouette, then flashes white and is revealed with a bounce, rays and confetti.
        private IEnumerator Reveal(CharacterData data)
        {
            _page.SetCharacter(data, data.characterType.ToString());
            var card = _page.Card;
            card.color = card.sprite != null ? Silhouette : card.color;
            yield return Animate(0.4f, t =>
            {
                card.canvasRenderer.SetAlpha(t);
                card.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, EaseOutBack(t));
            });
            yield return Wait(0.35f);

            _revealed = data.characterType;
            if (card.sprite != null) { card.color = Color.white; }
            _page.Flash.color = Color.white;
            _page.Burst();
            yield return Animate(0.55f, t =>
            {
                _page.Flash.color = new Color(1f, 1f, 1f, 1f - t);
                card.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, EaseOutBack(t));
                _page.Rays.canvasRenderer.SetAlpha(t);
            });
        }

        private void LateUpdate()
        {
            if (IsOpen) { _page.Spin(); }
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

        // Only a tap (or Esc) moves on; one before MinShowSeconds is ignored.
        private IEnumerator WaitForTap()
        {
            _tapped = false;
            while (!(_tapped && Time.unscaledTime - _shownAt >= MinShowSeconds))
            {
                _tapped = false;
                yield return null;
            }
            _tapped = false;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
