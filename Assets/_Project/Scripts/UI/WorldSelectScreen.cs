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
    /// completed; only Meadow Ruins is open on a fresh save. Per the World Select mockup each card is just the world's
    /// art (name baked in) with a round play button in its bottom-left corner, under the wooden banner header; a
    /// locked world's card and button are greyed out and can't be entered. Without art, the card falls back to a
    /// coloured panel with the world name as text.
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
        public IReadOnlyList<Card> Cards => _cards;

        private readonly List<Card> _cards = new();
        private const float CardWidth = 570f;
        private const float CardHeight = CardWidth * 9f / 16f;   // the WS_ card art is 16:9
        // Play button: measured off the mockup (centre ~13% in from the left, ~16% up from the bottom), a little
        // bigger than the mockup so it's an easy tap on a phone.
        private const float PlayButtonSize = 84f;
        private static readonly Vector2 PlayButtonCentre = new(74f, 52f);
        private static readonly Color LockedTint = new(0.4f, 0.4f, 0.42f, 1f);

        public WorldSelectScreen(Transform canvas, Action<WorldType> onEnter, MenuArt art)
        {
            art ??= new MenuArt();
            var root = UIKit.NewRect("WorldSelect", canvas);
            UIKit.Stretch(root);
            Root = root.gameObject;
            var bg = Root.AddComponent<Image>();
            bg.color = UIKit.Dark;

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
                var card = UIKit.Panel(root, $"Card_{world}", UIKit.Card);
                card.raycastTarget = false;
                UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2((col - 1) * 600f, 85f - row * 345f), new Vector2(CardWidth, CardHeight));

                var name = UIKit.Label(card.transform, "Name", world.ToString(), 44, TextAnchor.MiddleCenter);
                UIKit.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(520f, 60f));

                var captured = world;
                Button button;
                if (art.playButton != null)
                {
                    button = UIKit.MakeButton(card.transform, "PlayButton", "", Color.white, () => onEnter?.Invoke(captured));
                    button.image.sprite = art.playButton;
                    button.image.preserveAspect = true;
                    var colors = button.colors;
                    colors.disabledColor = LockedTint;   // greyed with the card while the world is locked
                    button.colors = colors;
                    UIKit.Place(button.image.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), PlayButtonCentre, new Vector2(PlayButtonSize, PlayButtonSize));
                }
                else
                {
                    button = UIKit.MakeButton(card.transform, "PlayButton", "PLAY", UIKit.Good, () => onEnter?.Invoke(captured), 32);
                    UIKit.Place(button.image.rectTransform, Vector2.zero, Vector2.zero, new Vector2(18f, 18f), new Vector2(170f, 64f));
                }

                _cards.Add(new Card { world = world, root = card.gameObject, button = button });
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
