using System;
using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Gameplay HUD, kept to the minimum so the level reads clearly: one Cluck icon per life top-right (one disappears
    /// with each death) and the round pause button bottom-left (no boss hit counter since 2026-09-29). Everything sits inside the device safe area. Refreshed every frame by GameFlow.
    /// Phase 6 (Arcade's HUD): the coin balance (coin glyph + count) top-left.
    /// On-screen controls, one row along the bottom, all the same size: hold-to-run left and right, then pause,
    /// bottom-left; the character swap and the Locker centred; the played character's ability card and jump
    /// bottom-right, jump in the corner.
    /// Ability cooldown (Arcade's setup): after each use the card greys out with a seconds countdown for
    /// CharacterData.abilityCooldown (5s). Meanwhile a spinning coin badge on its top-left corner skips the wait for
    /// SkipCooldownCoinsCost coins (tappable only when affordable), and a Watch Ad button above the card skips it for
    /// a rewarded ad (shown only while an ad is loaded; the level is frozen while it plays). The life icons are the played character giving a
    /// thumbs up.
    /// Without art the lives fall back to red squares and pause to a plain "II" button.
    /// </summary>
    public class HudScreen
    {
        public GameObject Root { get; private set; }
        public Button PauseButton { get; private set; }
        public Button LockerButton { get; private set; }
        public Button SwapButton { get; private set; }
        private readonly Text _coins;

        private readonly List<Image> _lifeIcons = new();

        private const float EdgeMargin = 40f;
        private const float LifeIconHeight = 96f;
        private const float LifeIconGap = 10f;
        private const float ControlSize = 150f;      // every HUD button: bigger than the round menu buttons
        private const float ControlGap = 24f;
        private readonly Sprite _defaultLife;
        private CharacterType? _lifeCharacter;
        private readonly Sprite[] _abilityIcons;
        private readonly Image _ability;
        private readonly Text _abilityLabel;
        private static readonly Color AbilitySpent = new(0.45f, 0.45f, 0.45f, 0.7f);
        private readonly Text _cooldownText;
        private readonly Button _skipCoinButton;
        private readonly Button _skipAdButton;
        private CharacterController2D _player;
        private readonly RectTransform _controlRow;   // the tallest bottom-row control (the Watch Ad button sits above it)
        private static readonly Vector3[] Corners = new Vector3[4];

        /// <summary>Coins to skip the ability cooldown (Arcade's SkipCooldownCoinsCost).</summary>
        public const int SkipCooldownCoinsCost = 3;
        private const float SkipBadgeSize = 64f;
        public bool SkipCoinShown => _skipCoinButton.gameObject.activeSelf;
        public bool SkipAdShown => _skipAdButton.gameObject.activeSelf;
        public Button SkipCoinButton => _skipCoinButton;
        public Button SkipAdButton => _skipAdButton;
        private static readonly Color PlainButton = new(0.2f, 0.25f, 0.38f, 0.8f);

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

        public HudScreen(Transform canvas, Action onPause, Action onLocker, Action onSwap, MenuArt art, ShopArt shop)
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
            // One row along the bottom, every button the same size (ControlSize) and ControlGap apart:
            // left, right, pause bottom-left | swap, Locker centred | ability, jump bottom-right (jump in the corner).
            const float pitch = ControlSize + ControlGap;
            var left = HoldControl(safe, "MoveLeftButton", shop.moveLeftButton, "<", Vector2.zero, new Vector2(EdgeMargin, EdgeMargin));
            left.Pressed += () => PlayerInputReader.TouchLeftHeld = true;
            left.Released += () => PlayerInputReader.TouchLeftHeld = false;
            var right = HoldControl(safe, "MoveRightButton", shop.moveRightButton, ">", Vector2.zero,
                new Vector2(EdgeMargin + pitch, EdgeMargin));
            right.Pressed += () => PlayerInputReader.TouchRightHeld = true;
            right.Released += () => PlayerInputReader.TouchRightHeld = false;
            UIKit.Place(PauseButton.image.rectTransform, Vector2.zero, Vector2.zero,
                new Vector2(EdgeMargin + 2f * pitch, EdgeMargin), Vector2.one * ControlSize);

            var jump = HoldControl(safe, "JumpButton", shop.jumpButton, "^", Vector2.right, new Vector2(-EdgeMargin, EdgeMargin));
            jump.Pressed += PlayerInputReader.PressTouchJump;
            jump.Released += () => PlayerInputReader.TouchJumpHeld = false;
            _controlRow = (RectTransform)jump.transform;

            // Ability: the played character's ability card, left of jump; dimmed once its uses are spent.
            _abilityIcons = shop.abilityIcons;
            var ability = HoldControl(safe, "AbilityButton", null, "ABILITY", Vector2.right, new Vector2(-EdgeMargin - pitch, EdgeMargin));
            ability.Pressed += PlayerInputReader.PressTouchAbility;
            _ability = ability.GetComponent<Image>();
            _abilityLabel = ability.GetComponentInChildren<Text>();
            if (_abilityLabel != null) { _abilityLabel.fontSize = 30; }

            // Cooldown countdown: whole seconds left, big over the greyed card.
            _cooldownText = UIKit.Label(ability.transform, "CooldownText", "", 72, TextAnchor.MiddleCenter, Color.white);
            _cooldownText.fontStyle = FontStyle.Bold;
            _cooldownText.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            UIKit.Stretch(_cooldownText.rectTransform);

            // Skip for coins: a coin with the price on the card's top-left corner, drawn over the card so a tap on
            // it never reaches the card's own press. The coin turns like the revive prompt's.
            _skipCoinButton = UIKit.MakeButton(ability.transform, "SkipCooldownCoinBadge", "", Color.clear, SkipWithCoins);
            UIKit.Place(_skipCoinButton.image.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(8f, -8f),
                Vector2.one * SkipBadgeSize);
            var badgeCoin = shop.coinIcon != null
                ? UIKit.Picture(_skipCoinButton.transform, "Coin", shop.coinIcon)
                : UIKit.Panel(_skipCoinButton.transform, "Coin", UIKit.Accent);
            badgeCoin.raycastTarget = false;
            UIKit.Stretch(badgeCoin.rectTransform);
            badgeCoin.gameObject.AddComponent<SpinAroundY>();
            _skipCoinButton.targetGraphic = badgeCoin;   // the disabled tint greys the coin when it can't be afforded
            var price = UIKit.Label(_skipCoinButton.transform, "Price", SkipCooldownCoinsCost.ToString(), 40, TextAnchor.MiddleCenter, Color.white);
            price.fontStyle = FontStyle.Bold;
            price.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);
            UIKit.Stretch(price.rectTransform);
            _skipCoinButton.transform.Find("Label").gameObject.SetActive(false);
            _skipCoinButton.gameObject.SetActive(false);

            // Skip for a rewarded ad: the Watch Ad art (512x214) just above the ability card.
            float adHeight = ControlSize * 214f / 512f;
            _skipAdButton = UIKit.MakeButton(safe, "WatchAdSkipCooldownButton", shop.watchAd != null ? "" : "WATCH AD",
                shop.watchAd != null ? Color.white : new Color(0.2f, 0.25f, 0.38f, 0.95f), SkipWithAd, 24);
            if (shop.watchAd != null)
            {
                _skipAdButton.image.sprite = shop.watchAd;
                _skipAdButton.image.preserveAspect = true;
            }
            UIKit.Place(_skipAdButton.image.rectTransform, Vector2.right, Vector2.right,
                new Vector2(-EdgeMargin - pitch, EdgeMargin + ControlSize + 10f), new Vector2(ControlSize, adHeight));
            _skipAdButton.gameObject.SetActive(false);

            LockerButton = UIKit.MakeButton(safe, "LockerButton", shop.lockerIcon != null ? "" : "LOCKER",
                shop.lockerIcon != null ? Color.white : new Color(0.2f, 0.25f, 0.38f, 0.95f), () => onLocker?.Invoke(), 26);
            if (shop.lockerIcon != null)
            {
                LockerButton.image.sprite = shop.lockerIcon;
                LockerButton.image.preserveAspect = true;
            }
            UIKit.Place(LockerButton.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 0f),
                new Vector2(ControlGap * 0.5f, EdgeMargin), Vector2.one * ControlSize);

            // Swap character: bottom-centre, left of the Locker (the pair centred on the screen).
            SwapButton = UIKit.MakeButton(safe, "SwapButton", art.swapCharacterButton != null ? "" : "SWAP",
                art.swapCharacterButton != null ? Color.white : new Color(0.2f, 0.25f, 0.38f, 0.95f), () => onSwap?.Invoke(), 30);
            if (art.swapCharacterButton != null)
            {
                SwapButton.image.sprite = art.swapCharacterButton;
                SwapButton.image.preserveAspect = true;
            }
            UIKit.Place(SwapButton.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 0f),
                new Vector2(-ControlGap * 0.5f, EdgeMargin), Vector2.one * ControlSize);

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

            Root.SetActive(false);
        }

        private void SkipWithCoins()
        {
            if (_player == null || _player.AbilityReady)
            {
                return;
            }
            if (SaveManager.Instance != null && SaveManager.Instance.SpendCoins(SkipCooldownCoinsCost))
            {
                _player.SkipCooldown();
            }
        }

        private void SkipWithAd()
        {
            var player = _player;
            if (player == null || player.AbilityReady || AdManager.Instance == null)
            {
                return;
            }

            // Freeze the level while the ad plays; the callback always comes (AdManager's timeout fallback).
            float timeScale = Time.timeScale;
            Time.timeScale = 0f;
            AdManager.Instance.ShowRewardedAd("skip_cooldown_via_ad", rewarded =>
            {
                Time.timeScale = timeScale;
                if (rewarded && player != null)
                {
                    player.SkipCooldown();
                }
            });
        }

        // A hold-to-press control (no click action): its art, or a plain labelled square.
        private static HoldButton HoldControl(Transform parent, string name, Sprite sprite, string fallback, Vector2 corner, Vector2 offset)
        {
            var image = UIKit.Panel(parent, name, sprite != null ? Color.white : new Color(0.2f, 0.25f, 0.38f, 0.8f));
            if (sprite != null)
            {
                image.sprite = sprite;
                // Fills the whole ControlSize box: left.png is 287x256, so keeping its aspect drew it ~11% smaller
                // than the (square) art of every other button in the row.
                image.preserveAspect = false;
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

                _ability.sprite = ShopArt.At(_abilityIcons, (int)gm.CurrentCharacter);
                _ability.preserveAspect = true;
                if (_abilityLabel != null) { _abilityLabel.gameObject.SetActive(_ability.sprite == null); }
            }

            _player = player;
            // Keep the level's ground above the control row (screen-space overlay canvas: corners are screen pixels).
            _controlRow.GetWorldCorners(Corners);
            CameraFollow2D.BottomScreenReserve = Screen.height > 0 ? Mathf.Max(0f, Corners[1].y) / Screen.height : 0f;
            float cooldown = player != null ? player.CooldownRemaining : 0f;
            bool ready = cooldown <= 0f;
            _ability.color = _ability.sprite != null ? (ready ? Color.white : AbilitySpent) : (ready ? PlainButton : AbilitySpent);
            _cooldownText.text = ready ? "" : Mathf.CeilToInt(cooldown).ToString();
            // Checked every frame so a coin pickup mid-cooldown makes the badge tappable at once (as in Arcade).
            _skipCoinButton.gameObject.SetActive(!ready);
            _skipCoinButton.interactable = SaveManager.Instance != null && SaveManager.Instance.CoinBalance >= SkipCooldownCoinsCost;
            // Never a dead button: only while cooling down and an ad is actually loaded.
            _skipAdButton.gameObject.SetActive(!ready && AdManager.Instance != null && AdManager.Instance.IsRewardedAdReady);
            for (int i = 0; i < _lifeIcons.Count; i++)
            {
                // Icons are laid out left to right; lives are lost from the right-hand end.
                _lifeIcons[i].gameObject.SetActive(i < run.livesRemaining);
            }
        }
    }
}
