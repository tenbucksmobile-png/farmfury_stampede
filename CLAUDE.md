# CLAUDE.md

This file provides guidance to Claude Code when working in this repository.

# Farm Fury: Stampede

Side-scrolling platformer in the Farm Fury universe. Players pick one of eight farm animals and fight through hand-designed levels across six territories the Harvest Robots have occupied — running, jumping, and using each character's signature ability to defeat robots and reach the end of the level. Every level is completable by any unlocked character, but each also hides secrets that only one specific character's ability can reach. The character-before-level choice (not mid-level swapping, like Rush or Arcade) is the central differentiator.

Full design spec: `FarmFury_Stampede_GDD.md`

## Status

Phases 1-4 done (foundation; controller; prefab-based levels + Harvester/Drone; 8 characters, abilities, gated secrets, unlock ladder). **Phase 5a (robot variety, the reusable boss pattern, the real progression UI, and lives) is built and machine-verified but not yet hand-playtested.** A scripted headless run drove the robots, the boss, World/Level Select, HUD, lives and pause through the real UI buttons. Feel and difficulty (boss stagger/speed, Scout sight range, Chaser speed, 3 lives) need a human pass.

Phase 5a architecture:
- **Robots** (`Scripts/Robots`): `RobotController` base (a contact is a stomp/ability hit via overridable `TakeHit()`; bosses override it and `CanHurtPlayer`). `GroundPatrolRobot` holds the patrol/wall/ledge logic; `HarvesterRobot` is it as-is, `ScoutRobot` (speed 3.8; turns to face a player who is behind it within sight range with line of sight), `ChaserRobot` (fixed 6.4 u/s vs the player's 8; wakes inside its aggro range = marker patrolDistance; stops at walls/ledges; stompable). `BarrierUnit` (1x3 solid on the Ground layer, refuses every hit, only Billy's Charge Break via `IChargeBreakable`; on open ground it can be jumped over, so use it only in capped passages such as a Chamber). `RobotType.Commander` was appended to the enum.
- **Boss pattern** (`CommanderBoss`, read the comment block at the top): boss level = normal level prefab with `Boss()` (no goal); 3 hits, 1.5s stagger between hits (harmless, un-hittable), speeds up per hit, reinforcement waves via `RobotSpawnPoint.wave` spawned by `LevelLoader.SpawnWave(n)` on the nth hit; defeat -> `GameManager.CompleteLevel()`. Worlds 2-6 duplicate the prefab/RobotData and author a new arena.
- **Lives**: 3 per attempt (`GameManager.LivesPerAttempt`). Every death (pit, robot, drowning) costs one and respawns at the checkpoint; the third calls `EndLevel(false)` -> failure panel -> auto-return to Level Select after 2.5s. `StartLevel` always resets to 3.
- **Progression rules** (`SaveManager`): a world unlocks when the previous world's boss level is cleared (`SetWorldBossCleared` is the test hook); a level unlocks when the previous level in its world is completed; the boss unlocks after all regular levels. Stars (`StarCalculator`): 1 completed, +1 for >=75% of normal crops, +1 for finding the level's gated secret (or finishing without dying if it has none); time is not scored yet. "Secret found" = a secret-cluster crop was collected (`CropPickup.isSecretCluster`, persisted), which drives the Level Select "?" icon.
- **UI** (`Scripts/UI`, uGUI built in code by `UIKit`): `GameFlow` owns navigation (World Select -> Level Select -> Character Select -> HUD/Pause/Results) driven by `GameManager.StateChanged`; menus hide the player and unload the level. Screens: `WorldSelectScreen`, `LevelSelectScreen`, `CharacterSelectScreen`, `HudScreen`, `PauseScreen` (Play/Settings stub/Restart Level/Quit), `ResultsScreen`. Esc = pause/back. **F1 toggles `DebugPanel`** (complete a world's levels, flip boss cleared, unlock all worlds, level count cheats, reset save).
- **Real character art** lives in `Assets/_Project/Sprites/Characters/` (never in `Sprites/Placeholder/`, which setup regenerates). Setup imports the frames listed in `CharacterArtFiles` (StampedePhase5aSetup) at 480 pixels per unit, builds a `CharacterSpriteSet` asset (idle/run/jump per direction + defeat) and assigns it to the character's `CharacterData.spriteSet`; `CharacterSpriteAnimator` swaps frames by controller state. Directional art is swapped, not flipped (`HasDirectionalArt` stops the flip); characters without a set keep their placeholder and flip. Only Cluck has art so far. To add a character: drop the frames, add its file list to `CharacterArtFiles`, re-run setup. Deaths now hold the defeat pose ~0.6s at the spot (`LevelLoader.RespawnPlayer`, guarded by `IsRespawning` so repeat contacts cost one life).
- **Real robot art** lives in `Assets/_Project/Sprites/Robots/` (imported by `ImportRobotArt` in setup, 480 ppu, same as characters). `RobotController` has optional `spriteRight` / `spriteLeft` / `defeatSprite`; `SetFacing(bool)` swaps sprites when both sides exist, otherwise flips (Drone and Commander have a single frame). Setup assigns them per prefab via `RobotArt(right, left, defeat)`: Harvester = `Robot_Harvest_*`, Scout = `ScoutRobot_*`, Chaser = `DriftRobot_*`, Drone = `Drone.png`, Commander = `Commander_Alert` / `Commander_Defeated`; generic `Robot_defeated` for the ground robots. Not wired yet: `Robot_left.png` (unassigned), the two `*_explode` frames, Barrier Unit art (still placeholder). Not yet verified in Unity.
- **Setup**: menu **Farm Fury Stampede -> Phase 5a -> Run Setup** regenerates everything (supersedes the Phase 3/4 setup). Levels: Meadow Ruins 1-8 + `MeadowRuins_Boss`; Chaser in level 4, Scouts in 6 and 8, level 5's secret is a Barrier-sealed chamber (Billy).

Headless testing tips: `Time.captureFramerate = 50` gives deterministic fixed-step play mode under `-batchmode -nographics`; set `InputSystem.settings.backgroundBehavior = IgnoreFocus` and `editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView` or injected keyboard input is ignored. UI buttons can be driven with `button.onClick.Invoke()`. `Tilemap.GetUsedTilesCount()` counts tile *types*, not tiles. `Object.GetInstanceID()` is obsolete in Unity 6.5. A project reimport can add minutes before play mode starts.

Layers `Ground` (6) and `Player` (7) exist. `com.unity.2d.tilemap.editor` was removed from the manifest (merged into `com.unity.2d.tilemap` in Unity 6.5). Open the project at the repo root, not the nested `FarmFury_Stampede/` folder Unity's GitHub flow can create.

## Stack

- Unity 6.5, 2D template, Universal Render Pipeline (URP)
- C#, single-scene architecture (`Game.unity`), data-driven via ScriptableObjects
- Packages: 2D Sprite, 2D Tilemap Editor (the one package difference from Rush — Stampede is tile-based level geometry, not a scrolling chunk system), TextMeshPro, Input System
- Backend (planned, not yet built): Supabase, matching Arcade's proven `UnityIAPServices`/`StoreController` + LevelPlay approach rather than Rush's still-unverified backend assumptions (see GDD Section 12)
- Art: Kling AI + Photopea cleanup; Audio: Suno AI, Pixabay, Freesound

## Related Farm Fury projects

- `Farm Fury` (main game) — defensive combat, shelved
- `Farm Fury: Arcade` — Pac-Man-style stealth crop reclamation, standalone repo at `C:\Users\Personel\Desktop\FarmFury_Arcade`, near-launch
- `Farm Fury: Rush` — endless runner, standalone repo at `C:\Users\Personel\Desktop\FarmFury_Rush`, Phase 2 of 6 complete
- `Farm Fury: Stampede` (this repo) — side-scrolling platformer

All active titles share a Supabase backend (planned) for cross-game player identity, cross-promotion, and shared cosmetic unlocks. Keep data schema compatible with the others when touching save/cloud-sync code.

## Build order — six phases

Each phase is its own Claude Code session, only started once the previous phase is verified working in the Unity editor. Do not implement a later phase's systems while working an earlier one. This mirrors the phase structure that worked for Rush.

1. **Project foundation** — folder structure, core singletons (`GameManager`, `DataManager`, `SaveManager`, `AudioManager`), ScriptableObject definitions (`CharacterData`, `RobotData`, `LevelData`, `WorldData`), enums, empty `Game.unity` scene, placeholder data. No gameplay. Built.
2. **Core movement & controls** — Cluck only. Kinematic `Rigidbody2D` + `MovePosition` movement, coyote time, jump buffering, one hand-built test level in Meadow Ruins. Highest-risk phase — don't proceed until jump/movement genuinely feels good in playtesting, same lesson as Rush's Phase 2. Built and playtested.
3. **Level system & World 1** — Prefab-based level loading from `LevelData`, all 8 Meadow Ruins levels, Harvester and Drone robot AI, checkpoints, crop collection. Built; hand-playtest pending (see Status).
4. **Characters & abilities** — remaining 7 characters and all 8 unique abilities, character-select-before-level UI, ability-gated secrets, the 5/10/15/20/30/40 unlock ladder. Built; hand-playtest pending (see Status).
5. **Remaining worlds, bosses & progression UI** — Worlds 2–6, per-world bosses, Robot Overlord finale, World Select/Level Select screens, HUD, pause. Split into sub-phases: **5a done** (robots, boss pattern, UI, lives; hand-playtest pending); 5b Frozen Tundra, 5c Watermill Village, 5d Sky Islands, 5e Sunken City, 5f Robot Mothership + Overlord.
6. **Monetisation & polish** — Unity IAP (Arcade's proven product catalog, adapted), LevelPlay ad mediation, Firebase Analytics, Supabase cloud save, cross-promo, tutorial, launch polish.

## Key architectural facts (from the GDD)

- **8 characters**: Cluck (Flutter Jump, starter), Bessie (Ground Pound, starter), Percy (Roll Dash, unlock @5 levels), Woolly (Cloud Step, @10), Ducky (Skip Dash, @15), Horace (Rear Vault & Horseshoe Throw, @20), Gerald (Puff Glide, @30), Billy (Charge Break, @40).
- **Design rule**: every main path through every level must be completable by *any* unlocked character — abilities gate secrets and bonus areas, never the critical path.
- **5 robot types**: Harvester (ground patrol), Scout (fast patrol), Drone (flying, needs a jump-into or Horace's horseshoe), Barrier Unit (stationary, only Billy's Charge Break clears it), Chaser (pursues at a fixed sub-player speed).
- **6 worlds**, reused from the shelved original Farm Fury's world designs, reframed as platformer territories: Meadow Ruins (tutorial), Frozen Tundra (reduced ice traction), Watermill Village (fire-spread hazards), Sky Islands (wind gusts), Sunken City (reduced-gravity underwater), Robot Mothership (zero-G, Robot Overlord finale). 46 levels total for v1 launch.
- **Controller genuinely differs from Rush's** despite the shared engine: Rush keeps the character fixed in X while the world scrolls past it; Stampede needs a conventional platformer setup where the *character* traverses a Tilemap-space level with a following camera. The Physics2D lesson from Rush still applies — anything needing reliable collision must use a kinematic `Rigidbody2D` moved via `MovePosition`, never `transform.Translate`.
- **Level authoring**: Unity Tilemap + Composite Collider2D for geometry, with level content still defined as data (ScriptableObject descriptors referencing tilemap layouts), matching the franchise's data-driven convention.
- **Ability model**: per-level-use limit, not a real-time cooldown (Arcade/Rush use timer-based cooldowns; a level-based game fits a use-limit better).

## Working with this repo

- Standalone repo (like Rush and Arcade), separate from the shared home-directory repo the rest of this machine's projects live in.
- Follow the exact folder structure specified in the Phase 1 prompt (`Assets/_Project/Scripts/{Core,Data,Data/Enums,Movement,Characters,Robots,LevelSystem,UI,Utilities}`, `Assets/_Project/ScriptableObjects/{Characters,Robots,Levels,Worlds}`, etc.) so it matches franchise conventions for future Claude Code sessions working across all four repos.
- Namespace convention: `FarmFuryStampede.<Area>` (`Core`, `Data`, `Utilities`, ...), matching the folder it lives in.
- Don't link this repo to GitHub via Unity's own project-creation/GitHub-linking flow — it will silently create a *new* default template project and a *differently-named* GitHub repo instead of using the existing one. Manage git for this repo the same way as Rush and Arcade: plain `git` from the command line, this repo's own `.git`, remote `origin` pointed at `github.com/tenbucksmobile-png/farmfury_stampede`.
- `pandas_ta`/freqtrade conventions from other repos on this machine do not apply here — this is a Unity/C# project, unrelated stack.
