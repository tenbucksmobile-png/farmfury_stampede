using System.Linq;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Arcade's world leaderboard detail, opened from Leaderboards: the world's card on the left and its best
    /// records on wooden plaques on the right - stars, levels cleared, gated secrets found and whether the boss has
    /// fallen. Stampede's scoring has no points or times yet (GDD: time isn't scored), so these are its records.
    /// </summary>
    public class WorldDetailScreen : OverlayScreen
    {
        private static readonly Vector2 CardSize = new(720f, 405f);
        private const float RowHeight = 96f;
        private const float RowGap = 20f;
        private const float LabelWidth = 420f;
        private const float ValueWidth = 340f;

        private readonly Image _card;
        private readonly Text _title;
        private readonly Text _stars, _levels, _secrets, _boss;

        public WorldDetailScreen(Transform canvas, MenuArt art, ShopArt shop) : base(canvas, "WorldDetail", art, shop)
        {
            HeaderSign(Shop.leaderboardSign, "LEADERBOARDS");

            _card = UIKit.Picture(Rect, "WorldCard", null);
            UIKit.Place(_card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40f, -110f), CardSize);

            _title = UIKit.Label(Rect, "WorldName", "", 60, TextAnchor.MiddleCenter, UIKit.Accent);
            _title.fontStyle = FontStyle.Bold;
            _title.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            UIKit.Place(_title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40f, -110f), CardSize);

            float top = -110f + 2f * (RowHeight + RowGap) - (RowHeight + RowGap) * 0.5f;
            _stars = StatRow("Stars", top);
            _levels = StatRow("Levels cleared", top - (RowHeight + RowGap));
            _secrets = StatRow("Secrets found", top - 2f * (RowHeight + RowGap));
            _boss = StatRow("Boss", top - 3f * (RowHeight + RowGap));
        }

        private Text StatRow(string label, float y)
        {
            var name = UIKit.Label(Rect, label + "Label", label, 44, TextAnchor.MiddleLeft, Color.white);
            name.fontStyle = FontStyle.Bold;
            name.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);
            UIKit.Place(name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, y), new Vector2(LabelWidth, RowHeight));

            var plaque = PlaqueButton(Rect, label + "Value", "", 44, null);
            plaque.interactable = false;
            var colors = plaque.colors;
            colors.disabledColor = Color.white;
            plaque.colors = colors;
            UIKit.Place(plaque.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f + LabelWidth, y), new Vector2(ValueWidth, RowHeight));
            return plaque.GetComponentInChildren<Text>();
        }

        public void Show(WorldType world)
        {
            var dm = DataManager.Instance;
            var save = SaveManager.Instance;
            var data = dm != null ? dm.GetWorldData(world) : null;

            _card.sprite = data != null ? data.selectCardArt : null;
            _card.gameObject.SetActive(_card.sprite != null);
            _title.text = _card.sprite == null ? LeaderboardsScreen.WorldName(world) : "";

            var levels = dm != null ? dm.GetWorldLevels(world) : new System.Collections.Generic.List<LevelData>();
            if (levels.Count == 0 || save == null)
            {
                _stars.text = _levels.text = _secrets.text = "-";
                _boss.text = "Soon";
            }
            else
            {
                var gated = levels.Where(l => l.hasCharacterGatedSecret).ToList();
                _stars.text = $"{save.GetWorldStars(world)} / {save.GetWorldMaxStars(world)}";
                _levels.text = $"{levels.Count(l => save.IsLevelCompleted(l.levelId))} / {levels.Count}";
                _secrets.text = $"{gated.Count(l => save.IsSecretFound(l.levelId))} / {gated.Count}";
                _boss.text = save.IsWorldBossCleared(world) ? "Beaten!" : "Not yet";
            }

            Show();
        }
    }
}
