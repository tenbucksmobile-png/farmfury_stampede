using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// The in-level character swap (HUD swap button, or Tab): the level freezes behind the current world's Character
    /// backdrop (WorldData.characterSwapBackground, "Character" lettering painted in; MenuArt's Meadow Ruins one for a
    /// world without its own), fitted whole like the results art, and the eight framed cards sit in a carousel ported
    /// from Arcade's Choose Character (CardCarouselController): drag or tap a side card to bring it to the centre, tap
    /// the centred card to become that character on the spot (GameManager.SwapCharacter). It opens centred on the
    /// character being played, which has a gold frame; locked cards are dark silhouettes (they can be browsed, not
    /// chosen). The X, Esc or Tab closes it without a change; Left/Right and Enter work on a keyboard. The next level
    /// starts as whoever was played last (SaveManager.LastCharacter). Cards are built lazily on first open, when
    /// DataManager has the characters.
    /// </summary>
    public class SwapCharacterScreen
    {
        public bool IsOpen => _root.activeSelf;

        // In the backdrop's 1280x720 art pixels: the lettering is painted at y 72..200, the cards sit below it.
        private static readonly Rect CarouselBox = new(0f, 235f, 1280f, 350f);
        private const float FramePad = 0.05f;   // gold frame margin, as a fraction of the card
        private static readonly Color Gold = new(1f, 0.84f, 0.25f, 1f);
        private static readonly Color Silhouette = new(0.08f, 0.07f, 0.12f, 1f);

        private readonly GameObject _root;
        private readonly Image _backdrop;
        private readonly RectTransform _carouselRect;
        private readonly CardCarouselController _carousel;
        private readonly MenuArt _art;
        private readonly List<CharacterType> _order = new();
        private readonly List<RectTransform> _items = new();
        private readonly List<Button> _buttons = new();
        private readonly Dictionary<CharacterType, Image> _frames = new();

        public SwapCharacterScreen(Transform canvas, MenuArt art)
        {
            _art = art ?? new MenuArt();
            var root = UIKit.NewRect("SwapCharacter", canvas);
            UIKit.Stretch(root);
            _root = root.gameObject;
            var veil = _root.AddComponent<Image>();   // behind the fitted backdrop (and the screen without art)
            veil.color = _art.characterSwapBackground != null ? Color.black : new Color(0f, 0f, 0f, 0.75f);

            if (_art.characterSwapBackground != null)
            {
                _backdrop = UIKit.Picture(root, "Backdrop", _art.characterSwapBackground);
                _backdrop.preserveAspect = false;
                var fit = _backdrop.gameObject.AddComponent<FitContent>();
                fit.contentSize = new Vector2(1280f, 720f);
                fit.spriteSize = new Vector2(1280f + 2f * ResultsScreen.ArtPad, 720f);
                _carouselRect = UIKit.NewRect("Cards", _backdrop.transform);
                ResultsScreen.PlaceInArt(_carouselRect, CarouselBox);
            }
            else
            {
                var title = UIKit.Label(root, "Title", "SWAP CHARACTER", 90, TextAnchor.MiddleCenter, Gold);
                title.fontStyle = FontStyle.Bold;
                title.raycastTarget = false;
                var outline = title.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.24f, 0.11f, 0.03f, 1f);
                outline.effectDistance = new Vector2(4f, -4f);
                UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 390f), new Vector2(1400f, 130f));
                _carouselRect = UIKit.NewRect("Cards", root);
                UIKit.Place(_carouselRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(1920f, 420f));
            }

            // The container's invisible raycast Image catches drags that start between cards.
            var dragArea = _carouselRect.gameObject.AddComponent<Image>();
            dragArea.color = new Color(0f, 0f, 0f, 0f);
            _carousel = _carouselRect.gameObject.AddComponent<CardCarouselController>();

            var safe = UIKit.NewRect("SafeArea", root);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            var close = UIKit.MakeButton(safe, "CloseButton", _art.quitButton != null ? "" : "X",
                _art.quitButton != null ? Color.white : new Color(0.6f, 0.2f, 0.2f, 1f), Close, 50);
            if (_art.quitButton != null)
            {
                close.image.sprite = _art.quitButton;
                close.image.preserveAspect = true;
            }
            UIKit.Place(close.image.rectTransform, Vector2.one, Vector2.one, new Vector2(-40f, -40f), Vector2.one * UIKit.RoundButtonSize);

            _root.SetActive(false);
        }

        /// <summary>Freezes the level and shows the carousel, centred on the character being played.</summary>
        public void Open()
        {
            BuildCards();
            var gm = GameManager.Instance;
            var save = SaveManager.Instance;
            int start = 0;
            for (int i = 0; i < _order.Count; i++)
            {
                var type = _order[i];
                bool unlocked = save == null || save.IsCharacterUnlocked(type);
                bool current = gm != null && gm.CurrentCharacter == type;
                _buttons[i].image.color = unlocked ? Color.white : Silhouette;
                _frames[type].gameObject.SetActive(current);
                if (current) { start = i; }
            }
            UseWorldBackdrop();

            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            _carousel.SetItems(_items, _buttons, start, i => Choose(_order[i]));
            PlayerInputReader.ReleaseTouch();   // the HUD buttons are covered; nothing stays held through the freeze
            Time.timeScale = 0f;
        }

        /// <summary>Closes without a change and lets the level run again.</summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            Hide();
            var gm = GameManager.Instance;
            if (gm != null && gm.CurrentState == GameState.Playing && !gm.ReviveDecisionPending)
            {
                Time.timeScale = 1f;
            }
        }

        /// <summary>Hides it without touching the time scale (the game left the level; the new state owns it).</summary>
        public void Hide() => _root.SetActive(false);

        /// <summary>
        /// Becomes that character (if unlocked) and closes; what tapping the centred card does. A locked character
        /// stays open, so the player can keep browsing.
        /// </summary>
        public bool Choose(CharacterType type)
        {
            if (SaveManager.Instance != null && !SaveManager.Instance.IsCharacterUnlocked(type))
            {
                return false;
            }
            bool swapped = GameManager.Instance != null && GameManager.Instance.SwapCharacter(type);
            Close();
            return swapped;
        }

        /// <summary>Keyboard while open: Left/Right (or A/D) move the carousel, Enter/Space picks the centred card.</summary>
        public void HandleKeys(Keyboard keyboard)
        {
            if (!IsOpen || keyboard == null || _order.Count == 0) { return; }
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) { _carousel.Step(-1); }
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) { _carousel.Step(1); }
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) { Choose(_order[_carousel.CenterIndex]); }
        }

        // The current level's world's own Character backdrop when it has one, else MenuArt's.
        private void UseWorldBackdrop()
        {
            if (_backdrop == null) { return; }
            var gm = GameManager.Instance;
            var data = DataManager.Instance;
            var level = gm != null ? gm.CurrentLevel : null;
            var world = level != null && data != null ? data.GetWorldData(level.worldType) : null;
            _backdrop.sprite = world != null && world.characterSwapBackground != null ? world.characterSwapBackground : _art.characterSwapBackground;
        }

        private void BuildCards()
        {
            if (_order.Count > 0 || DataManager.Instance == null)
            {
                return;
            }

            foreach (var data in DataManager.Instance.GetAllCharacters())
            {
                var type = data.characterType;
                var item = UIKit.NewRect($"Item_{type}", _carouselRect);

                // Gold frame behind the card being played.
                var frame = UIKit.Panel(item, "Frame", Gold);
                frame.raycastTarget = false;
                UIKit.Stretch(frame.rectTransform);
                frame.rectTransform.anchorMin = new Vector2(-FramePad, -FramePad);
                frame.rectTransform.anchorMax = new Vector2(1f + FramePad, 1f + FramePad);

                var sprite = data.selectCard != null ? data.selectCard : data.placeholderSprite;
                // No direct onClick here: the carousel adds the listener and calls Choose only for the centred card.
                var button = UIKit.MakeButton(item, "Card", sprite != null ? "" : data.displayName, data.uiColor, null, 30);
                if (sprite != null)
                {
                    button.image.sprite = sprite;
                    button.image.preserveAspect = true;
                }
                // Locked cards are tinted to a silhouette directly (Open); they stay interactable so a tap still
                // brings them to the centre.
                UIKit.Stretch(button.image.rectTransform);

                _order.Add(type);
                _items.Add(item);
                _buttons.Add(button);
                _frames[type] = frame;
            }
        }
    }
}
