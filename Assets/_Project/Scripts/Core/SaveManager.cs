using FarmFuryStampede.Data;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Core
{
    /// <summary>
    /// Persists player progress via PlayerPrefs (character unlocks, per-level stars) behind a
    /// stable API. Cluck and Bessie are unlocked by default on a fresh save (GDD Section 4 —
    /// both are "available from start").
    /// </summary>
    public class SaveManager : MonoSingleton<SaveManager>
    {
        private const string CharacterUnlockedKeyPrefix = "FFS_Unlocked_";
        private const string LevelStarsKeyPrefix = "FFS_Stars_";

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

        /// <summary>
        /// Ensures default values exist for keys that have never been written (e.g. the starter
        /// characters). PlayerPrefs values are read lazily by the other getters, so this mainly
        /// guarantees Cluck and Bessie are unlocked on a fresh install.
        /// </summary>
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

        /// <summary>Returns whether the given character has been unlocked.</summary>
        public bool IsCharacterUnlocked(CharacterType type)
        {
            return PlayerPrefs.GetInt(CharacterUnlockedKeyPrefix + type, 0) == 1;
        }

        /// <summary>Marks the given character as unlocked and saves immediately.</summary>
        public void UnlockCharacter(CharacterType type)
        {
            PlayerPrefs.SetInt(CharacterUnlockedKeyPrefix + type, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Returns the stars earned for the given level id (0 if never completed).</summary>
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
    }
}
