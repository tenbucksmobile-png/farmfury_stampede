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
    /// Gameplay HUD (GDD Section 7): crops top-left, lives top-right, character portrait + ability uses
    /// bottom-left, pause top-centre. Refreshed every frame by GameFlow so it always reflects the live run.
    /// </summary>
    public class HudScreen
    {
        public GameObject Root { get; private set; }
        public Button PauseButton { get; private set; }

        private Text _crops;
        private Text _lives;
        private readonly List<Image> _lifeIcons = new();
        private Image _portrait;
        private Text _ability;
        private Text _boss;

        public string CropsText => _crops.text;
        public string AbilityText => _ability.text;
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

        public HudScreen(Transform canvas, Action onPause)
        {
            var root = UIKit.NewRect("Hud", canvas);
            UIKit.Stretch(root);
            Root = root.gameObject;

            _crops = UIKit.Label(root, "Crops", "Crops: 0", 44, TextAnchor.MiddleLeft, UIKit.Accent);
            UIKit.Place(_crops.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(520f, 64f));

            _lives = UIKit.Label(root, "LivesLabel", "Lives", 40, TextAnchor.MiddleRight);
            UIKit.Place(_lives.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-250f, -30f), new Vector2(160f, 64f));
            for (int i = 0; i < GameManager.LivesPerAttempt; i++)
            {
                var icon = UIKit.Panel(root, $"Life{i + 1}", new Color(0.9f, 0.25f, 0.3f, 1f));
                UIKit.Place(icon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f - i * 62f, -38f), new Vector2(48f, 48f));
                _lifeIcons.Add(icon);
            }

            PauseButton = UIKit.MakeButton(root, "PauseButton", "II", new Color(0.2f, 0.25f, 0.38f, 0.95f), () => onPause?.Invoke(), 40);
            UIKit.Place(PauseButton.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(110f, 64f));

            _boss = UIKit.Label(root, "BossHits", "", 40, TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.5f));
            UIKit.Place(_boss.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(700f, 56f));

            var frame = UIKit.Panel(root, "PortraitFrame", new Color(0f, 0f, 0f, 0.55f));
            UIKit.Place(frame.rectTransform, Vector2.zero, Vector2.zero, new Vector2(30f, 30f), new Vector2(520f, 110f));
            _portrait = UIKit.Panel(frame.transform, "Portrait", Color.white);
            UIKit.Place(_portrait.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(86f, 86f));
            _portrait.preserveAspect = true;
            _ability = UIKit.Label(frame.transform, "AbilityUses", "", 34, TextAnchor.MiddleLeft);
            UIKit.Place(_ability.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(116f, 0f), new Vector2(390f, 100f));

            Root.SetActive(false);
        }

        /// <summary>Reads the live run/player state into the labels.</summary>
        public void Refresh(GameManager gm, CharacterController2D player)
        {
            var run = gm.RunState;
            int total = run.totalNormalCrops + run.totalSecretCrops;
            _crops.text = $"Crops: {run.cropsCollectedThisRun}/{total}";

            for (int i = 0; i < _lifeIcons.Count; i++)
            {
                _lifeIcons[i].gameObject.SetActive(i < run.livesRemaining);
            }

            if (player != null && player.Data != null)
            {
                _portrait.sprite = player.Data.placeholderSprite;
                _ability.text = $"{player.Data.displayName}\n{player.Data.abilityType}: {player.UsesRemaining}/{player.UsesPerLevel}";
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
