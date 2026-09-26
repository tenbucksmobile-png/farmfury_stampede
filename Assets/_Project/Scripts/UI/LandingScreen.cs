using System;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// The first screen when the game opens (GameState.MainMenu), laid out from the landing mockup: the finished
    /// poster art (FF_StampedePoster.png - logo, sign and Cluck painted in) across the screen, a round play button
    /// bottom-left, Exit and the settings cog bottom-right (buttons kept inside the device safe area).
    /// Play -> World Select, Exit -> quit the app, cog -> Shop &amp; Settings.
    /// Button positions are in the 1920x1080 reference canvas, measured off the 1280x720 mockup (x1.5). The poster is
    /// drawn slightly zoomed out from "cover" (<see cref="PosterZoom"/>, via <see cref="ZoomedCover"/>) so less of it
    /// is cropped; the gap it leaves shows the plain farm scene (Canvas.png, the poster without logo/sign/Cluck) as a
    /// cover backdrop behind it, and the poster's edges are faded (the _Soft copy setup makes) so the two blend.
    /// Any remaining overflow is cropped mostly off the bottom (the road) so the logo at the top survives.
    /// Without the poster the screen shows a plain title instead.
    /// </summary>
    public class LandingScreen
    {
        public GameObject Root { get; private set; }
        public Button PlayButton { get; private set; }
        public Button ExitButton { get; private set; }
        public Button SettingsButton { get; private set; }

        private const float EdgeMargin = 70f;
        // Gap between the button row and the bottom of the safe area.
        private const float BottomMargin = 40f;
        // Round buttons share the game-wide size; Exit (552x256 art, same height as the 256px round art) is drawn at
        // the same scale, so all three are the same height and sit level.
        private const float ButtonSize = UIKit.RoundButtonSize;
        private static readonly Vector2 ExitSize = new(ButtonSize * 552f / 256f, ButtonSize);
        private const float ButtonGap = 30f;
        private const float BackdropPivotY = 0.8f;   // cover-crop takes 80% of the overflow off the bottom
        private const float PosterZoom = 0.92f;      // poster size as a fraction of full cover

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
            UIKit.SetBackdrop(backdrop, art.landingBackdrop);

            if (art.landingPoster != null)
            {
                var poster = UIKit.Picture(root, "Poster", art.landingPoster);
                poster.preserveAspect = false;
                var fit = poster.gameObject.AddComponent<ZoomedCover>();
                fit.aspect = art.landingPoster.rect.width / art.landingPoster.rect.height;
                fit.zoom = art.landingBackdrop != null ? PosterZoom : 1f;   // nothing to show behind it: keep covering
                fit.pivotY = BackdropPivotY;
            }
            else
            {
                var title = UIKit.Label(root, "Title", "FARM FURY: STAMPEDE", 96, TextAnchor.MiddleCenter, UIKit.Accent);
                title.fontStyle = FontStyle.Bold;
                UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(1600f, 140f));
            }

            var safe = UIKit.NewRect("SafeArea", root);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            PlayButton = ArtButton(safe, "PlayButton", art.playButton, "PLAY", onPlay,
                new Vector2(0f, 0f), new Vector2(EdgeMargin + 60f, BottomMargin), new Vector2(ButtonSize, ButtonSize));
            SettingsButton = ArtButton(safe, "SettingsButton", art.settingsButton, "SET", onSettings,
                new Vector2(1f, 0f), new Vector2(-EdgeMargin, BottomMargin), new Vector2(ButtonSize, ButtonSize));
            ExitButton = ArtButton(safe, "ExitButton", art.exitButton, "EXIT", onExit,
                new Vector2(1f, 0f), new Vector2(-EdgeMargin - ButtonSize - ButtonGap, BottomMargin), ExitSize);

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
