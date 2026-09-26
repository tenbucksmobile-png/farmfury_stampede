using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Character select shown after a level is picked: a grid of the 8 characters, locked ones greyed out and not
    /// selectable, an unlocked one starts the level with that character via GameManager.StartLevel. Logic
    /// (Open / IsSelectable / TrySelect) is separate from the uGUI so it can be driven without clicking.
    /// </summary>
    public class CharacterSelectScreen : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public LevelData PendingLevel { get; private set; }
        public IReadOnlyDictionary<CharacterType, Button> SlotButtons => _slots;

        private GameObject _root;
        private Text _title;
        private RawImage _backdrop;
        private const float BackdropBrightness = 0.45f;   // dimmed so the cards stand out
        // Only the part of the world backdrop below its baked-in title (top 37%) is shown, so the title doesn't peek
        // out around the header banner: sky and hills without the name.
        private const float BackdropVisibleFraction = 0.62f;
        private readonly Dictionary<CharacterType, Button> _slots = new();
        // Card art: no caption under the card (the name is baked in). Cards are anchored to the top, under the banner,
        // in two rows that fit the shortest phone canvas (~980 reference px tall).
        private const float CardSize = 310f, CardColumnStep = 380f;
        private const float CardFirstRowFromTop = 445f, CardRowStep = 340f;
        // Banner wide enough to cover the world title baked into the (dimmed) backdrop behind it.
        private static readonly Vector2 BannerSize = new(620f, 259f);
        private const float BackButtonSize = UIKit.RoundButtonSize, EdgeMargin = 16f;   // matches Level Select

        /// <summary>Builds the panel under the given canvas (once). Called by GameFlow.</summary>
        public void Build(Transform canvas, MenuArt art = null)
        {
            if (_root != null)
            {
                return;
            }

            art ??= new MenuArt();

            var root = UIKit.NewRect("CharacterSelect", canvas);
            UIKit.Stretch(root);
            _root = root.gameObject;
            _root.AddComponent<Image>().color = UIKit.Dark;
            _backdrop = UIKit.NewRect("Backdrop", root).gameObject.AddComponent<RawImage>();
            _backdrop.raycastTarget = false;
            _backdrop.transform.SetAsFirstSibling();
            UIKit.Stretch(_backdrop.rectTransform);
            _backdrop.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            _backdrop.gameObject.SetActive(false);

            _title = UIKit.Label(root, "Title", "", 52, TextAnchor.MiddleCenter, UIKit.Accent);
            UIKit.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1500f, 80f));
            if (art.characterSelectBanner != null)
            {
                // The wooden banner replaces the "Choose a character for ..." text.
                var banner = UIKit.Picture(root, "HeaderBanner", art.characterSelectBanner);
                UIKit.Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), BannerSize);
                _title.gameObject.SetActive(false);
            }

            var characters = DataManager.Instance.GetAllCharacters();
            for (int i = 0; i < characters.Count; i++)
            {
                var data = characters[i];
                var captured = data.characterType;
                int col = i % 4, row = i / 4;
                var slotCentre = new Vector2((col - 1.5f) * 400f, 170f - row * 360f);
                var button = UIKit.MakeButton(root, $"Slot_{data.characterType}", "", data.uiColor, () => TrySelect(captured), 30);

                if (data.selectCard != null)
                {
                    // The card is the whole button (its name is baked in), with no caption; a locked card is dimmed by
                    // the Button's disabled colour.
                    button.image.sprite = data.selectCard;
                    button.image.preserveAspect = true;
                    UIKit.Place(button.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                        new Vector2((col - 1.5f) * CardColumnStep, -(CardFirstRowFromTop + row * CardRowStep)), new Vector2(CardSize, CardSize));
                    button.GetComponentInChildren<Text>().gameObject.SetActive(false);
                    _slots[data.characterType] = button;
                    continue;
                }

                UIKit.Place(button.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), slotCentre, new Vector2(380f, 320f));

                var portrait = UIKit.Panel(button.transform, "Portrait", Color.white);
                portrait.sprite = data.placeholderSprite;
                portrait.preserveAspect = true;
                UIKit.Place(portrait.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(120f, 120f));
                portrait.raycastTarget = false;

                var label = button.GetComponentInChildren<Text>();
                label.color = Color.black;
                label.alignment = TextAnchor.LowerCenter;
                UIKit.Stretch(label.rectTransform, 10f, 10f, 10f, 140f);
                _slots[data.characterType] = button;
            }

            if (art.backButton != null)
            {
                // Round back button in the top-left corner of the safe area, like Level Select.
                var safe = UIKit.NewRect("SafeArea", root);
                safe.gameObject.AddComponent<SafeAreaFitter>();
                var back = UIKit.MakeButton(safe, "CancelButton", "", Color.white, Close);
                back.image.sprite = art.backButton;
                back.image.preserveAspect = true;
                UIKit.Place(back.image.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(EdgeMargin, -EdgeMargin),
                    new Vector2(BackButtonSize, BackButtonSize));
            }
            else
            {
                var cancel = UIKit.MakeButton(root, "CancelButton", "< Back", new Color(0.25f, 0.3f, 0.45f, 1f), Close, 34);
                UIKit.Place(cancel.image.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(240f, 70f));
            }

            _root.SetActive(false);
        }

        /// <summary>Opens the grid for the given level (CharacterSelect state).</summary>
        public void Open(LevelData level)
        {
            PendingLevel = level;
            IsOpen = true;
            GameManager.Instance.SetState(GameState.CharacterSelect);
            Refresh();
            if (_root != null)
            {
                _root.SetActive(true);
            }
        }

        /// <summary>Closes without starting anything and goes back to Level Select.</summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            if (_root != null)
            {
                _root.SetActive(false);
            }
            GameManager.Instance.SetState(GameState.LevelSelect);
        }

        private void SetBackdrop(Sprite sprite)
        {
            _backdrop.gameObject.SetActive(sprite != null);
            if (sprite == null)
            {
                return;
            }

            // The sprite fills its texture; take the bottom BackdropVisibleFraction of it (UV y runs bottom-up).
            _backdrop.texture = sprite.texture;
            _backdrop.uvRect = new Rect(0f, 0f, 1f, BackdropVisibleFraction);
            _backdrop.color = new Color(BackdropBrightness, BackdropBrightness, BackdropBrightness, 1f);
            _backdrop.GetComponent<AspectRatioFitter>().aspectRatio =
                sprite.rect.width / (sprite.rect.height * BackdropVisibleFraction);
        }

        public bool IsSelectable(CharacterType type)
        {
            return SaveManager.Instance != null && SaveManager.Instance.IsCharacterUnlocked(type);
        }

        /// <summary>Starts the pending level with the character. Returns false (and does nothing) if it is locked.</summary>
        public bool TrySelect(CharacterType type)
        {
            if (!IsOpen || PendingLevel == null || !IsSelectable(type))
            {
                return false;
            }

            IsOpen = false;
            if (_root != null)
            {
                _root.SetActive(false);
            }
            GameManager.Instance.StartLevel(PendingLevel, type);
            return true;
        }

        /// <summary>Hides the panel without changing game state (GameFlow does that when leaving the state).</summary>
        public void HideVisual()
        {
            IsOpen = false;
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void Refresh()
        {
            if (_root == null || PendingLevel == null)
            {
                return;
            }

            _title.text = $"Choose a character for {PendingLevel.displayName}";
            var world = DataManager.Instance.GetWorldData(PendingLevel.worldType);
            SetBackdrop(world != null ? world.levelSelectBackground : null);
            foreach (var data in DataManager.Instance.GetAllCharacters())
            {
                bool unlocked = IsSelectable(data.characterType);
                var button = _slots[data.characterType];
                button.interactable = unlocked;
                if (data.selectCard != null)
                {
                    // Card art stays untinted; the Button's disabled colour dims a locked card.
                    button.image.color = Color.white;
                    continue;
                }

                button.image.color = unlocked ? data.uiColor : new Color(0.3f, 0.3f, 0.32f, 1f);
                button.GetComponentInChildren<Text>().text = unlocked
                    ? $"{data.displayName}\n{data.abilityType}\n{data.abilityUsesPerLevel} uses per level"
                    : $"{data.displayName}\nLOCKED\nClear {data.unlockLevelsRequired} levels";
            }
        }
    }
}
