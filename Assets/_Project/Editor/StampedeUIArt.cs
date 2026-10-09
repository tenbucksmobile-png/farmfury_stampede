using System.Collections.Generic;
using System.IO;
using System.Linq;
using FarmFuryStampede.Data;
using FarmFuryStampede.UI;
using UnityEditor;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Imports the real gameplay-object art dropped into Assets/_Project/Sprites/UI (checkpoint flag, goal
    /// flag, the barrier-chamber backdrop, and a crop/collectible variety pool) and hands sprites to
    /// <see cref="StampedePhase5aSetup"/>. Never overwrites the source files; the placeholders it replaces
    /// still live in Sprites/Placeholder for any pair that's missing.
    /// </summary>
    public static class StampedeUIArt
    {
        private const string UIDir = "Assets/_Project/Sprites/UI";

        // Anchored (pivot bottom-centre) so the art sits on the ground the way the old pole+flag prefabs did.
        private const string CheckpointIdleFile = "CheckpointFlag.png";
        private const string CheckpointActiveFile = "CheckpointFlag_pass.png";
        private const string GoalFile = "GoalFlag.png"; // checkered finish flag (was LevelComplete.png, which is now the "Level Complete!" lettering)
        private const float CheckpointPixelsPerUnit = 143f;  // 500px tall -> ~3.5 units, matching the old checkpoint pole+flag height
        private const float GoalPixelsPerUnit = 100f;         // 500px tall -> 5 units, matching the old goal pole height (taller, more decorative)

        private const string ChamberBackdropFile = "BarrierChamberWall.png";
        private const float ChamberPixelsPerUnit = 200f;     // arbitrary; LevelBuilder rescales it to fit each chamber exactly

        // Centred icons, scaled per-file so every crop reads at roughly the same on-screen size regardless
        // of its source resolution.
        private const float CropTargetWorldSize = 0.6f;   // well under the 1.5-unit characters/robots
        // The smiling corn kernel only: the cob was camouflaged against the cobs on the backdrop corn stalks as an
        // everyday pickup, so it is kept for the (rarer, off-path) secret crops. The other crop art (maize pellet, carrot, cabbage, cherry, grain sack, loaf, apple, sunflower pellet) stays in Sprites/UI unused.
        private static readonly string[] NormalCropFiles = { "CornKernel.png" };
        // Per-marker art override (CropSpawnPoint.visualOverride): the bonus coin on a stone tower top.
        private const string CoinFile = "Collectable Coin.png";
        private static readonly string[] SecretCropFiles = { "CornCob.png" };   // the golden cob marks a gated secret
        // Frozen Tundra's everyday crop in place of the corn kernel (LevelAssets.cropSprite).
        private const string TundraCropFile = "FrozenTundra/BlueBerry.png";
        // ... and Watermill Village's (the golden acorn).
        private const string WatermillCropFile = "WatermillVillage/GoldenAcorn.png";
        // ... and Sky Islands' (the cloud plum).
        private const string SkyCropFile = "SkyIsland/FloatingPlum.png";
        // ... and Sunken City's: pearls, and the clam holding one for the secret-cluster crops.
        private const string SunkenCropFile = "SunkenCity/pearl.png";
        private const string SunkenSecretCropFile = "SunkenCity/Clam.png";
        // ... and the Mothership's: glowing containment pods (energy cells); no art came for a crop, so the pod stands in.
        private const string MothershipCropFile = "MotherShip/ContainmentPod.png";

        // Crops drawn bigger than CropTargetWorldSize. Their pivot is lowered so every crop's bottom edge sits at
        // the same height as a standard one, keeping the big icons clear of the grass too.
        private static readonly Dictionary<string, float> CropSizeOverrides = new()
        {
            { "CornKernel.png", 0.9f },          // mockup size: reads clearly on the stone blocks
            { CoinFile, 0.9f },
            { "CornCob.png", 1.2f },
            { RarePelletFile, 1.3f },            // the rare pellet: bigger than any crop, so it reads as special
            { TundraCropFile, 1.6f },            // the berry fills ~56% of its 500px frame: ~0.9 visible, like the kernel
            { WatermillCropFile, 1f },
            { SkyCropFile, 1.1f },
            { SunkenCropFile, 0.8f },            // a round pearl reads big at the kernel's size
            { SunkenSecretCropFile, 1.5f },      // the clam fills ~3/4 of its frame: ~1.15 visible, like the cob
            { MothershipCropFile, 1.1f },        // the pod fills ~85% of its frame height: ~0.95 visible               // the plum sits on a cloud: drawn a little bigger so the fruit reads           // fills its frame; a touch taller than the kernel, as it is a tall shape
        };
        // The rare pellet (RarePelletPickup): a glowing crystal apple at the end of each gated secret.
        private const string RarePelletFile = "RarePellets_apple.png";

        // Framed Character Select cards (name baked into the art).
        private const float CardPixelsPerUnit = 100f; // UI-only; the Image sizes it
        private static readonly Dictionary<CharacterType, string> CharacterCardFiles = new()
        {
            { CharacterType.Cluck, "Cluck_Chicken.png" },
            { CharacterType.Bessie, "Bessie_Cow.png" },
            { CharacterType.Percy, "Percy_Pig.png" },
            { CharacterType.Woolly, "Wooly_Sheep.png" },
            { CharacterType.Ducky, "Ducky_Duck.png" },
            { CharacterType.Horace, "Horace_Horse.png" },
            { CharacterType.Gerald, "Gerald_Turkey.png" },
            { CharacterType.Billy, "Billy_Goat.png" },
        };

        // ---- Menu art ----------------------------------------------------------------------------------------
        // UI-only sprites (an Image sizes them; pixels-per-unit is irrelevant). Signs/boards live in Sprites/UI; the
        // per-world World Select card (WS_ prefix, name centred) and Level Select backdrop (name along the top) live
        // in Sprites/Environment. Filenames don't all match the WorldType names (SkyIsland, Mothership).
        private const string EnvironmentDir = "Assets/_Project/Sprites/Environment";
        // Level Select tiles (per the Level Select mockup): padlock plaque when locked, the question-mark plaque for
        // the next level to play, the Cluck board with 1-3 gold stars once completed (the 1-3 star boards double as the Results art), the boss shield. Btn_back.png is the
        // round Level Select back button. World Select: WorldUnlocked.png doubles as its header banner (each card is
        // itself the tap target). SelectLevelSign.png is imported but not used: no mockup has it.
        // Landing screen (per the landing mockup): the FF_StampedePoster.png poster (logo, sign and Cluck painted in)
        // with the Btn_play / Exit / Btn_settings buttons on top. Btn_home.png is World Select's
        // top-left button back to the landing screen; Environment/Canvas.png is World Select's backdrop.
        private const string SelectLevelSignFile = "SelectLevelSign.png";
        private const string LevelTileLockedFile = "LevelTile_Locked.png";
        private const string LevelTileNextFile = "LevelTile_question.png";
        private const string BackButtonFile = "Btn_back.png";
        private const string BossShieldFile = "Boss_Shield.png";
        private const string PlayButtonFile = "Btn_play.png";
        private const string HomeButtonFile = "Btn_home.png";
        // Gameplay HUD: Cluck giving a thumbs up is the life icon, Btn_pause.png the round pause button.
        private const string LifeIconFile = "CluckThumbsUp.png";
        private const string PauseButtonFile = "Btn_pause.png";
        private const string WorldSelectBackgroundPath = EnvironmentDir + "/Canvas.png";
        // Level Complete / Level Failed (per their mockups): full backdrops with the titles painted in, the greyed star
        // cut from LevelComplete_Canvas.png for unearned stars, and Btn_quit.png as the X back to Level Select.
        // The screens use *_Wide.png copies that setup writes with ResultsScreen.ArtPad pixels added to each side
        // (see BuildWideResultsArt), so the full artwork fits a wide phone without bars; edit the originals.
        private const string LevelCompleteSourcePath = EnvironmentDir + "/LevelComplete_Canvas.png";
        private const string LevelFailedSourcePath = EnvironmentDir + "/LevelFailed_Canvas.png";
        private const string LevelCompleteBackgroundPath = EnvironmentDir + "/LevelComplete_Canvas_Wide.png";
        private const string LevelFailedBackgroundPath = EnvironmentDir + "/LevelFailed_Canvas_Wide.png";
        // Frozen Tundra's results backdrops (2026-10-01): FT_Paralax_Far.png (aurora, KlingAI watermark painted out)
        // with the logo, the UI/LevelComplete.png / LevelFailed.png lettering and Meadow's three gold stars composed
        // onto it at Meadow Ruins' positions, so the greyed stars, score and buttons line up. Widened the same way.
        private const string TundraCompleteSourcePath = EnvironmentDir + "/FT_LevelComplete_Canvas.png";
        private const string TundraFailedSourcePath = EnvironmentDir + "/FT_LevelFailed_Canvas.png";
        private const string TundraCompleteBackgroundPath = EnvironmentDir + "/FT_LevelComplete_Canvas_Wide.png";
        private const string TundraFailedBackgroundPath = EnvironmentDir + "/FT_LevelFailed_Canvas_Wide.png";
        // ... and its New Character page backdrop: the same aurora with only the logo (the page adds the lettering).
        private const string TundraNewCharacterSourcePath = EnvironmentDir + "/FT_NewCharacter_Canvas.png";
        private const string TundraNewCharacterBackgroundPath = EnvironmentDir + "/FT_NewCharacter_Canvas_Wide.png";
        // The in-level character swap: CharacterCanvas.png (sunset hills, "Character" painted in) for Meadow Ruins and
        // any world without its own; FT_Character_Canvas.png = FT_Paralax_Far.png scaled to 1280x720 (as the other
        // Tundra canvases) with the UI/Character.png lettering composed where CharacterCanvas has it (x 377-937,
        // y 72-200). Widened the same way.
        private const string CharacterSwapSourcePath = EnvironmentDir + "/CharacterCanvas.png";
        private const string CharacterSwapBackgroundPath = EnvironmentDir + "/CharacterCanvas_Wide.png";
        private const string TundraCharacterSwapSourcePath = EnvironmentDir + "/FT_Character_Canvas.png";
        private const string TundraCharacterSwapBackgroundPath = EnvironmentDir + "/FT_Character_Canvas_Wide.png";
        // Watermill Village's menu canvases (2026-10-03): MenuCanvas.png (the mill and river) scaled to 1280x720 with
        // the logo, the lettering and Meadow's three gold stars composed at the Tundra canvases' positions. Widened the
        // same way.
        private const string WatermillCompleteSourcePath = EnvironmentDir + "/WV_LevelComplete_Canvas.png";
        private const string WatermillFailedSourcePath = EnvironmentDir + "/WV_LevelFailed_Canvas.png";
        private const string WatermillNewCharacterSourcePath = EnvironmentDir + "/WV_NewCharacter_Canvas.png";
        private const string WatermillCharacterSourcePath = EnvironmentDir + "/WV_Character_Canvas.png";
        // Sky Islands' menu canvases (2026-10-04): SkyIslands.png (sky over a sea of clouds) scaled to 1280x720 with the
        // logo, lettering and gold stars composed exactly where the Watermill canvases have them (found by template
        // matching; a script, not in the repo). Widened the same way.
        private const string SkyCompleteSourcePath = EnvironmentDir + "/SI_LevelComplete_Canvas.png";
        private const string SkyFailedSourcePath = EnvironmentDir + "/SI_LevelFailed_Canvas.png";
        private const string SkyNewCharacterSourcePath = EnvironmentDir + "/SI_NewCharacter_Canvas.png";
        private const string SkyCharacterSourcePath = EnvironmentDir + "/SI_Character_Canvas.png";
        // Sunken City's (2026-10-04): SunkenCity.png made the same way.
        private const string SunkenCompleteSourcePath = EnvironmentDir + "/SC_LevelComplete_Canvas.png";
        private const string SunkenFailedSourcePath = EnvironmentDir + "/SC_LevelFailed_Canvas.png";
        private const string SunkenNewCharacterSourcePath = EnvironmentDir + "/SC_NewCharacter_Canvas.png";
        private const string SunkenCharacterSourcePath = EnvironmentDir + "/SC_Character_Canvas.png";
        // The Robot Mothership's (2026-10-04): RobotMothership.png made the same way.
        private const string MothershipCompleteSourcePath = EnvironmentDir + "/RM_LevelComplete_Canvas.png";
        private const string MothershipFailedSourcePath = EnvironmentDir + "/RM_LevelFailed_Canvas.png";
        private const string MothershipNewCharacterSourcePath = EnvironmentDir + "/RM_NewCharacter_Canvas.png";
        private const string MothershipCharacterSourcePath = EnvironmentDir + "/RM_Character_Canvas.png";
        private static string Wide(string source) => source.Replace(".png", "_Wide.png");
        // Pause (per its mockup): the same kind of backdrop, "Pause" and the logo painted in, widened the same way.
        private const string PauseSourcePath = EnvironmentDir + "/Pause_Canvas.png";
        private const string PauseBackgroundPath = EnvironmentDir + "/Pause_Canvas_Wide.png";
        // New Character page (per its mockup): the same kind of backdrop, widened the same way.
        // The New Character page (2026-09-29 mockup): NewCharacter_Canvas.png with its painted wooden sign replaced by
        // the matching sky from Pause_Canvas.png (same painting), so the NewCharacterTitle lettering sits on clean sky.
        private const string NewCharacterSourcePath = EnvironmentDir + "/NewCharacter_Plain.png";
        private const string NewCharacterBackgroundPath = EnvironmentDir + "/NewCharacter_Plain_Wide.png";
        private const string NewCharacterTitleFile = "NewCharacterTitle.png";
        // The painted Farm Fury logo's rows (from the top of the 720px art): the left margin extends the sky here
        // instead of mirroring, so no piece of the logo is copied into it.
        private const int LogoRowTop = 20, LogoRowBottom = 160;
        private const string StarEmptyFile = "LevelComplete_StarEmpty.png";
        private const string QuitButtonFile = "Btn_quit.png";
        private const string ExitButtonFile = "Exit.png";
        private const string SettingsButtonFile = "Btn_settings.png";
        // The landing poster, shown full-screen as it is (logo, sign, Cluck and robots painted in). It is its own
        // scene, so no backdrop is drawn behind it any more (was: a zoomed-out, edge-faded copy over Canvas.png).
        // It is painted wide (2560x1164, ~2.2:1) with everything important in the central 16:9, so it covers phones
        // up to ~2.2:1 without being enlarged; narrower screens only lose plain field/sky off the sides.
        private const string LandingPosterFile = "FF_StampedePoster.png";
        private static readonly string[] StarBoardFiles = { "LevelWin_1.png", "LevelComplete_2.png", "LevelComplete_3.png" };
        private const string NewCharacterSignFile = "NewCharacter.png";
        private const string WorldUnlockedSignFile = "WorldUnlocked.png";
        private static readonly Dictionary<WorldType, string> WorldArtNames = new()
        {
            { WorldType.MeadowRuins, "MeadowRuins" },
            { WorldType.FrozenTundra, "FrozenTundra" },
            { WorldType.WatermillVillage, "WatermillVillage" },
            { WorldType.SkyIslands, "SkyIsland" },
            { WorldType.SunkenCity, "SunkenCity" },
            { WorldType.RobotMothership, "Mothership" },
            { WorldType.DustbowlCanyon, "DustbowlCanyon" },
            { WorldType.HarvestFairground, "HarvestFairground" },
            { WorldType.CropFactory, "CropFactory" },
        };
        // World Select card files whose name differs from the Level Select backdrop's (WS_<name>.png).
        private static readonly Dictionary<WorldType, string> WorldCardNames = new()
        {
            { WorldType.HarvestFairground, "Fairground" },
        };
        private static string CardName(WorldType world) =>
            WorldCardNames.TryGetValue(world, out string card) ? card : WorldArtNames[world];
        // World Select carousel badges (UI/Badge_<name>.png: wooden shield, name baked in, 500x500). A world whose
        // badge file is missing shows its WS_ card in the carousel instead.
        private static readonly Dictionary<WorldType, string> WorldBadgeFiles = new()
        {
            { WorldType.MeadowRuins, "Badge_Meadowruins.png" },
            { WorldType.FrozenTundra, "Badge_FrozenTundra.png" },
            { WorldType.WatermillVillage, "Badge_WatermillVillage.png" },
            { WorldType.SkyIslands, "Badge_SkyWorld.png" },
            { WorldType.SunkenCity, "Badge_SunkenCity.png" },
            { WorldType.RobotMothership, "Badge_Mothership.png" },
            { WorldType.DustbowlCanyon, "Badge_DustbowlCanyon.png" },
            { WorldType.HarvestFairground, "Badge_HarvestFairground.png" },
            { WorldType.CropFactory, "Badge_CropFactory.png" },
        };

        // ---- World scale -------------------------------------------------------------------------------------
        // Every world prop (obstacles, barrels, bales, buildings, trees, crops-in-the-field, fences) is sized from ONE
        // table of real-world heights, so nothing is out of proportion with anything else: a barrel can never
        // out-grow a tree. The sprite's pixels-per-unit is worked out from the height of its opaque pixels, so
        // transparent padding in the source doesn't skew it, and replacement art keeps the same world size.
        // Only the playable actors (characters/robots, 1.5 units, cartoon-proportioned), pickups and 1-unit stone
        // blocks sit outside the table. At 0.65 units a metre the tallest props (~8.5) still fit under the camera's
        // ~13 units of sky above the floor.
        public const float UnitsPerMetre = 0.65f;

        // Ground obstacles (LevelBuilder.PlaceObstacles). Pivot y = where the art meets the ground (the rock has
        // transparent padding below it, the bale's straw skirt reaches almost to the bottom).
        private static readonly (string file, float pivotY, float metres)[] ObstacleFiles =
        {
            ("Rock.png", 0.15f, 3.2f),      // a boulder: ~2.1 units, its 2.1x1.95 collider
            // Round bale on its side, 1.5 units tall like a barrel. The opaque art includes a loose straw skirt and
            // wisps: the bale body is ~77% of that height (320 of 418px), so 3 m of art gives a 1.5-unit body. Pivot
            // y = the body's base (the skirt below it tucks into the ground or the bale underneath).
            ("Haybail.png", 0.16f, 3.0f),
        };

        /// <summary>Imports every file this class uses, with settings matched to how it's used. Idempotent; missing files warn and are skipped.</summary>
        public static void ImportUIArt()
        {
            BuildWideResultsArt(LevelCompleteSourcePath, LevelCompleteBackgroundPath);
            BuildWideResultsArt(LevelFailedSourcePath, LevelFailedBackgroundPath);
            BuildWideResultsArt(TundraCompleteSourcePath, TundraCompleteBackgroundPath);
            BuildWideResultsArt(TundraFailedSourcePath, TundraFailedBackgroundPath);
            BuildWideResultsArt(TundraNewCharacterSourcePath, TundraNewCharacterBackgroundPath);
            BuildWideResultsArt(PauseSourcePath, PauseBackgroundPath);
            BuildWideResultsArt(NewCharacterSourcePath, NewCharacterBackgroundPath);
            BuildWideResultsArt(CharacterSwapSourcePath, CharacterSwapBackgroundPath);
            BuildWideResultsArt(TundraCharacterSwapSourcePath, TundraCharacterSwapBackgroundPath);
            foreach (string source in new[] { WatermillCompleteSourcePath, WatermillFailedSourcePath, WatermillNewCharacterSourcePath, WatermillCharacterSourcePath,
                         SkyCompleteSourcePath, SkyFailedSourcePath, SkyNewCharacterSourcePath, SkyCharacterSourcePath,
                         SunkenCompleteSourcePath, SunkenFailedSourcePath, SunkenNewCharacterSourcePath, SunkenCharacterSourcePath,
                         MothershipCompleteSourcePath, MothershipFailedSourcePath, MothershipNewCharacterSourcePath, MothershipCharacterSourcePath })
            {
                BuildWideResultsArt(source, Wide(source));
            }
            ImportAnchored(CheckpointIdleFile, CheckpointPixelsPerUnit);
            ImportAnchored(CheckpointActiveFile, CheckpointPixelsPerUnit);
            ImportAnchored(GoalFile, GoalPixelsPerUnit);
            ImportCentered(ChamberBackdropFile, ChamberPixelsPerUnit);
            foreach (string file in NormalCropFiles) { ImportCropIcon(file); }
            foreach (string file in SecretCropFiles) { ImportCropIcon(file); }
            ImportCropIcon(CoinFile);
            ImportCropIcon(RarePelletFile);
            ImportCropIcon(TundraCropFile);
            ImportCropIcon(WatermillCropFile);
            ImportCropIcon(SkyCropFile);
            ImportCropIcon(SunkenCropFile);
            ImportCropIcon(SunkenSecretCropFile);
            ImportCropIcon(MothershipCropFile);
            foreach (string file in CharacterCardFiles.Values) { ImportCentered(file, CardPixelsPerUnit); }
            foreach (var (file, pivotY, metres) in ObstacleFiles) { ImportToScale(file, pivotY, metres); }

            ImportCentered(LedgeFile, 253f); // 253px tall -> 1 unit, one tile
            ImportCentered(StoneBlockFile, 292f); // 292px tall -> 1 unit, one tile
            ImportCentered(BridgeFile, 100f);     // LevelBuilder sizes it to the span (Bridge())

            foreach (var (file, pivotY, metres) in SceneryFiles) { ImportToScale(file, pivotY, metres); }
            ImportToScale(BarrelFile, BarrelPivotY, BarrelMetres);
            ImportTundraArt();
            ImportWatermillArt();
            ImportSkyArt();
            ImportSunkenArt();
            ImportMothershipArt();

            foreach (string file in MenuSpriteFiles()) { ImportMenuSprite(file); }
        }

        // Every menu sprite as a project path.
        private static IEnumerable<string> MenuSpriteFiles()
        {
            yield return $"{UIDir}/{SelectLevelSignFile}";
            yield return $"{UIDir}/{BossShieldFile}";
            yield return $"{UIDir}/{LevelTileLockedFile}";
            yield return $"{UIDir}/{LevelTileNextFile}";
            yield return $"{UIDir}/{BackButtonFile}";
            yield return $"{UIDir}/{PlayButtonFile}";
            yield return $"{UIDir}/{HomeButtonFile}";
            yield return $"{UIDir}/{LifeIconFile}";
            yield return $"{UIDir}/{PauseButtonFile}";
            yield return $"{UIDir}/{SwapButtonFile}";
            yield return WorldSelectBackgroundPath;
            yield return LevelCompleteBackgroundPath;
            yield return LevelFailedBackgroundPath;
            yield return TundraCompleteBackgroundPath;
            yield return TundraFailedBackgroundPath;
            yield return TundraNewCharacterBackgroundPath;
            yield return PauseBackgroundPath;
            yield return NewCharacterBackgroundPath;
            yield return CharacterSwapBackgroundPath;
            yield return TundraCharacterSwapBackgroundPath;
            yield return Wide(WatermillCompleteSourcePath);
            yield return Wide(WatermillFailedSourcePath);
            yield return Wide(WatermillNewCharacterSourcePath);
            yield return Wide(WatermillCharacterSourcePath);
            yield return Wide(SkyCompleteSourcePath);
            yield return Wide(SkyFailedSourcePath);
            yield return Wide(SkyNewCharacterSourcePath);
            yield return Wide(SkyCharacterSourcePath);
            yield return Wide(SunkenCompleteSourcePath);
            yield return Wide(SunkenFailedSourcePath);
            yield return Wide(SunkenNewCharacterSourcePath);
            yield return Wide(SunkenCharacterSourcePath);
            yield return Wide(MothershipCompleteSourcePath);
            yield return Wide(MothershipFailedSourcePath);
            yield return Wide(MothershipNewCharacterSourcePath);
            yield return Wide(MothershipCharacterSourcePath);
            yield return $"{UIDir}/{StarEmptyFile}";
            yield return $"{UIDir}/{QuitButtonFile}";
            yield return $"{UIDir}/{ExitButtonFile}";
            yield return $"{UIDir}/{SettingsButtonFile}";
            yield return $"{UIDir}/{LandingPosterFile}";
            foreach (string file in StarBoardFiles) { yield return $"{UIDir}/{file}"; }
            yield return $"{UIDir}/{NewCharacterSignFile}";
            yield return $"{UIDir}/{NewCharacterTitleFile}";
            yield return $"{UIDir}/{WorldUnlockedSignFile}";
            foreach (string badge in WorldBadgeFiles.Values)
            {
                if (File.Exists($"{UIDir}/{badge}")) { yield return $"{UIDir}/{badge}"; }
            }
            foreach (var (world, name) in WorldArtNames)
            {
                yield return $"{EnvironmentDir}/WS_{CardName(world)}.png";
                yield return $"{EnvironmentDir}/{name}.png";
            }
        }

        private static void ImportMenuSprite(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[UIArt] Missing menu art {path}; that screen keeps its plain look.");
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 100f;
            importer.maxTextureSize = 4096;   // the 2560px-wide landing poster would otherwise be halved to 2048
            SetPivot(importer, new Vector2(0.5f, 0.5f));
            importer.SaveAndReimport();
        }

        public static Sprite LevelTileLocked() => Load(LevelTileLockedFile);
        public static Sprite LevelTileNext() => Load(LevelTileNextFile);
        public static Sprite BackButton() => Load(BackButtonFile);
        public static Sprite BossShield() => Load(BossShieldFile);
        public static Sprite PlayButton() => Load(PlayButtonFile);
        public static Sprite HomeButton() => Load(HomeButtonFile);
        public static Sprite LifeIcon() => Load(LifeIconFile);
        public static Sprite PauseButton() => Load(PauseButtonFile);
        public static Sprite SwapButton() => Load(SwapButtonFile);
        // The HUD's in-level character swap (Arcade's swap art).
        private const string SwapButtonFile = "SwapCharacterIcon.png";
        public static Sprite QuitButton() => Load(QuitButtonFile);
        /// <summary>The corn kernel pickup, reused as the Level Complete score icon.</summary>
        public static Sprite ScoreIcon() => Load(NormalCropFiles[0]);
        public static Sprite LevelCompleteStarEmpty() => Load(StarEmptyFile);
        public static Sprite LevelCompleteBackground() => AssetDatabase.LoadAssetAtPath<Sprite>(LevelCompleteBackgroundPath);
        public static Sprite PauseBackground() => AssetDatabase.LoadAssetAtPath<Sprite>(PauseBackgroundPath);
        public static Sprite NewCharacterBackground() => AssetDatabase.LoadAssetAtPath<Sprite>(NewCharacterBackgroundPath);
        public static Sprite LevelFailedBackground() => AssetDatabase.LoadAssetAtPath<Sprite>(LevelFailedBackgroundPath);
        public static Sprite CharacterSwapBackground() => AssetDatabase.LoadAssetAtPath<Sprite>(CharacterSwapBackgroundPath);
        /// <summary>A world's own character swap backdrop, or null for the shared Meadow Ruins one.</summary>
        public static Sprite WorldCharacterSwapBackground(WorldType world) => world switch
        {
            WorldType.FrozenTundra => AssetDatabase.LoadAssetAtPath<Sprite>(TundraCharacterSwapBackgroundPath),
            WorldType.WatermillVillage => AssetDatabase.LoadAssetAtPath<Sprite>(Wide(WatermillCharacterSourcePath)),
            WorldType.SkyIslands => AssetDatabase.LoadAssetAtPath<Sprite>(Wide(SkyCharacterSourcePath)),
            WorldType.SunkenCity => AssetDatabase.LoadAssetAtPath<Sprite>(Wide(SunkenCharacterSourcePath)),
            WorldType.RobotMothership => AssetDatabase.LoadAssetAtPath<Sprite>(Wide(MothershipCharacterSourcePath)),
            _ => null,
        };
        /// <summary>A world's own New Character page backdrop, or null for the shared sunset one.</summary>
        public static Sprite WorldNewCharacterBackground(WorldType world) => world switch
        {
            WorldType.FrozenTundra => AssetDatabase.LoadAssetAtPath<Sprite>(TundraNewCharacterBackgroundPath),
            WorldType.WatermillVillage => AssetDatabase.LoadAssetAtPath<Sprite>(Wide(WatermillNewCharacterSourcePath)),
            WorldType.SkyIslands => AssetDatabase.LoadAssetAtPath<Sprite>(Wide(SkyNewCharacterSourcePath)),
            WorldType.SunkenCity => AssetDatabase.LoadAssetAtPath<Sprite>(Wide(SunkenNewCharacterSourcePath)),
            WorldType.RobotMothership => AssetDatabase.LoadAssetAtPath<Sprite>(Wide(MothershipNewCharacterSourcePath)),
            _ => null,
        };
        /// <summary>A world's own Level Complete / Level Failed backdrop, or null for the shared Meadow Ruins one.</summary>
        public static Sprite WorldResultsBackground(WorldType world, bool complete) => world switch
        {
            WorldType.FrozenTundra => AssetDatabase.LoadAssetAtPath<Sprite>(complete ? TundraCompleteBackgroundPath : TundraFailedBackgroundPath),
            WorldType.WatermillVillage => AssetDatabase.LoadAssetAtPath<Sprite>(Wide(complete ? WatermillCompleteSourcePath : WatermillFailedSourcePath)),
            WorldType.SkyIslands => AssetDatabase.LoadAssetAtPath<Sprite>(Wide(complete ? SkyCompleteSourcePath : SkyFailedSourcePath)),
            WorldType.SunkenCity => AssetDatabase.LoadAssetAtPath<Sprite>(Wide(complete ? SunkenCompleteSourcePath : SunkenFailedSourcePath)),
            WorldType.RobotMothership => AssetDatabase.LoadAssetAtPath<Sprite>(Wide(complete ? MothershipCompleteSourcePath : MothershipFailedSourcePath)),
            _ => null,
        };
        /// <summary>World Select's backdrop: the sunset farm (Environment/Canvas.png).</summary>
        public static Sprite WorldSelectBackground() => AssetDatabase.LoadAssetAtPath<Sprite>(WorldSelectBackgroundPath);
        public static Sprite ExitButton() => Load(ExitButtonFile);
        public static Sprite SettingsButton() => Load(SettingsButtonFile);
        public static Sprite LandingPoster() => Load(LandingPosterFile);
        public static Sprite LandingBackdrop() => null;   // the poster covers the screen on its own

        // Writes a copy of a results backdrop with ResultsScreen.ArtPad columns added to each side, mirrored out from
        // the edge (the stone walls and hills carry on naturally); across the logo's rows on the left the edge column
        // is repeated instead (plain sky). Rebuilt on every setup run so replaced art is picked up.
        internal static void BuildWideResultsArt(string source, string destination)
        {
            if (!File.Exists(source))
            {
                Debug.LogWarning($"[UIArt] Missing {source}; the results screens keep their plain look.");
                return;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(source));
            int w = texture.width, h = texture.height, pad = ResultsScreen.ArtPad, wide = w + 2 * pad;
            var src = texture.GetPixels32();
            var dst = new Color32[wide * h];
            for (int y = 0; y < h; y++)
            {
                int fromTop = h - 1 - y;   // pixel rows run bottom-up
                bool logoRow = fromTop >= LogoRowTop && fromTop < LogoRowBottom;
                for (int x = 0; x < w; x++) { dst[y * wide + pad + x] = src[y * w + x]; }
                for (int d = 0; d < pad; d++)
                {
                    int mirror = Mathf.Min(d, w - 1);
                    dst[y * wide + pad - 1 - d] = src[y * w + (logoRow ? 0 : mirror)];
                    dst[y * wide + pad + w + d] = src[y * w + (w - 1 - mirror)];
                }
            }

            var result = new Texture2D(wide, h, TextureFormat.RGBA32, false);
            result.SetPixels32(dst);
            File.WriteAllBytes(destination, result.EncodeToPNG());
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(result);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>Results board for 1, 2 or 3 stars.</summary>
        public static Sprite StarBoard(int stars) => Load(StarBoardFiles[Mathf.Clamp(stars, 1, 3) - 1]);
        public static Sprite NewCharacterSign() => Load(NewCharacterSignFile);
        public static Sprite NewCharacterTitle() => Load(NewCharacterTitleFile);
        public static Sprite WorldUnlockedSign() => Load(WorldUnlockedSignFile);

        /// <summary>The world's World Select card art (name centred), or null.</summary>
        public static Sprite WorldSelectCard(WorldType world) =>
            WorldArtNames.ContainsKey(world) ? AssetDatabase.LoadAssetAtPath<Sprite>($"{EnvironmentDir}/WS_{CardName(world)}.png") : null;

        /// <summary>The world's World Select badge (shield, name baked in), or null when it has none yet.</summary>
        public static Sprite WorldSelectBadge(WorldType world) =>
            WorldBadgeFiles.TryGetValue(world, out string file) ? Load(file) : null;

        /// <summary>The world's Level Select backdrop (name along the top), or null.</summary>
        public static Sprite LevelSelectBackground(WorldType world) =>
            WorldArtNames.TryGetValue(world, out string name) ? AssetDatabase.LoadAssetAtPath<Sprite>($"{EnvironmentDir}/{name}.png") : null;

        /// <summary>World height in units of a piece of art whose opaque pixels span the given real-world height.</summary>
        public static float WorldHeight(float metres) => metres * UnitsPerMetre;

        // Imports a feet-pivoted prop so its opaque pixels are exactly WorldHeight(metres) tall.
        private static void ImportToScale(string file, float pivotY, float metres)
        {
            var importer = BeginImport(file);
            if (importer == null) { return; }
            int opaque = OpaqueHeightPixels($"{UIDir}/{file}");
            importer.spritePixelsPerUnit = Mathf.Max(opaque, 1) / WorldHeight(metres);
            SetPivot(importer, new Vector2(0.5f, pivotY));
            FinishImport(importer);
        }

        /// <summary>
        /// Imports a world robot frame (Sprites/UI/...) so its visible art is 'height' units tall whatever its frame
        /// size, feet on its lowest visible row, and returns it (null if the file is missing). Used for the robots a
        /// world swaps in through LevelAssets.robotArt.
        /// </summary>
        public static Sprite ImportActor(string file, float height)
        {
            var importer = BeginImport(file);
            if (importer == null) { return null; }
            string path = $"{UIDir}/{file}";
            importer.spritePixelsPerUnit = Mathf.Max(OpaqueHeightPixels(path), 1) / height;
            SetPivot(importer, new Vector2(0.5f, BottomPaddingFraction(path)));
            FinishImport(importer);
            return Load(file);
        }

        // Rows between the first and last with any pixel over ~8% alpha, read from the source PNG.
        private static int OpaqueHeightPixels(string path)
        {
            var texture = new Texture2D(2, 2);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path))) { return 0; }
                Color32[] pixels = texture.GetPixels32();
                int w = texture.width, h = texture.height, top = -1, bottom = -1;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (pixels[y * w + x].a > 20)
                        {
                            if (bottom < 0) { bottom = y; }
                            top = y;
                            break;
                        }
                    }
                }
                return bottom < 0 ? h : top - bottom + 1;
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        /// <summary>
        /// Fraction of the PNG's height that is empty below its lowest visible row (3+ pixels over ~16% alpha). A feet pivot at
        /// this height stands the art on the ground instead of on its transparent margin.
        /// </summary>
        public static float BottomPaddingFraction(string path)
        {
            var texture = new Texture2D(2, 2);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path))) { return 0f; }
                Color32[] pixels = texture.GetPixels32();
                int w = texture.width, h = texture.height;
                // A row counts once it has a few clearly visible pixels, so faint stray specks under the feet
                // (Horace_right has some ~30px below its hooves) don't hold the pivot down.
                for (int y = 0; y < h; y++)
                {
                    int visible = 0;
                    for (int x = 0; x < w; x++)
                    {
                        if (pixels[y * w + x].a > 40 && ++visible >= 3) { return (float)y / h; }
                    }
                }
                return 0f;
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        /// <summary>
        /// The walkable top of an obstacle's art and the width it stands on, in units from its pivot: the median top
        /// edge across the middle half of its opaque columns (so a branch, a mushroom or a tilted log end doesn't set
        /// the height), and 80% of its opaque width. Read from the source PNG; (0, 0) when it can't be read.
        /// </summary>
        public static Vector2 ObstacleTopAndWidth(Sprite sprite)
        {
            string path = sprite != null ? AssetDatabase.GetAssetPath(sprite) : null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) { return Vector2.zero; }
            var texture = new Texture2D(2, 2);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path))) { return Vector2.zero; }
                Color32[] pixels = texture.GetPixels32();
                int w = texture.width, h = texture.height;
                var tops = new int[w];
                int left = -1, right = -1;
                for (int x = 0; x < w; x++)
                {
                    tops[x] = -1;
                    for (int y = h - 1; y >= 0; y--)
                    {
                        if (pixels[y * w + x].a > 20) { tops[x] = y; break; }
                    }
                    if (tops[x] >= 0) { if (left < 0) { left = x; } right = x; }
                }
                if (left < 0) { return Vector2.zero; }

                int span = right - left + 1;
                var middle = new List<int>();
                for (int x = left + span / 4; x <= right - span / 4; x++) { if (tops[x] >= 0) { middle.Add(tops[x]); } }
                middle.Sort();
                float topFraction = (middle[middle.Count / 2] + 1f) / h;
                float pivotFraction = sprite.pivot.y / sprite.rect.height;
                Vector2 size = sprite.bounds.size;
                return new Vector2((topFraction - pivotFraction) * size.y, 0.8f * span / w * size.x);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        // Barrel for LevelBuilder.BarrelPyramid: a 2.3 m tun, 1.5 units - the same height as a hay bale - on a
        // matching collider. Pivot y = the barrel's base (34px of padding).
        private const string BarrelFile = "Wooden Barrel.png";
        private const float BarrelMetres = 2.3f;
        private const float BarrelPivotY = 0.068f;

        public static Sprite Barrel() => Load(BarrelFile);
        public static Sprite Haybale() => Load("Haybail.png");

        // Background scenery (no collider): the automatic barn/windmill of LevelBuilder.PlaceScenery and the
        // hand-authored farm backdrop of PlaceFarmBackdrop. Pivot y = where the art meets the ground (the barn has
        // debris/straw drawn in front of its base, which the grass covers). Real-world heights -> units at 0.65/m.
        private static readonly (string file, float pivotY, float metres)[] SceneryFiles =
        {
            ("DamagedBarn.png", 0.12f, 9f),     // ~5.9 units
            ("Windmill.png", 0.02f, 12f),       // ~7.8 (sail tips)
            ("FarmSilo.png", 0.02f, 12f),       // ~7.8
            ("OakTree.png", 0.08f, 13f),        // ~8.5; pivot where the roots meet the ground
            ("GnarledTree.png", 0.01f, 8f),     // ~5.2
            ("WaterWheel.png", 0.12f, 5f),      // ~3.3
            ("CornStalk_1.png", 0.01f, 2.6f),   // ~1.7; pivot at the cut stem (per-stalk random scale on top)
            ("CornStalk_2.png", 0.01f, 2.6f),
            ("WoodenCart.png", 0.13f, 3.4f),    // ~2.2, bigger than a barrel; pivot at the wheel bottoms
            ("WoodenFence.png", 0.22f, 1.2f),   // ~0.8 tall (sections tile edge to edge)
            ("WildFlowers.png", 0.08f, 0.9f),   // ~0.6
            ("Plane.png", 0.5f, 2.2f),          // not to scale: a distant plane, drawn ~1.4 units across the sky
            ("SecretSign.png", 0.01f, 4.6f),    // ~3 units: the secret-passage signpost (apple bursting through), feet at the post's foot
        };
        // ---- Frozen Tundra (World 2) props, in Sprites/UI/FrozenTundra, to the same world scale. Each stands in
        // for a Meadow Ruins farm prop in the shared scenery layout (LevelBuilder.StandardFarm), see TundraBackdrop.
        private const string TundraDir = "FrozenTundra/";
        private static readonly (string file, float pivotY, float metres)[] TundraFiles =
        {
            ("Igloo.png", 0.03f, 4.5f),            // ~2.9 units (the barn's place)
            ("FrozenWaterfall.png", 0.05f, 9f),    // ~5.9 (the windmill's)
            ("IceSpire.png", 0.01f, 10f),          // ~6.5 (the silo's)
            ("IceTree.png", 0.02f, 8f),            // ~5.2 (the oak's)
            ("IceCrystals.png", 0.04f, 4f),        // ~2.6 (the gnarled tree's)
            ("BuriedRuin.png", 0.03f, 5f),         // ~3.3 (the water wheel's)
            ("Snowdrift.png", 0.02f, 2.5f),        // ~1.6 (the cart's, in front)
            ("IceSign.png", 0.05f, 2.6f),          // ~1.7: the THIN ICE warning at the start of every ice stretch
            ("IceBoulder.png", 0.05f, 3.2f),       // ~2.1: the random obstacle (as the rock), on its 2.1x1.95 collider
            ("IceBarrel.png", 0.1f, 2.6f),         // ~1.7 with its snow puff: the barrel-pyramid barrel
        };
        // One-cell tiles and slabs: pixels-per-unit = the opaque height, so each is one unit tall.
        private static readonly (string file, float pixelsPerUnit)[] TundraBlocks =
        {
            ("Ice_block.png", 110f),    // StoneBlocks() squares and the Ice tilemap (IceFlat)
            ("Snow_block.png", 93f),    // Floating() / SecretLedge() slabs
            ("WoodBlock.png", 122f),    // Bessie's breakable floor
        };

        // ---- Watermill Village (World 3) props, in Sprites/UI/WatermillVillage/Props: the 2048px originals in the
        // folder above had a white studio background, cut out (with its soft shadow) and trimmed to 768px by a script
        // on 2026-10-03; edit the Props copies. Same world scale; each stands in for a Meadow farm prop (WatermillBackdrop).
        private const string WatermillDir = "WatermillVillage/Props/";
        private static readonly (string file, float pivotY, float metres)[] WatermillFiles =
        {
            ("Cottage.png", 0.02f, 5.5f),          // ~3.6 units (the barn's place)
            ("WaterWheel.png", 0.03f, 6.5f),       // ~4.2 (the windmill's)
            ("OldWell.png", 0.03f, 3.6f),          // ~2.3 (the water wheel's)
            ("StoneArch.png", 0.04f, 7f),          // ~4.6 (the silo's)
            ("WeepingWillow.png", 0.02f, 9f),      // ~5.9 (the oak's)
            ("RiverReed.png", 0.04f, 2.4f),        // ~1.6 (the gnarled tree's)
            ("FlowerBasket.png", 0.02f, 1.3f),     // ~0.8 (the cart's, in front)
            ("WoodenCrate.png", 0.05f, 3.2f),      // ~2.1: random obstacle on the 2.1x1.95 collider
            ("MossyLog.png", 0.06f, 2.6f),         // ~1.7: random obstacle
            ("Wooddock.png", 0.03f, 3.4f),         // ~2.2: the mooring post at each end of a River()
        };

        private static void ImportWatermillArt()
        {
            foreach (var (file, pivotY, metres) in WatermillFiles) { ImportToScale(WatermillDir + file, pivotY, metres); }
            ImportRiverArt();
        }

        public static Sprite Watermill(string file) => Load(WatermillDir + file);

        // ---- Rivers (LevelBuilder.River / Boat / Piranha). Props/River.png is River.png cropped to its waves, the see-
        // through foam band filled white (as the art reads on white) and deep water added below (a script, 2026-10-04;
        // edit River.png and re-make it, or edit the Props copy). 4 units per repeat, top-pivoted, full-rect so the
        // renderer can tile it sideways.
        private const string RiverFile = WatermillDir + "River.png";
        private const string BoatFile = "WatermillVillage/FishingBoat.png";       // the user's transparent 500px boat
        private const string PiranhaFile = "WatermillVillage/WaterRobot.png";     // the river robot, faces right
        private const float RiverRepeatWidth = 4f, PiranhaWidth = 1.7f;

        private static void ImportRiverArt()
        {
            var river = BeginImport(RiverFile);
            if (river != null)
            {
                river.GetSourceTextureWidthAndHeight(out int w, out _);
                river.spritePixelsPerUnit = w / RiverRepeatWidth;
                var settings = new TextureImporterSettings();
                river.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                river.SetTextureSettings(settings);
                river.wrapMode = TextureWrapMode.Repeat;
                river.maxTextureSize = 2048;
                SetPivot(river, new Vector2(0.5f, 1f));
                FinishImport(river);
            }
            ImportCentered(BoatFile, 100f);   // LevelBuilder scales it to each boat's length
            var piranha = BeginImport(PiranhaFile);
            if (piranha != null)
            {
                piranha.GetSourceTextureWidthAndHeight(out int w, out _);
                piranha.spritePixelsPerUnit = w / PiranhaWidth;
                SetPivot(piranha, new Vector2(0.5f, 0.5f));
                FinishImport(piranha);
            }
        }

        public static Sprite River() => Load(RiverFile);

        // ---- Sky Islands (World 4) art, in Sprites/UI/SkyIsland (transparent already). Props to the world scale; each
        // stands in for a Meadow farm prop (SkyBackdrop). The air balloon doubles as scenery (moored on the ground) and
        // as the Balloon() lift, which rescales it to its deck whatever its import size.
        private const string SkyDir = "SkyIsland/";
        private static readonly (string file, float pivotY, float metres)[] SkyFiles =
        {
            ("SkyTemple.png", 0.03f, 5.5f),        // ~3.6 units (the barn's place)
            ("Windvane.png", 0.01f, 6f),           // ~3.9 (the windmill's)
            ("StonePillar.png", 0.02f, 7f),        // ~4.6 (the silo's)
            ("GiantMushroom.png", 0.12f, 8f),      // ~5.2 (the oak's); its floating clod half in the grass
            ("AirBalloon.png", 0.01f, 6f),         // ~3.9 (the water wheel's): moored on the ground
            ("Kite.png", 0.5f, 2.6f),              // not to scale: the sky drifter in the plane's place
            ("HangingLantern.png", 0f, 2.2f),      // ~1.4, hung under every main-path bridge (feet-pivoted at its foot)
        };
        // Centred pieces: (file, pixels per unit).
        private static readonly (string file, float ppu)[] SkyBlocks =
        {
            ("CloudLedge.png", 252f),       // one cloud of CloudTile.png (flat top): every ledge and moving ledge, 1 unit tall
            ("Cloud.png", 356f),            // the puffy cloud: StoneBlocks() squares, 1 unit tall
            ("UpdraftSpiral.png", 100f),    // LevelBuilder stretches it over each updraft column
            ("CloudFill.png", 128f),        // the cloud body under the cloud surface (a generated tileable fill), one cell
            ("AnchorCloud.png", 344f),      // the floating storm cloud (LevelBuilder.StormCloud), ~1.8 wide before its 2.5x
        };

        private static void ImportSkyArt()
        {
            foreach (var (file, pivotY, metres) in SkyFiles) { ImportToScale(SkyDir + file, pivotY, metres); }
            foreach (var (file, ppu) in SkyBlocks) { ImportCentered(SkyDir + file, ppu); }
        }

        public static Sprite Sky(string file) => Load(SkyDir + file);

        // ---- Sunken City (World 5) art, in Sprites/UI/SunkenCity (transparent already, except FloatingRock.png, whose
        // white background a script cut out into FloatingRock_Cut.png). Props to the world scale, pivoted where each
        // stands on the sand; each stands in for a Meadow farm prop (SunkenBackdrop).
        private const string SunkenDir = "SunkenCity/";
        private static readonly (string file, float pivotY, float metres)[] SunkenFiles =
        {
            ("SunkenArchway.png", 0.11f, 5.5f),    // ~3.6 units (the barn's place)
            ("SunkenStatue.png", 0.07f, 5f),       // ~3.3 (the windmill's)
            ("Kelp.png", 0.02f, 6f),               // ~3.9 (the silo's)
            ("CoralFormation.png", 0.1f, 4f),      // ~2.6 (the oak's)
            ("Floral.png", 0.08f, 4.5f),           // ~2.9 (the gnarled tree's, and the reef rows); bigger since 2026-10-09
            ("ShipsWheel.png", 0.15f, 3f),         // ~2.0 (the water wheel's), half sunk in the sand
            ("Anchor.png", 0.09f, 3f),             // ~2.0 (the cart's)
            ("Submarine.png", 0.5f, 3f),           // not to scale: the drifter in the plane's place (and every moving ledge, rescaled)
            ("TreasureChest.png", 0.1f, 2.4f),     // ~1.6: random obstacle (its collider follows the art)
            ("SunkenPillar.png", 0.14f, 2.6f),     // ~1.7: random obstacle, a fallen column
            ("AirVent.png", 0.06f, 3.4f),          // ~2.2: the air vent at every Updraft() (the user's Kling art, 2026-10-09)
        };
        private static readonly (string file, float ppu)[] SunkenBlocks =
        {
            ("FloatingRock_Cut.png", 768f),    // StoneBlocks() squares, 1 unit
            ("JellyFish.png", 222f),           // the jellyfish in the Drone's place, ~1.8 tall
            ("SeabedFill.png", 256f),          // the rock body under the seabed surface, one cell
            ("Bubble.png", 128f),              // a generated bubble, 1 unit (BubbleStream sizes it)
        };

        private static void ImportSunkenArt()
        {
            foreach (var (file, pivotY, metres) in SunkenFiles) { ImportToScale(SunkenDir + file, pivotY, metres); }
            foreach (var (file, ppu) in SunkenBlocks) { ImportCentered(SunkenDir + file, ppu); }
            // Any Fish*.png dropped in swims in the background (SunkenFishFacingRight says which way each faces).
            foreach (string fish in SunkenFishFiles()) { ImportToScale(SunkenDir + fish, 0.5f, 1.4f); }
        }

        private static IEnumerable<string> SunkenFishFiles() =>
            Directory.Exists($"{UIDir}/{SunkenDir}")
                ? Directory.GetFiles($"{UIDir}/{SunkenDir}", "Fish*.png").Select(Path.GetFileName).OrderBy(f => f)
                : Enumerable.Empty<string>();

        // The fish art faces left unless listed here (Fish_1-4 and 6 face left, Fish_5 right).
        private static readonly HashSet<string> SunkenFishFacingRight = new() { "Fish_5.png" };

        /// <summary>Sunken City's background fish (any Sprites/UI/SunkenCity/Fish*.png) and whether each faces right.</summary>
        public static (Sprite art, bool facesRight)[] SunkenFish() => SunkenFishFiles()
            .Select(f => (art: Sunken(f), facesRight: SunkenFishFacingRight.Contains(f)))
            .Where(f => f.art != null).ToArray();

        /// <summary>The air vent at every Sunken City Updraft().</summary>
        public static Sprite SunkenVent() => Sunken("AirVent.png");

        public static Sprite Sunken(string file) => Load(SunkenDir + file);

        // ---- Robot Mothership (World 6) art, in Sprites/UI/MotherShip (transparent already). Props to the world scale;
        // each stands in for a Meadow farm prop (MothershipBackdrop). The carrier (Ship.png) is both the background
        // drifter and every moving ledge (rescaled by LevelBuilder).
        private const string MothershipDir = "MotherShip/";
        private static readonly (string file, float pivotY, float metres)[] MothershipFiles =
        {
            ("ControlTower.png", 0.08f, 6f),       // ~3.9 units (the barn's place); its spike in the deck
            ("SateliteDish.png", 0.07f, 5f),       // ~3.3 (the windmill's)
            ("WarningBeacon.png", 0.03f, 6.5f),    // ~4.2 (the silo's)
            ("EnergyShield.png", 0.06f, 4f),       // ~2.6 (the oak's)
            ("Ship.png", 0.5f, 3.4f),              // not to scale: the drifter in the plane's place (and every moving ledge)
            ("LaserEmitter.png", 0.2f, 2.4f),      // ~1.6: random obstacle (its collider follows the art)
            ("PlasmaConduit.png", 0.2f, 2.4f),     // ~1.6: random obstacle
            ("RocketBooster.png", 0.15f, 3f),      // ~2.0 (the cart's), a spare engine on the deck
        };
        private static readonly (string file, float ppu)[] MothershipBlocks =
        {
            ("SteelGirder.png", 402f),        // every ledge, 1 unit (square slabs)
            ("CircuitPanel.png", 343f),       // StoneBlocks() squares, 1 unit
            ("Drone.png", 246f),              // the purple quadcopter in the Drone's place, ~2 wide
            ("GravitySling.png", 100f),       // stood at the foot of every gravity lift (LevelBuilder sizes it)
            ("GravityBeam.png", 100f),        // the lift's beam (a generated glow), stretched over the column
            ("HullFill.png", 256f),           // the hull body under the deck, one cell
        };

        private static void ImportMothershipArt()
        {
            foreach (var (file, pivotY, metres) in MothershipFiles) { ImportToScale(MothershipDir + file, pivotY, metres); }
            foreach (var (file, ppu) in MothershipBlocks) { ImportCentered(MothershipDir + file, ppu); }
        }

        public static Sprite Mothership(string file) => Load(MothershipDir + file);

        /// <summary>The Mothership's scenery in the farm-prop roles; the rest are left empty.</summary>
        internal static FarmBackdropArt MothershipBackdrop() => new()
        {
            barn = Mothership("ControlTower.png"),
            windmill = Mothership("SateliteDish.png"),
            silo = Mothership("WarningBeacon.png"),
            oak = Mothership("EnergyShield.png"),
            cart = Mothership("RocketBooster.png"),
            plane = Mothership("Ship.png"),
        };

        /// <summary>Sunken City's scenery in the farm-prop roles.</summary>
        internal static FarmBackdropArt SunkenBackdrop() => new()
        {
            barn = Sunken("SunkenArchway.png"),
            windmill = Sunken("SunkenStatue.png"),
            silo = Sunken("Kelp.png"),
            oak = Sunken("CoralFormation.png"),
            gnarledTree = Sunken("Floral.png"),
            waterWheel = Sunken("ShipsWheel.png"),
            cart = Sunken("Anchor.png"),
            plane = Sunken("Submarine.png"),
        };

        /// <summary>Sky Islands' scenery in the farm-prop roles; the gnarled tree and cart have no Sky art and are left out.</summary>
        internal static FarmBackdropArt SkyBackdrop() => new()
        {
            barn = Sky("SkyTemple.png"),
            windmill = Sky("Windvane.png"),
            silo = Sky("StonePillar.png"),
            oak = Sky("GiantMushroom.png"),
            waterWheel = Sky("AirBalloon.png"),
            plane = Sky("Kite.png"),
        };
        public static Sprite Boat() => Load(BoatFile);
        public static Sprite Piranha() => Load(PiranhaFile);

        /// <summary>
        /// Watermill Village's props in the farm-prop roles. Meadow's corn fields and fences are Meadow-only (removed
        /// 2026-10-03; the wildflowers only grow at fence ends, so they go too); the biplane stays.
        /// </summary>
        internal static FarmBackdropArt WatermillBackdrop(FarmBackdropArt meadow) => new()
        {
            plane = meadow.plane,
            barn = Watermill("Cottage.png") ?? meadow.barn,
            windmill = Watermill("WaterWheel.png") ?? meadow.windmill,
            waterWheel = Watermill("OldWell.png") ?? meadow.waterWheel,
            silo = Watermill("StoneArch.png") ?? meadow.silo,
            oak = Watermill("WeepingWillow.png") ?? meadow.oak,
            gnarledTree = Watermill("RiverReed.png") ?? meadow.gnarledTree,
            cart = Watermill("FlowerBasket.png") ?? meadow.cart,
        };

        /// <summary>Imports the Frozen Tundra prop and block art (called by ImportUIArt).</summary>
        private static void ImportTundraArt()
        {
            foreach (var (file, pivotY, metres) in TundraFiles) { ImportToScale(TundraDir + file, pivotY, metres); }
            foreach (var (file, ppu) in TundraBlocks) { ImportCentered(TundraDir + file, ppu); }
        }

        public static Sprite Tundra(string file) => Load(TundraDir + file);

        /// <summary>Frozen Tundra's scenery in the farm-prop roles; the farm-only pieces (corn, fence, flowers, biplane) stay empty.</summary>
        internal static FarmBackdropArt TundraBackdrop() => new()
        {
            barn = Tundra("Igloo.png"),
            windmill = Tundra("FrozenWaterfall.png"),
            silo = Tundra("IceSpire.png"),
            oak = Tundra("IceTree.png"),
            gnarledTree = Tundra("IceCrystals.png"),
            waterWheel = Tundra("BuriedRuin.png"),
            cart = Tundra("Snowdrift.png"),
        };

        /// <summary>The secret-passage signpost (LevelBuilder.SecretPassage): marks both ends of a passage.</summary>
        public static Sprite SecretSign() => Load("SecretSign.png");
        public static Sprite Barn() => Load("DamagedBarn.png");
        public static Sprite Windmill() => Load("Windmill.png");

        /// <summary>The farm backdrop art that imported successfully (missing pieces are null / left out).</summary>
        internal static FarmBackdropArt FarmBackdrop() => new()
        {
            cornStalks = Load(new[] { "CornStalk_1.png", "CornStalk_2.png" }),
            barn = Load("DamagedBarn.png"),
            windmill = Load("Windmill.png"),
            silo = Load("FarmSilo.png"),
            gnarledTree = Load("GnarledTree.png"),
            oak = Load("OakTree.png"),
            waterWheel = Load("WaterWheel.png"),
            cart = Load("WoodenCart.png"),
            fence = Load("WoodenFence.png"),
            wildflowers = Load("WildFlowers.png"),
            plane = Load("Plane.png"),
        };

        // Slab art for SecretLedge platforms (LevelBuilder.AddStoneSlabs rescales each slab to its slot).
        private const string LedgeFile = "Stone_Block.png";
        public static Sprite LedgeStone() => Load(LedgeFile);

        // Square block art for LevelBuilder.StoneBlocks() bonus platforms, one block per tile.
        private const string StoneBlockFile = "Stone_Square.png";
        // Rope bridge (211x46): the deck along the lower third, rope rails above; repeated across each Bridge() span.
        private const string BridgeFile = "Bridge.png";
        public static Sprite Bridge() => Load(BridgeFile);
        public static Sprite StoneBlock() => Load(StoneBlockFile);
        public static Sprite Coin() => Load(CoinFile);

        /// <summary>The obstacle art that imported successfully.</summary>
        public static Sprite[] Obstacles()
        {
            var list = new List<Sprite>();
            foreach (var (file, _, _) in ObstacleFiles)
            {
                var sprite = Load(file);
                if (sprite != null) { list.Add(sprite); }
            }
            return list.ToArray();
        }

        public static Sprite CheckpointIdle() => Load(CheckpointIdleFile);
        public static Sprite CheckpointActive() => Load(CheckpointActiveFile);
        public static Sprite GoalFlag() => Load(GoalFile);
        public static Sprite ChamberBackdrop() => Load(ChamberBackdropFile);
        public static Sprite[] NormalCrops() => Load(NormalCropFiles);
        public static Sprite[] SecretCrops() => Load(SecretCropFiles);
        public static Sprite TundraCrop() => Load(TundraCropFile);
        public static Sprite WatermillCrop() => Load(WatermillCropFile);
        public static Sprite SkyCrop() => Load(SkyCropFile);
        public static Sprite SunkenCrop() => Load(SunkenCropFile);
        public static Sprite SunkenSecretCrop() => Load(SunkenSecretCropFile);
        public static Sprite MothershipCrop() => Load(MothershipCropFile);
        public static Sprite RarePellet() => Load(RarePelletFile);

        /// <summary>The character's Character Select card, or null if it has none.</summary>
        public static Sprite CharacterCard(CharacterType type) =>
            CharacterCardFiles.TryGetValue(type, out string file) ? Load(file) : null;

        private static Sprite Load(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UIDir}/{file}");

        private static Sprite[] Load(string[] files)
        {
            var list = new System.Collections.Generic.List<Sprite>();
            foreach (string file in files)
            {
                var sprite = Load(file);
                if (sprite != null)
                {
                    list.Add(sprite);
                }
            }
            return list.ToArray();
        }

        private static void ImportAnchored(string file, float pixelsPerUnit)
        {
            var importer = BeginImport(file);
            if (importer == null) { return; }
            importer.spritePixelsPerUnit = pixelsPerUnit;
            SetPivot(importer, new Vector2(0.5f, 0f));
            FinishImport(importer);
        }

        private static void ImportCentered(string file, float pixelsPerUnit)
        {
            var importer = BeginImport(file);
            if (importer == null) { return; }
            importer.spritePixelsPerUnit = pixelsPerUnit;
            SetPivot(importer, new Vector2(0.5f, 0.5f));
            FinishImport(importer);
        }

        private static void ImportCropIcon(string file)
        {
            string path = $"{UIDir}/{file}";
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[UIArt] Missing art file {path}; skipped.");
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.GetSourceTextureWidthAndHeight(out int w, out int h);
            int maxDimension = Mathf.Max(w, h);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            float size = CropSizeOverrides.TryGetValue(file, out float overrideSize) ? overrideSize : CropTargetWorldSize;
            importer.spritePixelsPerUnit = Mathf.Max(maxDimension, 1) / size;
            // Oversized icons keep their bottom edge CropTargetWorldSize/2 below the placement point, like a standard
            // icon; standard-size ones stay centred (pivot 0.5).
            float worldHeight = size * h / Mathf.Max(maxDimension, 1);
            // Measured from the art's lowest visible row, so an icon with an empty margin below it doesn't float.
            float pivotY = Mathf.Min(0.5f, BottomPaddingFraction(path) + CropTargetWorldSize * 0.5f / Mathf.Max(worldHeight, 0.01f));
            SetPivot(importer, new Vector2(0.5f, pivotY));
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static TextureImporter BeginImport(string file)
        {
            string path = $"{UIDir}/{file}";
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[UIArt] Missing art file {path}; skipped.");
                return null;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            return importer;
        }

        private static void FinishImport(TextureImporter importer) => importer.SaveAndReimport();

        /// <summary>
        /// Sets a single sprite's pivot. Assigning <c>importer.spritePivot</c> alone is not enough: Unity ignores it
        /// unless the sprite alignment is Custom (the default is Center), which is what left the bottom-anchored
        /// flags centred on - and half buried in - the ground.
        /// </summary>
        public static void SetPivot(TextureImporter importer, Vector2 pivot)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
        }
    }
}
