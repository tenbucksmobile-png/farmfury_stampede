using System;
using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Level Select for one world: a grid of level tiles (regular levels, then the boss) showing lock state and
    /// stars, plus a small "?" icon on any COMPLETED level whose character-gated secret has not been found yet
    /// (found = a crop of the secret cluster was collected, not merely the level cleared).
    /// </summary>
    public class LevelSelectScreen
    {
        public class Tile
        {
            public LevelData level;
            public GameObject root;
            public Button button;
            public bool unlocked;
            public bool completed;
            public int stars;
            public GameObject secretIcon;
            public GameObject lockLabel;
        }

        public GameObject Root { get; private set; }
        public WorldType World { get; private set; }
        public IReadOnlyList<Tile> Tiles => _tiles;

        private readonly List<Tile> _tiles = new();
        private readonly Transform _gridParent;
        private readonly Text _title;
        private readonly Text _empty;
        private readonly Action<LevelData> _onPick;

        public LevelSelectScreen(Transform canvas, Action<LevelData> onPick, Action onBack)
        {
            _onPick = onPick;

            var root = UIKit.NewRect("LevelSelect", canvas);
            UIKit.Stretch(root);
            Root = root.gameObject;
            Root.AddComponent<Image>().color = UIKit.Dark;

            _title = UIKit.Label(root, "Title", "", 60, TextAnchor.MiddleCenter, UIKit.Accent);
            UIKit.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1300f, 90f));

            var back = UIKit.MakeButton(root, "BackButton", "< Worlds", new Color(0.25f, 0.3f, 0.45f, 1f), () => onBack?.Invoke(), 34);
            UIKit.Place(back.image.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(240f, 70f));

            _gridParent = UIKit.NewRect("Grid", root);
            UIKit.Stretch((RectTransform)_gridParent);

            _empty = UIKit.Label(root, "Empty", "No levels in this world yet", 44, TextAnchor.MiddleCenter, UIKit.Muted);
            UIKit.Place(_empty.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 80f));

            Root.SetActive(false);
        }

        /// <summary>Rebuilds the grid for the world from the current save.</summary>
        public void Show(WorldType world)
        {
            World = world;
            var data = DataManager.Instance.GetWorldData(world);
            _title.text = data != null ? data.displayName : world.ToString();

            foreach (var tile in _tiles)
            {
                UnityEngine.Object.Destroy(tile.root);
            }
            _tiles.Clear();

            var levels = DataManager.Instance.GetWorldLevels(world);
            _empty.gameObject.SetActive(levels.Count == 0);
            var save = SaveManager.Instance;

            for (int i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                int col = i % 5, row = i / 5;
                bool unlocked = save.IsLevelUnlocked(level);
                bool completed = save.IsLevelCompleted(level.levelId);
                int stars = save.GetLevelStars(level.levelId);
                bool secretUnfound = completed && level.hasCharacterGatedSecret && !save.IsSecretFound(level.levelId);

                var tile = UIKit.Panel(_gridParent, $"Tile_{level.levelId}", unlocked ? (level.isBossLevel ? new Color(0.45f, 0.2f, 0.2f, 1f) : UIKit.Card) : UIKit.CardLocked);
                UIKit.Place(tile.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2((col - 2) * 340f, 110f - row * 270f), new Vector2(320f, 240f));

                string number = level.isBossLevel ? "BOSS" : (i + 1).ToString();
                var numberLabel = UIKit.Label(tile.transform, "Number", number, level.isBossLevel ? 46 : 64, TextAnchor.MiddleCenter);
                UIKit.Place(numberLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(300f, 80f));
                var nameLabel = UIKit.Label(tile.transform, "Name", level.displayName, 26, TextAnchor.MiddleCenter, unlocked ? Color.white : UIKit.Muted);
                UIKit.Place(nameLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(300f, 70f));

                var bar = UIKit.StarBar(tile.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-45f, 18f), 30f);
                UIKit.SetStars(bar, stars);

                var lockLabel = UIKit.Label(tile.transform, "Lock", "LOCKED", 34, TextAnchor.MiddleCenter, new Color(1f, 0.5f, 0.5f));
                UIKit.Place(lockLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(300f, 44f));
                lockLabel.gameObject.SetActive(!unlocked);
                if (!unlocked)
                {
                    foreach (var star in bar)
                    {
                        star.gameObject.SetActive(false);
                    }
                }

                var icon = UIKit.Panel(tile.transform, "SecretIcon", new Color(0.85f, 0.3f, 0.9f, 1f));
                UIKit.Place(icon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(50f, 50f));
                var mark = UIKit.Label(icon.transform, "Mark", "?", 36);
                UIKit.Stretch(mark.rectTransform);
                icon.gameObject.SetActive(secretUnfound);

                var captured = level;
                var button = tile.gameObject.AddComponent<Button>();
                button.targetGraphic = tile;
                button.interactable = unlocked;
                button.onClick.AddListener(() => _onPick?.Invoke(captured));

                _tiles.Add(new Tile
                {
                    level = level, root = tile.gameObject, button = button, unlocked = unlocked, completed = completed,
                    stars = stars, secretIcon = icon.gameObject, lockLabel = lockLabel.gameObject
                });
            }
        }
    }
}
