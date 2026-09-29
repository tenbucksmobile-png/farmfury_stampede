using System.Collections.Generic;
using System.Linq;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// Owns the menu/HUD screens and the navigation between them:
    /// Landing -> World Select -> Level Select -> Character Select -> Playing (HUD) -> Paused / Results -> Level Select.
    /// The screens are built in code under one canvas; GameManager's state decides which are visible.
    /// Menus hide the player and unload the level; StartLevel loads it again.
    /// The Settings / Shop family (ported from Arcade, see <see cref="OverlayScreen"/>) opens on top of any screen:
    /// the settings cog (landing, Level Complete) opens the menu hub, Pause's Settings opens Settings.
    /// Phase 6 (Arcade's monetisation): the revive prompt on the last life, the HUD's Locker, the Level Complete
    /// Double Coins ad, and the bottom banner ad on Pause and Level Failed (never with Remove Ads).
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        [SerializeField] private CharacterSelectScreen characterSelect;
        [SerializeField] private MenuArt menuArt = new();
        [SerializeField] private ShopArt shopArt = new();
        [SerializeField] private LeaderboardArt leaderboardArt = new();

        public static GameFlow Instance { get; private set; }

        public LandingScreen Landing { get; private set; }
        public MenuHubScreen MenuHub { get; private set; }
        public SettingsScreen Settings { get; private set; }
        public ShopScreen ShopHub { get; private set; }
        public ParentalGate Gate { get; private set; }
        public RevivePromptScreen Revive { get; private set; }
        public LockerScreen Locker { get; private set; }
        public NewCharacterScreen NewCharacter { get; private set; }
        public HudScreen Hud { get; private set; }
        public WorldSelectScreen Worlds { get; private set; }
        public LevelSelectScreen Levels { get; private set; }
        public PauseScreen Pause { get; private set; }
        public ResultsScreen Results { get; private set; }
        public RarePelletCelebration PelletCelebration { get; private set; }
        public SwapCharacterScreen Swap { get; private set; }
        public CharacterSelectScreen CharacterSelect => characterSelect;
        public WorldType CurrentWorld { get; private set; } = WorldType.MeadowRuins;

        private GameManager _gm;
        private Transform _canvasTransform;
        private readonly List<OverlayScreen> _overlays = new();

        private void Awake()
        {
            Instance = this;

            var canvas = UIKit.CreateCanvas("UICanvas", 10, transform);
            UIKit.EnsureEventSystem(transform);

            Landing = new LandingScreen(canvas.transform, EnterWorldSelect, ExitGame, OpenMenuHub, menuArt);
            Hud = new HudScreen(canvas.transform, OpenPause, OpenLocker, OpenSwap, menuArt, shopArt);
            Worlds = new WorldSelectScreen(canvas.transform, EnterLevelSelect, OpenWorldShop, EnterLanding, menuArt, shopArt);
            Levels = new LevelSelectScreen(canvas.transform, PickLevel, EnterWorldSelect, menuArt);
            _canvasTransform = canvas.transform;
            Results = new ResultsScreen(canvas.transform, PlayNextLevel, RestartLevel, LeaveResults, EnterLanding, OpenMenuHub, menuArt, shopArt);
            Pause = new PauseScreen(canvas.transform, ResumeFromPause, OpenSettings, RestartLevel, QuitToLevelSelect, EnterLanding, menuArt);
            Swap = new SwapCharacterScreen(canvas.transform, menuArt);
            PelletCelebration = RarePelletCelebration.Create(canvas.transform, menuArt);
        }

        /// <summary>The HUD's swap button (and Tab): the character picker, over a frozen level.</summary>
        public void OpenSwap()
        {
            if (_gm.CurrentState == GameState.Playing && !_gm.ReviveDecisionPending && !PelletCelebration.IsOpen)
            {
                Swap.Open();
            }
        }

        /// <summary>
        /// The Settings / Shop overlays. Built in Start: the Worlds page needs DataManager for the world cards.
        /// Each opens on top of whatever is showing and its back button returns to it.
        /// </summary>
        private void BuildOverlays()
        {
            var c = _canvasTransform;
            Gate = Add(new ParentalGate(c, menuArt, shopArt));
            var useCoins = Add(new UseCoinsPrompt(c, menuArt, shopArt));
            Revive = Add(new RevivePromptScreen(c, menuArt, shopArt));
            NewCharacter = Add(new NewCharacterScreen(c, menuArt, shopArt));
            var legal = Add(new LegalScreen(c, menuArt, shopArt));
            var story = Add(new CharacterStoryScreen(c, menuArt, shopArt));
            var worldDetail = Add(new WorldDetailScreen(c, menuArt, shopArt, leaderboardArt));
            var worldShop = Add(new ItemPurchaseScreen(c, "WorldPurchase", menuArt, shopArt, Gate, useCoins, null, menuArt.worldUnlockedSign,
                "NEW WORLDS", WorldItems(), new Vector2(500f, 281f), 30f, shopArt.worldPrice));
            _worldShop = worldShop;
            worldShop.Closed += () => { if (Worlds.Root.activeSelf) { Worlds.Refresh(); } };   // a bought world opens at once
            var leaderboards = Add(new LeaderboardsScreen(c, menuArt, shopArt, leaderboardArt, worldDetail.Show, worldShop.Show));

            var itemCell = new Vector2(OverlayScreen.ItemWidth, OverlayScreen.ItemHeight);
            int trailsFirst = StoreProducts.Hats.Length;
            int machinesFirst = trailsFirst + StoreProducts.Trails.Length;
            var hats = Add(new ItemPurchaseScreen(c, "CosmeticsHats", menuArt, shopArt, Gate, useCoins, shopArt.hatsBanner, null, "HATS & CAPS",
                Items(StoreProducts.Hats, shopArt.hatItems, 0), itemCell, 77f));
            var trails = Add(new ItemPurchaseScreen(c, "CosmeticsTrails", menuArt, shopArt, Gate, useCoins, shopArt.trailsBanner, null, "TRAILS",
                Items(StoreProducts.Trails, shopArt.trailItems, trailsFirst), itemCell, 50f));
            var machines = Add(new ItemPurchaseScreen(c, "CosmeticsMachines", menuArt, shopArt, Gate, useCoins, shopArt.machinesBanner, null, "MACHINES",
                Items(StoreProducts.Machines, shopArt.machineItems, machinesFirst), itemCell, 100f));
            var chooser = Add(new CosmeticsChooserScreen(c, menuArt, shopArt, hats.Show, trails.Show, machines.Show));
            Locker = Add(new LockerScreen(c, menuArt, shopArt, type => type switch
            {
                CosmeticType.Hat => hats,
                CosmeticType.Trail => trails,
                _ => machines,
            }));
            var coins = Add(new CoinPurchaseScreen(c, menuArt, shopArt, Gate));

            ShopHub = Add(new ShopScreen(c, menuArt, shopArt, Gate, coins.Show, worldShop.Show, chooser.Show));
            Settings = Add(new SettingsScreen(c, menuArt, shopArt, leaderboards.Show, story.Show, legal.Show));
            MenuHub = Add(new MenuHubScreen(c, menuArt, shopArt, Settings.Show, ShopHub.Show));
        }

        private ItemPurchaseScreen _worldShop;

        /// <summary>A paid world's card on World Select: the world shop ($3.99 each).</summary>
        private void OpenWorldShop() => _worldShop?.Show();

        private T Add<T>(T overlay) where T : OverlayScreen
        {
            _overlays.Add(overlay);
            return overlay;
        }

        // A price plaque per product; the label (shown only without art) is the cosmetic's name.
        private static ItemPurchaseScreen.Item[] Items(string[] productIds, Sprite[] sprites, int firstNameIndex) =>
            productIds.Select((id, i) => new ItemPurchaseScreen.Item(id, ShopArt.At(sprites, i),
                StoreProducts.Cosmetics[firstNameIndex + i].name)).ToArray();

        /// <summary>The Worlds page: IAPManager.PurchasableWorlds (purchaseRequired worlds; none yet, so it says "coming soon").</summary>
        private static ItemPurchaseScreen.Item[] WorldItems()
        {
            return IAPManager.PurchasableWorlds()
                .Select(w => DataManager.Instance.GetWorldData(w))
                .Where(w => w != null)
                .Select(w => new ItemPurchaseScreen.Item(StoreProducts.World(w.worldType), w.selectCardArt, w.displayName))
                .ToArray();
        }

        private OverlayScreen TopOverlay() =>
            _overlays.Where(o => o.IsOpen).OrderByDescending(o => o.Root.transform.GetSiblingIndex()).FirstOrDefault();

        private void CloseOverlays()
        {
            foreach (var overlay in _overlays)
            {
                overlay.Hide();
            }
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
                if (Revive != null) { _gm.ReviveOffered -= Revive.Show; }
            }
        }

        private void Start()
        {
            _gm = GameManager.Instance;
            characterSelect.Build(_canvasTransform, menuArt);   // needs DataManager, so not in Awake
            BuildOverlays();
            _gm.StateChanged += ApplyState;
            _gm.ReviveOffered += Revive.Show;
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
            if (Revive != null && Revive.IsOpen) { Revive.Tick(); }
            if (Results.Root.activeSelf) { Results.Tick(); }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscape();
            }
            else if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                if (Swap.IsOpen) { Swap.Close(); } else { OpenSwap(); }
            }
        }

        private void HandleEscape()
        {
            if (PelletCelebration != null && PelletCelebration.IsOpen)
            {
                PelletCelebration.Skip();
                return;
            }
            if (Swap != null && Swap.IsOpen)
            {
                Swap.Close();
                return;
            }

            var overlay = TopOverlay();
            if (overlay != null)
            {
                overlay.Hide();
                return;
            }

            switch (_gm.CurrentState)
            {
                case GameState.Playing: OpenPause(); break;
                case GameState.Paused: ResumeFromPause(); break;
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

        /// <summary>The settings cog (landing, Level Complete): the menu hub over that screen; Back returns to it.</summary>
        public void OpenMenuHub()
        {
            MenuHub.Show();
        }

        /// <summary>The HUD's Locker button: equip owned cosmetics mid-level (pauses while open).</summary>
        public void OpenLocker()
        {
            Locker?.Open();
        }

        /// <summary>Pause's Settings: straight to the Settings panel, over the pause menu.</summary>
        public void OpenSettings()
        {
            Settings.Show();
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

            // No character pick before a level any more: it starts as whoever the player last played (swapped to
            // or started with); they can swap mid-level from the HUD.
            _gm.StartLevel(level, SaveManager.Instance.LastCharacter);
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
        /// Level Complete's play button: back to Level Select, where the next level now shows as the question mark
        /// (the character is picked from there, as for every level). After the world's last level (the boss),
        /// World Select, where the newly unlocked world is waiting.
        /// </summary>
        public void PlayNextLevel()
        {
            var level = _gm.CurrentLevel;
            if (level == null || level.isBossLevel)
            {
                EnterWorldSelect();
                return;
            }

            QuitToLevelSelect();
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
            // Drop the UI focus on every screen change: otherwise a button that had focus stays the target of the
            // menu "submit" key (Space / Enter), so the jump key held at the goal could press a results button.
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            bool inLevel = state == GameState.Playing || state == GameState.Paused
                || state == GameState.LevelComplete || state == GameState.LevelFailed;

            CloseOverlays();
            if (state != GameState.Playing) { PelletCelebration.Close(); Swap.Hide(); }
            Landing.Root.SetActive(state == GameState.MainMenu);
            Hud.Root.SetActive(inLevel);
            Worlds.Root.SetActive(state == GameState.WorldSelect);
            Levels.Root.SetActive(state == GameState.LevelSelect);

            if (state == GameState.WorldSelect) { Worlds.Refresh(); }
            if (state == GameState.LevelSelect) { Levels.Show(CurrentWorld); }
            if (state != GameState.CharacterSelect) { characterSelect.HideVisual(); }

            if (state == GameState.Paused) { Pause.Show(); } else { Pause.Hide(); }

            // Arcade's banner placements: Pause and Level Failed only, and never once Remove Ads is owned.
            var ads = AdManager.Instance;
            if (ads != null)
            {
                bool banner = (state == GameState.Paused || state == GameState.LevelFailed)
                    && SaveManager.Instance != null && !SaveManager.Instance.AdsRemoved;
                if (banner) { ads.ShowBanner(); } else { ads.HideBanner(); }
            }

            if (state == GameState.LevelComplete)
            {
                Results.ShowComplete(_gm);
                // A character unlocked by this clear gets its own page (with confetti) over the results.
                if (_gm.RunState.newlyUnlockedCharacters.Count > 0)
                {
                    NewCharacter.ShowUnlocks(_gm.RunState.newlyUnlockedCharacters);
                }
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
