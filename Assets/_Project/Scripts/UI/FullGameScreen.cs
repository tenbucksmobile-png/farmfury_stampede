using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Web demo: the "full game" panel. Opened by tapping any world other than Meadow Ruins on World Select (that
    /// world's badge, "<World> is in the full game"), and once after the Meadow Ruins boss is beaten ("You cleared
    /// Meadow Ruins!"). The full game is not in the stores yet, so it only says it is coming to Google Play and the
    /// App Store - add the store badges and links here once it is. Back (or Esc) closes it.
    /// </summary>
    public class FullGameScreen : OverlayScreen
    {
        private const string ComingSoon = "Coming soon to Google Play and the App Store.";

        private readonly Image _badge;
        private readonly Text _title;
        private readonly Text _body;
        private System.Action _onClosed;

        public FullGameScreen(Transform canvas, MenuArt art, ShopArt shop) : base(canvas, "FullGame", art, shop)
        {
            _title = UIKit.Label(Rect, "Title", "", 76, TextAnchor.MiddleCenter, UIKit.Accent);
            _title.fontStyle = FontStyle.Bold;
            _title.gameObject.AddComponent<Outline>().effectDistance = new Vector2(4f, -4f);
            UIKit.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1500f, 120f));

            _badge = UIKit.Picture(Rect, "WorldBadge", null);
            _badge.preserveAspect = true;
            _badge.raycastTarget = false;
            UIKit.Place(_badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(460f, 460f));

            _body = UIKit.Label(Rect, "Body", "", 44, TextAnchor.MiddleCenter, Color.white);
            _body.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            UIKit.Place(_body.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(1400f, 150f));
        }

        /// <summary>A world that isn't in the demo was tapped.</summary>
        public void ShowLockedWorld(WorldType world)
        {
            var data = DataManager.Instance != null ? DataManager.Instance.GetWorldData(world) : null;
            string name = data != null ? data.displayName : world.ToString();
            Open($"{name} is in the full game", $"The demo has Meadow Ruins. {ComingSoon}", data, null);
        }

        /// <summary>The demo's last level is beaten; onClosed runs when the panel is closed.</summary>
        public void ShowDemoComplete(System.Action onClosed)
        {
            var data = DataManager.Instance != null ? DataManager.Instance.GetWorldData(WorldType.FrozenTundra) : null;
            Open("You cleared Meadow Ruins!", $"Frozen Tundra and seven more worlds are waiting in the full game. {ComingSoon}", data, onClosed);
        }

        private void Open(string title, string body, WorldData badgeWorld, System.Action onClosed)
        {
            _onClosed = onClosed;
            _title.text = title;
            _body.text = body;
            var art = badgeWorld == null ? null : badgeWorld.selectBadge != null ? badgeWorld.selectBadge : badgeWorld.selectCardArt;
            _badge.sprite = art;
            _badge.gameObject.SetActive(art != null);
            Show();
        }

        public override void Hide()
        {
            bool wasOpen = IsOpen;
            base.Hide();
            if (wasOpen)
            {
                var then = _onClosed;
                _onClosed = null;
                then?.Invoke();
            }
        }
    }
}
