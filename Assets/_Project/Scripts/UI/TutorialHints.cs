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
    /// Beginner tips on Meadow Ruins 1-4 (2026-11-19, ported from Farm Fury Cannonfire's TutorialHints): a speech bubble
    /// at the top of the screen and a cartoon pointing hand (Resources/Hint/HintHand.png, Cannonfire's Kling art) aimed
    /// at the HUD control the tip is about. Nothing on any other level.
    ///   MeadowRuins_01  "Hold the arrows to run" (hand on the arrows) until the player has run a few units, then
    ///                   "Tap here to jump - tap again in the air to double jump!" (hand on jump) until they jump,
    ///                   then "Jump on top of a robot to stomp it!" for a few seconds.
    ///   MeadowRuins_02  the played character's ability (hand on the ability card) until it is used.
    ///   MeadowRuins_03  "Press here to change characters" (hand on the swap button) until the picker opens or 8 s.
    ///   MeadowRuins_04  the secret sign just ahead of the start (hand over it) until the player goes down or 10 s.
    /// Any level: the first time this session the player swaps to an animal, the hand points at the ability button and
    /// the bubble says what that animal's ability does (5 s or until used).
    /// Off phones the lines also name the keys (A/D or arrows, Space, K/J/Shift, Tab).
    /// Restarts on every load of those levels (LevelLoader.LevelLoaded), so a retry shows it again. Its own overlay
    /// canvas (order 20, over the UI canvas) is shown only while a level is being played and time runs, so it never
    /// draws over Pause, the swap picker, the revive prompt or the results.
    /// </summary>
    public class TutorialHints : MonoBehaviour
    {
        private HudScreen _hud;
        private Canvas _canvas;
        private RectTransform _bubble;
        private Text _text;
        private Image _hand;
        private Coroutine _routine;
        private static readonly Vector3[] Corners = new Vector3[4];
        private CharacterType? _lastCharacter;
        private static readonly HashSet<CharacterType> _abilityShown = new();   // this session

        private const float HandHeight = 150f;

        public static TutorialHints Create(Transform parent, HudScreen hud)
        {
            var go = new GameObject("TutorialHints");
            go.transform.SetParent(parent, false);
            var hints = go.AddComponent<TutorialHints>();
            hints._hud = hud;
            hints.Build();
            return hints;
        }

        private void OnEnable() => LevelLoader.LevelLoaded += OnLevelLoaded;
        private void OnDisable() => LevelLoader.LevelLoaded -= OnLevelLoaded;

        private static bool Keys => !Application.isMobilePlatform && !Application.isEditor;

        private void OnLevelLoaded(LevelData level)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            HideAll();
            IEnumerator r = (level != null ? level.levelId : "") switch
            {
                "MeadowRuins_01" => RunAndJumpLesson(),
                "MeadowRuins_02" => AbilityLesson(),
                "MeadowRuins_03" => SwapLesson(),
                "MeadowRuins_04" => SecretLesson(),
                _ => null,
            };
            if (r != null) _routine = StartCoroutine(Lesson(r));
            _lastCharacter = Player != null ? Player.Character : (CharacterType?)null;
            if (_lastCharacter.HasValue) _abilityShown.Add(_lastCharacter.Value);
        }

        private IEnumerator Lesson(IEnumerator lesson)
        {
            yield return lesson;
            _routine = null;
        }

        // On any level: the first time this session the player swaps to an animal, point at the ability button and say
        // what that animal's ability does (unless a lesson is already talking).
        private void WatchForSwap()
        {
            var player = Player;
            if (player == null || !_lastCharacter.HasValue || player.Character == _lastCharacter.Value) return;
            _lastCharacter = player.Character;
            if (!_abilityShown.Add(player.Character) || _routine != null) return;
            _routine = StartCoroutine(SwapAbilityTip(player));
        }

        private IEnumerator SwapAbilityTip(CharacterController2D player)
        {
            string line = AbilityLine(player.Character);
            Say(Keys ? line + "  (K, J or Shift)" : line);
            float t = 0f;
            while (player != null && player.AbilityReady && t < 5f)
            {
                PointDownAt(_hud.AbilityRect, t);
                t += Time.deltaTime;
                yield return null;
            }
            HideAll();
            _routine = null;
        }

        private void LateUpdate()
        {
            var gm = GameManager.Instance;
            bool show = gm != null && gm.CurrentState == GameState.Playing && Time.timeScale > 0f
                        && _hud != null && _hud.Root.activeInHierarchy;
            if (_canvas.enabled != show) _canvas.enabled = show;
            if (show) WatchForSwap();
        }

        // ── the lessons ───────────────────────────────────────────────────────────────────────────────────────────────
        private static CharacterController2D Player => LevelLoader.Instance != null ? LevelLoader.Instance.Player : null;

        // Waits out the level's first moment (camera snap, any fade) in scaled time, so a paused start holds the tip back.
        private static IEnumerator Settle()
        {
            yield return null;
            yield return new WaitForSeconds(0.6f);
        }

        private IEnumerator RunAndJumpLesson()
        {
            yield return Settle();
            var player = Player;
            if (player == null) yield break;

            Say(Keys ? "Hold the arrows to run  (or A / D, the arrow keys)" : "Hold the arrows to run");
            float startX = player.transform.position.x, t = 0f;
            while (player != null && Mathf.Abs(player.transform.position.x - startX) < 3f)
            {
                PointDownAt(_hud.MoveRect, t);
                t += Time.deltaTime;
                yield return null;
            }
            HideAll();
            yield return new WaitForSeconds(0.4f);

            Say(Keys ? "Tap here to jump - tap again in the air to double jump!  (Space)"
                     : "Tap here to jump - tap again in the air to double jump!");
            t = 0f;
            while (player != null && player.IsGrounded && t < 12f)
            {
                PointDownAt(_hud.JumpRect, t);
                t += Time.deltaTime;
                yield return null;
            }
            HideAll();
            yield return new WaitForSeconds(1.2f);

            Say("Jump on top of a robot to stomp it!");
            yield return new WaitForSeconds(4f);
            HideAll();
        }

        private IEnumerator AbilityLesson()
        {
            yield return Settle();
            var player = Player;
            if (player == null) yield break;

            string line = AbilityLine(player.Character);
            Say(Keys ? line + "  (K, J or Shift)" : line);
            float t = 0f;
            while (player != null && player.AbilityReady && t < 15f)
            {
                PointDownAt(_hud.AbilityRect, t);
                t += Time.deltaTime;
                yield return null;
            }
            HideAll();
            if (player != null && !player.AbilityReady)
            {
                Say("Great! It recharges in a few seconds - every animal has its own ability.");
                yield return new WaitForSeconds(3.5f);
                HideAll();
            }
        }

        private static string AbilityLine(CharacterType c) => c switch
        {
            CharacterType.Cluck => "Your ability button! Cluck lobs an egg that knocks out a robot!",
            CharacterType.Bessie => "Your ability button! Bessie's Ground Pound sends a quake at the robots!",
            CharacterType.Percy => "Your ability button! Percy's Roll Dash bowls robots over!",
            CharacterType.Woolly => "Your ability button! Woolly fires a wool tuft, and drops a cloud to stand on mid-air!",
            CharacterType.Ducky => "Your ability button! Ducky sends a water spout rolling at the robots!",
            CharacterType.Horace => "Your ability button! Horace throws a horseshoe at the robots!",
            CharacterType.Gerald => "Your ability button! Gerald blows a feather gust that knocks robots out!",
            CharacterType.Billy => "Your ability button! Billy slings mud that knocks out robots!",
            _ => "This is your ability button - press it!",
        };

        private IEnumerator SwapLesson()
        {
            yield return Settle();
            Say(Keys ? "Press here to change characters  (Tab)" : "Press here to change characters");
            var flow = GameFlow.Instance;
            float t = 0f;
            // Time stops while the picker is open, so the timer stops with it; the tip ends once it has been opened.
            while (t < 8f && (flow == null || flow.Swap == null || !flow.Swap.IsOpen))
            {
                PointDownAt(_hud.SwapButton != null ? (RectTransform)_hud.SwapButton.transform : null, t);
                t += Time.deltaTime;
                yield return null;
            }
            HideAll();
        }

        private IEnumerator SecretLesson()
        {
            yield return Settle();
            SecretPassageDoor sign = null;
            foreach (var door in FindObjectsByType<SecretPassageDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!door.returnsToLevel) { sign = door; break; }
            }
            if (sign == null) yield break;

            Say("See the secret sign? Walk into it to find a hidden room!");
            var signCollider = sign.GetComponent<Collider2D>();
            var player = Player;
            float t = 0f;
            while (t < 10f && sign != null && player != null && !InPassage(player, sign))
            {
                var cam = Camera.main;
                if (cam != null && signCollider != null)
                {
                    var b = signCollider.bounds;
                    Vector3 screen = cam.WorldToScreenPoint(new Vector3(b.center.x, b.max.y, 0f));
                    PointDownAtScreen(screen, t);
                }
                t += Time.deltaTime;
                yield return null;
            }
            HideAll();
        }

        // Below the level means down in the passage room (built at floor y -40).
        private static bool InPassage(CharacterController2D player, SecretPassageDoor sign)
            => player.transform.position.y < sign.transform.position.y - 15f;

        // ── the hand ──────────────────────────────────────────────────────────────────────────────────────────────────
        // Finger down, just above the control's top edge, bobbing toward it.
        private void PointDownAt(RectTransform target, float t)
        {
            if (target == null || !target.gameObject.activeInHierarchy) { _hand.enabled = false; return; }
            target.GetWorldCorners(Corners);   // overlay canvas: screen pixels
            var top = new Vector3((Corners[1].x + Corners[2].x) * 0.5f, Corners[1].y, 0f);
            PointDownAtScreen(top, t);
        }

        private void PointDownAtScreen(Vector3 screen, float t)
        {
            float scale = _canvas.scaleFactor;
            float bob = Mathf.Abs(Mathf.Sin(t * 5f)) * 18f;
            _hand.enabled = true;
            _hand.rectTransform.position = screen + new Vector3(0f, (HandHeight * 0.5f + 10f + bob) * scale, 0f);
            _hand.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }

        // ── UI ────────────────────────────────────────────────────────────────────────────────────────────────────────
        private void Build()
        {
            _canvas = UIKit.CreateCanvas("TutorialHintsCanvas", 20, transform);
            Destroy(_canvas.GetComponent<GraphicRaycaster>());   // never takes a touch

            _bubble = UIKit.NewRect("Bubble", _canvas.transform);
            UIKit.Place(_bubble, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1180f, 120f));
            var bg = _bubble.gameObject.AddComponent<Image>();
            bg.sprite = RoundedSprite();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.08f, 0.1f, 0.16f, 0.82f);
            bg.raycastTarget = false;

            _text = UIKit.Label(_bubble, "Text", "", 46, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.75f));
            _text.fontStyle = FontStyle.Bold;
            _text.resizeTextForBestFit = true;
            _text.resizeTextMinSize = 26;
            _text.resizeTextMaxSize = 46;
            _text.raycastTarget = false;
            UIKit.Stretch(_text.rectTransform);
            _text.rectTransform.offsetMin = new Vector2(30f, 10f);
            _text.rectTransform.offsetMax = new Vector2(-30f, -10f);

            _hand = UIKit.Picture(_canvas.transform, "Hand", HandSprite());
            _hand.preserveAspect = true;
            _hand.raycastTarget = false;
            _hand.rectTransform.sizeDelta = new Vector2(HandHeight, HandHeight);
            if (_hand.sprite == null) _hand.color = Color.clear;
            HideAll();
        }

        private static Sprite HandSprite()
        {
            var tex = Resources.Load<Texture2D>("Hint/HintHand");
            return tex == null ? null
                : Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private void Say(string line)
        {
            _text.text = line;
            _bubble.gameObject.SetActive(true);
            StartCoroutine(PopIn(_bubble));
        }

        private static IEnumerator PopIn(RectTransform rt)
        {
            for (float e = 0f; e < 0.25f; e += Time.unscaledDeltaTime)
            {
                float k = e / 0.25f;
                rt.localScale = Vector3.one * (k < 0.7f ? Mathf.Lerp(0.6f, 1.08f, k / 0.7f) : Mathf.Lerp(1.08f, 1f, (k - 0.7f) / 0.3f));
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        private void HideAll()
        {
            if (_bubble != null) _bubble.gameObject.SetActive(false);
            if (_hand != null) _hand.enabled = false;
        }

        private static Sprite _rounded;
        private static Sprite RoundedSprite()
        {
            if (_rounded != null) return _rounded;
            const int N = 64, R = 24;
            var tex = new Texture2D(N, N, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(R - x - 0.5f, x + 0.5f - (N - R)));
                    float dy = Mathf.Max(0f, Mathf.Max(R - y - 0.5f, y + 0.5f - (N - R)));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(R - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f)));
                }
            }
            tex.Apply();
            _rounded = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(R, R, R, R));
            return _rounded;
        }
    }
}
