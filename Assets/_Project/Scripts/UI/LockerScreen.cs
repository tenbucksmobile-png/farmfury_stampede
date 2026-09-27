using System;
using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Arcade's Locker, opened from the HUD's Locker button mid-level (the game pauses while it's open): the Locker
    /// sign, a scrolling grid of the cosmetics the player owns - each a framed tile with the item, its name and
    /// "Equipped" (with the tick badge) or "Tap to Equip"; tapping toggles it - then the Hats &amp; Caps / Trails /
    /// Machines banners to their purchase pages, and a "You may like..." card suggesting something not yet owned.
    /// Hats go on the character being played (caps and cowboy hats in that character's own design); a machine
    /// always belongs to its own character (Cluck, Bessie or Horace) and shows when playing as them.
    /// </summary>
    public class LockerScreen : OverlayScreen
    {
        private readonly struct Entry
        {
            public readonly string name;
            public readonly CosmeticType type;
            public readonly string productId;
            public readonly CharacterType? owner;   // machines: fixed character

            public Entry(string name, CosmeticType type, string productId, CharacterType? owner = null)
            {
                this.name = name;
                this.type = type;
                this.productId = productId;
                this.owner = owner;
            }
        }

        private static readonly Entry[] Catalog =
        {
            new("Baseball Cap", CosmeticType.Hat, StoreProducts.Hats[1]),
            new("Cowboy Hat", CosmeticType.Hat, StoreProducts.Hats[2]),
            new("Sombrero", CosmeticType.Hat, StoreProducts.Hats[0]),
            new("Chef Hat", CosmeticType.Hat, StoreProducts.Hats[3]),
            new("Crown", CosmeticType.Hat, StoreProducts.Hats[4]),
            new("Corn Husk Trail", CosmeticType.Trail, StoreProducts.Trails[2]),
            new("Ember Trail", CosmeticType.Trail, StoreProducts.Trails[3]),
            new("Sparkle Dust Trail", CosmeticType.Trail, StoreProducts.Trails[1]),
            new("Rainbow Ribbon Trail", CosmeticType.Trail, StoreProducts.Trails[0]),
            new("Confetti Trail", CosmeticType.Trail, StoreProducts.Trails[4]),
            new("Bubbles Trail", CosmeticType.Trail, StoreProducts.Trails[5]),
            new("Cluck's Tractor", CosmeticType.Skin, StoreProducts.Machines[0], CharacterType.Cluck),
            new("Bessie's Milk Tanker", CosmeticType.Skin, StoreProducts.Machines[1], CharacterType.Bessie),
            new("Horace's Hay Baler", CosmeticType.Skin, StoreProducts.Machines[2], CharacterType.Horace),
        };

        private static readonly Color NameColor = new(0.35f, 0.18f, 0.05f);
        private static readonly Color EquippedColor = new(0.15f, 0.5f, 0.2f);
        private static readonly Color OwnedColor = new(0.35f, 0.28f, 0.18f);
        private const float TileSize = 260f;
        private const float TileGap = 20f;
        private const float TileInset = 0.23f;
        private const float GridTop = 385f;                 // below the header sign
        private const float BannerWidth = 330f;
        private static readonly float BannerHeight = BannerWidth * 246f / 619f;
        private const float BannerGap = 30f;
        private const float BannerRowBottom = 830f;         // from the top of the 1080 canvas

        private readonly RectTransform _grid;
        private readonly GameObject _suggestion;
        private readonly Image _suggestionIcon;
        private readonly Text _suggestionText;
        private readonly List<GameObject> _tiles = new();
        private readonly Func<CosmeticType, OverlayScreen> _purchasePage;
        private Entry _suggested;
        private bool _pausedByLocker;

        public LockerScreen(Transform canvas, MenuArt art, ShopArt shop, Func<CosmeticType, OverlayScreen> purchasePage)
            : base(canvas, "Locker", art, shop)
        {
            _purchasePage = purchasePage;
            HeaderSign(Shop.lockerBanner, "LOCKER");

            // Owned items: a vertical scroll of 4-wide tiles between the header and the banner row.
            float bannerTop = BannerRowBottom - BannerHeight;
            var viewport = UIKit.NewRect("TileScroll", Rect);
            UIKit.Place(viewport, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -GridTop),
                new Vector2(4f * TileSize + 3f * TileGap + 40f, bannerTop - 20f - GridTop));
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            _grid = UIKit.NewRect("TileGrid", viewport);
            _grid.anchorMin = new Vector2(0f, 1f);
            _grid.anchorMax = new Vector2(1f, 1f);
            _grid.pivot = new Vector2(0.5f, 1f);
            _grid.offsetMin = _grid.offsetMax = Vector2.zero;
            var layout = _grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(TileSize, TileSize);
            layout.spacing = new Vector2(TileGap, TileGap);
            layout.padding = new RectOffset(0, 0, 10, 10);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;
            layout.childAlignment = TextAnchor.UpperCenter;
            _grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = _grid;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;


            // Category banners -> the purchase pages.
            float step = BannerWidth + BannerGap;
            Banner("HatsBannerButton", Shop.hatsBanner, "HATS & CAPS", -step, bannerTop, CosmeticType.Hat);
            Banner("TrailsBannerButton", Shop.trailsBanner, "TRAILS", 0f, bannerTop, CosmeticType.Trail);
            Banner("MachinesBannerButton", Shop.machinesBanner, "MACHINES", step, bannerTop, CosmeticType.Skin);

            // "You may like: X!" - Cluck's marketing card, mid-screen, only while nothing is owned yet (the owned
            // tiles take its place after the first purchase); tapping it opens that item's page.
            var card = IconButton(Rect, "Suggestion", Shop.lockerSuggestion, "", () => _purchasePage(_suggested.type)?.Show());
            if (Shop.lockerSuggestion == null) { card.image.color = new Color(0.97f, 0.94f, 0.86f, 1f); }
            float cardHeight = 300f;
            float cardWidth = Shop.lockerSuggestion != null ? cardHeight * Shop.lockerSuggestion.rect.width / Shop.lockerSuggestion.rect.height : 780f;
            float gridMiddle = -(GridTop + bannerTop - 20f) * 0.5f;   // centred in the space between the header and the banners
            UIKit.Place(card.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, gridMiddle), new Vector2(cardWidth, cardHeight));
            _suggestion = card.gameObject;
            _suggestionIcon = UIKit.Picture(card.transform, "Icon", null);
            UIKit.Place(_suggestionIcon.rectTransform, new Vector2(0.62f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(140f, 140f));
            _suggestionText = UIKit.Label(card.transform, "Text", "", 38, TextAnchor.MiddleCenter, NameColor);
            _suggestionText.fontStyle = FontStyle.Bold;
            UIKit.Place(_suggestionText.rectTransform, new Vector2(0.62f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(cardWidth * 0.6f, 70f));
        }

        private void Banner(string name, Sprite sprite, string fallback, float x, float top, CosmeticType type)
        {
            var button = IconButton(Rect, name, sprite, fallback, () =>
            {
                var page = _purchasePage(type);
                if (page == null) { return; }
                page.Closed -= Refresh;
                page.Closed += Refresh;
                page.Show();
            });
            UIKit.Place(button.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -top), new Vector2(BannerWidth, BannerHeight));
        }

        /// <summary>From the HUD: pauses a running level while the Locker is open.</summary>
        public void Open()
        {
            var gm = GameManager.Instance;
            _pausedByLocker = gm != null && gm.CurrentState == GameState.Playing;
            if (_pausedByLocker)
            {
                gm.PauseGame();   // a state change closes overlays, so pause before showing
            }
            Show();
        }

        public override void Hide()
        {
            if (!IsOpen)
            {
                return;   // the close-all on Open's own pause must not clear the flag
            }

            bool resume = _pausedByLocker;
            _pausedByLocker = false;
            base.Hide();
            if (resume && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
            {
                GameManager.Instance.ResumeGame();
            }
        }

        protected override void OnShow() => Refresh();

        private static CharacterType Active =>
            GameManager.Instance != null ? GameManager.Instance.CurrentCharacter : CharacterType.Cluck;

        private static CharacterType Wearer(Entry entry) => entry.owner ?? Active;

        private static string CosmeticId(Entry entry) => IAPManager.CosmeticIdFor(entry.productId, Wearer(entry));

        private static bool IsEquipped(Entry entry, string id)
        {
            var save = SaveManager.Instance;
            return entry.type == CosmeticType.Trail
                ? save.GetEquippedTrail() == id
                : save.GetEquippedCosmetic(entry.type, Wearer(entry)) == id;
        }

        private void Refresh()
        {
            foreach (var tile in _tiles)
            {
                UnityEngine.Object.Destroy(tile);
            }
            _tiles.Clear();

            var save = SaveManager.Instance;
            var notOwned = new List<Entry>();
            if (save != null)
            {
                foreach (var entry in Catalog)
                {
                    string id = CosmeticId(entry);
                    if (save.IsCosmeticOwned(id)) { BuildTile(entry, id); } else { notOwned.Add(entry); }
                }
            }
            bool ownsAny = _tiles.Count > 0;
            _suggestion.SetActive(!ownsAny && notOwned.Count > 0);
            if (!ownsAny && notOwned.Count > 0)
            {
                _suggested = notOwned[UnityEngine.Random.Range(0, notOwned.Count)];
                _suggestionText.text = $"You may like: {_suggested.name}!";
                _suggestionIcon.sprite = Preview(CosmeticId(_suggested));
                _suggestionIcon.gameObject.SetActive(_suggestionIcon.sprite != null);
            }
        }

        private static Sprite Preview(string id)
        {
            var data = DataManager.Instance != null ? DataManager.Instance.GetCosmeticData(id) : null;
            return data != null ? data.previewSprite : null;
        }

        private void BuildTile(Entry entry, string id)
        {
            bool equipped = IsEquipped(entry, id);
            var tile = IconButton(_grid, $"Tile_{id}", Shop.cardFrame, "", () => Toggle(entry, id));
            if (Shop.cardFrame == null) { tile.image.color = new Color(0.97f, 0.94f, 0.86f, 1f); }
            _tiles.Add(tile.gameObject);

            var content = UIKit.NewRect("Content", tile.transform);
            content.anchorMin = new Vector2(TileInset, TileInset);
            content.anchorMax = new Vector2(1f - TileInset, 1f - TileInset);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.MiddleCenter;
            column.spacing = 4f;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            var icon = UIKit.Picture(content, "Icon", Preview(id));
            icon.gameObject.SetActive(true);
            var iconSize = icon.gameObject.AddComponent<LayoutElement>();
            iconSize.preferredHeight = 70f;
            iconSize.flexibleHeight = 1f;

            var name = UIKit.Label(content, "Name", entry.name, 20, TextAnchor.MiddleCenter, NameColor);
            name.fontStyle = FontStyle.Bold;
            var status = UIKit.Label(content, "Status", equipped ? "Equipped" : "Tap to Equip", 18, TextAnchor.MiddleCenter,
                equipped ? EquippedColor : OwnedColor);
            if (entry.owner.HasValue && !equipped)
            {
                status.text = $"Tap to Equip ({entry.owner.Value})";
            }

            if (equipped && Shop.ownedBadge != null)
            {
                var badge = UIKit.Picture(tile.transform, "EquippedBadge", Shop.ownedBadge);
                UIKit.Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(52f, 52f));
            }
        }

        // Tap: equip it, or take it off if it's already on.
        private void Toggle(Entry entry, string id)
        {
            var save = SaveManager.Instance;
            string value = IsEquipped(entry, id) ? string.Empty : id;
            if (entry.type == CosmeticType.Trail)
            {
                save.SetEquippedTrail(value);
            }
            else
            {
                save.SetEquippedCosmetic(entry.type, Wearer(entry), value);
            }
            IAPManager.RefreshPlayerCosmetics();
            Refresh();
        }
    }
}
