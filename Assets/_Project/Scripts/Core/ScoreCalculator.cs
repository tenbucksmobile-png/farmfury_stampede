namespace FarmFuryStampede.Core
{
    /// <summary>
    /// Points for a completed level, the Leaderboard's High Score (the best per level, summed over a world):
    /// every crop collected, every robot defeated and every star earned. The weights are first guesses, not
    /// playtested; stars weigh most so a cleaner run always beats a slower, grindier one.
    /// </summary>
    public static class ScoreCalculator
    {
        public const int PointsPerCrop = 10;
        public const int PointsPerRobot = 25;
        public const int PointsPerStar = 100;

        public static int Compute(LevelRunState run, int stars)
        {
            return run.cropsCollectedThisRun * PointsPerCrop
                + run.robotsDefeatedThisRun * PointsPerRobot
                + stars * PointsPerStar;
        }
    }
}
