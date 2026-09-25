using System.Collections.Generic;
using FarmFuryStampede.Data;
using UnityEngine;

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
        public int normalCropsCollected;
        public int secretCropsCollected;
        public int robotsDefeatedThisRun;
        public int deathsThisRun;

        /// <summary>Set by LevelLoader when the level loads: how many crops (normal / secret-cluster) it spawned.</summary>
        public int totalNormalCrops;
        public int totalSecretCrops;

        /// <summary>Lives left this attempt. GameManager sets it to LivesPerAttempt on StartLevel.</summary>
        public int livesRemaining;

        public int starsEarned;
        public bool bossCleared;
        /// <summary>True when this boss clear opened the next world for the first time (not a replay, not the finale).</summary>
        public bool worldUnlocked;

        /// <summary>True once a checkpoint has been touched this run.</summary>
        public bool hasCheckpoint;

        /// <summary>Where the player respawns: the level's start point until a checkpoint is touched.</summary>
        public Vector2 respawnPosition;

        /// <summary>Characters unlocked by completing this level (for the results/unlock toast).</summary>
        public readonly List<CharacterType> newlyUnlockedCharacters = new();

        public void Reset()
        {
            newlyUnlockedCharacters.Clear();
            cropsCollectedThisRun = 0;
            normalCropsCollected = 0;
            secretCropsCollected = 0;
            robotsDefeatedThisRun = 0;
            deathsThisRun = 0;
            totalNormalCrops = 0;
            totalSecretCrops = 0;
            livesRemaining = 0;
            starsEarned = 0;
            bossCleared = false;
            worldUnlocked = false;
            hasCheckpoint = false;
            respawnPosition = Vector2.zero;
        }

        public void CollectCrop(bool secretCluster)
        {
            cropsCollectedThisRun++;
            if (secretCluster)
            {
                secretCropsCollected++;
            }
            else
            {
                normalCropsCollected++;
            }
        }

        public void DefeatRobot()
        {
            robotsDefeatedThisRun++;
        }

        /// <summary>Called by LevelLoader once the level's PlayerStartPoint is known.</summary>
        public void SetLevelStart(Vector2 position)
        {
            respawnPosition = position;
            hasCheckpoint = false;
        }

        public void SetCheckpoint(Vector2 position)
        {
            respawnPosition = position;
            hasCheckpoint = true;
        }
    }
}
