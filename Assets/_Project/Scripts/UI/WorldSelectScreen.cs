using System;
using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// World Select: six world cards (GDD Section 7) with lock state and stars earned. A world unlocks once the
    /// previous world's boss level has been completed; only Meadow Ruins is open on a fresh save. Locked cards
    /// stay locked until their content exists.
    /// </summary>
    public class WorldSelectScreen
    {
        public class Card
        {
            public WorldType world;
            public GameObject root;
            public Button button;
            public Text status;
            public bool unlocked;
        }

        public GameObject Root { get; private set; }
        public IReadOnlyList<Card> Cards => _cards;

        private readonly List<Card> _cards = new();

        public WorldSelectScreen(Transform canvas, Action<WorldType> onEnter)
        {
            var root = UIKit.NewRect("WorldSelect", canvas);
            UIKit.Stretch(root);
            Root = root.gameObject;
            var bg = Root.AddComponent<Image>();
            bg.color = UIKit.Dark;

            var title = UIKit.Label(root, "Title", "SELECT A WORLD", 64, TextAnchor.MiddleCenter, UIKit.Accent);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1200f, 90f));

            foreach (WorldType world in Enum.GetValues(typeof(WorldType)))
            {
                int index = (int)world;
                int col = index % 3, row = index / 3;
                var card = UIKit.Panel(root, $"Card_{world}", UIKit.Card);
                UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2((col - 1) * 590f, 130f - row * 330f), new Vector2(560f, 300f));

                var name = UIKit.Label(card.transform, "Name", world.ToString(), 44, TextAnchor.MiddleCenter);
                UIKit.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(520f, 60f));
                var status = UIKit.Label(card.transform, "Status", "", 30, TextAnchor.MiddleCenter, UIKit.Muted);
                UIKit.Place(status.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(520f, 90f));

                var captured = world;
                var button = UIKit.MakeButton(card.transform, "EnterButton", "ENTER", UIKit.Good, () => onEnter?.Invoke(captured), 34);
                UIKit.Place(button.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(260f, 64f));

                _cards.Add(new Card { world = world, root = card.gameObject, button = button, status = status });
            }

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
                card.root.GetComponent<Image>().color = card.unlocked ? UIKit.Card : UIKit.CardLocked;

                var nameLabel = card.root.transform.Find("Name").GetComponent<Text>();
                nameLabel.text = data != null ? data.displayName : card.world.ToString();

                if (card.unlocked)
                {
                    int levels = DataManager.Instance.GetWorldLevels(card.world).Count;
                    card.status.text = levels == 0
                        ? "No levels yet"
                        : $"Stars {save.GetWorldStars(card.world)}/{save.GetWorldMaxStars(card.world)}\nBoss: {(save.IsWorldBossCleared(card.world) ? "CLEARED" : "not cleared")}";
                }
                else
                {
                    var previous = DataManager.Instance.GetWorldData(card.world - 1);
                    card.status.text = $"LOCKED\nBeat the boss of {(previous != null ? previous.displayName : (card.world - 1).ToString())}";
                }
            }
        }
    }
}
