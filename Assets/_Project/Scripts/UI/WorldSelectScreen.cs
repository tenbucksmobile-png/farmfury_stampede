using System;
using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// World Select: nine world cards in a 3x3 grid - the six free story worlds (GDD Section 7), then the three paid
    /// post-finale worlds (WorldData.purchaseRequired). A story world unlocks once the previous world's boss level has
    /// been completed; only Meadow Ruins is open on a fresh save. Each card is just the world's art (name baked in),
    /// under the wooden banner header, and the whole card is the tap target that opens the world's Level Select; a
    /// locked story world's card is greyed out and can't be entered. A paid world not yet bought stays in full colour
    /// with the $3.99 price sign (ShopArt.worldPrice) on its lower edge, and tapping it opens the world shop.
    /// Without art, a card falls back to a coloured panel with the world name as text. The grid is a 16:9-card board
    /// fitted between the banner and the bottom of the device safe area, so it scales as one piece and never
    /// overlaps. The screen sits on the sunset-farm backdrop (Canvas.png), cover-cropped to the device. The round home
    /// button (Btn_home.png) top-left of the safe area returns to the landing screen.
    /// </summary>
    public class WorldSelectScreen
    {
        public class Card
        {
            public WorldType world;
            public GameObject root;
            public Button button;
            public bool unlocked;
            public bool forSale;
            public GameObject priceSign;
        }

        public GameObject Root { get; private set; }
        public Button HomeButton { get; private set; }
        public IReadOnlyList<Card> Cards => _cards;

        private readonly List<Card> _cards = new();
        private static readonly Color LockedTint = new(0.4f, 0.4f, 0.42f, 1f);
        // Same size as every other round menu button.
        private const float HomeButtonSize = UIKit.RoundButtonSize;
        private const float EdgeMargin = 40f;
        // Grid geometry in card units: 3 columns of 16:9 cards with Gap between them.
        private const int Columns = 3;
        private const float CardW = 16f, CardH = 9f, Gap = 0.9f;
        // Space kept for the banner above the grid (from the top of the safe area), and below it.
        private const float GridTop = 215f, GridBottom = 24f, GridSide = 24f;
        // The price sign's box on a card, in card fractions: centred low on the card, clear of the baked-in title.
        private static readonly Rect PriceSignBox = new(0.33f, 0.03f, 0.34f, 0.3f);

        private readonly Action<WorldType> _onEnter;
        private readonly Action _onBuyWorld;

        public WorldSelectScreen(Transform canvas, Action<WorldType> onEnter, Action onBuyWorld, Action onHome, MenuArt art, ShopArt shop)
        {
            art ??= new MenuArt();
            shop ??= new ShopArt();
            _onEnter = onEnter;
            _onBuyWorld = onBuyWorld;
            var root = UIKit.NewRect("WorldSelect", canvas);
            UIKit.Stretch(root);
            Root = root.gameObject;
            var bg = Root.AddComponent<Image>();
            bg.color = UIKit.Dark;
            UIKit.SetBackdrop(UIKit.Backdrop(root), art.worldSelectBackground);

            var safe = UIKit.NewRect("SafeArea", root);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            if (art.worldSelectBanner != null)
            {
                var banner = UIKit.Picture(safe, "HeaderBanner", art.worldSelectBanner);
                UIKit.Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(760f, 186f));
            }
            else
            {
                var title = UIKit.Label(safe, "Title", "SELECT A WORLD", 64, TextAnchor.MiddleCenter, UIKit.Accent);
                UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1200f, 90f));
            }

            // The grid area below the banner, and in it a board of the grid's exact aspect (fitted, never cropped).
            var area = UIKit.NewRect("GridArea", safe);
            UIKit.Stretch(area, GridSide, GridBottom, GridSide, GridTop);
            var worlds = (WorldType[])Enum.GetValues(typeof(WorldType));
            int rows = Mathf.CeilToInt(worlds.Length / (float)Columns);
            float boardW = Columns * CardW + (Columns - 1) * Gap;
            float boardH = rows * CardH + (rows - 1) * Gap;
            var board = UIKit.NewRect("Board", area);
            UIKit.Stretch(board);
            var fitter = board.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = boardW / boardH;

            foreach (var world in worlds)
            {
                int index = (int)world;
                int col = index % Columns, row = index / Columns;
                var captured = world;
                // The whole card is the button. Refresh() already tints a locked card, so the Button's own disabled
                // tint stays white rather than darkening it a second time.
                var button = UIKit.MakeButton(board, $"Card_{world}", "", UIKit.Card, () => Tapped(captured));
                var colors = button.colors;
                colors.disabledColor = Color.white;
                button.colors = colors;
                var card = button.image;
                float x0 = col * (CardW + Gap), yTop = row * (CardH + Gap);
                var rt = card.rectTransform;
                rt.anchorMin = new Vector2(x0 / boardW, 1f - (yTop + CardH) / boardH);
                rt.anchorMax = new Vector2((x0 + CardW) / boardW, 1f - yTop / boardH);
                rt.offsetMin = rt.offsetMax = Vector2.zero;

                var name = UIKit.Label(card.transform, "Name", world.ToString(), 40, TextAnchor.MiddleCenter);
                name.raycastTarget = false;
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 10;
                UIKit.Stretch(name.rectTransform, 12f, 12f, 12f, 12f);

                var price = UIKit.Picture(card.transform, "PriceSign", shop.worldPrice);
                var priceRt = price.rectTransform;
                priceRt.anchorMin = PriceSignBox.min;
                priceRt.anchorMax = PriceSignBox.max;
                priceRt.offsetMin = priceRt.offsetMax = Vector2.zero;

                _cards.Add(new Card { world = world, root = card.gameObject, button = button, priceSign = price.gameObject });
            }

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

        private void Tapped(WorldType world)
        {
            var card = _cards.Find(c => c.world == world);
            if (card == null) { return; }
            if (card.unlocked) { _onEnter?.Invoke(world); }
            else if (card.forSale) { _onBuyWorld?.Invoke(); }
        }

        public void Refresh()
        {
            var save = SaveManager.Instance;
            foreach (var card in _cards)
            {
                var data = DataManager.Instance.GetWorldData(card.world);
                card.unlocked = save.IsWorldUnlocked(card.world);
                card.forSale = !card.unlocked && data != null && data.purchaseRequired;
                card.button.interactable = card.unlocked || card.forSale;

                // Card art has the world name baked in; a locked story world's art is greyed out, a paid world on sale
                // stays bright under its price sign.
                var cardImage = card.root.GetComponent<Image>();
                var art = data != null ? data.selectCardArt : null;
                bool bright = card.unlocked || card.forSale;
                cardImage.sprite = art;
                cardImage.color = art != null
                    ? (bright ? Color.white : LockedTint)
                    : (bright ? UIKit.Card : UIKit.CardLocked);

                var nameLabel = card.root.transform.Find("Name").GetComponent<Text>();
                nameLabel.text = data != null ? data.displayName : card.world.ToString();
                nameLabel.gameObject.SetActive(art == null);

                var priceImage = card.priceSign.GetComponent<Image>();
                card.priceSign.SetActive(card.forSale && priceImage.sprite != null);
            }
        }
    }
}
