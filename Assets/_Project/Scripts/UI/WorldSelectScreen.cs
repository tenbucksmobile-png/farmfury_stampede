using System;
using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// World Select: six world cards (GDD Section 7). A world unlocks once the previous world's boss level has been
    /// completed; only Meadow Ruins is open on a fresh save. Each card is just the world's art (name baked in), under
    /// the wooden banner header, and the whole card is the tap target that opens the world's Level Select; a locked
    /// world's card is greyed out and can't be entered. Without art, the card falls back to a coloured panel with the
    /// world name as text. The screen sits on the sunset-farm backdrop (Canvas.png), cover-cropped to the device.
    /// The round home button (Btn_home.png) top-left of the safe area returns to the landing screen.
    /// </summary>
    public class WorldSelectScreen
    {
        public class Card
        {
            public WorldType world;
            public GameObject root;
            public Button button;
            public bool unlocked;
        }

        public GameObject Root { get; private set; }
        public Button HomeButton { get; private set; }
        public IReadOnlyList<Card> Cards => _cards;

        private readonly List<Card> _cards = new();
        private const float CardWidth = 570f;
        private const float CardHeight = CardWidth * 9f / 16f;   // the WS_ card art is 16:9
        private static readonly Color LockedTint = new(0.4f, 0.4f, 0.42f, 1f);
        // Same size as every other round menu button.
        private const float HomeButtonSize = UIKit.RoundButtonSize;
        private const float EdgeMargin = 40f;

        public WorldSelectScreen(Transform canvas, Action<WorldType> onEnter, Action onHome, MenuArt art)
        {
            art ??= new MenuArt();
            var root = UIKit.NewRect("WorldSelect", canvas);
            UIKit.Stretch(root);
            Root = root.gameObject;
            var bg = Root.AddComponent<Image>();
            bg.color = UIKit.Dark;
            UIKit.SetBackdrop(UIKit.Backdrop(root), art.worldSelectBackground);

            if (art.worldSelectBanner != null)
            {
                var banner = UIKit.Picture(root, "HeaderBanner", art.worldSelectBanner);
                UIKit.Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(760f, 186f));
            }
            else
            {
                var title = UIKit.Label(root, "Title", "SELECT A WORLD", 64, TextAnchor.MiddleCenter, UIKit.Accent);
                UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1200f, 90f));
            }

            foreach (WorldType world in Enum.GetValues(typeof(WorldType)))
            {
                int index = (int)world;
                int col = index % 3, row = index / 3;
                var captured = world;
                // The whole card is the button. Refresh() already tints a locked card, so the Button's own disabled
                // tint stays white rather than darkening it a second time.
                var button = UIKit.MakeButton(root, $"Card_{world}", "", UIKit.Card, () => onEnter?.Invoke(captured));
                var colors = button.colors;
                colors.disabledColor = Color.white;
                button.colors = colors;
                var card = button.image;
                UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2((col - 1) * 600f, 85f - row * 345f), new Vector2(CardWidth, CardHeight));

                var name = UIKit.Label(card.transform, "Name", world.ToString(), 44, TextAnchor.MiddleCenter);
                name.raycastTarget = false;
                UIKit.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(520f, 60f));

                _cards.Add(new Card { world = world, root = card.gameObject, button = button });
            }

            var safe = UIKit.NewRect("SafeArea", root);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            HomeButton = UIKit.MakeButton(safe, "HomeButton", art.homeButton != null ? "" : "Home",
                new Color(0.62f, 0.38f, 0.17f, 1f), () => onHome?.Invoke(), 34);
            if (art.homeButton != null)
            {
                HomeButton.image.sprite = art.homeButton;
                HomeButton.image.color = Color.white;
                HomeButton.image.preserveAspect = true;
            }
            UIKit.Place(HomeButton.image.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(EdgeMargin, -EdgeMargin),
                new Vector2(HomeButtonSize, HomeButtonSize));

            Root.SetActive(false);
        }

        public void Refresh()
        {
            var save = SaveManager.Instance;
            foreach (var card in _cards)
            {
                var data = DataManager.Instance.GetWorldData(card.world);
                card.unlocked = save.IsWorldUnlocked(card.world);
                card.button.interactable = card.unlocked;

                // Card art has the world name baked in; a locked world's art is greyed out.
                var cardImage = card.root.GetComponent<Image>();
                var art = data != null ? data.selectCardArt : null;
                cardImage.sprite = art;
                cardImage.color = art != null
                    ? (card.unlocked ? Color.white : LockedTint)
                    : (card.unlocked ? UIKit.Card : UIKit.CardLocked);

                var nameLabel = card.root.transform.Find("Name").GetComponent<Text>();
                nameLabel.text = data != null ? data.displayName : card.world.ToString();
                nameLabel.gameObject.SetActive(art == null);
            }
        }
    }
}
