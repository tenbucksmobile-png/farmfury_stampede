using System;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Shop &amp; Settings, opened from the landing screen's cog. Placeholder until it's designed: the landing poster
    /// dimmed, a Shop panel and a Settings panel (both "coming soon"; the shop arrives with Phase 6's IAP work) and the
    /// round back button top-left of the safe area, which returns to the landing screen.
    /// </summary>
    public class ShopSettingsScreen
    {
        public GameObject Root { get; private set; }
        public Button BackButton { get; private set; }

        private const float BackButtonSize = 120f;
        private const float EdgeMargin = 40f;

        public ShopSettingsScreen(Transform canvas, Action onBack, MenuArt art)
        {
            art ??= new MenuArt();
            var root = UIKit.NewRect("ShopSettings", canvas);
            UIKit.Stretch(root);
            Root = root.gameObject;
            var bg = Root.AddComponent<Image>();
            bg.color = UIKit.Dark;
            UIKit.SetBackdrop(UIKit.Backdrop(root), art.landingPoster, 0.45f);

            var title = UIKit.Label(root, "Title", "SHOP & SETTINGS", 72, TextAnchor.MiddleCenter, UIKit.Accent);
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<Outline>().effectDistance = new Vector2(4f, -4f);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1300f, 100f));

            Section(root, "Shop", "SHOP", "Coming soon", -380f);
            Section(root, "Settings", "SETTINGS", "Coming soon", 380f);

            var safe = UIKit.NewRect("SafeArea", root);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            BackButton = UIKit.MakeButton(safe, "BackButton", art.backButton != null ? "" : "<",
                new Color(0.62f, 0.38f, 0.17f, 1f), () => onBack?.Invoke(), 72);
            if (art.backButton != null)
            {
                BackButton.image.sprite = art.backButton;
                BackButton.image.color = Color.white;
                BackButton.image.preserveAspect = true;
            }
            UIKit.Place(BackButton.image.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(EdgeMargin, -EdgeMargin),
                new Vector2(BackButtonSize, BackButtonSize));

            Root.SetActive(false);
        }

        private static void Section(Transform parent, string name, string heading, string body, float x)
        {
            var panel = UIKit.Panel(parent, name, new Color(0.45f, 0.27f, 0.12f, 0.92f));
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -40f), new Vector2(680f, 640f));

            var head = UIKit.Label(panel.transform, "Heading", heading, 56, TextAnchor.MiddleCenter, UIKit.Accent);
            head.fontStyle = FontStyle.Bold;
            UIKit.Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(600f, 80f));

            var text = UIKit.Label(panel.transform, "Body", body, 40, TextAnchor.MiddleCenter, UIKit.Muted);
            UIKit.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 80f));
        }
    }
}
