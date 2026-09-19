using FarmFuryStampede.Data;

namespace FarmFuryStampede.Core
{
    /// <summary>
    /// 1-3 stars per level (GDD Section 5), from what the attempt did:
    ///   1 star  - completed the level
    ///   2 stars - also collected at least 75% of the level's normal crops
    ///   3 stars - also found the level's character-gated secret (if it has one), otherwise finished without dying
    /// Time is not scored yet (the GDD lists it; a par time needs playtest data first).
    /// </summary>
    public static class StarCalculator
    {
        public const float CropFractionForSecondStar = 0.75f;

        public static int Compute(LevelData level, LevelRunState run)
        {
            int stars = 1;

            bool enoughCrops = run.totalNormalCrops == 0
                || run.normalCropsCollected >= run.totalNormalCrops * CropFractionForSecondStar;
            if (enoughCrops)
            {
                stars++;
            }

            bool thirdStar;
            if (level != null && level.hasCharacterGatedSecret)
            {
                thirdStar = SaveManager.Instance != null && SaveManager.Instance.IsSecretFound(level.levelId);
            }
            else
            {
                thirdStar = run.deathsThisRun == 0;
            }
            if (thirdStar)
            {
                stars++;
            }

            return stars;
        }
    }
}
