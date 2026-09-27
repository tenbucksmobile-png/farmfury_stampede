using System;
using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
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
    /// Phase 6 (Arcade's HUD): the coin balance (coin glyph + count) top-left.
    /// On-screen controls: hold-to-run left and right, then pause, along the bottom-left; jump in the bottom-right
    /// corner with the Locker beside it. The life icons are the played character giving a thumbs up.
    /// Without art the lives fall back to red squares and pause to a plain "II" button.
    /// </summary>
    public class HudScreen
    {
        public GameObject Root { get; private set; }
        public Button PauseButton { get; private set; }
        public Button LockerButton { get; private set; }
        private readonly Text _coins;

        private readonly List<Image> _lifeIcons = new();
        private readonly Text _boss;

        private const float EdgeMargin = 40f;
        private const float LifeIconHeight = 96f;
        private const float LifeIconGap = 10f;
        private const float ControlSize = 150f;      // left / right / jump: bigger than the round menu buttons
        private const float ControlGap = 24f;
        private readonly Sprite _defaultLife;
        private CharacterType? _lifeCharacter;

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

        public HudScreen(Transform canvas, Action onPause, Action onLocker, MenuArt art, ShopArt shop)
        {
            art ??= new MenuArt();
            shop ??= new ShopArt();
            var root = UIKit.NewRect("Hud", canvas);
            UIKit.Stretch(root);
            Root = root.gameObject;

            var safe = UIKit.NewRect("SafeArea", root);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // Lives: a row of thumbs-up icons (the played character's) in the top-right corner, the rightmost first
            // to go. Square boxes with the art's aspect kept, so every character's icon fits.
            _defaultLife = art.lifeIcon;
            float iconWidth = LifeIconHeight, iconHeight = LifeIconHeight;
            for (int i = 0; i < GameManager.LivesPerAttempt; i++)
            {
                var icon = UIKit.Panel(safe, $"Life{i + 1}", Color.white);
                icon.preserveAspect = true;
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
            // Bottom-left: left, right, then pause (vertically centred on the bigger controls).
            float roundLift = (ControlSize - UIKit.RoundButtonSize) * 0.5f;
            var left = HoldControl(safe, "MoveLeftButton", shop.moveLeftButton, "<", Vector2.zero, new Vector2(EdgeMargin, EdgeMargin));
            left.Pressed += () => PlayerInputReader.TouchLeftHeld = true;
            left.Released += () => PlayerInputReader.TouchLeftHeld = false;
            var right = HoldControl(safe, "MoveRightButton", shop.moveRightButton, ">", Vector2.zero,
                new Vector2(EdgeMargin + ControlSize + ControlGap, EdgeMargin));
            right.Pressed += () => PlayerInputReader.TouchRightHeld = true;
            right.Released += () => PlayerInputReader.TouchRightHeld = false;
            UIKit.Place(PauseButton.image.rectTransform, Vector2.zero, Vector2.zero,
                new Vector2(EdgeMargin + 2f * (ControlSize + ControlGap), EdgeMargin + roundLift), Vector2.one * UIKit.RoundButtonSize);

            // Bottom-right: jump in the corner, the Locker beside it.
            var jump = HoldControl(safe, "JumpButton", shop.jumpButton, "^", Vector2.right, new Vector2(-EdgeMargin, EdgeMargin));
            jump.Pressed += PlayerInputReader.PressTouchJump;
            jump.Released += () => PlayerInputReader.TouchJumpHeld = false;

            LockerButton = UIKit.MakeButton(safe, "LockerButton", shop.lockerIcon != null ? "" : "LOCKER",
                shop.lockerIcon != null ? Color.white : new Color(0.2f, 0.25f, 0.38f, 0.95f), () => onLocker?.Invoke(), 26);
            if (shop.lockerIcon != null)
            {
                LockerButton.image.sprite = shop.lockerIcon;
                LockerButton.image.preserveAspect = true;
            }
            UIKit.Place(LockerButton.image.rectTransform, Vector2.right, Vector2.right,
                new Vector2(-EdgeMargin - ControlSize - ControlGap, EdgeMargin + roundLift), Vector2.one * UIKit.RoundButtonSize);

            // Coin balance, top-left (the lives have the top-right).
            const float coinSize = 72f;
            var coin = shop.coinIcon != null ? UIKit.Picture(safe, "CoinIcon", shop.coinIcon) : UIKit.Panel(safe, "CoinIcon", UIKit.Accent);
            coin.raycastTarget = false;
            UIKit.Place(coin.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(EdgeMargin, -EdgeMargin * 0.5f - 12f), Vector2.one * coinSize);
            _coins = UIKit.Label(safe, "CoinBalanceText", "0", 56, TextAnchor.MiddleLeft, Color.white);
            _coins.fontStyle = FontStyle.Bold;
            _coins.horizontalOverflow = HorizontalWrapMode.Overflow;
            _coins.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            UIKit.Place(_coins.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(EdgeMargin + coinSize + 14f, -EdgeMargin * 0.5f - 12f),
                new Vector2(400f, coinSize));

            _boss = UIKit.Label(safe, "BossHits", "", 40, TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.5f));
            UIKit.Place(_boss.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(700f, 56f));

            Root.SetActive(false);
        }

        // A hold-to-press control (no click action): its art, or a plain labelled square.
        private static HoldButton HoldControl(Transform parent, string name, Sprite sprite, string fallback, Vector2 corner, Vector2 offset)
        {
            var image = UIKit.Panel(parent, name, sprite != null ? Color.white : new Color(0.2f, 0.25f, 0.38f, 0.8f));
            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
            }
            else
            {
                var label = UIKit.Label(image.transform, "Label", fallback, 72);
                UIKit.Stretch(label.rectTransform);
            }
            UIKit.Place(image.rectTransform, corner, corner, offset, Vector2.one * ControlSize);
            return image.gameObject.AddComponent<HoldButton>();
        }

        /// <summary>Reads the live run state into the HUD: one life icon per life left.</summary>
        public void Refresh(GameManager gm, CharacterController2D player)
        {
            var run = gm.RunState;
            _coins.text = SaveManager.Instance != null ? SaveManager.Instance.CoinBalance.ToString() : "0";
            if (_lifeCharacter != gm.CurrentCharacter)
            {
                _lifeCharacter = gm.CurrentCharacter;
                var data = DataManager.Instance != null ? DataManager.Instance.GetCharacterData(gm.CurrentCharacter) : null;
                var sprite = data != null && data.lifeIcon != null ? data.lifeIcon : _defaultLife;
                foreach (var icon in _lifeIcons)
                {
                    icon.sprite = sprite;
                    icon.color = sprite != null ? Color.white : new Color(0.9f, 0.25f, 0.3f, 1f);
                }
            }
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
