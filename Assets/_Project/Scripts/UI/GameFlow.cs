using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Owns the menu/HUD screens and the navigation between them:
    /// Landing (Shop &amp; Settings) -> World Select -> Level Select -> Character Select -> Playing (HUD) -> Paused / Results -> Level Select.
    /// The screens are built in code under one canvas; GameManager's state decides which are visible.
    /// Menus hide the player and unload the level; StartLevel loads it again.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        [SerializeField] private CharacterSelectScreen characterSelect;
        [SerializeField] private MenuArt menuArt = new();

        public static GameFlow Instance { get; private set; }

        public LandingScreen Landing { get; private set; }
        public ShopSettingsScreen ShopSettings { get; private set; }
        public HudScreen Hud { get; private set; }
        public WorldSelectScreen Worlds { get; private set; }
        public LevelSelectScreen Levels { get; private set; }
        public PauseScreen Pause { get; private set; }
        public ResultsScreen Results { get; private set; }
        public CharacterSelectScreen CharacterSelect => characterSelect;
        public WorldType CurrentWorld { get; private set; } = WorldType.MeadowRuins;

        private GameManager _gm;
        private Transform _canvasTransform;

        private void Awake()
        {
            Instance = this;

            var canvas = UIKit.CreateCanvas("UICanvas", 10, transform);
            UIKit.EnsureEventSystem(transform);

            Landing = new LandingScreen(canvas.transform, EnterWorldSelect, ExitGame, OpenShopSettings, menuArt);
            ShopSettings = new ShopSettingsScreen(canvas.transform, CloseShopSettings, menuArt);
            Hud = new HudScreen(canvas.transform, OpenPause, menuArt);
            Worlds = new WorldSelectScreen(canvas.transform, EnterLevelSelect, EnterLanding, menuArt);
            Levels = new LevelSelectScreen(canvas.transform, PickLevel, EnterWorldSelect, menuArt);
            _canvasTransform = canvas.transform;
            Results = new ResultsScreen(canvas.transform, PlayNextLevel, RestartLevel, LeaveResults, EnterLanding, OpenShopSettings, menuArt);
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
            characterSelect.Build(_canvasTransform, menuArt);   // needs DataManager, so not in Awake
            _gm.StateChanged += ApplyState;
            EnterLanding();
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
            if (ShopSettings.Root.activeSelf)
            {
                CloseShopSettings();
                return;
            }

            switch (_gm.CurrentState)
            {
                case GameState.Playing: OpenPause(); break;
                case GameState.Paused:
                    if (Pause.SettingsOpen) { Pause.CloseSettings(); } else { ResumeFromPause(); }
                    break;
                case GameState.CharacterSelect: characterSelect.Close(); break;
                case GameState.LevelSelect: EnterWorldSelect(); break;
                case GameState.WorldSelect: EnterLanding(); break;
                case GameState.LevelComplete:
                case GameState.LevelFailed: QuitToLevelSelect(); break;
            }
        }

        // ------------------------------------------------------------ navigation

        /// <summary>The landing screen: where the game opens.</summary>
        public void EnterLanding()
        {
            LeaveGameplay();
            _gm.SetState(GameState.MainMenu);
        }

        /// <summary>Shop &amp; Settings over whatever screen opened it (landing, Level Complete); Back returns to it.</summary>
        public void OpenShopSettings()
        {
            ShopSettings.Root.transform.SetAsLastSibling();
            ShopSettings.Root.SetActive(true);
        }

        public void CloseShopSettings()
        {
            ShopSettings.Root.SetActive(false);
        }

        /// <summary>Exit on the landing screen: quits the app (stops Play mode in the editor).</summary>
        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

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

        /// <summary>
        /// Level Complete's play button: Character Select for the next level in the world (the character is chosen
        /// before every level); after the world's last level (the boss), World Select.
        /// </summary>
        public void PlayNextLevel()
        {
            var level = _gm.CurrentLevel;
            if (level == null)
            {
                EnterWorldSelect();
                return;
            }

            var levels = DataManager.Instance.GetWorldLevels(level.worldType);
            int index = levels.IndexOf(level);
            var next = index >= 0 && index + 1 < levels.Count ? levels[index + 1] : null;
            if (next == null || !SaveManager.Instance.IsLevelUnlocked(next))
            {
                if (next == null) { EnterWorldSelect(); } else { QuitToLevelSelect(); }
                return;
            }

            CurrentWorld = next.worldType;
            LeaveGameplay();
            characterSelect.Open(next);
        }

        private void LeaveGameplay()
        {
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

            Landing.Root.SetActive(state == GameState.MainMenu);
            if (state != GameState.MainMenu) { ShopSettings.Root.SetActive(false); }
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
                Results.ShowFailed();   // waits for a button: retry, Level Select or home
            }
            else
            {
                Results.Hide();
            }
        }
    }
}
