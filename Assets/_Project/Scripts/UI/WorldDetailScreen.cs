using System.Collections.Generic;
using System.Linq;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// A world's leaderboard page, per the Leaderboard mockup (Leaderboard_Mock2.png): the world's name lettering
    /// top-left, the LeaderBoard sign top-right and three evenly spaced rows of records over the wooden sign, each
    /// consolidated over every level of the world the player has completed:
    ///   Best Farm Fury - the thumbs-up of the character who set the most level best scores | coin: coins earned here
    ///   High Score     - the best score of each level, summed                              | kernel: best crops / total
    ///   Fastest Time   - the fastest clear of each level, summed                            | apple: rare pellets found / total
    /// Scores come from <see cref="ScoreCalculator"/>; records are kept by SaveManager.RecordLevelResult.
    /// </summary>
    public class WorldDetailScreen : LeaderboardPage
    {
        // Mockup geometry: three rows on an even pitch, four columns (label, value, icon, value).
        private static readonly float[] RowY = { 345f, 425f, 505f };
        private const float LabelLeft = 128f, LabelHeight = 56f, LabelMaxWidth = 360f;
        private const float ValueX = 595f, IconX = 868f, SecondValueX = 1068f;
        private const float PlaqueWidth = 136f, PlaqueHeight = 62f, IconBox = 78f, CharacterIconSize = 90f;
        private static readonly Rect TitleBox = new(92f, 58f, 300f, 180f);
        private static readonly Color Dimmed = new(1f, 1f, 1f, 0.35f);

        private readonly Image _title;
        private readonly Text _titleText;
        private readonly Image _bestCharacter;
        private readonly Text _highScore, _fastestTime, _coins, _crops, _pellets;

        public WorldDetailScreen(Transform canvas, MenuArt art, ShopArt shop, LeaderboardArt boards)
            : base(canvas, "WorldDetail", art, shop, boards)
        {
            _title = UIKit.Picture(Board, "WorldName", null);
            PlaceInArt(_title.rectTransform, TitleBox);
            _titleText = OutlinedText(Board, "WorldNameText", "", ValueGold);
            PlaceInArt(_titleText.rectTransform, TitleBox);

            RowLabel("BestFarmFury", Boards.bestLabel, "Best Farm Fury", RowY[0]);
            RowLabel("HighScore", Boards.highScoreLabel, "High Score", RowY[1]);
            RowLabel("FastestTime", Boards.fastestTimeLabel, "Fastest Time", RowY[2]);

            _bestCharacter = UIKit.Picture(Board, "BestCharacter", null);
            PlaceInArt(_bestCharacter.rectTransform, Centred(ValueX, RowY[0], CharacterIconSize, CharacterIconSize));
            _highScore = ValuePlaque("HighScoreValue", ValueX, RowY[1]);
            _fastestTime = ValuePlaque("FastestTimeValue", ValueX, RowY[2]);

            Icon("CoinIcon", Boards.coin, RowY[0]);
            Icon("KernelIcon", Art.scoreIcon, RowY[1]);
            Icon("PelletIcon", Art.rarePellet, RowY[2]);
            _coins = ValuePlaque("CoinsValue", SecondValueX, RowY[0]);
            _crops = ValuePlaque("CropsValue", SecondValueX, RowY[1]);
            _pellets = ValuePlaque("PelletsValue", SecondValueX, RowY[2]);
        }

        // A lettering label, left-aligned, as wide as its art at the row height (capped short of the value column).
        private void RowLabel(string name, Sprite sprite, string fallback, float y)
        {
            float width = sprite != null
                ? Mathf.Min(LabelMaxWidth, LabelHeight * sprite.rect.width / sprite.rect.height)
                : LabelMaxWidth;
            var box = new Rect(LabelLeft, y - LabelHeight * 0.5f, width, LabelHeight);
            if (sprite != null)
            {
                PlaceInArt(UIKit.Picture(Board, name, sprite).rectTransform, box);
                return;
            }

            var text = OutlinedText(Board, name, fallback, ValueGold);
            text.alignment = TextAnchor.MiddleLeft;
            PlaceInArt(text.rectTransform, box);
        }

        private void Icon(string name, Sprite sprite, float y)
        {
            if (sprite != null)
            {
                PlaceInArt(UIKit.Picture(Board, name, sprite).rectTransform, Centred(IconX, y, IconBox, IconBox));
            }
        }

        // The golden plaque with the value on it in outlined lettering.
        private Text ValuePlaque(string name, float x, float y)
        {
            var box = Centred(x, y, PlaqueWidth, PlaqueHeight);
            if (Shop.plaque != null)
            {
                var plaque = UIKit.Picture(Board, name + "Plaque", Shop.plaque);
                plaque.preserveAspect = false;
                if (Shop.plaque.border != Vector4.zero)
                {
                    plaque.type = Image.Type.Sliced;
                    plaque.pixelsPerUnitMultiplier = Shop.plaque.rect.height / 70f;
                }
                PlaceInArt(plaque.rectTransform, box);
            }

            var text = OutlinedText(Board, name, "-", Color.white);
            // Keep the digits inside the plaque's flat face (the art has a thick rim and a drop shadow below).
            PlaceInArt(text.rectTransform, new Rect(box.x + 14f, box.y + 8f, box.width - 28f, box.height - 20f));
            return text;
        }

        public void Show(WorldType world)
        {
            var sprite = Boards.WorldName(world);
            _title.sprite = sprite;
            _title.gameObject.SetActive(sprite != null);
            _titleText.text = sprite == null ? LeaderboardsScreen.WorldName(world) : "";

            var dm = DataManager.Instance;
            var save = SaveManager.Instance;
            var levels = dm != null ? dm.GetWorldLevels(world) : new List<LevelData>();
            var played = save != null ? levels.Where(l => save.IsLevelCompleted(l.levelId)).ToList() : new List<LevelData>();

            // Best Farm Fury: whoever holds the most level best scores (ties: the higher summed score).
            var best = save == null ? null : played
                .Select(l => (character: save.GetBestScoreCharacter(l.levelId), score: save.GetBestScore(l.levelId)))
                .Where(r => r.character.HasValue)
                .GroupBy(r => r.character.Value)
                .OrderByDescending(g => g.Count()).ThenByDescending(g => g.Sum(r => r.score))
                .Select(g => (CharacterType?)g.Key)
                .FirstOrDefault();
            var bestData = dm != null ? dm.GetCharacterData(best ?? CharacterType.Cluck) : null;
            _bestCharacter.sprite = bestData != null && bestData.lifeIcon != null ? bestData.lifeIcon : Art.lifeIcon;
            _bestCharacter.color = best.HasValue ? Color.white : Dimmed;
            _bestCharacter.gameObject.SetActive(_bestCharacter.sprite != null);

            var timed = save == null ? new List<float>() : played.Select(l => save.GetFastestTime(l.levelId)).Where(t => t > 0f).ToList();
            _highScore.text = played.Count > 0 ? played.Sum(l => save.GetBestScore(l.levelId)).ToString("N0") : "-";
            _fastestTime.text = timed.Count > 0 ? FormatTime(timed.Sum()) : "-";

            _coins.text = save != null ? save.GetWorldCoinsEarned(world).ToString("N0") : "-";
            _crops.text = played.Count > 0
                ? $"{played.Sum(l => save.GetBestCrops(l.levelId))}/{played.Sum(l => save.GetLevelCropTotal(l.levelId))}"
                : "-";

            var pelletLevels = levels.Where(HasRarePellet).ToList();
            _pellets.text = pelletLevels.Count > 0 && save != null
                ? $"{pelletLevels.Count(l => save.IsRarePelletFound(l.levelId))}/{pelletLevels.Count}"
                : "-";

            Show();
        }

        private static bool HasRarePellet(LevelData level) =>
            level != null && level.levelPrefab != null && level.levelPrefab.GetComponentInChildren<RarePelletPickup>(true) != null;

        /// <summary>m:ss, or h:mm:ss from an hour up.</summary>
        public static string FormatTime(float seconds)
        {
            int total = Mathf.RoundToInt(seconds);
            int h = total / 3600, m = total / 60 % 60, s = total % 60;
            return h > 0 ? $"{h}:{m:00}:{s:00}" : $"{m}:{s:00}";
        }
    }
}
