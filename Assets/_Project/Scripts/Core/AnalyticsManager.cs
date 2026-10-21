using FarmFuryStampede.Utilities;

namespace FarmFuryStampede.Core
{
    /// <summary>
    /// Web demo (branch web-demo): no analytics. Unity Gaming Services are removed in this branch; this keeps the
    /// main game's AnalyticsManager API with every event a no-op.
    /// </summary>
    public class AnalyticsManager : MonoSingleton<AnalyticsManager>
    {
        public bool IsInitialized => false;

        public void LogLevelStart(int levelIndex, string levelName) { }
        public void LogLevelComplete(int levelIndex, int stars, int score, float elapsedSeconds) { }
        public void LogLevelFailed(int levelIndex, float elapsedSeconds) { }
        public void LogPurchase(string productId, string priceString) { }
        public void LogAdShown(string placementType, string placementName) { }
    }
}
