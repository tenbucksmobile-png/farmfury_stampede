using FarmFuryStampede.Utilities;

namespace FarmFuryStampede.Core
{
    /// <summary>
    /// Web demo (branch web-demo): no ads. The LevelPlay SDK is removed in this branch; this keeps the main game's
    /// AdManager API so nothing else changes - no ad is ever ready, so every "Watch Ad" button stays hidden or dimmed,
    /// a rewarded request reports "not rewarded" and level loads go straight on.
    /// </summary>
    public class AdManager : MonoSingleton<AdManager>
    {
        public bool IsInitialized => false;
        public bool IsRewardedAdReady => false;
        public bool IsInterstitialAdReady => false;
        public bool IsBannerAdReady => false;

        public void ShowRewardedAd(string placementName, System.Action<bool> onResult) => onResult?.Invoke(false);

        public void NotifyLevelLoaded(System.Action onReady) => onReady?.Invoke();

        public void ShowBanner() { }

        public void HideBanner() { }
    }
}
