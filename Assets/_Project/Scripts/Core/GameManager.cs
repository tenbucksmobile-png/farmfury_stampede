using FarmFuryStampede.Data;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Core
{
    /// <summary>
    /// Owns the top-level game flow state. Single scene architecture: transitions happen by
    /// changing <see cref="CurrentState"/>, not by loading scenes.
    /// </summary>
    public class GameManager : MonoSingleton<GameManager>
    {
        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        public LevelData CurrentLevel { get; private set; }
        public CharacterType CurrentCharacter { get; private set; }

        /// <summary>Begins a level attempt with the given level and character, moving to the Playing state.</summary>
        public void StartLevel(LevelData level, CharacterType character)
        {
            CurrentLevel = level;
            CurrentCharacter = character;
            CurrentState = GameState.Playing;

            Debug.Log($"[GameManager] StartLevel: level={level?.levelId}, character={character}.");
        }

        /// <summary>Ends the current level attempt, recording stars and persisting progress.</summary>
        /// <param name="completed">True if the level was completed, false if it was failed.</param>
        /// <param name="stars">Stars earned this attempt (ignored when not completed).</param>
        public void EndLevel(bool completed, int stars)
        {
            CurrentState = completed ? GameState.LevelComplete : GameState.LevelFailed;

            if (completed && CurrentLevel != null && SaveManager.Instance != null)
            {
                SaveManager.Instance.SetLevelStars(CurrentLevel.levelId, stars);
                SaveManager.Instance.SaveProgress();
            }

            Debug.Log($"[GameManager] EndLevel: completed={completed}, stars={stars}, state={CurrentState}.");
        }

        /// <summary>Pauses gameplay without ending the level attempt.</summary>
        public void PauseGame()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
        }

        /// <summary>Resumes gameplay from a paused state.</summary>
        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused)
            {
                return;
            }

            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
        }

        /// <summary>Moves to the given menu/selection state. Used by UI flows outside gameplay.</summary>
        public void SetState(GameState state)
        {
            CurrentState = state;
        }
    }
}
