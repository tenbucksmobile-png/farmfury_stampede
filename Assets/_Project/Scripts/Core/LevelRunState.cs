namespace FarmFuryStampede.Core
{
    /// <summary>
    /// Runtime state for the level attempt currently in progress. Plain class owned by
    /// <see cref="GameManager"/> and reset on every StartLevel. Kept separate from DataManager,
    /// which only holds read-only reference data.
    /// </summary>
    public class LevelRunState
    {
        public int cropsCollectedThisRun;

        public void Reset()
        {
            cropsCollectedThisRun = 0;
        }

        public void CollectCrop()
        {
            cropsCollectedThisRun++;
        }
    }
}
