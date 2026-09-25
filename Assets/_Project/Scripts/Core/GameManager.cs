using System;
using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Core
{
    /// <summary>
    /// Owns the top-level game flow state. Single scene architecture: transitions happen by
    /// changing <see cref="CurrentState"/>, not by loading scenes. UI (GameFlow) reacts to
    /// <see cref="StateChanged"/>.
    /// </summary>
    public class GameManager : MonoSingleton<GameManager>
    {
        /// <summary>Lives per level attempt (Arcade's proven "3 free respawns" convention).</summary>
        public const int LivesPerAttempt = 3;

        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        public LevelData CurrentLevel { get; private set; }
        public CharacterType CurrentCharacter { get; private set; }

        /// <summary>Runtime state of the level attempt in progress (crops collected, lives, checkpoint...).</summary>
        public LevelRunState RunState { get; } = new();

        /// <summary>Raised whenever <see cref="CurrentState"/> changes.</summary>
        public event Action<GameState> StateChanged;

        private void ChangeState(GameState state)
        {
            CurrentState = state;
            StateChanged?.Invoke(state);
        }

        /// <summary>
        /// Begins a level attempt: resets run state (a fresh attempt always has full lives), has
        /// <see cref="LevelLoader"/> replace whatever level is loaded with the given one, then moves to Playing.
        /// </summary>
        public void StartLevel(LevelData level, CharacterType character)
        {
            CurrentLevel = level;
            CurrentCharacter = character;
            RunState.Reset();
            RunState.livesRemaining = LivesPerAttempt;
            Time.timeScale = 1f;

            if (LevelLoader.Instance != null && level != null)
            {
                LevelLoader.Instance.LoadLevel(level, character);
            }

            ChangeState(GameState.Playing);

            Debug.Log($"[GameManager] StartLevel: level={level?.levelId}, character={character}, lives={RunState.livesRemaining}.");
        }

        /// <summary>
        /// A death (pit fall, robot contact, drowning). Costs a life and respawns at the last checkpoint (or the
        /// level start); losing the last life ends the attempt with EndLevel(completed: false).
        /// </summary>
        public void RespawnPlayer()
        {
            if (CurrentState != GameState.Playing || LevelLoader.Instance == null || LevelLoader.Instance.IsRespawning)
            {
                return;   // not playing, or already in the defeat pose (repeat contacts must not cost extra lives)
            }

            RunState.deathsThisRun++;
            RunState.livesRemaining = Mathf.Max(0, RunState.livesRemaining - 1);

            if (RunState.livesRemaining <= 0)
            {
                Debug.Log("[GameManager] Out of lives.");
                LevelLoader.Instance.FreezePlayerDefeated();
                EndLevel(completed: false, stars: 0);
                return;
            }

            LevelLoader.Instance.RespawnPlayer(RunState.respawnPosition);
            Debug.Log($"[GameManager] Death #{RunState.deathsThisRun}: {RunState.livesRemaining} lives left, respawn at " +
                      $"{RunState.respawnPosition} (checkpoint={RunState.hasCheckpoint}).");
        }

        /// <summary>
        /// Completes the current level: scores 1-3 stars (see <see cref="StarCalculator"/>) and calls
        /// EndLevel(true). Used by the level goal and by boss encounters.
        /// </summary>
        public void CompleteLevel()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            EndLevel(completed: true, stars: StarCalculator.Compute(CurrentLevel, RunState));
        }

        /// <summary>Ends the current level attempt, recording progress when completed.</summary>
        /// <param name="completed">True if the level was completed, false if it was failed.</param>
        /// <param name="stars">Stars earned this attempt (ignored when not completed).</param>
        public void EndLevel(bool completed, int stars)
        {
            RunState.starsEarned = completed ? stars : 0;

            if (completed && CurrentLevel != null && SaveManager.Instance != null)
            {
                SaveManager.Instance.SetLevelStars(CurrentLevel.levelId, stars);
                var unlocked = SaveManager.Instance.RecordLevelCompleted(CurrentLevel.levelId);
                RunState.newlyUnlockedCharacters.AddRange(unlocked);
                if (CurrentLevel.isBossLevel)
                {
                    RunState.bossCleared = true;
                    RunState.worldUnlocked = CurrentLevel.worldType != WorldType.RobotMothership
                        && !SaveManager.Instance.IsWorldBossCleared(CurrentLevel.worldType);
                    SaveManager.Instance.SetWorldBossCleared(CurrentLevel.worldType, true);
                }
                SaveManager.Instance.SaveProgress();
            }

            ChangeState(completed ? GameState.LevelComplete : GameState.LevelFailed);

            Debug.Log($"[GameManager] EndLevel: completed={completed}, stars={stars}, state={CurrentState}.");
        }

        /// <summary>Pauses gameplay without ending the level attempt.</summary>
        public void PauseGame()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            Time.timeScale = 0f;
            ChangeState(GameState.Paused);
        }

        /// <summary>Resumes gameplay from a paused state.</summary>
        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            ChangeState(GameState.Playing);
        }

        /// <summary>Moves to the given menu/selection state. Used by UI flows outside gameplay.</summary>
        public void SetState(GameState state)
        {
            Time.timeScale = 1f;
            ChangeState(state);
        }
    }
}
