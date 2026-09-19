using System.Collections.Generic;
using System.Linq;
using FarmFuryStampede.Data;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Core
{
    /// <summary>
    /// Persists player progress via PlayerPrefs behind a stable API: per-level stars, distinct levels
    /// completed, and character unlocks. Cluck and Bessie are unlocked on a fresh save (GDD Section 4);
    /// the rest unlock when the distinct-level-completion count reaches each CharacterData's
    /// unlockLevelsRequired (0, 0, 5, 10, 15, 20, 30, 40).
    /// </summary>
    public class SaveManager : MonoSingleton<SaveManager>
    {
        private const string CharacterUnlockedKeyPrefix = "FFS_Unlocked_";
        private const string LevelStarsKeyPrefix = "FFS_Stars_";
        private const string LevelCompletedKeyPrefix = "FFS_Completed_";
        private const string CompletedCountKey = "FFS_CompletedCount";
        private const string DebugCountOffsetKey = "FFS_DebugCountOffset";
        private const string BossClearedKeyPrefix = "FFS_BossCleared_";
        private const string SecretFoundKeyPrefix = "FFS_SecretFound_";

        private static readonly CharacterType[] DefaultUnlockedCharacters =
        {
            CharacterType.Cluck,
            CharacterType.Bessie
        };

        protected override void Awake()
        {
            base.Awake();
            LoadProgress();
        }

        /// <summary>Flushes all pending PlayerPrefs writes to disk.</summary>
        public void SaveProgress()
        {
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] Progress saved.");
        }

        /// <summary>Ensures the starter characters are unlocked on a fresh install.</summary>
        public void LoadProgress()
        {
            foreach (var character in DefaultUnlockedCharacters)
            {
                if (!PlayerPrefs.HasKey(CharacterUnlockedKeyPrefix + character))
                {
                    UnlockCharacter(character);
                }
            }

            Debug.Log("[SaveManager] Progress loaded.");
        }

        // ------------------------------------------------------------ characters

        public bool IsCharacterUnlocked(CharacterType type)
        {
            return PlayerPrefs.GetInt(CharacterUnlockedKeyPrefix + type, 0) == 1;
        }

        public void UnlockCharacter(CharacterType type)
        {
            PlayerPrefs.SetInt(CharacterUnlockedKeyPrefix + type, 1);
            PlayerPrefs.Save();
        }

        // ------------------------------------------------------------ level progress

        public int GetLevelStars(string levelId)
        {
            return PlayerPrefs.GetInt(LevelStarsKeyPrefix + levelId, 0);
        }

        /// <summary>Records stars for the given level id, keeping the best result across attempts.</summary>
        public void SetLevelStars(string levelId, int stars)
        {
            int best = Mathf.Max(GetLevelStars(levelId), stars);
            PlayerPrefs.SetInt(LevelStarsKeyPrefix + levelId, best);
        }

        public bool IsLevelCompleted(string levelId)
        {
            return PlayerPrefs.GetInt(LevelCompletedKeyPrefix + levelId, 0) == 1;
        }

        /// <summary>Distinct levels completed at least once (plus the debug offset, if any).</summary>
        public int CompletedLevelCount =>
            Mathf.Max(0, PlayerPrefs.GetInt(CompletedCountKey, 0) + PlayerPrefs.GetInt(DebugCountOffsetKey, 0));

        /// <summary>
        /// Records a completion. Only the first completion of a level increments the count. Then unlocks
        /// every character whose threshold is now met; returns the ones newly unlocked.
        /// </summary>
        public List<CharacterType> RecordLevelCompleted(string levelId)
        {
            if (!IsLevelCompleted(levelId))
            {
                PlayerPrefs.SetInt(LevelCompletedKeyPrefix + levelId, 1);
                PlayerPrefs.SetInt(CompletedCountKey, PlayerPrefs.GetInt(CompletedCountKey, 0) + 1);
            }

            return EvaluateUnlocks();
        }

        /// <summary>Unlocks every character whose unlockLevelsRequired is met by the current count.</summary>
        public List<CharacterType> EvaluateUnlocks()
        {
            var newlyUnlocked = new List<CharacterType>();
            if (DataManager.Instance == null)
            {
                return newlyUnlocked;
            }

            int count = CompletedLevelCount;
            foreach (var data in DataManager.Instance.GetAllCharacters())
            {
                if (!IsCharacterUnlocked(data.characterType) && count >= data.unlockLevelsRequired)
                {
                    UnlockCharacter(data.characterType);
                    newlyUnlocked.Add(data.characterType);
                    Debug.Log($"[SaveManager] Unlocked {data.characterType} (needs {data.unlockLevelsRequired}, have {count}).");
                }
            }

            return newlyUnlocked;
        }

        // ------------------------------------------------------------ secrets, worlds, level locks

        /// <summary>True once any crop of the level's character-gated secret cluster has been collected.</summary>
        public bool IsSecretFound(string levelId)
        {
            return PlayerPrefs.GetInt(SecretFoundKeyPrefix + levelId, 0) == 1;
        }

        public void MarkSecretFound(string levelId)
        {
            if (!IsSecretFound(levelId))
            {
                PlayerPrefs.SetInt(SecretFoundKeyPrefix + levelId, 1);
                Debug.Log($"[SaveManager] Secret found in {levelId}.");
            }
        }

        public bool IsWorldBossCleared(WorldType world)
        {
            return PlayerPrefs.GetInt(BossClearedKeyPrefix + world, 0) == 1;
        }

        /// <summary>Set when a boss level is completed. Also the manual test hook for worlds with no content yet.</summary>
        public void SetWorldBossCleared(WorldType world, bool cleared)
        {
            PlayerPrefs.SetInt(BossClearedKeyPrefix + world, cleared ? 1 : 0);
        }

        /// <summary>A world unlocks once the previous world's boss level has been completed. Meadow Ruins is always open.</summary>
        public bool IsWorldUnlocked(WorldType world)
        {
            return world == WorldType.MeadowRuins || IsWorldBossCleared(world - 1);
        }

        /// <summary>
        /// Level lock rule: the first level of a world is open; each later level opens once the previous level
        /// in the world has been completed; the boss level opens once every regular level is completed.
        /// </summary>
        public bool IsLevelUnlocked(LevelData level)
        {
            if (level == null || DataManager.Instance == null || !IsWorldUnlocked(level.worldType))
            {
                return false;
            }

            var levels = DataManager.Instance.GetWorldLevels(level.worldType);
            int index = levels.IndexOf(level);
            if (index <= 0)
            {
                return index == 0;
            }

            if (level.isBossLevel)
            {
                return levels.Where(l => !l.isBossLevel).All(l => IsLevelCompleted(l.levelId));
            }

            return IsLevelCompleted(levels[index - 1].levelId);
        }

        /// <summary>Best stars over all the world's levels.</summary>
        public int GetWorldStars(WorldType world)
        {
            return DataManager.Instance == null ? 0 : DataManager.Instance.GetWorldLevels(world).Sum(l => GetLevelStars(l.levelId));
        }

        public int GetWorldMaxStars(WorldType world)
        {
            return DataManager.Instance == null ? 0 : DataManager.Instance.GetWorldLevels(world).Count * 3;
        }

        // ------------------------------------------------------------ debug (testing only)

        /// <summary>DEBUG: marks every regular (non-boss) level of the world completed with one star.</summary>
        public void DebugCompleteWorldLevels(WorldType world)
        {
            foreach (var level in DataManager.Instance.GetWorldLevels(world).Where(l => !l.isBossLevel))
            {
                SetLevelStars(level.levelId, 1);
                RecordLevelCompleted(level.levelId);
            }
        }


        /// <summary>
        /// DEBUG: makes CompletedLevelCount equal n without clearing real levels, then evaluates unlocks.
        /// Never lowers unlocks; use <see cref="DebugResetProgress"/> to start over.
        /// </summary>
        public List<CharacterType> DebugSetCompletedLevelCount(int n)
        {
            int real = PlayerPrefs.GetInt(CompletedCountKey, 0);
            PlayerPrefs.SetInt(DebugCountOffsetKey, n - real);
            return EvaluateUnlocks();
        }

        /// <summary>DEBUG: wipes all progress (completions, stars, unlocks) back to a fresh save.</summary>
        public void DebugResetProgress()
        {
            if (DataManager.Instance != null)
            {
                foreach (var level in DataManager.Instance.GetAllLevels())
                {
                    PlayerPrefs.DeleteKey(LevelCompletedKeyPrefix + level.levelId);
                    PlayerPrefs.DeleteKey(LevelStarsKeyPrefix + level.levelId);
                    PlayerPrefs.DeleteKey(SecretFoundKeyPrefix + level.levelId);
                }

                foreach (var character in DataManager.Instance.GetAllCharacters())
                {
                    PlayerPrefs.DeleteKey(CharacterUnlockedKeyPrefix + character.characterType);
                }
            }

            foreach (WorldType world in System.Enum.GetValues(typeof(WorldType)))
            {
                PlayerPrefs.DeleteKey(BossClearedKeyPrefix + world);
            }

            PlayerPrefs.DeleteKey(CompletedCountKey);
            PlayerPrefs.DeleteKey(DebugCountOffsetKey);
            LoadProgress();
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] DEBUG: progress reset to a fresh save.");
        }
    }
}
