using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// The New Character page shown over Level Complete when the level unlocked a character, in the same look as the
    /// in-level rare-pellet reveal (NewCharacterPage, the 2026-09-29 mockup): sunset backdrop with the logo, the "New
    /// Character" lettering, the character's framed card over the sun with golden rays turning behind it, and
    /// confetti. Several unlocks queue; a tap anywhere shows the next, then returns to Level Complete. The character
    /// is already unlocked in the save.
    /// </summary>
    public class NewCharacterScreen : OverlayScreen
    {
        private readonly NewCharacterPage _page;
        private readonly Queue<CharacterType> _queue = new();

        public NewCharacterScreen(Transform canvas, MenuArt art, ShopArt shop) : base(canvas, "NewCharacter", art, shop, posterBackdrop: false)
        {
            BackButton.gameObject.SetActive(false);
            var rootImage = Root.GetComponent<Image>();
            var tap = Root.AddComponent<Button>();
            tap.targetGraphic = rootImage;
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(Next);

            _page = new NewCharacterPage(Rect, Art);
            _page.Frame.SetAsFirstSibling();
            rootImage.color = _page.HasBackdrop ? Color.black : UIKit.Dark;
            Root.AddComponent<RaysSpinner>().Page = _page;
        }

        /// <summary>Shows each newly unlocked character in turn.</summary>
        public void ShowUnlocks(IEnumerable<CharacterType> characters)
        {
            _queue.Clear();
            foreach (var c in characters) { _queue.Enqueue(c); }
            if (_queue.Count > 0)
            {
                _page.UseWorldBackdrop();
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
            _page.SetCharacter(data, type.ToString());
            _page.Burst();
        }

        // Turns the page's rays while the screen is showing (OverlayScreen isn't a MonoBehaviour).
        private class RaysSpinner : MonoBehaviour
        {
            [System.NonSerialized] public NewCharacterPage Page;
            private void LateUpdate() => Page?.Spin();
        }
    }
}
