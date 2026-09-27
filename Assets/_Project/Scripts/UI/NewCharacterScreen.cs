using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// The New Character page (per its mockup), shown over Level Complete when the level unlocked a character: the
    /// full New Character canvas (sign and logo painted in; Environment/NewCharacter_Canvas.png, widened by setup like
    /// the results art) with the character's framed card (CharacterData.selectCard, name baked in) in the space in the
    /// middle, and confetti raining down. Several unlocks queue; a tap anywhere shows the next, then returns to Level
    /// Complete. The character is already unlocked in the save, so Character Select offers it from now on.
    /// Without the canvas: the dark panel with the "NEW CHARACTER" title and the card.
    /// </summary>
    public class NewCharacterScreen : OverlayScreen
    {
        // The card's box in the 1280x720 canvas (the empty space under the sign, left of the windmill).
        private static readonly Rect CardBox = new(538f, 313f, 260f, 260f);

        private readonly Image _card;
        private readonly Text _name;
        private readonly ConfettiBurst _confetti;
        private readonly Queue<CharacterType> _queue = new();

        public NewCharacterScreen(Transform canvas, MenuArt art, ShopArt shop) : base(canvas, "NewCharacter", art, shop, posterBackdrop: false)
        {
            BackButton.gameObject.SetActive(false);
            var rootImage = Root.GetComponent<Image>();
            rootImage.color = UIKit.Dark;
            var tap = Root.AddComponent<Button>();
            tap.targetGraphic = rootImage;
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(Next);

            if (Art.newCharacterBackground != null)
            {
                var backdrop = UIKit.Picture(Rect, "Backdrop", Art.newCharacterBackground);
                backdrop.gameObject.SetActive(true);
                var fit = backdrop.gameObject.AddComponent<FitContent>();
                fit.contentSize = new Vector2(1280f, 720f);
                fit.spriteSize = new Vector2(1280f + 2f * ResultsScreen.ArtPad, 720f);
                backdrop.transform.SetAsFirstSibling();
                _card = UIKit.Picture(backdrop.transform, "CharacterCard", null);
                ResultsScreen.PlaceInArt(_card.rectTransform, CardBox);
            }
            else
            {
                var title = UIKit.Label(Rect, "Title", "NEW CHARACTER!", 80, TextAnchor.MiddleCenter, UIKit.Accent);
                title.fontStyle = FontStyle.Bold;
                UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1300f, 120f));
                _card = UIKit.Picture(Rect, "CharacterCard", null);
                UIKit.Place(_card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(420f, 420f));
            }

            // Only drawn when the character has no framed card (its name is baked into the card art).
            _name = UIKit.Label(_card.transform, "Name", "", 56, TextAnchor.MiddleCenter, UIKit.Accent);
            _name.fontStyle = FontStyle.Bold;
            _name.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            UIKit.Stretch(_name.rectTransform);

            var particles = UIKit.NewRect("Confetti", Rect);
            UIKit.Stretch(particles);
            _confetti = particles.gameObject.AddComponent<ConfettiBurst>();
            _confetti.particlesRoot = particles;
        }

        /// <summary>Shows each newly unlocked character in turn.</summary>
        public void ShowUnlocks(IEnumerable<CharacterType> characters)
        {
            _queue.Clear();
            foreach (var c in characters) { _queue.Enqueue(c); }
            if (_queue.Count > 0)
            {
                Show();
                ShowNext();
            }
        }

        private void Next()
        {
            if (_queue.Count > 0) { ShowNext(); } else { Hide(); }
        }

        private void ShowNext()
        {
            var type = _queue.Dequeue();
            var data = DataManager.Instance != null ? DataManager.Instance.GetCharacterData(type) : null;
            var card = data != null ? (data.selectCard != null ? data.selectCard : data.placeholderSprite) : null;
            _card.sprite = card;
            _card.gameObject.SetActive(true);
            _card.color = card != null ? Color.white : new Color(0f, 0f, 0f, 0f);
            _name.text = data == null || data.selectCard == null ? (data != null ? data.displayName : type.ToString()) : "";
            _confetti.transform.SetAsLastSibling();
            _confetti.Burst(90, 3f);
        }
    }
}
