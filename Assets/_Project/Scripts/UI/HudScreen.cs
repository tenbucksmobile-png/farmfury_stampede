using System;
using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Robots;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Gameplay HUD, kept to the minimum so the level reads clearly: one Cluck icon per life top-right (one disappears
    /// with each death), the round pause button bottom-left, and the Commander's hit count top-centre in a boss
    /// level. Everything sits inside the device safe area. Refreshed every frame by GameFlow.
    /// Without art the lives fall back to red squares and pause to a plain "II" button.
    /// </summary>
    public class HudScreen
    {
        public GameObject Root { get; private set; }
        public Button PauseButton { get; private set; }

        private readonly List<Image> _lifeIcons = new();
        private readonly Text _boss;

        private const float EdgeMargin = 40f;
        private const float LifeIconHeight = 96f;
        private const float LifeIconGap = 10f;

        public string BossText => _boss.gameObject.activeSelf ? _boss.text : "";
        public int LifeIconsShown
        {
            get
            {
                int n = 0;
                foreach (var icon in _lifeIcons)
                {
                    if (icon.gameObject.activeSelf) n++;
                }
                return n;
            }
        }

        public HudScreen(Transform canvas, Action onPause, MenuArt art)
        {
            art ??= new MenuArt();
            var root = UIKit.NewRect("Hud", canvas);
            UIKit.Stretch(root);
            Root = root.gameObject;

            var safe = UIKit.NewRect("SafeArea", root);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // Lives: a row of Cluck icons in the top-right corner, the rightmost first to go.
            Sprite life = art.lifeIcon;
            float iconWidth = life != null ? LifeIconHeight * life.rect.width / life.rect.height : 48f;
            float iconHeight = life != null ? LifeIconHeight : 48f;
            for (int i = 0; i < GameManager.LivesPerAttempt; i++)
            {
                var icon = life != null ? UIKit.Picture(safe, $"Life{i + 1}", life) : UIKit.Panel(safe, $"Life{i + 1}", new Color(0.9f, 0.25f, 0.3f, 1f));
                icon.raycastTarget = false;
                float x = -EdgeMargin - (GameManager.LivesPerAttempt - 1 - i) * (iconWidth + LifeIconGap);
                UIKit.Place(icon.rectTransform, Vector2.one, Vector2.one, new Vector2(x, -EdgeMargin * 0.5f), new Vector2(iconWidth, iconHeight));
                _lifeIcons.Add(icon);
            }

            PauseButton = UIKit.MakeButton(safe, "PauseButton", art.pauseButton != null ? "" : "II",
                art.pauseButton != null ? Color.white : new Color(0.2f, 0.25f, 0.38f, 0.95f), () => onPause?.Invoke(), 40);
            if (art.pauseButton != null)
            {
                PauseButton.image.sprite = art.pauseButton;
                PauseButton.image.preserveAspect = true;
            }
            UIKit.Place(PauseButton.image.rectTransform, Vector2.zero, Vector2.zero, new Vector2(EdgeMargin, EdgeMargin),
                Vector2.one * UIKit.RoundButtonSize);

            _boss = UIKit.Label(safe, "BossHits", "", 40, TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.5f));
            UIKit.Place(_boss.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(700f, 56f));

            Root.SetActive(false);
        }

        /// <summary>Reads the live run state into the HUD: one life icon per life left.</summary>
        public void Refresh(GameManager gm, CharacterController2D player)
        {
            var run = gm.RunState;
            for (int i = 0; i < _lifeIcons.Count; i++)
            {
                // Icons are laid out left to right; lives are lost from the right-hand end.
                _lifeIcons[i].gameObject.SetActive(i < run.livesRemaining);
            }

            var boss = CommanderBoss.Active;
            bool showBoss = boss != null && boss.isActiveAndEnabled;
            _boss.gameObject.SetActive(showBoss);
            if (showBoss)
            {
                _boss.text = $"Commander hits: {boss.Hits}/{boss.HitsToDefeat}{(boss.IsStaggered ? "  (stunned!)" : "")}";
            }
        }
    }
}
