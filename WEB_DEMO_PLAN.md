# Farm Fury: Stampede - Website demo (Meadow Ruins)

Branch `web-demo`, worktree folder `Desktop\FarmFury_Technical\FarmFury_Stampede_Web` (created 2026-10-21 from `main`
at `52298fb`). The mobile game stays on `main` in `Desktop\FarmFury_Stampede`; nothing here is merged back. Bring a
main-game fix over with `git cherry-pick <sha>`. Modelled on Farm Fury: Arcade's web demo
(`Desktop\FarmFury_Technical\FarmFury_Arcade_Web`, its `WEB_DEMO_PLAN.md`), website build only (no YouTube Playable).

Goal: a free demo for marketing at **www.farmfurygames.com/play/stampede/** - Meadow Ruins' 11 levels and the Commander
boss, every character (the seven Meadow Ruins rare pellets unlock all six locked ones), the cherry power-up.

## What this branch changes

- **Removed:** packages `com.unity.purchasing`, `com.unity.services.levelplay`, `com.unity.services.analytics` (and the
  androidjni / unityanalytics modules), `Assets/MobileDependencyResolver`, `Assets/Resources/BillingMode.json`.
  `AdManager` and `AnalyticsManager` are API-compatible no-op stubs (no ad is ever ready, so every Watch Ad button
  stays hidden or dimmed); `IAPManager` keeps its coin/cosmetic helpers but every money purchase fails at once.
- **Meadow Ruins only:** `Core/WebDemo.cs`; `SaveManager.IsWorldUnlocked` = Meadow Ruins only. World Select still
  shows all nine badges; tapping any other world opens `UI/FullGameScreen.cs` ("<World> is in the full game ... coming
  soon to Google Play and the App Store"). Beating the Meadow Ruins boss: Level Complete's play button opens the
  same panel ("You cleared Meadow Ruins!", Frozen Tundra's badge), then World Select. No World Unlocked page.
- **Hidden:** the landing Exit button, the HUD Locker button, Leaderboards in Settings; the settings cog (landing and
  Level Complete) opens Settings directly (no Shop / merch hub). Coins stay (revive and ability skip cost coins).
- **Trim (`Editor/WebDemoBuilder.cs`, menu Farm Fury Stampede > Web Demo):** "Trim To Meadow Ruins" drops every other
  world's LevelData from DataManager, all cosmetics, the other worlds' music, and the other worlds' menu backdrops /
  cards (badges kept). "Apply Web Size Settings" = Arcade's WebGL texture/audio overrides (actors 256, sprites 512,
  backdrops 2048; crunched DXT5 or ASTC; AAC audio), splash off, stripping High, IL2CPP optimise for size. "Build
  Website" does both, then builds into `Builds/Website` with the `StampedeEmbed` template (full-window page for an
  iframe, loading bar, "turn your phone sideways" hint, postMessage `{source:"farmfury-stampede", ...}`), gzip +
  decompression fallback. **No hashed file names:** on 6000.5.6f1 they loop the build ("Backend has requested a
  buildprogram run 6 times"); instead the build stamps `?v=<UTC build time>` on every `Build/` link in index.html,
  so a new upload is fetched past the host's one-day cache. It also deletes the Burst debug folder (not for upload).

## Working rules

- **Never run Phase 5a Run Setup here without re-running Trim afterwards** - it re-adds every world (Build Website
  trims anyway).
- Unity 6000.5.6f1 needs its WebGL Build Support module (installed through Unity Hub 2026-10-21).

## Upload (cPanel, domains.co.za - same host as Arcade's demo)

`Builds/farmfury-stampede-web.zip` holds the *contents* of `Builds/Website` (index.html + Build/). Create
`public_html/play/stampede/`, upload the zip **into that folder**, Extract there, delete the zip. **Never extract it in
`public_html/play/`** - that is Arcade's demo (its index.html + hashed `Build/` files); on 2026-10-21 the first upload
landed there and replaced Arcade's index.html (fix kit: `Desktop\FarmFury_Technicalarmfurygames-website\stampede-fix\`).
The home page's Stampede slide (`RushStage` in the site's index.html) has its own Play button, patched to
`/play/stampede/` in that kit; a new Claude Design export of the site will undo it. Check `https://www.farmfurygames.com/play/stampede/` and one
file under `Build/` return 200. The page caches for a day: hard-refresh (Ctrl+F5). See Arcade's CLAUDE.md "Website &
legal pages" for the host's gotchas (a zip is not unpacked on upload, only `index.html` is served for a folder).

## Progress

- 2026-10-21: branch, stubs, Meadow-only flow, Full Game panel, trim/build tools, embed template written. Compiles;
  website build 20.9 MB (data 14.6 MB + wasm 7.1 MB gzipped). Tested in headless Edge from a local server
  (`ThreadingHTTPServer`; plain `python -m http.server` failed the wasm download, as Arcade found): boots in ~3 s,
  landing (no Exit), World Select, Full Game panel on Frozen Tundra, Level Select, Meadow Ruins 1 played (run,
  jump, a Scout costs a life), Pause. Headless testing needs `Emulation.setFocusEmulationEnabled` - with
  runInBackground off the game pauses in an unfocused page. Not yet uploaded.
