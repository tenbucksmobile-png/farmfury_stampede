using System;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// The first screen when the game opens (GameState.MainMenu), laid out from the landing mockup: the finished
    /// poster art (FF_StampedePoster.png - logo, sign and Cluck painted in) filling the screen, a round play button
    /// bottom-left, Exit and the settings cog bottom-right (buttons kept inside the device safe area).
    /// Play -> World Select, Exit -> quit the app, cog -> Shop &amp; Settings.
    /// Button positions are in the 1920x1080 reference canvas, measured off the 1280x720 mockup (x1.5). The poster
    /// covers the screen; on phones wider than 16:9 the overflow is cropped mostly off the bottom (the road) so the
    /// logo at the top survives. Without the poster the screen shows a plain title instead.
    /// </summary>
    public class LandingScreen
    {
        public GameObject Root { get; private set; }
        public Button PlayButton { get; private set; }
        public Button ExitButton { get; private set; }
        public Button SettingsButton { get; private set; }

        private const float EdgeMargin = 70f;
        // Gap between the button row and the bottom of the safe area. Exit is shorter than the round buttons, so it
        // sits 5 higher to keep the three centres level.
        private const float BottomMargin = 40f;
        private const float BackdropPivotY = 0.8f;   // cover-crop takes 80% of the overflow off the bottom

        public LandingScreen(Transform canvas, Action onPlay, Action onExit, Action onSettings, MenuArt art)
        {
            art ??= new MenuArt();
            var root = UIKit.NewRect("Landing", canvas);
            UIKit.Stretch(root);
            Root = root.gameObject;
            var bg = Root.AddComponent<Image>();
            bg.color = UIKit.Dark;
            var backdrop = UIKit.Backdrop(root);
            backdrop.rectTransform.pivot = new Vector2(0.5f, BackdropPivotY);
            UIKit.SetBackdrop(backdrop, art.landingPoster);

            if (art.landingPoster == null)
            {
                var title = UIKit.Label(root, "Title", "FARM FURY: STAMPEDE", 96, TextAnchor.MiddleCenter, UIKit.Accent);
                title.fontStyle = FontStyle.Bold;
                UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(1600f, 140f));
            }

            var safe = UIKit.NewRect("SafeArea", root);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            PlayButton = ArtButton(safe, "PlayButton", art.playButton, "PLAY", onPlay,
                new Vector2(0f, 0f), new Vector2(EdgeMargin + 60f, BottomMargin), new Vector2(170f, 170f));
            SettingsButton = ArtButton(safe, "SettingsButton", art.settingsButton, "SET", onSettings,
                new Vector2(1f, 0f), new Vector2(-EdgeMargin, BottomMargin), new Vector2(165f, 165f));
            ExitButton = ArtButton(safe, "ExitButton", art.exitButton, "EXIT", onExit,
                new Vector2(1f, 0f), new Vector2(-EdgeMargin - 200f, BottomMargin + 5f), new Vector2(340f, 158f));

            Root.SetActive(false);
        }

        // A button showing its art (sized to the rect, aspect kept); without art, a plain labelled button.
        private static Button ArtButton(Transform parent, string name, Sprite sprite, string fallbackLabel, Action onClick,
            Vector2 corner, Vector2 offset, Vector2 size)
        {
            var button = UIKit.MakeButton(parent, name, sprite != null ? "" : fallbackLabel,
                sprite != null ? Color.white : new Color(0.85f, 0.55f, 0.15f, 1f), () => onClick?.Invoke(), 40);
            if (sprite != null)
            {
                button.image.sprite = sprite;
                button.image.preserveAspect = true;
            }
            UIKit.Place(button.image.rectTransform, corner, corner, offset, size);
            return button;
        }
    }
}
