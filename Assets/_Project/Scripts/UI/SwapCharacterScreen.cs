using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// The in-level character swap (HUD swap button, or Tab): the level freezes behind a dim, and every character's
    /// framed card is laid out in two rows of four. Unlocked cards can be tapped to become that character on the spot
    /// (GameManager.SwapCharacter); locked ones are dark silhouettes; the one being played has a gold frame. Tapping
    /// outside the cards, the X, Esc or Tab closes it without a change. The next level starts as whoever was played
    /// last (SaveManager.LastCharacter). Built lazily on first open, when DataManager has the characters.
    /// </summary>
    public class SwapCharacterScreen
    {
        public bool IsOpen => _root.activeSelf;

        private const float CardSize = 250f, ColumnStep = 300f, RowStep = 300f, FramePad = 14f;
        private static readonly Color Gold = new(1f, 0.84f, 0.25f, 1f);
        private static readonly Color Silhouette = new(0.08f, 0.07f, 0.12f, 1f);

        private readonly GameObject _root;
        private readonly RectTransform _grid;
        private readonly MenuArt _art;
        private readonly Dictionary<CharacterType, (Button button, Image frame)> _cards = new();

        public SwapCharacterScreen(Transform canvas, MenuArt art)
        {
            _art = art ?? new MenuArt();
            var root = UIKit.NewRect("SwapCharacter", canvas);
            UIKit.Stretch(root);
            _root = root.gameObject;
            var dim = _root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.75f);
            var outside = _root.AddComponent<Button>();
            outside.targetGraphic = dim;
            outside.transition = Selectable.Transition.None;
            outside.onClick.AddListener(Close);

            var title = UIKit.Label(root, "Title", "SWAP CHARACTER", 90, TextAnchor.MiddleCenter, Gold);
            title.fontStyle = FontStyle.Bold;
            title.raycastTarget = false;
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.24f, 0.11f, 0.03f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 390f), new Vector2(1400f, 130f));

            _grid = UIKit.NewRect("Cards", root);
            UIKit.Place(_grid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(ColumnStep * 4f, RowStep * 2f));

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

        /// <summary>Freezes the level and shows the cards (current one framed, locked ones dark).</summary>
        public void Open()
        {
            BuildCards();
            var gm = GameManager.Instance;
            var save = SaveManager.Instance;
            foreach (var (type, card) in _cards)
            {
                bool unlocked = save == null || save.IsCharacterUnlocked(type);
                card.button.interactable = unlocked;
                card.button.image.color = unlocked ? Color.white : Silhouette;
                card.frame.gameObject.SetActive(gm != null && gm.CurrentCharacter == type);
            }

            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
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

        /// <summary>Becomes that character (if allowed) and closes. Also what a card tap does.</summary>
        public bool Choose(CharacterType type)
        {
            bool swapped = GameManager.Instance != null && GameManager.Instance.SwapCharacter(type);
            Close();
            return swapped;
        }

        private void BuildCards()
        {
            if (_cards.Count > 0 || DataManager.Instance == null)
            {
                return;
            }

            var characters = DataManager.Instance.GetAllCharacters();
            for (int i = 0; i < characters.Count; i++)
            {
                var data = characters[i];
                var type = data.characterType;
                int col = i % 4, row = i / 4;
                var centre = new Vector2((col - 1.5f) * ColumnStep, (0.5f - row) * RowStep);

                // Gold frame behind the card being played.
                var frame = UIKit.Panel(_grid, $"Frame_{type}", Gold);
                frame.raycastTarget = false;
                UIKit.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), centre, Vector2.one * (CardSize + FramePad * 2f));

                var sprite = data.selectCard != null ? data.selectCard : data.placeholderSprite;
                var button = UIKit.MakeButton(_grid, $"Card_{type}", sprite != null ? "" : data.displayName, data.uiColor, () => Choose(type), 30);
                if (sprite != null)
                {
                    button.image.sprite = sprite;
                    button.image.preserveAspect = true;
                }
                // Locked cards are tinted to a silhouette directly (Open), so the disabled tint must not grey them.
                var colors = button.colors;
                colors.disabledColor = Color.white;
                button.colors = colors;
                UIKit.Place(button.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), centre, Vector2.one * CardSize);
                _cards[type] = (button, frame);
            }
        }
    }
}
