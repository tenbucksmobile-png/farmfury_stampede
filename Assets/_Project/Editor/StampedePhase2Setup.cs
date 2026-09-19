using System;
using System.Collections.Generic;
using System.IO;
using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Re-runnable Phase 2 bootstrap: (re)generates placeholder sprites, tiles, the Cluck and Crop prefabs,
    /// and the hand-built Meadow Ruins test level inside Game.unity. Safe to run repeatedly; everything it
    /// creates lives under the "Phase2Level" root (plus the camera component), which is rebuilt each run.
    /// </summary>
    public static class StampedePhase2Setup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string SpritesDir = "Assets/_Project/Sprites/Placeholder";
        private const string PrefabsDir = "Assets/_Project/Prefabs";
        private const string CluckDataPath = "Assets/_Project/ScriptableObjects/Characters/CharacterData_Cluck.asset";
        private const string RootName = "Phase2Level";
        private const string SquareSpritePath = SpritesDir + "/Square.png";
        private const string GroundTilePath = SpritesDir + "/GroundTile.asset";
        private const string PlatformTilePath = SpritesDir + "/PlatformTile.asset";
        private const string CluckPrefabPath = PrefabsDir + "/Cluck.prefab";
        private const string CropPrefabPath = PrefabsDir + "/Crop.prefab";

        private const float CluckMoveSpeed = 8f;
        private const float CluckJumpHeight = 3.5f;

        [MenuItem("Farm Fury Stampede/Phase 2/Run Setup")]
        public static void RunSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Phase 2 Setup", "Exit Play mode before running setup.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            int groundLayer = EnsureLayer("Ground");
            int playerLayer = EnsureLayer("Player");

            Directory.CreateDirectory(SpritesDir);
            Directory.CreateDirectory(PrefabsDir);

            Sprite squareSprite = CreateSprite("Square", 16, SquarePixel);
            Sprite cropSprite = CreateSprite("Crop", 16, CropPixel);
            Sprite cluckSprite = CreateSprite("Cluck", 16, CluckPixel);

            Tile groundTile = CreateTile("GroundTile", squareSprite, new Color(0.45f, 0.62f, 0.28f));
            Tile platformTile = CreateTile("PlatformTile", squareSprite, new Color(0.72f, 0.55f, 0.32f));

            TuneCluckData();

            BuildCluckPrefab(cluckSprite, groundLayer, playerLayer);
            BuildCropPrefab(cropSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildScene(groundLayer);
        }

        // ---------------------------------------------------------------- layers

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
                    Debug.Log($"[Phase2Setup] Created layer '{layerName}' at index {i}.");
                    return i;
                }
            }

            throw new InvalidOperationException($"No free user layer slot for '{layerName}'.");
        }

        // ---------------------------------------------------------------- placeholder art

        private static Color32 SquarePixel(int x, int y, int size)
        {
            bool edge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
            byte v = (byte)(edge ? 215 : 255);
            return new Color32(v, v, v, 255);
        }

        private static Color32 CropPixel(int x, int y, int size)
        {
            float c = (size - 1) * 0.5f;
            float dx = x - c;
            float dy = y - c;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > 6.5f)
            {
                return new Color32(0, 0, 0, 0);
            }
            return d > 5.2f ? new Color32(200, 110, 20, 255) : new Color32(255, 170, 40, 255);
        }

        // Faces right by default: yellow body, orange beak, black eye.
        private static Color32 CluckPixel(int x, int y, int size)
        {
            if (x >= 14 && y >= 6 && y <= 9)
            {
                return new Color32(255, 130, 20, 255);
            }
            if (x == 11 && y == 11)
            {
                return new Color32(20, 20, 20, 255);
            }
            if (x >= 1 && x <= 13 && y >= 0 && y <= 14)
            {
                return new Color32(255, 235, 120, 255);
            }
            return new Color32(0, 0, 0, 0);
        }

        private static Sprite CreateSprite(string spriteName, int size, Func<int, int, int, Color32> pixel)
        {
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
            importer.spritePixelsPerUnit = size; // every placeholder is exactly 1 world unit
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Tile CreateTile(string tileName, Sprite sprite, Color color)
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
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static void TuneCluckData()
        {
            var data = AssetDatabase.LoadAssetAtPath<CharacterData>(CluckDataPath);
            if (data == null)
            {
                Debug.LogWarning($"[Phase2Setup] {CluckDataPath} not found; Cluck will use controller defaults.");
                return;
            }

            data.moveSpeed = CluckMoveSpeed;
            data.jumpHeight = CluckJumpHeight;
            EditorUtility.SetDirty(data);
        }

        // ---------------------------------------------------------------- prefabs

        private static GameObject BuildCluckPrefab(Sprite sprite, int groundLayer, int playerLayer)
        {
            var root = new GameObject("Cluck") { layer = playerLayer };

            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, 0.95f);
            box.offset = Vector2.zero;

            var visualObject = new GameObject("Visual") { layer = playerLayer };
            visualObject.transform.SetParent(root.transform, false);
            var renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10;

            root.AddComponent<PlayerInputReader>();
            var controller = root.AddComponent<CharacterController2D>();

            var so = new SerializedObject(controller);
            so.FindProperty("visual").objectReferenceValue = renderer;
            so.FindProperty("groundMask").intValue = 1 << groundLayer;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, $"{PrefabsDir}/Cluck.prefab");
        }

        private static GameObject BuildCropPrefab(Sprite sprite)
        {
            var root = new GameObject("Crop");

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 5;

            var circle = root.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.4f;

            var pooled = root.AddComponent<PooledObject>();
            var so = new SerializedObject(pooled);
            so.FindProperty("poolKey").stringValue = "Crop";
            so.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<CropPickup>();

            return SavePrefab(root, $"{PrefabsDir}/Crop.prefab");
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------------------------------------------------------------- level

        private static void BuildScene(int groundLayer)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // OpenScene unloads unused assets, which invalidates any object references held from before
            // it (SetTile with a destroyed Tile silently places nothing). Always reload from disk here.
            var squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
            var groundTile = AssetDatabase.LoadAssetAtPath<Tile>(GroundTilePath);
            var platformTile = AssetDatabase.LoadAssetAtPath<Tile>(PlatformTilePath);
            var cluckPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CluckPrefabPath);
            var cropPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CropPrefabPath);
            if (squareSprite == null || groundTile == null || platformTile == null || cluckPrefab == null || cropPrefab == null)
            {
                Debug.LogError("[Phase2Setup] A generated asset failed to load; aborting scene build.");
                return;
            }

            DestroyIfPresent(RootName);
            DestroyIfPresent("Phase1Test"); // Phase 1 harness would fire fake EndLevel calls during play.

            var root = new GameObject(RootName);

            // Tilemaps: solid ground, plus thin raised platforms. Each merges into one composite collider.
            var gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(root.transform, false);
            gridObject.AddComponent<Grid>();

            Tilemap ground = CreateTilemap(gridObject.transform, "Ground", groundLayer, 0);
            Tilemap platforms = CreateTilemap(gridObject.transform, "Platforms", groundLayer, 1);

            // Solid ground segments [xStart, xEnd), three tiles deep, top surface at y = 0.
            FillRect(ground, groundTile, -4, 30, -3, -1);   // flat opening stretch
            FillRect(ground, groundTile, 33, 48, -3, -1);   // after gap 1 (3 wide)
            FillRect(ground, groundTile, 52, 80, -3, -1);   // after gap 2 (4 wide)
            FillRect(ground, groundTile, 85, 128, -3, -1);  // after gap 3 (5 wide)
            FillRect(ground, groundTile, -5, -5, -3, 8);    // left wall
            FillRect(ground, groundTile, 128, 128, -3, 8);  // right wall

            // Raised platforms (one tile thick). Top surfaces: A = 3, B = 5, C = 2, D = 3.
            FillRect(platforms, platformTile, 60, 63, 2, 2);   // A: needs a near-full jump from the ground
            FillRect(platforms, platformTile, 67, 70, 4, 4);   // B: only reachable from A
            FillRect(platforms, platformTile, 95, 98, 1, 1);   // C: ledge with jump-buffer landing
            FillRect(platforms, platformTile, 105, 107, 2, 2); // D: second ledge

            FinishTilemap(ground);
            FinishTilemap(platforms);

            int tileCount = ground.GetUsedTilesCount() + platforms.GetUsedTilesCount();
            if (tileCount == 0)
            {
                Debug.LogError("[Phase2Setup] Tilemaps are empty; level geometry was not created.");
            }

            // Crops
            var cropsParent = new GameObject("Crops");
            cropsParent.transform.SetParent(root.transform, false);
            int cropCount = 0;
            foreach (Vector2 position in CropPositions())
            {
                var crop = (GameObject)PrefabUtility.InstantiatePrefab(cropPrefab);
                crop.transform.SetParent(cropsParent.transform, false);
                crop.transform.position = position;
                cropCount++;
            }

            // Pit-death zone under the lowest platform/gap (ground bottoms out at y = -3).
            var pit = new GameObject("PitDeathZone");
            pit.transform.SetParent(root.transform, false);
            pit.transform.position = new Vector3(62f, -9f, 0f);
            var pitBox = pit.AddComponent<BoxCollider2D>();
            pitBox.isTrigger = true;
            pitBox.size = new Vector2(180f, 4f);
            pit.AddComponent<PitDeathZone>();

            BuildGoal(root.transform, squareSprite, new Vector3(122f, 0f, 0f));

            // Player
            var player = (GameObject)PrefabUtility.InstantiatePrefab(cluckPrefab);
            player.transform.SetParent(root.transform, false);
            player.transform.position = new Vector3(0f, 0.6f, 0f);
            var controller = player.GetComponent<CharacterController2D>();

            // Pool + harness
            var poolObject = new GameObject("ObjectPool");
            poolObject.transform.SetParent(root.transform, false);
            poolObject.AddComponent<ObjectPool>();

            var harnessObject = new GameObject("Phase2TestHarness");
            harnessObject.transform.SetParent(root.transform, false);
            var harness = harnessObject.AddComponent<Phase2TestHarness>();
            var harnessSo = new SerializedObject(harness);
            harnessSo.FindProperty("player").objectReferenceValue = controller;
            harnessSo.ApplyModifiedPropertiesWithoutUndo();

            SetUpCamera(player.transform, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[Phase2Setup] Done. Built test level with {cropCount} crops. Open Game.unity, use the Game tab " +
                      "(not Device Simulator), and press Play. A/D or arrows to move, Space to jump, R to restart.");
        }

        private static void DestroyIfPresent(string objectName)
        {
            GameObject existing;
            while ((existing = GameObject.Find(objectName)) != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static Tilemap CreateTilemap(Transform grid, string tilemapName, int layer, int sortingOrder)
        {
            var go = new GameObject(tilemapName) { layer = layer };
            go.transform.SetParent(grid, false);
            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        // Inclusive on both ends of the y range; exclusive at xEnd unless xStart == xEnd (single column).
        private static void FillRect(Tilemap tilemap, Tile tile, int xStart, int xEnd, int yMin, int yMax)
        {
            int xLast = xStart == xEnd ? xStart : xEnd - 1;
            for (int x = xStart; x <= xLast; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        private static void FinishTilemap(Tilemap tilemap)
        {
            tilemap.CompressBounds();

            var tilemapCollider = tilemap.gameObject.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            var composite = tilemap.gameObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

            var body = tilemap.gameObject.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = tilemap.gameObject.AddComponent<Rigidbody2D>();
            }
            body.bodyType = RigidbodyType2D.Static;
        }

        private static void BuildGoal(Transform parent, Sprite squareSprite, Vector3 position)
        {
            var goal = new GameObject("Goal");
            goal.transform.SetParent(parent, false);
            goal.transform.position = position;

            var trigger = goal.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.5f, 10f);
            trigger.offset = new Vector2(0f, 4f);
            goal.AddComponent<LevelGoal>();

            AddVisual(goal.transform, "Pole", squareSprite, new Color(0.9f, 0.9f, 0.9f), new Vector3(0f, 2.5f, 0f), new Vector3(0.15f, 5f, 1f));
            AddVisual(goal.transform, "Flag", squareSprite, new Color(1f, 0.85f, 0.2f), new Vector3(0.5f, 4.4f, 0f), new Vector3(0.9f, 0.6f, 1f));
        }

        private static void AddVisual(Transform parent, string visualName, Sprite sprite, Color color, Vector3 localPosition, Vector3 scale)
        {
            var go = new GameObject(visualName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 2;
        }

        private static IEnumerable<Vector2> CropPositions()
        {
            // Flat opening stretch
            foreach (float x in new[] { 6f, 8f, 10f, 12f })
            {
                yield return new Vector2(x, 0.5f);
            }

            // Gap 1 (30..33) arc
            yield return new Vector2(30.5f, 1.8f);
            yield return new Vector2(31.5f, 2.3f);
            yield return new Vector2(32.5f, 1.8f);

            yield return new Vector2(40f, 0.5f);

            // Gap 2 (48..52) arc
            yield return new Vector2(48.5f, 1.6f);
            yield return new Vector2(49.5f, 2.3f);
            yield return new Vector2(50.5f, 2.3f);
            yield return new Vector2(51.5f, 1.6f);

            // Platform A (top 3) and B (top 5)
            foreach (float x in new[] { 60.5f, 61.5f, 62.5f })
            {
                yield return new Vector2(x, 3.5f);
            }
            yield return new Vector2(67.5f, 5.5f);
            yield return new Vector2(69.5f, 5.5f);

            // Gap 3 (80..85) arc
            yield return new Vector2(81f, 1.8f);
            yield return new Vector2(82f, 2.5f);
            yield return new Vector2(83f, 2.5f);
            yield return new Vector2(84f, 1.8f);

            // Ledges C (top 2) and D (top 3)
            yield return new Vector2(95.5f, 2.5f);
            yield return new Vector2(96.5f, 2.5f);
            yield return new Vector2(106f, 3.5f);

            // Run to the goal
            yield return new Vector2(112f, 0.5f);
        }

        private static void SetUpCamera(Transform target, CharacterController2D controller)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = GameObject.Find("Main Camera");
                camera = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
            }

            if (camera == null)
            {
                Debug.LogError("[Phase2Setup] No Main Camera found in Game.unity; camera follow not wired.");
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = 6f;
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
            so.FindProperty("useXBounds").boolValue = true;
            so.FindProperty("minX").floatValue = -4f;
            so.FindProperty("maxX").floatValue = 128f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
