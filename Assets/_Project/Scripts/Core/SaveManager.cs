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
    /// unlockLevelsRequired (0, 0, 5, 10, 15, 20, 30, 40). Each rare pellet found unlocks the next locked character
    /// in that order straight away (CollectRarePellet), so pellets are the fast route and the level count the
    /// fallback. Unlocks are never taken back.
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
        private const string RarePelletKeyPrefix = "FFS_RarePellet_";
        private const string RarePelletCountKey = "FFS_RarePelletCount";
        private const string PassageCoinsKeyPrefix = "FFS_PassageCoins_";   // bit i = the level's i-th passage coin taken
        private const string LastCharacterKey = "FFS_LastCharacter";
        private const string MusicOnKey = "FFS_MusicOn";
        private const string CoinBalanceKey = "FFS_Coins";
        private const string AdsRemovedKey = "FFS_AdsRemoved";
        private const string LevelsSinceInterstitialKey = "FFS_LevelsSinceInterstitial";
        private const string CosmeticOwnedKeyPrefix = "FFS_CosmeticOwned_";
        private const string EquippedHatKeyPrefix = "FFS_EquippedHat_";
        private const string EquippedSkinKeyPrefix = "FFS_EquippedSkin_";
        private const string EquippedTrailKey = "FFS_EquippedTrail";
        private const string WorldPurchasedKeyPrefix = "FFS_WorldPurchased_";
        // Leaderboard records (best completed run per level; coins earned per world).
        private const string BestScoreKeyPrefix = "FFS_BestScore_";
        private const string BestScoreCharacterKeyPrefix = "FFS_BestScoreChar_";
        private const string FastestTimeKeyPrefix = "FFS_FastestTime_";
        private const string BestCropsKeyPrefix = "FFS_BestCrops_";
        private const string CropTotalKeyPrefix = "FFS_CropTotal_";
        private const string WorldCoinsEarnedKeyPrefix = "FFS_WorldCoins_";

        private static readonly CharacterType[] DefaultUnlockedCharacters =
        {
            CharacterType.Cluck,
            CharacterType.Bessie
        };

        /// <summary>Coins (Phase 6 economy, as in Arcade): earned per level, spent on revives and cosmetics.</summary>
        public int CoinBalance { get; private set; }

        /// <summary>Raised whenever the coin balance changes (HUD chip, shop screens).</summary>
        public event System.Action<int> CoinsChanged;

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
            CoinBalance = GetProtectedInt(CoinBalanceKey, 0);

            foreach (var character in DefaultUnlockedCharacters)
            {
                if (!PlayerPrefs.HasKey(CharacterUnlockedKeyPrefix + character))
                {
                    UnlockCharacter(character);
                }
            }

            Debug.Log("[SaveManager] Progress loaded.");
        }

        // ------------------------------------------------------------ settings

        /// <summary>Settings' music toggle; on by default.</summary>
        public bool MusicOn
        {
            get => PlayerPrefs.GetInt(MusicOnKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(MusicOnKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        // ------------------------------------------------------------ coins & monetisation (ported from Arcade)

        public void AddCoins(int amount)
        {
            CoinBalance += amount;
            PersistCoinBalance();
        }

        /// <summary>Spends coins if the balance covers it; false (nothing spent) otherwise.</summary>
        public bool SpendCoins(int amount)
        {
            if (CoinBalance < amount)
            {
                return false;
            }

            CoinBalance -= amount;
            PersistCoinBalance();
            return true;
        }

        private void PersistCoinBalance()
        {
            SetProtectedInt(CoinBalanceKey, CoinBalance);
            PlayerPrefs.Save();
            CoinsChanged?.Invoke(CoinBalance);
        }

        /// <summary>Remove Ads owned: no interstitials or banners (rewarded ads stay, they're opt-in).</summary>
        public bool AdsRemoved
        {
            get => GetProtectedBool(AdsRemovedKey, false);
            set => SetProtectedBool(AdsRemovedKey, value);
        }

        public int LevelsSinceLastInterstitial => PlayerPrefs.GetInt(LevelsSinceInterstitialKey, 0);

        public void SetLevelsSinceLastInterstitial(int count)
        {
            PlayerPrefs.SetInt(LevelsSinceInterstitialKey, count);
        }

        public bool IsCosmeticOwned(string cosmeticId)
        {
            return !string.IsNullOrEmpty(cosmeticId) && GetProtectedBool(CosmeticOwnedKeyPrefix + cosmeticId, false);
        }

        public void SetCosmeticOwned(string cosmeticId)
        {
            if (!string.IsNullOrEmpty(cosmeticId))
            {
                SetProtectedBool(CosmeticOwnedKeyPrefix + cosmeticId, true);
            }
        }

        /// <summary>The hat or skin a character wears (empty = none). Hats and skins are per character.</summary>
        public string GetEquippedCosmetic(CosmeticType type, CharacterType character)
        {
            return PlayerPrefs.GetString(EquippedKeyPrefix(type) + character, string.Empty);
        }

        /// <summary>Equipping a skin (a machine) takes the hat off: a hat on a tractor has nowhere to sit.</summary>
        public void SetEquippedCosmetic(CosmeticType type, CharacterType character, string cosmeticId)
        {
            PlayerPrefs.SetString(EquippedKeyPrefix(type) + character, cosmeticId ?? string.Empty);
            if (type == CosmeticType.Skin && !string.IsNullOrEmpty(cosmeticId))
            {
                PlayerPrefs.SetString(EquippedHatKeyPrefix + character, string.Empty);
            }
        }

        /// <summary>The trail is shared by every character.</summary>
        public string GetEquippedTrail() => PlayerPrefs.GetString(EquippedTrailKey, string.Empty);

        public void SetEquippedTrail(string cosmeticId)
        {
            PlayerPrefs.SetString(EquippedTrailKey, cosmeticId ?? string.Empty);
        }

        public bool IsWorldPurchased(WorldType world) => GetProtectedBool(WorldPurchasedKeyPrefix + world, false);

        public void SetWorldPurchased(WorldType world)
        {
            SetProtectedBool(WorldPurchasedKeyPrefix + world, true);
            PlayerPrefs.Save();
        }

        private static string EquippedKeyPrefix(CosmeticType type) =>
            type == CosmeticType.Skin ? EquippedSkinKeyPrefix : EquippedHatKeyPrefix;

        // Arcade's tamper check for purchased/earned values: each int is stored with a checksum salted by the device
        // id, so hand-editing the prefs file resets the value instead of granting coins or items. A value saved
        // before its checksum existed is adopted rather than wiped.
        private static string ProtectedSalt => SystemInfo.deviceUniqueIdentifier;

        private static int ComputeChecksum(string key, int value)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + key.GetHashCode();
                hash = hash * 31 + value;
                hash = hash * 31 + ProtectedSalt.GetHashCode();
                return hash;
            }
        }

        private static void SetProtectedInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.SetInt(key + "_chk", ComputeChecksum(key, value));
        }

        private static int GetProtectedInt(string key, int defaultValue)
        {
            int value = PlayerPrefs.GetInt(key, defaultValue);
            string checksumKey = key + "_chk";
            if (!PlayerPrefs.HasKey(checksumKey))
            {
                SetProtectedInt(key, value);
                return value;
            }

            if (PlayerPrefs.GetInt(checksumKey) != ComputeChecksum(key, value))
            {
                Debug.LogWarning($"[SaveManager] Integrity check failed for '{key}' - resetting to default.");
                SetProtectedInt(key, defaultValue);
                return defaultValue;
            }
            return value;
        }

        private static bool GetProtectedBool(string key, bool defaultValue) => GetProtectedInt(key, defaultValue ? 1 : 0) == 1;

        private static void SetProtectedBool(string key, bool value) => SetProtectedInt(key, value ? 1 : 0);

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

        /// <summary>Whether this level's index-th secret-passage coin has been taken (they pay once, then stay gone).</summary>
        public bool IsPassageCoinCollected(string levelId, int index)
        {
            return index >= 0 && index < 31 && (PlayerPrefs.GetInt(PassageCoinsKeyPrefix + levelId, 0) & (1 << index)) != 0;
        }

        public void MarkPassageCoinCollected(string levelId, int index)
        {
            if (index >= 0 && index < 31)
            {
                PlayerPrefs.SetInt(PassageCoinsKeyPrefix + levelId, PlayerPrefs.GetInt(PassageCoinsKeyPrefix + levelId, 0) | (1 << index));
            }
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

        // ------------------------------------------------------------ rare pellets

        /// <summary>Rare pellets found so far (one per level at most), the pellet route to character unlocks.</summary>
        public int RarePelletCount => PlayerPrefs.GetInt(RarePelletCountKey, 0);

        public bool IsRarePelletFound(string levelId)
        {
            return PlayerPrefs.GetInt(RarePelletKeyPrefix + levelId, 0) == 1;
        }

        /// <summary>
        /// Records the level's rare pellet (once per level; a repeat does nothing and returns an empty list) and
        /// unlocks the next locked character with it, then saves. Returns the characters unlocked (empty when every
        /// character was already unlocked).
        /// </summary>
        public List<CharacterType> CollectRarePellet(string levelId)
        {
            if (IsRarePelletFound(levelId))
            {
                return new List<CharacterType>();
            }

            PlayerPrefs.SetInt(RarePelletKeyPrefix + levelId, 1);
            Debug.Log($"[SaveManager] Rare pellet found in {levelId} ({RarePelletCount + 1} total).");
            return AddRarePellet();
        }

        /// <summary>DEBUG: a rare pellet without a level (F1 panel): unlocks the next character like a real one.</summary>
        public List<CharacterType> DebugAddRarePellet() => AddRarePellet();

        private List<CharacterType> AddRarePellet()
        {
            PlayerPrefs.SetInt(RarePelletCountKey, RarePelletCount + 1);
            var unlocked = new List<CharacterType>();
            var next = NextLockedCharacter();
            if (next != null)
            {
                UnlockCharacter(next.characterType);
                unlocked.Add(next.characterType);
                Debug.Log($"[SaveManager] Rare pellet unlocked {next.characterType}.");
            }
            PlayerPrefs.Save();
            return unlocked;
        }

        /// <summary>The next character still locked, in the unlock ladder's order; null when all are unlocked.</summary>
        public CharacterData NextLockedCharacter()
        {
            if (DataManager.Instance == null)
            {
                return null;
            }

            return DataManager.Instance.GetAllCharacters()
                .Where(c => !IsCharacterUnlocked(c.characterType))
                .OrderBy(c => c.unlockLevelsRequired)
                .ThenBy(c => (int)c.characterType)
                .FirstOrDefault();
        }

        /// <summary>
        /// The character the player last played as (swapped to, or started a level with): the next level starts as
        /// them. Cluck on a fresh save, or when that character is locked.
        /// </summary>
        public CharacterType LastCharacter
        {
            get
            {
                var type = (CharacterType)PlayerPrefs.GetInt(LastCharacterKey, (int)CharacterType.Cluck);
                return System.Enum.IsDefined(typeof(CharacterType), type) && IsCharacterUnlocked(type) ? type : CharacterType.Cluck;
            }
            set
            {
                PlayerPrefs.SetInt(LastCharacterKey, (int)value);
                PlayerPrefs.Save();
            }
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

        /// <summary>
        /// A story world unlocks once the previous world's boss level has been completed (Meadow Ruins is always open).
        /// A purchase-gated world (WorldData.purchaseRequired: the three paid post-finale worlds) unlocks when bought.
        /// </summary>
        public bool IsWorldUnlocked(WorldType world)
        {
            var data = DataManager.Instance != null ? DataManager.Instance.GetWorldData(world) : null;
            if (data != null && data.purchaseRequired)
            {
                return IsWorldPurchased(world);   // the paid post-finale worlds: buying one opens it, no boss needed
            }
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

        // ------------------------------------------------------------ leaderboard records

        /// <summary>
        /// Records a completed run for the Leaderboard, keeping each record's best: the highest score (and the
        /// character who set it), the fastest time, the most crops (with the level's crop total, so a world can show
        /// "collected / total"). Saved with the rest of the level's progress by the caller.
        /// </summary>
        public void RecordLevelResult(string levelId, int score, float seconds, int crops, int cropTotal, CharacterType character)
        {
            if (score > GetBestScore(levelId) || !PlayerPrefs.HasKey(BestScoreKeyPrefix + levelId))
            {
                PlayerPrefs.SetInt(BestScoreKeyPrefix + levelId, score);
                PlayerPrefs.SetInt(BestScoreCharacterKeyPrefix + levelId, (int)character);
            }

            float fastest = GetFastestTime(levelId);
            if (seconds > 0f && (fastest <= 0f || seconds < fastest))
            {
                PlayerPrefs.SetFloat(FastestTimeKeyPrefix + levelId, seconds);
            }

            PlayerPrefs.SetInt(BestCropsKeyPrefix + levelId, Mathf.Max(GetBestCrops(levelId), crops));
            PlayerPrefs.SetInt(CropTotalKeyPrefix + levelId, Mathf.Max(cropTotal, crops));
        }

        public int GetBestScore(string levelId) => PlayerPrefs.GetInt(BestScoreKeyPrefix + levelId, 0);

        /// <summary>The character who set the level's best score, or null before it has been completed.</summary>
        public CharacterType? GetBestScoreCharacter(string levelId)
        {
            if (!PlayerPrefs.HasKey(BestScoreCharacterKeyPrefix + levelId))
            {
                return null;
            }
            var type = (CharacterType)PlayerPrefs.GetInt(BestScoreCharacterKeyPrefix + levelId);
            return System.Enum.IsDefined(typeof(CharacterType), type) ? type : null;
        }

        /// <summary>Fastest completion in seconds (paused time excluded); 0 = none yet.</summary>
        public float GetFastestTime(string levelId) => PlayerPrefs.GetFloat(FastestTimeKeyPrefix + levelId, 0f);

        public int GetBestCrops(string levelId) => PlayerPrefs.GetInt(BestCropsKeyPrefix + levelId, 0);

        /// <summary>Crops the level holds (as of its last completion); 0 before it has been completed.</summary>
        public int GetLevelCropTotal(string levelId) => PlayerPrefs.GetInt(CropTotalKeyPrefix + levelId, 0);

        /// <summary>Coins earned by playing the world's levels (payouts and Double Coins), for the Leaderboard.</summary>
        public int GetWorldCoinsEarned(WorldType world) => PlayerPrefs.GetInt(WorldCoinsEarnedKeyPrefix + world, 0);

        public void AddWorldCoinsEarned(WorldType world, int amount)
        {
            if (amount > 0)
            {
                PlayerPrefs.SetInt(WorldCoinsEarnedKeyPrefix + world, GetWorldCoinsEarned(world) + amount);
            }
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

        /// <summary>DEBUG: adds coins (F1 panel), for testing revives and coin purchases.</summary>
        public void DebugAddCoins(int amount) => AddCoins(amount);

        /// <summary>DEBUG: wipes all progress (completions, stars, unlocks) back to a fresh save. Purchases and coins are kept.</summary>
        /// <summary>DEBUG: grants (or takes back) one cosmetic. Taking it back also unequips it everywhere.</summary>
        public void DebugSetCosmeticOwned(CosmeticData item, bool owned)
        {
            if (item == null || string.IsNullOrEmpty(item.cosmeticId))
            {
                return;
            }

            if (owned)
            {
                SetCosmeticOwned(item.cosmeticId);
                return;
            }

            PlayerPrefs.DeleteKey(CosmeticOwnedKeyPrefix + item.cosmeticId);
            PlayerPrefs.DeleteKey(CosmeticOwnedKeyPrefix + item.cosmeticId + "_chk");
            foreach (CharacterType character in System.Enum.GetValues(typeof(CharacterType)))
            {
                foreach (var type in new[] { CosmeticType.Hat, CosmeticType.Skin })
                {
                    if (GetEquippedCosmetic(type, character) == item.cosmeticId)
                    {
                        PlayerPrefs.SetString(EquippedKeyPrefix(type) + character, string.Empty);
                    }
                }
            }
            if (GetEquippedTrail() == item.cosmeticId)
            {
                SetEquippedTrail(string.Empty);
            }
        }

        public void DebugResetProgress()
        {
            if (DataManager.Instance != null)
            {
                foreach (var level in DataManager.Instance.GetAllLevels())
                {
                    PlayerPrefs.DeleteKey(LevelCompletedKeyPrefix + level.levelId);
                    PlayerPrefs.DeleteKey(LevelStarsKeyPrefix + level.levelId);
                    PlayerPrefs.DeleteKey(SecretFoundKeyPrefix + level.levelId);
                    PlayerPrefs.DeleteKey(RarePelletKeyPrefix + level.levelId);
                    PlayerPrefs.DeleteKey(PassageCoinsKeyPrefix + level.levelId);
                    PlayerPrefs.DeleteKey(BestScoreKeyPrefix + level.levelId);
                    PlayerPrefs.DeleteKey(BestScoreCharacterKeyPrefix + level.levelId);
                    PlayerPrefs.DeleteKey(FastestTimeKeyPrefix + level.levelId);
                    PlayerPrefs.DeleteKey(BestCropsKeyPrefix + level.levelId);
                    PlayerPrefs.DeleteKey(CropTotalKeyPrefix + level.levelId);
                }

                foreach (var character in DataManager.Instance.GetAllCharacters())
                {
                    PlayerPrefs.DeleteKey(CharacterUnlockedKeyPrefix + character.characterType);
                }
            }

            foreach (WorldType world in System.Enum.GetValues(typeof(WorldType)))
            {
                PlayerPrefs.DeleteKey(BossClearedKeyPrefix + world);
                PlayerPrefs.DeleteKey(WorldCoinsEarnedKeyPrefix + world);
            }

            PlayerPrefs.DeleteKey(CompletedCountKey);
            PlayerPrefs.DeleteKey(DebugCountOffsetKey);
            PlayerPrefs.DeleteKey(RarePelletCountKey);
            PlayerPrefs.DeleteKey(LastCharacterKey);
            LoadProgress();
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] DEBUG: progress reset to a fresh save.");
        }
    }
}
