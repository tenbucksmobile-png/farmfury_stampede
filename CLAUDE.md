# CLAUDE.md

This file provides guidance to Claude Code when working in this repository.

# Farm Fury: Stampede

Side-scrolling platformer in the Farm Fury universe. Players pick one of eight farm animals and fight through hand-designed levels across six territories the Harvest Robots have occupied — running, jumping, and using each character's signature ability to defeat robots and reach the end of the level. Every level is completable by any unlocked character, but each also hides secrets that only one specific character's ability can reach. The character-before-level choice (not mid-level swapping, like Rush or Arcade) is the central differentiator.

Full design spec: `FarmFury_Stampede_GDD.md`

## Status

Phase 1 (foundation) built. Phase 2 (core movement & controls) **built, compiles, and the setup script runs cleanly headless; not yet playtested by hand** — jump/movement feel is the phase's exit gate and needs a human in the Game tab (not Device Simulator).

Phase 2 workflow: menu **Farm Fury Stampede → Phase 2 → Run Setup** regenerates placeholder sprites, tiles, `Cluck`/`Crop` prefabs and the test level under `Phase2Level` in `Game.unity` (safe to re-run; it also removes the Phase1Test object). Play: A/D or arrows, Space, **R restarts**; an on-screen debug overlay shows state, crops, grounded, coyote and jump-buffer timers. Tuning lives on the `CharacterController2D` component; `moveSpeed`/`jumpHeight` are pulled from `CharacterData_Cluck` at Start.

Layers `Ground` (6) and `Player` (7) were added to TagManager. `com.unity.2d.tilemap.editor` was removed from the manifest (merged into `com.unity.2d.tilemap` in Unity 6.5; the dangling entry blocked package resolution).

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

1. **Project foundation** — folder structure, core singletons (`GameManager`, `DataManager`, `SaveManager`, `AudioManager`), ScriptableObject definitions (`CharacterData`, `RobotData`, `LevelData`, `WorldData`), enums, empty `Game.unity` scene, placeholder data. No gameplay. Built; verification pending (see Status above).
2. **Core movement & controls** — Cluck only. Kinematic `Rigidbody2D` + `MovePosition` movement, coyote time, jump buffering, one hand-built test level in Meadow Ruins. Highest-risk phase — don't proceed until jump/movement genuinely feels good in playtesting, same lesson as Rush's Phase 2.
3. **Level system & World 1** — Tilemap-based level loading from ScriptableObject descriptors, all 8 Meadow Ruins levels, ground-patrol and drone robot AI, checkpoint/level-complete flow, crop collection.
4. **Characters & abilities** — remaining 7 characters and all 8 unique abilities, character-select-before-level UI, ability-gated secrets, the 5/10/15/20/30/40 unlock ladder.
5. **Remaining worlds, bosses & progression UI** — Worlds 2–6, per-world bosses, Robot Overlord finale, World Select/Level Select screens, HUD, pause.
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
