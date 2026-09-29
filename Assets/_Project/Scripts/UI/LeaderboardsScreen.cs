using System;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Settings -> Leaderboards, per the Leaderboard mockup (Leaderboard_Mock.png): the Farm Fury Stampede logo
    /// top-left, the LeaderBoard sign top-right and the six world names as lettering in two rows of three over the
    /// wooden sign, in the mockup's order (Meadow Ruins, Watermill Village, Frozen Tundra / Sky Island, Sunken City,
    /// Mothership), on an even grid. Locked worlds are greyed. Tapping an unlocked world opens its
    /// <see cref="WorldDetailScreen"/>; a locked purchase-gated world opens the world shop; any other locked world
    /// says how to unlock it. The records are this device's own (no online board until the shared backend exists).
    /// </summary>
    public class LeaderboardsScreen : LeaderboardPage
    {
        // Mockup order, left to right, top row first.
        private static readonly WorldType[] GridOrder =
        {
            WorldType.MeadowRuins, WorldType.WatermillVillage, WorldType.FrozenTundra,
            WorldType.SkyIslands, WorldType.SunkenCity, WorldType.RobotMothership,
        };
        // Name slots: equal column pitch (246) and row pitch (170), sized so neighbouring names never touch.
        private static readonly float[] ColumnX = { 185f, 431f, 677f };
        private static readonly float[] RowY = { 365f, 535f };
        private const float NameWidth = 210f, NameHeight = 110f;
        private static readonly Rect LogoBox = new(105f, 68f, 135f, 95f);
        private static readonly Rect HintBox = new(100f, 612f, 700f, 40f);
        private static readonly Color LockedTint = new(0.55f, 0.55f, 0.55f, 0.8f);

        private readonly Button[] _names = new Button[GridOrder.Length];
        private readonly Text _hint;
        private readonly Action<WorldType> _onWorld;
        private readonly Action _onBuyWorld;

        public LeaderboardsScreen(Transform canvas, MenuArt art, ShopArt shop, LeaderboardArt boards,
            Action<WorldType> onWorld, Action onBuyWorld)
            : base(canvas, "Leaderboards", art, shop, boards)
        {
            _onWorld = onWorld;
            _onBuyWorld = onBuyWorld;

            if (Boards.logo != null)
            {
                PlaceInArt(UIKit.Picture(Board, "Logo", Boards.logo).rectTransform, LogoBox);
            }

            for (int i = 0; i < GridOrder.Length; i++)
            {
                var world = GridOrder[i];
                var sprite = Boards.WorldName(world);
                var button = UIKit.MakeButton(Board, $"World_{world}", "", Color.white, () => Tapped(world));
                if (sprite != null)
                {
                    button.image.sprite = sprite;
                    button.image.preserveAspect = true;
                }
                else
                {
                    button.image.color = Color.clear;   // text only, the whole slot still tappable
                    var label = button.GetComponentInChildren<Text>();
                    label.gameObject.SetActive(false);
                    var text = OutlinedText(button.transform, "Name", WorldName(world), ValueGold);
                    UIKit.Stretch(text.rectTransform);
                }
                PlaceInArt(button.image.rectTransform, Centred(ColumnX[i % 3], RowY[i / 3], NameWidth, NameHeight));
                _names[i] = button;
            }

            _hint = OutlinedText(Board, "Hint", "", Color.white);
            PlaceInArt(_hint.rectTransform, HintBox);
        }

        protected override void OnShow()
        {
            _hint.text = "";
            for (int i = 0; i < GridOrder.Length; i++)
            {
                bool unlocked = SaveManager.Instance != null && SaveManager.Instance.IsWorldUnlocked(GridOrder[i]);
                var tint = unlocked ? Color.white : LockedTint;
                var image = _names[i].image;
                if (image.sprite != null)
                {
                    image.color = tint;
                }
                else
                {
                    _names[i].transform.Find("Name").GetComponent<Text>().color = unlocked ? ValueGold : ValueGold * LockedTint;
                }
            }
        }

        private void Tapped(WorldType world)
        {
            var save = SaveManager.Instance;
            if (save != null && save.IsWorldUnlocked(world))
            {
                _onWorld?.Invoke(world);
                return;
            }

            var data = DataManager.Instance != null ? DataManager.Instance.GetWorldData(world) : null;
            if (data != null && data.purchaseRequired)
            {
                _onBuyWorld?.Invoke();
                return;
            }

            _hint.text = world == WorldType.MeadowRuins ? "" : $"Beat the {WorldName(world - 1)} boss to unlock {WorldName(world)}.";
        }

        public static string WorldName(WorldType world)
        {
            var data = DataManager.Instance != null ? DataManager.Instance.GetWorldData(world) : null;
            return data != null && !string.IsNullOrEmpty(data.displayName) ? data.displayName : world.ToString();
        }
    }
}
