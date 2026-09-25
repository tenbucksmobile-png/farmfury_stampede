using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FarmFuryStampede.Characters;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Robots;
using FarmFuryStampede.UI;
using FarmFuryStampede.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Re-runnable Phase 5a bootstrap (supersedes the Phase 3/4 setup). Regenerates placeholder sprites/tiles, the
    /// pooled gameplay prefabs (Player, Crop, the seven robots, Checkpoint, Goal, Breakable Wall, Cloud
    /// Platform, Horseshoe), the CharacterData / RobotData / WorldData / LevelData assets, the eleven Meadow
    /// Ruins level prefabs plus the boss level, and wires Game.unity (LevelLoader, pool, camera, character
    /// select, GameFlow UI, debug panel, DataManager lists). Safe to re-run: existing assets are updated in
    /// place (GUIDs preserved) and the scene objects it owns (the "LevelSystem" root) are rebuilt.
    /// </summary>
    public static class StampedePhase5aSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string SpritesDir = "Assets/_Project/Sprites/Placeholder";
        private const string PrefabsDir = "Assets/_Project/Prefabs";
        private const string RobotPrefabsDir = "Assets/_Project/Prefabs/Robots";
        private const string LevelPrefabsDir = "Assets/_Project/Prefabs/Levels";
        private const string RobotDataDir = "Assets/_Project/ScriptableObjects/Robots";
        private const string LevelDataDir = "Assets/_Project/ScriptableObjects/Levels";
        private const string CharacterDataDir = "Assets/_Project/ScriptableObjects/Characters";
        private const string WorldDataPath = "Assets/_Project/ScriptableObjects/Worlds/WorldData_MeadowRuins.asset";
        private const string RootName = "LevelSystem";

        private const string SquarePath = SpritesDir + "/Square.png";
        private const string GroundTilePath = SpritesDir + "/GroundTile.asset";
        private const string PlatformTilePath = SpritesDir + "/PlatformTile.asset";
        private const string BreakableTilePath = SpritesDir + "/BreakableTile.asset";
        private const string GroundSurfaceTileName = "GroundSurfaceTile";
        private const string WaterTilePath = SpritesDir + "/WaterTile.asset";
        private const string InvisibleTilePath = SpritesDir + "/InvisibleTile.asset";
        private const string OldClusterPrefabPath = PrefabsDir + "/Cluck.prefab";
        private const string PlayerPrefabPath = PrefabsDir + "/Player.prefab";
        private const string CropPrefabPath = PrefabsDir + "/Crop.prefab";
        private const string CheckpointPrefabPath = PrefabsDir + "/Checkpoint.prefab";
        private const string GoalPrefabPath = PrefabsDir + "/Goal.prefab";
        private const string BreakableWallPrefabPath = PrefabsDir + "/BreakableWall.prefab";
        private const string CloudPrefabPath = PrefabsDir + "/CloudPlatform.prefab";
        private const string HorseshoePrefabPath = PrefabsDir + "/Horseshoe.prefab";
        private const string HarvesterPrefabPath = RobotPrefabsDir + "/Harvester.prefab";
        private const string DronePrefabPath = RobotPrefabsDir + "/Drone.prefab";
        private const string ScoutPrefabPath = RobotPrefabsDir + "/Scout.prefab";
        private const string ChaserPrefabPath = RobotPrefabsDir + "/Chaser.prefab";
        private const string CommanderPrefabPath = RobotPrefabsDir + "/Commander.prefab";
        private const string BarrierPrefabPath = RobotPrefabsDir + "/BarrierUnit.prefab";
        private const string WorldDataDir = "Assets/_Project/ScriptableObjects/Worlds";

        // Real character art lives here (never overwritten by setup; the generated placeholders live in SpritesDir).
        private const string ArtDir = "Assets/_Project/Sprites/Characters";
        private const string RobotArtDir = "Assets/_Project/Sprites/Robots";
        // Characters and robots share one on-screen size: every 500px art frame (and every 16px placeholder) is
        // ActorVisualHeight units tall. Visual only - the colliders stay 0.95 (player) / ~1.1 (robots), which the
        // level layouts are validated against. Ground-standing actors are pivoted at the feet and their Visual
        // child sits on the collider's bottom edge, so the bigger art grows upwards instead of sinking into the floor.
        private const float ActorVisualHeight = 1.5f;
        private const float ArtFramePixels = 500f;
        private const float ArtPixelsPerUnit = ArtFramePixels / ActorVisualHeight;
        // Jump frames are spread-wing poses: the canvas is full but the body is much narrower, so at the shared
        // scale the character looks like it shrinks mid-air. Frames named "*jump*" are drawn this much bigger.
        private const float JumpFrameScale = 1.25f;

        private const int ObstaclesPerLevel = 2;   // rock / haybale, placed at random (seeded) spots
        private static readonly Vector2 FeetPivot = new Vector2(0.5f, 0f);
        private static readonly Vector2 CentrePivot = new Vector2(0.5f, 0.5f);
        private const float PlayerFeetY = -0.475f;     // bottom of the player's 0.95-tall collider
        private const float RobotFeetY = -0.5f;        // ground robots spawn 0.5 above the ground (LevelBuilder)
        private const float BossFeetY = -1.0f;         // the Commander spawns 1.0 above the ground
        private const string DroneArtFile = "Drone.png"; // flies, so stays centre-pivoted

        // Identical for every character by design (GDD/Phase 4 Section 1).
        private const float SharedMoveSpeed = 8f;
        private const float SharedJumpHeight = 3.5f;
        private const int AbilityUsesPerLevel = 3;

        private struct CharacterSpec
        {
            public CharacterType type;
            public string displayName;
            public AbilityType ability;
            public string description;
            public int unlockLevels;
            public Color ui;
        }

        private static readonly CharacterSpec[] Characters =
        {
            new CharacterSpec { type = CharacterType.Cluck, displayName = "Cluck the Chicken", ability = AbilityType.FlutterJump, unlockLevels = 0, ui = new Color(1f, 0.9f, 0.4f),
                description = "Flutter Jump: a brief mid-air flutter granting a second jump (once per airborne period)." },
            new CharacterSpec { type = CharacterType.Bessie, displayName = "Bessie the Cow", ability = AbilityType.GroundPound, unlockLevels = 0, ui = new Color(0.95f, 0.95f, 0.95f),
                description = "Ground Pound: a forced fast-fall; on landing breaks Breakable Floor tiles nearby and defeats robots beneath her." },
            new CharacterSpec { type = CharacterType.Percy, displayName = "Percy the Pig", ability = AbilityType.RollDash, unlockLevels = 5, ui = new Color(1f, 0.65f, 0.75f),
                description = "Roll Dash: curls up and dashes forward, defeating robots on contact and crossing small gaps." },
            new CharacterSpec { type = CharacterType.Woolly, displayName = "Woolly the Sheep", ability = AbilityType.CloudStep, unlockLevels = 10, ui = new Color(0.98f, 0.95f, 0.85f),
                description = "Cloud Step: spawns a temporary wool platform underfoot mid-air, once per jump." },
            new CharacterSpec { type = CharacterType.Ducky, displayName = "Ducky the Duck", ability = AbilityType.SkipDash, unlockLevels = 15, ui = new Color(0.4f, 0.8f, 0.75f),
                description = "Skip Dash: skims forward across the surface; the only character unaffected by Water tiles." },
            new CharacterSpec { type = CharacterType.Horace, displayName = "Horace the Horse", ability = AbilityType.RearVaultThrow, unlockLevels = 20, ui = new Color(0.65f, 0.45f, 0.25f),
                description = "Rear Vault & Horseshoe Throw: on the ground a much taller jump; in the air throws a horseshoe that defeats one robot at range." },
            new CharacterSpec { type = CharacterType.Gerald, displayName = "Gerald the Turkey", ability = AbilityType.PuffGlide, unlockLevels = 30, ui = new Color(0.55f, 0.35f, 0.25f),
                description = "Puff Glide: inflates and slow-falls for a few seconds, crossing chasms; defeats robots on contact while inflated." },
            new CharacterSpec { type = CharacterType.Billy, displayName = "Billy the Goat", ability = AbilityType.ChargeBreak, unlockLevels = 40, ui = new Color(0.78f, 0.78f, 0.82f),
                description = "Charge Break: charges forward, breaking Breakable Walls that nothing else can." },
        };

        [MenuItem("Farm Fury Stampede/Phase 5a/Run Setup")]
        public static void RunSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Phase 5a Setup", "Exit Play mode before running setup.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            int groundLayer = EnsureLayer("Ground");
            int playerLayer = EnsureLayer("Player");

            EnsureFolder(SpritesDir);
            EnsureFolder(RobotPrefabsDir);
            EnsureFolder(LevelPrefabsDir);
            EnsureFolder(RobotDataDir);
            EnsureFolder(LevelDataDir);
            EnsureFolder(CharacterDataDir);

            // ---- Stage 1: generate assets on disk.
            Sprite square = CreateSprite("Square", SquarePixel);
            Sprite cropSprite = CreateSprite("Crop", CropPixel);
            Sprite harvesterSprite = CreateActorSprite("Harvester", HarvesterPixel);
            Sprite droneSprite = CreateActorSprite("Drone", DronePixel, CentrePivot);
            Sprite barrierSprite = CreateSprite("Barrier", BarrierPixel);
            Sprite scoutSprite = CreateActorSprite("Scout", ScoutPixel);
            Sprite chaserSprite = CreateActorSprite("Chaser", ChaserPixel);
            Sprite commanderSprite = CreateActorSprite("Commander", CommanderPixel);
            Sprite barrierUnitSprite = CreateSprite("BarrierUnit", BarrierUnitPixel);
            Sprite horseshoeSprite = CreateSprite("Horseshoe", HorseshoePixel);
            CreateActorSprite("Char_Cluck", Painter(new Color32(255, 225, 90, 255),
                F(13, 6, 15, 8, 255, 130, 20), F(5, 13, 8, 14, 220, 40, 40), F(10, 10, 11, 11, 20, 20, 20)));
            CreateActorSprite("Char_Bessie", Painter(new Color32(245, 245, 245, 255),
                F(3, 3, 5, 5, 30, 30, 30), F(7, 6, 10, 8, 30, 30, 30), F(3, 13, 3, 14, 200, 170, 120), F(11, 13, 11, 14, 200, 170, 120),
                F(13, 3, 15, 7, 245, 160, 170), F(10, 10, 11, 11, 20, 20, 20)));
            CreateActorSprite("Char_Percy", Painter(new Color32(245, 160, 190, 255),
                F(3, 13, 4, 14, 230, 120, 160), F(9, 13, 10, 14, 230, 120, 160), F(13, 5, 15, 8, 225, 110, 150),
                F(14, 6, 14, 6, 120, 40, 70), F(14, 8, 14, 8, 120, 40, 70), F(10, 10, 11, 11, 20, 20, 20)));
            CreateActorSprite("Char_Woolly", Painter(new Color32(250, 245, 225, 255),
                F(0, 3, 0, 9, 250, 245, 225), F(14, 0, 14, 0, 250, 245, 225), F(3, 13, 10, 13, 250, 245, 225), F(9, 4, 13, 11, 70, 55, 55),
                F(10, 9, 11, 10, 250, 250, 250), F(11, 9, 11, 9, 20, 20, 20)));
            CreateActorSprite("Char_Ducky", Painter(new Color32(90, 190, 170, 255),
                F(13, 4, 15, 7, 255, 150, 30), F(5, 13, 8, 13, 60, 150, 130), F(3, 3, 8, 6, 70, 165, 145), F(10, 10, 11, 11, 20, 20, 20)));
            CreateActorSprite("Char_Horace", Painter(new Color32(150, 100, 55, 255),
                F(2, 8, 4, 14, 70, 45, 25), F(13, 3, 15, 9, 205, 165, 110), F(8, 13, 9, 14, 120, 80, 40), F(10, 10, 11, 11, 20, 20, 20)));
            CreateActorSprite("Char_Gerald", Painter(new Color32(110, 70, 50, 255),
                F(0, 8, 2, 14, 70, 45, 35), F(13, 6, 13, 7, 220, 40, 40), F(14, 8, 15, 9, 240, 200, 60), F(10, 10, 11, 11, 20, 20, 20)));
            CreateActorSprite("Char_Billy", Painter(new Color32(200, 200, 205, 255),
                F(4, 13, 5, 15, 240, 230, 200), F(8, 13, 9, 15, 240, 230, 200), F(12, 1, 13, 4, 90, 90, 100), F(10, 10, 11, 11, 20, 20, 20)));

            ImportCharacterArt();
            ImportRobotArt();
            StampedeEnvironmentArt.ImportBackgroundArt();
            StampedeUIArt.ImportUIArt();

            CreateTile("GroundTile", StampedeProceduralTiles.CreateGroundTileSprite(), Color.white);
            CreateTile("PlatformTile", StampedeProceduralTiles.CreatePlatformTileSprite(), Color.white);
            Sprite[] floorArt = StampedeEnvironmentArt.FloorTileArt();
            int surfaceTileCount = CreateGroundSurfaceTiles(floorArt);
            if (floorArt.Length > 0)
            {
                // Same grass-topped block as the surface around it, tinted a dry, cracked-looking orange-brown so
                // Bessie's Ground Pound spots still read as different from the solid ground next to them.
                CreateTile("BreakableTile", floorArt[0], BreakableFloorTint, FloorTileTransform(floorArt[0]));
            }
            else
            {
                CreateTile("BreakableTile", barrierSprite, new Color(0.85f, 0.6f, 0.4f));
            }
            CreateTile("WaterTile", square, new Color(0.25f, 0.5f, 0.95f, 0.6f));
            CreateTile("InvisibleTile", null, Color.white);   // collision only; art is drawn separately (stone ledges)

            BuildCloudPrefab(square, groundLayer);
            BuildHorseshoePrefab(horseshoeSprite);
            BuildBreakableWallPrefab(barrierSprite, groundLayer);
            BuildPlayerPrefab(groundLayer, playerLayer);
            BuildCropPrefab(cropSprite);
            BuildRobotPrefab<HarvesterRobot>("Harvester", RobotType.Harvester, harvesterSprite, HarvesterPrefabPath,
                art: RobotArt("Robot_Harvest_right.png", "Robot_Harvest_left.png", "Robot_defeated.png"));
            BuildRobotPrefab<DroneRobot>("Drone", RobotType.Drone, droneSprite, DronePrefabPath,
                art: RobotArt("Drone.png", null, null));
            BuildRobotPrefab<ScoutRobot>("Scout", RobotType.Scout, scoutSprite, ScoutPrefabPath,
                art: RobotArt("ScoutRobot_right.png", "ScoutRobot_left.png", "Robot_defeated.png"));
            BuildRobotPrefab<ChaserRobot>("Chaser", RobotType.Chaser, chaserSprite, ChaserPrefabPath,
                art: RobotArt("DriftRobot_right.png", "DriftRobot_left.png", "Robot_defeated.png"));
            BuildRobotPrefab<CommanderBoss>("Commander", RobotType.Commander, commanderSprite, CommanderPrefabPath, bossSize: true,
                art: RobotArt("Commander_Alert.png", null, "Commander_Defeated.png"));
            BuildBarrierUnitPrefab(barrierUnitSprite, groundLayer);
            BuildCheckpointPrefab(square);
            BuildGoalPrefab(square);

            AssetDatabase.DeleteAsset(OldClusterPrefabPath); // replaced by the generic Player prefab
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ---- Stage 2: open the scene FIRST. OpenScene unloads unused assets, which invalidates any
            // object reference held from before it, so everything below loads from disk by path.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var assets = new LevelAssets
            {
                groundTile = AssetDatabase.LoadAssetAtPath<Tile>(GroundTilePath),
                platformTile = AssetDatabase.LoadAssetAtPath<Tile>(PlatformTilePath),
                breakableTile = AssetDatabase.LoadAssetAtPath<Tile>(BreakableTilePath),
                groundSurfaceTiles = Enumerable.Range(0, surfaceTileCount)
                    .Select(i => AssetDatabase.LoadAssetAtPath<Tile>($"{SpritesDir}/{GroundSurfaceTileName}_{i}.asset"))
                    .ToArray(),
                waterTile = AssetDatabase.LoadAssetAtPath<Tile>(WaterTilePath),
                groundLayer = groundLayer,
                chamberBackdrop = StampedeUIArt.ChamberBackdrop(),
                obstacleSprites = StampedeUIArt.Obstacles(),
                barrelSprite = StampedeUIArt.Barrel(),
                haybaleSprite = StampedeUIArt.Haybale(),
                barnSprite = StampedeUIArt.Barn(),
                windmillSprite = StampedeUIArt.Windmill(),
                farmArt = StampedeUIArt.FarmBackdrop(),
                stoneBlockSprite = StampedeUIArt.StoneBlock(),
                coinSprite = StampedeUIArt.Coin(),
                ledgeSprite = StampedeUIArt.LedgeStone(),
                invisibleTile = AssetDatabase.LoadAssetAtPath<Tile>(InvisibleTilePath),
                obstaclesPerLevel = ObstaclesPerLevel
            };

            var harvesterRobot = AssetDatabase.LoadAssetAtPath<GameObject>(HarvesterPrefabPath);
            var droneRobot = AssetDatabase.LoadAssetAtPath<GameObject>(DronePrefabPath);
            var scoutRobot = AssetDatabase.LoadAssetAtPath<GameObject>(ScoutPrefabPath);
            var chaserRobot = AssetDatabase.LoadAssetAtPath<GameObject>(ChaserPrefabPath);
            var commanderRobot = AssetDatabase.LoadAssetAtPath<GameObject>(CommanderPrefabPath);
            var barrierRobot = AssetDatabase.LoadAssetAtPath<GameObject>(BarrierPrefabPath);
            if (assets.groundTile == null || assets.platformTile == null || assets.breakableTile == null || assets.waterTile == null
                || harvesterRobot == null || droneRobot == null || scoutRobot == null || chaserRobot == null
                || commanderRobot == null || barrierRobot == null)
            {
                Debug.LogError("[Phase5aSetup] A generated asset failed to load; aborting.");
                return;
            }

            var characterDatas = new List<CharacterData>();
            foreach (var spec in Characters)
            {
                characterDatas.Add(CreateCharacterData(spec));
            }

            var robotHarvester = CreateRobotData("Harvester", RobotType.Harvester, 2f,
                "Walks a fixed patrol path and turns at walls and ledge edges; stomped like a classic ground enemy.", harvesterRobot);
            var robotDrone = CreateRobotData("Drone", RobotType.Drone, 2.5f,
                "Hovers at a fixed height and sweeps back and forth; must be jumped onto from above.", droneRobot);
            var robotScout = CreateRobotData("Scout", RobotType.Scout, 3.8f,
                "Fast patrol; turns to face a player who slips behind it.", scoutRobot);
            var robotChaser = CreateRobotData("Chaser", RobotType.Chaser, 6.4f,
                "Pursues the player at a fixed speed a little under the player base speed.", chaserRobot, RobotBehaviour.Chase);
            var robotBarrier = CreateRobotData("BarrierUnit", RobotType.BarrierUnit, 0f,
                "Stationary; blocks the path outright. Only Billy Charge Break clears it.", barrierRobot, RobotBehaviour.Stationary, AbilityType.ChargeBreak);
            var robotCommander = CreateRobotData("Commander", RobotType.Commander, 2.5f,
                "World boss: patrols the arena, takes several stomps with a stagger window between them, calls reinforcements.", commanderRobot, RobotBehaviour.Guard);
            var robots = new[] { robotHarvester, robotDrone, robotScout, robotChaser, robotBarrier, robotCommander };

            var worldDatas = CreateWorldDatas();

            // ---- Stage 3: build and validate the Meadow Ruins level prefabs (11 + boss) and their LevelData.
            var errors = new List<string>();
            var levelDatas = new List<LevelData>();
            foreach (var builder in MeadowRuinsLevels.CreateAll())
            {
                string prefabPath = $"{LevelPrefabsDir}/{builder.Id}.prefab";
                GameObject prefab = builder.BuildPrefab(assets, prefabPath, errors);
                if (prefab == null)
                {
                    continue;
                }

                levelDatas.Add(CreateLevelData(builder, prefab));
                Debug.Log($"[Phase5aSetup] {builder.Id} '{builder.Title}': {builder.CropCount} crops " +
                          $"({builder.SecretCropCount} secret), {builder.RobotCount} robots, {builder.CheckpointCount} checkpoints" +
                          $"{(builder.HasGate ? $", gated secret for {builder.GatePrimary}" : "")}.");
            }

            if (errors.Count > 0)
            {
                foreach (string error in errors)
                {
                    Debug.LogError($"[Phase5aSetup] Level validation: {error}");
                }
                Debug.LogError("[Phase5aSetup] Level validation failed; the scene was NOT modified.");
                return;
            }

            SetWorldLevelCount(levelDatas.Count);
            AssetDatabase.SaveAssets();

            // ---- Stage 4: wire the scene.
            WireScene(scene, characterDatas, levelDatas, robots, worldDatas);
        }

        // ---------------------------------------------------------------- layers / folders

        private static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0)
            {
                return existing;
            }

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 6; i < layers.arraySize; i++)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[Phase5aSetup] Created layer '{layerName}' at index {i}.");
                    return i;
                }
            }

            throw new InvalidOperationException($"No free user layer slot for '{layerName}'.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ---------------------------------------------------------------- placeholder art

        private static Color32 Clear => new Color32(0, 0, 0, 0);

        private struct Feature
        {
            public int x0, y0, x1, y1;
            public Color32 color;
        }

        private static Feature F(int x0, int y0, int x1, int y1, byte r, byte g, byte b)
        {
            return new Feature { x0 = x0, y0 = y0, x1 = x1, y1 = y1, color = new Color32(r, g, b, 255) };
        }

        // Character silhouette: a rounded 13x13 body facing right, then coloured feature rectangles on top
        // (later features override earlier ones). Each character gets a distinct colour + feature set.
        private static Func<int, int, int, Color32> Painter(Color32 body, params Feature[] features)
        {
            return (x, y, size) =>
            {
                Color32 result = Clear;
                bool inBody = x >= 1 && x <= 13 && y >= 0 && y <= 12 && !((x == 1 || x == 13) && (y == 0 || y == 12));
                if (inBody)
                {
                    result = body;
                }
                foreach (var f in features)
                {
                    if (x >= f.x0 && x <= f.x1 && y >= f.y0 && y <= f.y1)
                    {
                        result = f.color;
                    }
                }
                return result;
            };
        }

        private static Color32 SquarePixel(int x, int y, int size)
        {
            bool edge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
            byte v = (byte)(edge ? 215 : 255);
            return new Color32(v, v, v, 255);
        }

        private static Color32 CropPixel(int x, int y, int size)
        {
            float c = (size - 1) * 0.5f;
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
            if (d > 6.5f)
            {
                return Clear;
            }
            return d > 5.2f ? new Color32(200, 110, 20, 255) : new Color32(255, 170, 40, 255);
        }

        // Robot-barrier styling: dark plate with orange hazard stripes.
        private static Color32 BarrierPixel(int x, int y, int size)
        {
            bool stripe = ((x + y) / 3) % 2 == 0;
            bool edge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
            if (edge) { return new Color32(50, 50, 55, 255); }
            return stripe ? new Color32(235, 140, 30, 255) : new Color32(85, 85, 95, 255);
        }

        private static Color32 HorseshoePixel(int x, int y, int size)
        {
            float dx = x - 7.5f;
            float dy = y - 7.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            bool ring = d >= 4.2f && d <= 6.6f;
            bool opening = dy < -1f && Mathf.Abs(dx) < 2.2f;
            return ring && !opening ? new Color32(185, 185, 195, 255) : Clear;
        }

        // Faces right: grey body on dark treads, red eye.
        private static Color32 HarvesterPixel(int x, int y, int size)
        {
            if (y <= 2 && x >= 1 && x <= 14) { return new Color32(55, 55, 65, 255); }
            if (x >= 11 && x <= 12 && y >= 8 && y <= 9) { return new Color32(230, 40, 40, 255); }
            if (x >= 2 && x <= 13 && y >= 3 && y <= 12) { return new Color32(125, 135, 148, 255); }
            if (x == 7 && y >= 13 && y <= 15) { return new Color32(90, 90, 100, 255); }
            return Clear;
        }

        // Faces right: round hull with a rotor bar and a red eye.
        private static Color32 DronePixel(int x, int y, int size)
        {
            if (y == 14 && x >= 1 && x <= 14) { return new Color32(80, 80, 90, 255); }
            if (x == 7 && y >= 12 && y <= 13) { return new Color32(80, 80, 90, 255); }
            float dx = x - 7.5f;
            float dy = y - 6f;
            if (dx * dx + dy * dy <= 30f)
            {
                if (x >= 10 && x <= 11 && y >= 6 && y <= 7) { return new Color32(230, 40, 40, 255); }
                return new Color32(150, 160, 175, 255);
            }
            return Clear;
        }

        // Faces right: light body on treads, cyan visor - reads as "fast".
        private static Color32 ScoutPixel(int x, int y, int size)
        {
            if (y <= 1 && x >= 1 && x <= 14) { return new Color32(50, 60, 70, 255); }
            if (x >= 10 && x <= 13 && y >= 8 && y <= 9) { return new Color32(80, 230, 240, 255); }
            if (x >= 3 && x <= 13 && y >= 2 && y <= 10) { return new Color32(190, 205, 215, 255); }
            if (x >= 1 && x <= 3 && y >= 4 && y <= 8) { return new Color32(120, 135, 150, 255); }
            return Clear;
        }

        // Faces right: dark red with spikes and a yellow eye - reads as "hunter".
        private static Color32 ChaserPixel(int x, int y, int size)
        {
            if (y == 13 && (x == 3 || x == 6 || x == 9 || x == 12)) { return new Color32(60, 20, 20, 255); }
            if (x >= 10 && x <= 12 && y >= 8 && y <= 9) { return new Color32(255, 230, 60, 255); }
            if (x >= 2 && x <= 13 && y >= 1 && y <= 12) { return new Color32(160, 45, 45, 255); }
            if (y == 0 && x >= 3 && x <= 12) { return new Color32(50, 30, 30, 255); }
            return Clear;
        }

        // Faces right: heavy steel body with a gold crown and a red eye (drawn 2 units tall by the prefab).
        private static Color32 CommanderPixel(int x, int y, int size)
        {
            if (y >= 13 && (x == 3 || x == 6 || x == 9 || x == 12)) { return new Color32(250, 200, 50, 255); }
            if (y == 12 && x >= 3 && x <= 12) { return new Color32(250, 200, 50, 255); }
            if (x >= 10 && x <= 12 && y >= 7 && y <= 8) { return new Color32(240, 40, 40, 255); }
            if (x >= 1 && x <= 14 && y >= 2 && y <= 11) { return new Color32(105, 115, 130, 255); }
            if (x >= 2 && x <= 13 && y <= 1) { return new Color32(50, 55, 65, 255); }
            return Clear;
        }

        // Stationary barrier: steel plate with orange hazard stripes and a red sensor eye.
        private static Color32 BarrierUnitPixel(int x, int y, int size)
        {
            if (x >= 6 && x <= 9 && y >= 6 && y <= 9) { return new Color32(235, 45, 45, 255); }
            return BarrierPixel(x, y, size);
        }

        // Character/robot placeholder: same on-screen size and pivot as the real art it stands in for.
        private static Sprite CreateActorSprite(string spriteName, Func<int, int, int, Color32> pixel, Vector2? pivot = null) =>
            CreateSprite(spriteName, pixel, ActorVisualHeight, pivot ?? FeetPivot);

        private static Sprite CreateSprite(string spriteName, Func<int, int, int, Color32> pixel,
            float worldSize = 1f, Vector2? pivot = null)
        {
            const int size = 16;
            string path = $"{SpritesDir}/{spriteName}.png";

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = pixel(x, y, size);
                }
            }
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size / worldSize; // 1 world unit unless it's an actor placeholder
            StampedeUIArt.SetPivot(importer, pivot ?? CentrePivot);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static readonly Color BreakableFloorTint = new Color(1f, 0.7f, 0.5f);

        // Stretches a floor variant so its dirt body fills the cell exactly; the grass overhangs the cell above.
        private static Matrix4x4 FloorTileTransform(Sprite variant) =>
            Matrix4x4.Scale(new Vector3(1f, StampedeEnvironmentArt.FloorTileScaleY(variant), 1f));

        // One tile per grass-topped floor variant (GroundSurfaceTile_0..n-1); stale extras from earlier art are removed.
        private static int CreateGroundSurfaceTiles(Sprite[] floorArt)
        {
            for (int i = floorArt.Length; AssetDatabase.LoadAssetAtPath<Tile>($"{SpritesDir}/{GroundSurfaceTileName}_{i}.asset") != null; i++)
            {
                AssetDatabase.DeleteAsset($"{SpritesDir}/{GroundSurfaceTileName}_{i}.asset");
            }

            for (int i = 0; i < floorArt.Length; i++)
            {
                CreateTile($"{GroundSurfaceTileName}_{i}", floorArt[i], Color.white, FloorTileTransform(floorArt[i]));
            }
            return floorArt.Length;
        }

        private static void CreateTile(string tileName, Sprite sprite, Color color, Matrix4x4? transform = null)
        {
            string path = $"{SpritesDir}/{tileName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = Tile.ColliderType.Grid;
            tile.transform = transform ?? Matrix4x4.identity;
            tile.flags = TileFlags.LockColor | TileFlags.LockTransform;
            EditorUtility.SetDirty(tile);
        }

        // ---------------------------------------------------------------- prefabs

        private static void BuildPlayerPrefab(int groundLayer, int playerLayer)
        {
            var root = new GameObject("Player") { layer = playerLayer };

            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, 0.95f);
            box.offset = Vector2.zero;

            var visualObject = new GameObject("Visual") { layer = playerLayer };
            visualObject.transform.SetParent(root.transform, false);
            visualObject.transform.localPosition = new Vector3(0f, PlayerFeetY, 0f);
            var renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/Char_Cluck.png");
            renderer.sortingOrder = 10;

            visualObject.AddComponent<CharacterSpriteAnimator>();   // swaps directional art frames when a character has them

            root.AddComponent<PlayerInputReader>();
            var controller = root.AddComponent<CharacterController2D>();

            var so = new SerializedObject(controller);
            so.FindProperty("visual").objectReferenceValue = renderer;
            so.FindProperty("groundMask").intValue = 1 << groundLayer;
            so.FindProperty("abilityPrefabs.cloudPlatform").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(CloudPrefabPath);
            so.FindProperty("abilityPrefabs.horseshoe").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(HorseshoePrefabPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, PlayerPrefabPath);
        }

        private static void BuildCloudPrefab(Sprite square, int groundLayer)
        {
            var root = new GameObject("CloudPlatform") { layer = groundLayer };

            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(2.2f, CloudPlatform.Thickness);

            var visual = AddVisual(root.transform, "Visual", square, new Color(0.95f, 0.97f, 1f), Vector3.zero,
                new Vector3(2.2f, CloudPlatform.Thickness, 1f), 6);
            visual.gameObject.layer = groundLayer;

            AddPooled(root, "CloudPlatform");
            var cloud = root.AddComponent<CloudPlatform>();
            var so = new SerializedObject(cloud);
            so.FindProperty("visual").objectReferenceValue = visual;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, CloudPrefabPath);
        }

        private static void BuildHorseshoePrefab(Sprite sprite)
        {
            var root = new GameObject("Horseshoe");

            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;

            var circle = root.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.35f;

            AddVisual(root.transform, "Visual", sprite, Color.white, Vector3.zero, Vector3.one, 9);

            AddPooled(root, "Horseshoe");
            root.AddComponent<Horseshoe>();

            SavePrefab(root, HorseshoePrefabPath);
        }

        private static void BuildBreakableWallPrefab(Sprite barrier, int groundLayer)
        {
            var root = new GameObject("BreakableWall") { layer = groundLayer };

            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 3f);
            box.offset = new Vector2(0f, 1.5f);

            var visual = AddVisual(root.transform, "Visual", barrier, Color.white, new Vector3(0f, 1.5f, 0f), new Vector3(1f, 3f, 1f), 3);
            visual.gameObject.layer = groundLayer;

            AddPooled(root, "BreakableWall");
            root.AddComponent<BreakableWall>();

            SavePrefab(root, BreakableWallPrefabPath);
        }

        private static void BuildCropPrefab(Sprite sprite)
        {
            var root = new GameObject("Crop");

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 5;

            var circle = root.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.4f;

            AddPooled(root, "Crop");
            var crop = root.AddComponent<CropPickup>();
            var so = new SerializedObject(crop);
            so.FindProperty("visual").objectReferenceValue = renderer;
            AssignSpriteArray(so.FindProperty("normalSprites"), StampedeUIArt.NormalCrops());
            AssignSpriteArray(so.FindProperty("secretSprites"), StampedeUIArt.SecretCrops());
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, CropPrefabPath);
        }

        private static void AssignSpriteArray(SerializedProperty arrayProperty, Sprite[] sprites)
        {
            arrayProperty.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }

        private static void BuildRobotPrefab<T>(string prefabName, RobotType type, Sprite sprite, string path, bool bossSize = false,
            (Sprite right, Sprite left, Sprite defeat) art = default)
            where T : RobotController
        {
            var root = new GameObject(prefabName);

            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(root.transform, false);
            var renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = art.right != null ? art.right : sprite;
            renderer.sortingOrder = 8;
            if (type != RobotType.Drone)
            {
                visualObject.transform.localPosition = new Vector3(0f, bossSize ? BossFeetY : RobotFeetY, 0f);
            }
            if (bossSize)
            {
                visualObject.transform.localScale = new Vector3(2f, 2f, 1f);
            }

            // Top slab: only ever stomps. Body: hurts unless the contact qualifies as a stomp.
            var stomp = new GameObject("StompDetector");
            stomp.transform.SetParent(root.transform, false);
            var stompBox = stomp.AddComponent<BoxCollider2D>();
            stompBox.isTrigger = true;
            stompBox.size = bossSize ? new Vector2(2.0f, 1.4f) : new Vector2(1.0f, 0.7f);
            stompBox.offset = bossSize ? new Vector2(0f, 0.7f) : new Vector2(0f, 0.35f);
            var stompZone = stomp.AddComponent<RobotContactZone>();

            var hurt = new GameObject("HurtDetector");
            hurt.transform.SetParent(root.transform, false);
            var hurtBox = hurt.AddComponent<BoxCollider2D>();
            hurtBox.isTrigger = true;
            hurtBox.size = bossSize ? new Vector2(1.8f, 1.1f) : new Vector2(0.9f, 0.55f);
            hurtBox.offset = bossSize ? new Vector2(0f, -0.45f) : new Vector2(0f, -0.15f);
            var hurtZone = hurt.AddComponent<RobotContactZone>();

            AddPooled(root, prefabName);
            var robot = root.AddComponent<T>();

            var robotSo = new SerializedObject(robot);
            robotSo.FindProperty("robotType").enumValueIndex = (int)type;
            robotSo.FindProperty("visual").objectReferenceValue = renderer;
            robotSo.FindProperty("spriteRight").objectReferenceValue = art.right;
            robotSo.FindProperty("spriteLeft").objectReferenceValue = art.left;
            robotSo.FindProperty("defeatSprite").objectReferenceValue = art.defeat;
            if (bossSize)
            {
                // GroundPatrolRobot wall/ledge probes use the robot half extents.
                robotSo.FindProperty("halfWidth").floatValue = 0.95f;
                robotSo.FindProperty("halfHeight").floatValue = 1.0f;
            }
            var colliders = robotSo.FindProperty("contactColliders");
            colliders.arraySize = 2;
            colliders.GetArrayElementAtIndex(0).objectReferenceValue = stompBox;
            colliders.GetArrayElementAtIndex(1).objectReferenceValue = hurtBox;
            robotSo.ApplyModifiedPropertiesWithoutUndo();

            var stompSo = new SerializedObject(stompZone);
            stompSo.FindProperty("robot").objectReferenceValue = robot;
            stompSo.FindProperty("isStompZone").boolValue = true;
            stompSo.ApplyModifiedPropertiesWithoutUndo();

            var hurtSo = new SerializedObject(hurtZone);
            hurtSo.FindProperty("robot").objectReferenceValue = robot;
            hurtSo.FindProperty("isStompZone").boolValue = false;
            hurtSo.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, path);
        }

        // Barrier Unit: a solid 1x3 robot on the Ground layer with no stomp/hurt zones.
        private static void BuildBarrierUnitPrefab(Sprite sprite, int groundLayer)
        {
            var root = new GameObject("BarrierUnit") { layer = groundLayer };

            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;

            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 3f);

            var visual = AddVisual(root.transform, "Visual", sprite, Color.white, Vector3.zero, new Vector3(1f, 3f, 1f), 3);
            visual.gameObject.layer = groundLayer;

            AddPooled(root, "BarrierUnit");
            var robot = root.AddComponent<BarrierUnit>();
            var so = new SerializedObject(robot);
            so.FindProperty("robotType").enumValueIndex = (int)RobotType.BarrierUnit;
            so.FindProperty("visual").objectReferenceValue = visual;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, BarrierPrefabPath);
        }

        private static void BuildCheckpointPrefab(Sprite square)
        {
            var root = new GameObject("Checkpoint");

            var trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.2f, 4f);
            trigger.offset = new Vector2(0f, 2f);

            Sprite idle = StampedeUIArt.CheckpointIdle();
            Sprite active = StampedeUIArt.CheckpointActive();
            SpriteRenderer flag;
            if (idle != null)
            {
                // Real art is a complete signpost (pole + flag) with a bottom-centre pivot, so it drops straight onto the marker.
                flag = AddVisual(root.transform, "Flag", idle, Color.white, Vector3.zero, Vector3.one, 3);
            }
            else
            {
                AddVisual(root.transform, "Pole", square, new Color(0.85f, 0.85f, 0.85f), new Vector3(0f, 1.5f, 0f), new Vector3(0.12f, 3f, 1f), 3);
                flag = AddVisual(root.transform, "Flag", square, Color.gray, new Vector3(0.45f, 2.6f, 0f), new Vector3(0.8f, 0.5f, 1f), 3);
            }

            AddPooled(root, "Checkpoint");
            var checkpoint = root.AddComponent<Checkpoint>();
            var so = new SerializedObject(checkpoint);
            so.FindProperty("flag").objectReferenceValue = flag;
            so.FindProperty("inactiveSprite").objectReferenceValue = idle;
            so.FindProperty("activeSprite").objectReferenceValue = active;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, CheckpointPrefabPath);
        }

        private static void BuildGoalPrefab(Sprite square)
        {
            var root = new GameObject("Goal");

            var trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.5f, 10f);
            trigger.offset = new Vector2(0f, 4f);

            Sprite goal = StampedeUIArt.GoalFlag();
            if (goal != null)
            {
                // Real art is a complete pole + checkered flag with a bottom-centre pivot; drops straight onto the marker.
                AddVisual(root.transform, "Flag", goal, Color.white, Vector3.zero, Vector3.one, 2);
            }
            else
            {
                AddVisual(root.transform, "Pole", square, new Color(0.9f, 0.9f, 0.9f), new Vector3(0f, 2.5f, 0f), new Vector3(0.15f, 5f, 1f), 2);
                AddVisual(root.transform, "Flag", square, new Color(1f, 0.85f, 0.2f), new Vector3(0.5f, 4.4f, 0f), new Vector3(0.9f, 0.6f, 1f), 2);
            }

            AddPooled(root, "Goal");
            root.AddComponent<LevelGoal>();

            SavePrefab(root, GoalPrefabPath);
        }

        private static void AddPooled(GameObject root, string key)
        {
            var pooled = root.AddComponent<PooledObject>();
            var so = new SerializedObject(pooled);
            so.FindProperty("poolKey").stringValue = key;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SpriteRenderer AddVisual(Transform parent, string visualName, Sprite sprite, Color color,
            Vector3 localPosition, Vector3 scale, int sortingOrder)
        {
            var go = new GameObject(visualName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        // ---------------------------------------------------------------- data assets

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static CharacterData CreateCharacterData(CharacterSpec spec)
        {
            var data = LoadOrCreate<CharacterData>($"{CharacterDataDir}/CharacterData_{spec.type}.asset");
            data.characterType = spec.type;
            data.displayName = spec.displayName;
            data.abilityType = spec.ability;
            data.abilityDescription = spec.description;
            data.abilityUsesPerLevel = AbilityUsesPerLevel;
            data.unlockLevelsRequired = spec.unlockLevels;
            data.moveSpeed = SharedMoveSpeed;
            data.jumpHeight = SharedJumpHeight;
            data.uiColor = spec.ui;
            data.selectCard = StampedeUIArt.CharacterCard(spec.type);
            data.placeholderSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/Char_{spec.type}.png");

            // Real art overrides the generated placeholder (portraits use the idle frame).
            var set = CreateCharacterSpriteSet(spec.type);
            data.spriteSet = set;
            if (set != null && set.idleRight != null)
            {
                data.placeholderSprite = set.idleRight;
            }

            EditorUtility.SetDirty(data);
            return data;
        }

        // ---------------------------------------------------------------- real character art

        // Frame files per character. Only characters listed here get real art; the rest keep placeholders.
        private static readonly Dictionary<CharacterType, string[]> CharacterArtFiles = new()
        {
            // idleRight, runRight1, runRight2, jumpRight, idleLeft, runLeft1, runLeft2, jumpLeft, defeat
            {
                CharacterType.Cluck, new[]
                {
                    "Cluck_right.png", "Cluck_right1.png", "Cluck_right2.png", "Cluck_Right_Jump.png",
                    "Clucky_Left_Stand.png", "Clucky_Left1.png", "Clucky_Left2.png", "Clucky_left_Jump.png",
                    "Clucky_Defeat.png"
                }
            },
        };

        private static string ArtPath(string file) => $"{ArtDir}/{file}";

        // Imports the art as smooth (bilinear) sprites at one common size. Idempotent.
        private static void ImportCharacterArt()
        {
            foreach (var pair in CharacterArtFiles)
            {
                foreach (string file in pair.Value)
                {
                    string path = ArtPath(file);
                    if (!File.Exists(path))
                    {
                        Debug.LogWarning($"[Phase5aSetup] Missing art file {path}; {pair.Key} falls back to its placeholder.");
                        continue;
                    }

                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    bool jumpFrame = file.IndexOf("jump", StringComparison.OrdinalIgnoreCase) >= 0;
                    importer.spritePixelsPerUnit = jumpFrame ? ArtPixelsPerUnit / JumpFrameScale : ArtPixelsPerUnit;
                    StampedeUIArt.SetPivot(importer, FeetPivot);
                    importer.filterMode = FilterMode.Bilinear;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
        }

        // Imports every robot frame as a smooth sprite at the same scale as the characters. Idempotent.
        private static void ImportRobotArt()
        {
            foreach (string path in Directory.GetFiles(RobotArtDir, "*.png"))
            {
                string assetPath = path.Replace('\\', '/');
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = ArtPixelsPerUnit;
                StampedeUIArt.SetPivot(importer, Path.GetFileName(assetPath) == DroneArtFile ? CentrePivot : FeetPivot);
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        // Right-facing, left-facing (null = flip the right frame) and defeat sprites; missing files come back null.
        private static (Sprite right, Sprite left, Sprite defeat) RobotArt(string right, string left, string defeat)
        {
            Sprite Load(string file) => file == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>($"{RobotArtDir}/{file}");
            var r = Load(right);
            var l = Load(left);
            if (r == null || l == null)
            {
                l = null;   // art must be a complete pair, otherwise flip
            }
            return (r, l, Load(defeat));
        }

        // Returns the sprite set for the character, or null when it has no (complete) art.
        private static CharacterSpriteSet CreateCharacterSpriteSet(CharacterType type)
        {
            if (!CharacterArtFiles.TryGetValue(type, out var files))
            {
                return null;
            }

            var sprites = new Sprite[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath(files[i]));
                if (sprites[i] == null)
                {
                    Debug.LogWarning($"[Phase5aSetup] Art frame {files[i]} for {type} did not load; using the placeholder instead.");
                    return null;
                }
            }

            var set = LoadOrCreate<CharacterSpriteSet>($"{CharacterDataDir}/CharacterSprites_{type}.asset");
            set.idleRight = sprites[0];
            set.runRight = new[] { sprites[1], sprites[2] };
            set.jumpRight = sprites[3];
            set.idleLeft = sprites[4];
            set.runLeft = new[] { sprites[5], sprites[6] };
            set.jumpLeft = sprites[7];
            set.defeat = sprites[8];
            EditorUtility.SetDirty(set);
            return set;
        }

        private static RobotData CreateRobotData(string robotName, RobotType type, float speed, string description, GameObject prefab,
            RobotBehaviour behaviour = RobotBehaviour.Patrol, AbilityType? requires = null)
        {
            var data = LoadOrCreate<RobotData>($"{RobotDataDir}/RobotData_{robotName}.asset");
            data.robotType = type;
            data.behaviour = behaviour;
            data.behaviourDescription = description;
            data.moveSpeed = speed;
            data.prefab = prefab;
            data.requiresCharacterAbility = requires;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static List<WorldData> CreateWorldDatas()
        {
            var specs = new (WorldType type, string name, int levels, string blurb)[]
            {
                (WorldType.MeadowRuins, "Meadow Ruins", 11, "Grassland tutorial world, wood/stone robot outposts."),
                (WorldType.FrozenTundra, "Frozen Tundra", 11, "Ice physics: reduced traction, frozen lake platforming."),
                (WorldType.WatermillVillage, "Watermill Village", 11, "Water-wheel village with timed fire-spread hazards."),
                (WorldType.SkyIslands, "Sky Islands", 11, "Vertical platforming across floating islands, wind gusts."),
                (WorldType.SunkenCity, "Sunken City", 11, "Flooded ruins: underwater sections with reduced gravity."),
                (WorldType.RobotMothership, "Robot Mothership", 11, "Zero-G platforming; the Robot Overlord waits at the end."),
            };

            var list = new List<WorldData>();
            foreach (var spec in specs)
            {
                string path = spec.type == WorldType.MeadowRuins
                    ? $"{WorldDataDir}/WorldData_MeadowRuins.asset"
                    : $"{WorldDataDir}/WorldData_{spec.type}.asset";
                var data = LoadOrCreate<WorldData>(path);
                data.worldType = spec.type;
                data.displayName = spec.name;
                data.levelCount = spec.levels;
                data.bossLevelId = $"{spec.type}_Boss";
                data.blurb = spec.blurb;
                data.selectCardArt = StampedeUIArt.WorldSelectCard(spec.type);
                data.levelSelectBackground = StampedeUIArt.LevelSelectBackground(spec.type);
                EditorUtility.SetDirty(data);
                list.Add(data);
            }
            return list;
        }

        private static LevelData CreateLevelData(LevelBuilder builder, GameObject prefab)
        {
            var data = LoadOrCreate<LevelData>($"{LevelDataDir}/LevelData_{builder.Id}.asset");
            data.levelId = builder.Id;
            data.displayName = builder.Title;
            data.worldType = WorldType.MeadowRuins;
            data.levelPrefab = prefab;
            data.isBossLevel = builder.IsBoss;
            data.hasCharacterGatedSecret = builder.HasGate;
            data.characterGatedSecretCharacter = builder.HasGate ? builder.GatePrimary : CharacterType.Cluck;
            data.alsoOpensSecret = builder.GateAlso;
            data.secretDescription = builder.GateDescription;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void SetWorldLevelCount(int count)
        {
            // WorldData.levelCount is the number of regular levels; the boss is tracked by bossLevelId.
            var world = AssetDatabase.LoadAssetAtPath<WorldData>(WorldDataPath);
            if (world != null)
            {
                world.levelCount = count - 1;
                EditorUtility.SetDirty(world);
            }
        }

        // ---------------------------------------------------------------- scene

        private static void WireScene(UnityEngine.SceneManagement.Scene scene, List<CharacterData> characters,
            List<LevelData> levels, RobotData[] robots, List<WorldData> worlds)
        {
            DestroyIfPresent("Phase2Level");   // stale Phase 2 test level, if present
            DestroyIfPresent("Phase1Test");
            DestroyIfPresent(RootName);

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var cropPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CropPrefabPath);
            var checkpointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CheckpointPrefabPath);
            var goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoalPrefabPath);
            var wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BreakableWallPrefabPath);

            var root = new GameObject(RootName);

            var container = new GameObject("CurrentLevel");
            container.transform.SetParent(root.transform, false);

            var poolObject = new GameObject("ObjectPool");
            poolObject.transform.SetParent(root.transform, false);
            poolObject.AddComponent<ObjectPool>();

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Player";
            player.transform.SetParent(root.transform, false);
            player.transform.position = new Vector3(0f, 1f, 0f);
            var controller = player.GetComponent<CharacterController2D>();

            CameraFollow2D follow = SetUpCamera(player.transform, controller);
            var background = StampedeEnvironmentArt.BuildBackground(root.transform, follow.GetComponent<Camera>());

            var loaderObject = new GameObject("LevelLoader");
            loaderObject.transform.SetParent(root.transform, false);
            var loader = loaderObject.AddComponent<LevelLoader>();
            var loaderSo = new SerializedObject(loader);
            loaderSo.FindProperty("levelContainer").objectReferenceValue = container.transform;
            loaderSo.FindProperty("player").objectReferenceValue = controller;
            loaderSo.FindProperty("cameraFollow").objectReferenceValue = follow;
            loaderSo.FindProperty("background").objectReferenceValue = background;
            loaderSo.FindProperty("cropPrefab").objectReferenceValue = cropPrefab;
            loaderSo.FindProperty("checkpointPrefab").objectReferenceValue = checkpointPrefab;
            loaderSo.FindProperty("goalPrefab").objectReferenceValue = goalPrefab;
            loaderSo.FindProperty("breakableWallPrefab").objectReferenceValue = wallPrefab;
            loaderSo.ApplyModifiedPropertiesWithoutUndo();

            var selectObject = new GameObject("CharacterSelectScreen");
            selectObject.transform.SetParent(root.transform, false);
            var characterSelect = selectObject.AddComponent<CharacterSelectScreen>();

            var flowObject = new GameObject("GameFlow");
            flowObject.transform.SetParent(root.transform, false);
            var flow = flowObject.AddComponent<GameFlow>();
            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("characterSelect").objectReferenceValue = characterSelect;
            flowSo.FindProperty("menuArt.levelTileLocked").objectReferenceValue = StampedeUIArt.LevelTileLocked();
            flowSo.FindProperty("menuArt.levelTileNext").objectReferenceValue = StampedeUIArt.LevelTileNext();
            flowSo.FindProperty("menuArt.backButton").objectReferenceValue = StampedeUIArt.BackButton();
            flowSo.FindProperty("menuArt.playButton").objectReferenceValue = StampedeUIArt.PlayButton();
            flowSo.FindProperty("menuArt.worldSelectBanner").objectReferenceValue = StampedeUIArt.WorldUnlockedSign();
            flowSo.FindProperty("menuArt.characterSelectBanner").objectReferenceValue = StampedeUIArt.NewCharacterSign();
            flowSo.FindProperty("menuArt.bossShield").objectReferenceValue = StampedeUIArt.BossShield();
            flowSo.FindProperty("menuArt.oneStarBoard").objectReferenceValue = StampedeUIArt.StarBoard(1);
            flowSo.FindProperty("menuArt.twoStarBoard").objectReferenceValue = StampedeUIArt.StarBoard(2);
            flowSo.FindProperty("menuArt.threeStarBoard").objectReferenceValue = StampedeUIArt.StarBoard(3);
            flowSo.FindProperty("menuArt.newCharacterSign").objectReferenceValue = StampedeUIArt.NewCharacterSign();
            flowSo.FindProperty("menuArt.worldUnlockedSign").objectReferenceValue = StampedeUIArt.WorldUnlockedSign();
            flowSo.ApplyModifiedPropertiesWithoutUndo();
            flowObject.AddComponent<DebugPanel>();

            WireDataManager(characters, levels, robots, worlds);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[Phase5aSetup] Done. {characters.Count} characters, {levels.Count} levels, {worlds.Count} worlds and " +
                      $"{robots.Length} robot types wired into Game.unity. Play in the Game tab (not Device Simulator): pick a world, " +
                      "a level, a character; A/D or arrows + Space + Shift/E (ability); Esc pauses; F1 = debug cheats.");
        }

        private static void WireDataManager(List<CharacterData> characters, List<LevelData> levels, RobotData[] robots, List<WorldData> worlds)
        {
            var dataManager = UnityEngine.Object.FindAnyObjectByType<DataManager>();
            if (dataManager == null)
            {
                Debug.LogError("[Phase5aSetup] No DataManager found in Game.unity; assign characters, levels and robots manually.");
                return;
            }

            var so = new SerializedObject(dataManager);
            AssignList(so.FindProperty("allCharacters"), characters);
            AssignList(so.FindProperty("allLevels"), levels);
            AssignList(so.FindProperty("allRobots"), robots);
            AssignList(so.FindProperty("allWorlds"), worlds);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignList<T>(SerializedProperty list, IList<T> items) where T : UnityEngine.Object
        {
            list.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        private static void DestroyIfPresent(string objectName)
        {
            GameObject existing;
            while ((existing = GameObject.Find(objectName)) != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static CameraFollow2D SetUpCamera(Transform target, CharacterController2D controller)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = GameObject.Find("Main Camera");
                camera = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
            }

            if (camera == null)
            {
                Debug.LogError("[Phase5aSetup] No Main Camera found in Game.unity; camera follow not wired.");
                return null;
            }

            camera.orthographic = true;
            camera.orthographicSize = 7.5f; // zoomed out so the ground and nearby robots stay in view from obstacle tops
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.53f, 0.78f, 0.95f);
            camera.transform.position = new Vector3(0f, 3f, -10f);

            var follow = camera.GetComponent<CameraFollow2D>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<CameraFollow2D>();
            }

            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = target;
            so.FindProperty("controller").objectReferenceValue = controller;
            so.ApplyModifiedPropertiesWithoutUndo();
            return follow;
        }
    }
}
