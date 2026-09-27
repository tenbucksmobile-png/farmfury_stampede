using System;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Arcade's Leaderboards screen (Settings -> Leaderboards): the Leaderboard sign over every world's World Select
    /// card (two rows of three here - Stampede has six worlds), locked worlds greyed. Tapping an unlocked world opens
    /// its <see cref="WorldDetailScreen"/>; a locked purchase-gated world opens the world shop; any other locked
    /// world shows how to unlock it. The stats are this device's own records (no online board until the shared
    /// Supabase backend exists).
    /// </summary>
    public class LeaderboardsScreen : OverlayScreen
    {
        private static readonly Vector2 CardSize = new(480f, 270f);   // World Select cards are 16:9
        private const float CardGap = 40f;
        private const float TopRowY = 20f;
        private static readonly Color LockedTint = new(0.65f, 0.65f, 0.65f, 1f);

        private readonly Button[] _cards;
        private readonly WorldType[] _worlds;
        private readonly Text _hint;
        private readonly Action<WorldType> _onWorld;
        private readonly Action _onBuyWorld;

        public LeaderboardsScreen(Transform canvas, MenuArt art, ShopArt shop, Action<WorldType> onWorld, Action onBuyWorld)
            : base(canvas, "Leaderboards", art, shop)
        {
            _onWorld = onWorld;
            _onBuyWorld = onBuyWorld;
            HeaderSign(Shop.leaderboardSign, "LEADERBOARDS");

            _worlds = (WorldType[])Enum.GetValues(typeof(WorldType));
            _cards = new Button[_worlds.Length];
            for (int i = 0; i < _worlds.Length; i++)
            {
                var world = _worlds[i];
                int column = i % 3, rowIndex = i / 3;
                _cards[i] = IconButton(Rect, $"WorldCard_{world}", null, world.ToString(), () => Tapped(world));
                UIKit.Place(_cards[i].image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2((column - 1) * (CardSize.x + CardGap), TopRowY - rowIndex * (CardSize.y + CardGap)), CardSize);
            }

            _hint = StatusText(30f);
        }

        protected override void OnShow()
        {
            _hint.text = "";
            for (int i = 0; i < _worlds.Length; i++)
            {
                var data = DataManager.Instance != null ? DataManager.Instance.GetWorldData(_worlds[i]) : null;
                var card = _cards[i];
                var label = card.GetComponentInChildren<Text>(true);
                if (data != null && data.selectCardArt != null)
                {
                    card.image.sprite = data.selectCardArt;
                    card.image.preserveAspect = true;
                    label.text = "";
                }
                else
                {
                    label.text = data != null ? data.displayName : _worlds[i].ToString();
                }

                bool unlocked = SaveManager.Instance != null && SaveManager.Instance.IsWorldUnlocked(_worlds[i]);
                var baseColor = card.image.sprite != null ? Color.white : Wood;
                card.image.color = unlocked ? baseColor : baseColor * LockedTint;
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
