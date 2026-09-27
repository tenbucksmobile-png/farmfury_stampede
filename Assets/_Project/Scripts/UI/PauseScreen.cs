using System;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Pause menu with Arcade's four-action shape: Play, Settings, Restart Level, Quit. Settings opens the
    /// Settings panel (SettingsScreen) on top of this menu.
    /// </summary>
    public class PauseScreen
    {
        public GameObject Root { get; private set; }
        public Button PlayButton { get; private set; }
        public Button SettingsButton { get; private set; }
        public Button RestartButton { get; private set; }
        public Button QuitButton { get; private set; }

        public PauseScreen(Transform canvas, Action onPlay, Action onSettings, Action onRestart, Action onQuit)
        {
            var root = UIKit.NewRect("Pause", canvas);
            UIKit.Stretch(root);
            Root = root.gameObject;
            Root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var menu = UIKit.Panel(root, "Menu", UIKit.Dark);
            UIKit.Place(menu.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 640f));

            var title = UIKit.Label(menu.transform, "Title", "PAUSED", 60, TextAnchor.MiddleCenter, UIKit.Accent);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(560f, 80f));

            PlayButton = MenuButton(menu.transform, "PlayButton", "Play", 0, UIKit.Good, () => onPlay?.Invoke());
            SettingsButton = MenuButton(menu.transform, "SettingsButton", "Settings", 1, UIKit.Card, () => onSettings?.Invoke());
            RestartButton = MenuButton(menu.transform, "RestartButton", "Restart Level", 2, UIKit.Card, () => onRestart?.Invoke());
            QuitButton = MenuButton(menu.transform, "QuitButton", "Quit", 3, new Color(0.6f, 0.25f, 0.25f, 1f), () => onQuit?.Invoke());

            Root.SetActive(false);
        }

        private static Button MenuButton(Transform parent, string name, string label, int index, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var button = UIKit.MakeButton(parent, name, label, color, onClick, 40);
            UIKit.Place(button.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f - index * 110f), new Vector2(440f, 84f));
            return button;
        }

        public void Show()
        {
            Root.SetActive(true);
        }

        public void Hide()
        {
            Root.SetActive(false);
        }
    }
}
