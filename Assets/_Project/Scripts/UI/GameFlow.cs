using System.Collections;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Owns the menu/HUD screens and the navigation between them:
    /// World Select -> Level Select -> Character Select -> Playing (HUD) -> Paused / Results -> Level Select.
    /// The screens are built in code under one canvas; GameManager's state decides which are visible.
    /// Menus hide the player and unload the level; StartLevel loads it again.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public const float FailedReturnSeconds = 2.5f;

        [SerializeField] private CharacterSelectScreen characterSelect;

        public static GameFlow Instance { get; private set; }

        public HudScreen Hud { get; private set; }
        public WorldSelectScreen Worlds { get; private set; }
        public LevelSelectScreen Levels { get; private set; }
        public PauseScreen Pause { get; private set; }
        public ResultsScreen Results { get; private set; }
        public CharacterSelectScreen CharacterSelect => characterSelect;
        public WorldType CurrentWorld { get; private set; } = WorldType.MeadowRuins;

        private GameManager _gm;
        private Coroutine _failedReturn;
        private Transform _canvasTransform;

        private void Awake()
        {
            Instance = this;

            var canvas = UIKit.CreateCanvas("UICanvas", 10, transform);
            UIKit.EnsureEventSystem(transform);

            Hud = new HudScreen(canvas.transform, OpenPause);
            Worlds = new WorldSelectScreen(canvas.transform, EnterLevelSelect);
            Levels = new LevelSelectScreen(canvas.transform, PickLevel, EnterWorldSelect);
            _canvasTransform = canvas.transform;
            Results = new ResultsScreen(canvas.transform, RestartLevel, LeaveResults);
            Pause = new PauseScreen(canvas.transform, ResumeFromPause, RestartLevel, QuitToLevelSelect);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            if (_gm != null)
            {
                _gm.StateChanged -= ApplyState;
            }
        }

        private void Start()
        {
            _gm = GameManager.Instance;
            characterSelect.Build(_canvasTransform);   // needs DataManager, so not in Awake
            _gm.StateChanged += ApplyState;
            EnterWorldSelect();
        }

        private void Update()
        {
            if (_gm == null)
            {
                return;
            }

            if (Hud.Root.activeSelf && LevelLoader.Instance != null)
            {
                Hud.Refresh(_gm, LevelLoader.Instance.Player);
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscape();
            }
        }

        private void HandleEscape()
        {
            switch (_gm.CurrentState)
            {
                case GameState.Playing: OpenPause(); break;
                case GameState.Paused:
                    if (Pause.SettingsOpen) { Pause.CloseSettings(); } else { ResumeFromPause(); }
                    break;
                case GameState.CharacterSelect: characterSelect.Close(); break;
                case GameState.LevelSelect: EnterWorldSelect(); break;
            }
        }

        // ------------------------------------------------------------ navigation

        public void EnterWorldSelect()
        {
            LeaveGameplay();
            _gm.SetState(GameState.WorldSelect);
        }

        public void EnterLevelSelect(WorldType world)
        {
            CurrentWorld = world;
            LeaveGameplay();
            _gm.SetState(GameState.LevelSelect);
        }

        /// <summary>A level tile was picked: locked levels do nothing, unlocked ones open character select.</summary>
        public void PickLevel(LevelData level)
        {
            if (level == null || !SaveManager.Instance.IsLevelUnlocked(level))
            {
                return;
            }

            characterSelect.Open(level);
        }

        public void OpenPause()
        {
            _gm.PauseGame();
        }

        public void ResumeFromPause()
        {
            _gm.ResumeGame();
        }

        /// <summary>Reloads the current level from its LevelData through LevelLoader with the same character.</summary>
        public void RestartLevel()
        {
            if (_gm.CurrentLevel == null)
            {
                return;
            }

            _gm.ResumeGame();
            _gm.StartLevel(_gm.CurrentLevel, _gm.CurrentCharacter);
        }

        public void QuitToLevelSelect()
        {
            EnterLevelSelect(_gm.CurrentLevel != null ? _gm.CurrentLevel.worldType : CurrentWorld);
        }

        private void LeaveResults()
        {
            QuitToLevelSelect();
        }

        private void LeaveGameplay()
        {
            if (_failedReturn != null)
            {
                StopCoroutine(_failedReturn);
                _failedReturn = null;
            }

            characterSelect.HideVisual();
            var loader = LevelLoader.Instance;
            if (loader != null)
            {
                loader.UnloadLevel();
                loader.Player.gameObject.SetActive(false);
            }
        }

        /// <summary>Re-reads the save into whichever menu is showing (after a debug change).</summary>
        public void RefreshCurrentScreen()
        {
            if (_gm.CurrentState == GameState.WorldSelect) { Worlds.Refresh(); }
            if (_gm.CurrentState == GameState.LevelSelect) { Levels.Show(CurrentWorld); }
        }

        // ------------------------------------------------------------ screens follow game state

        private void ApplyState(GameState state)
        {
            bool inLevel = state == GameState.Playing || state == GameState.Paused
                || state == GameState.LevelComplete || state == GameState.LevelFailed;

            Hud.Root.SetActive(inLevel);
            Worlds.Root.SetActive(state == GameState.WorldSelect);
            Levels.Root.SetActive(state == GameState.LevelSelect);

            if (state == GameState.WorldSelect) { Worlds.Refresh(); }
            if (state == GameState.LevelSelect) { Levels.Show(CurrentWorld); }
            if (state != GameState.CharacterSelect) { characterSelect.HideVisual(); }

            if (state == GameState.Paused) { Pause.Show(); } else { Pause.Hide(); }

            if (state == GameState.LevelComplete)
            {
                Results.ShowComplete(_gm);
            }
            else if (state == GameState.LevelFailed)
            {
                Results.ShowFailed();
                _failedReturn = StartCoroutine(ReturnAfterFailure());
            }
            else
            {
                Results.Hide();
            }
        }

        private IEnumerator ReturnAfterFailure()
        {
            yield return new WaitForSecondsRealtime(FailedReturnSeconds);
            _failedReturn = null;
            if (_gm.CurrentState == GameState.LevelFailed)
            {
                QuitToLevelSelect();
            }
        }
    }
}
