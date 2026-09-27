using FarmFuryStampede.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Arcade's revive prompt, shown when the last life is lost (GameManager.ReviveOffered): the hanging sign with
    /// "Revive for 5 coins" painted in, and three stacked buttons on its planks - Yes (spend the coins; greyed when
    /// they can't be afforded), Watch Ad (a rewarded ad; only while one is loaded) and No (Level Failed). A revive
    /// gives one life back at the last checkpoint. Closing it any other way (Esc) counts as No.
    /// </summary>
    public class RevivePromptScreen : OverlayScreen
    {
        private static readonly Vector2 PanelSize = new(1300f, 731f);    // the 666x375 sign, enlarged
        private const float ButtonHeight = 84f;
        private const float ButtonGap = 16f;
        private const float PlankCentreY = 15f;

        private readonly Button _yes, _watchAd, _no;
        private readonly Text _coins;
        private bool _deciding;

        public RevivePromptScreen(Transform canvas, MenuArt art, ShopArt shop) : base(canvas, "RevivePrompt", art, shop, posterBackdrop: false)
        {
            BackButton.gameObject.SetActive(false);

            var panel = UIKit.Picture(Rect, "PanelArt", Shop.revivePanel);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);
            if (Shop.revivePanel == null)
            {
                var question = UIKit.Label(Rect, "Question", $"Revive for {GameManager.ReviveCoinsCost} coins?", 60, TextAnchor.MiddleCenter, UIKit.Accent);
                UIKit.Place(question.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(1200f, 90f));
            }

            float width = ButtonHeight * 512f / 214f;   // Yes / No / WatchAd art are 512x214
            _yes = StackedButton("ReviveButton", Shop.yesButton, "YES", 0, width, Accept);
            _watchAd = StackedButton("WatchAdButton", Shop.watchAd, "WATCH AD", 1, width, WatchAd);
            _no = StackedButton("DeclineButton", Shop.noButton, "NO", 2, width, Hide);

            _coins = UIKit.Label(Rect, "CoinBalance", "", 40, TextAnchor.MiddleCenter, Color.white);
            _coins.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);
            UIKit.Place(_coins.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -PanelSize.y * 0.5f + 20f), new Vector2(800f, 60f));
        }

        private Button StackedButton(string name, Sprite sprite, string fallback, int row, float width, System.Action onClick)
        {
            var button = IconButton(Rect, name, sprite, fallback, onClick);
            float y = PlankCentreY + (1 - row) * (ButtonHeight + ButtonGap);
            UIKit.Place(button.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(width, ButtonHeight));
            return button;
        }

        protected override void OnShow()
        {
            _deciding = true;
            var save = SaveManager.Instance;
            _yes.interactable = save != null && save.CoinBalance >= GameManager.ReviveCoinsCost;
            _coins.text = save != null ? $"You have {save.CoinBalance} coins" : "";
            Tick();
        }

        /// <summary>Called every frame by GameFlow while open: the Watch Ad button shows only while an ad is ready.</summary>
        public void Tick()
        {
            bool ready = AdManager.Instance != null && AdManager.Instance.IsRewardedAdReady;
            if (_watchAd.gameObject.activeSelf != ready)
            {
                _watchAd.gameObject.SetActive(ready);
            }
        }

        private void Accept()
        {
            if (GameManager.Instance != null && GameManager.Instance.AcceptRevive())
            {
                Close();
            }
        }

        private void WatchAd()
        {
            AdManager.Instance?.ShowRewardedAd("continue_after_death", rewarded =>
            {
                if (rewarded && GameManager.Instance != null && GameManager.Instance.AcceptReviveViaAd())
                {
                    Close();
                }
            });
        }

        // Closed after a revive: no decline.
        private void Close()
        {
            _deciding = false;
            base.Hide();
        }

        /// <summary>No / Esc: the attempt ends.</summary>
        public override void Hide()
        {
            bool declined = _deciding && IsOpen;
            _deciding = false;
            base.Hide();
            if (declined)
            {
                GameManager.Instance?.DeclineRevive();
            }
        }
    }
}
