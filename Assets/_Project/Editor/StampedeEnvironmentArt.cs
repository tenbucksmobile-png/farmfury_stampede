using System.IO;
using FarmFuryStampede.LevelSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Imports the real Meadow Ruins parallax background art (<see cref="EnvironmentDir"/>) and builds/wires
    /// a <see cref="ParallaxBackground"/> into Game.unity. <see cref="ImportBackgroundArt"/> and
    /// <see cref="BuildBackground"/> are called from <see cref="StampedePhase5aSetup"/> so a normal Phase 5a
    /// re-run keeps the background in sync; the menu item lets it be re-run alone after dropping in new art.
    /// </summary>
    public static class StampedeEnvironmentArt
    {
        private const string EnvironmentDir = "Assets/_Project/Sprites/Environment";
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const float ArtPixelsPerUnit = 200f;
        private const float BaseY = 3f;

        // Cracked dirt with grass tufts and jagged broken edges - its own border reads as "about to give way",
        // which is why it's used for the BreakableFloor tile rather than the main (seamless) GroundTile.
        private const string BreakableFloorFile = "FloorTile.png";
        private const float BreakableFloorPixelsPerUnit = 411f; // native width -> 1 tile = 1 world unit

        // Far -> near. DistantBarn.png is intentionally excluded: its softer painterly style doesn't match
        // Layer1-3's flat-cartoon look yet - drop it in here once it's regenerated to match.
        private static readonly (string file, float factor, int sortingOrder)[] Layers =
        {
            ("Layer1.png", 0.08f, -30),
            ("Layer2.png", 0.35f, -20),
            ("Layer3.png", 0.65f, -10),
        };

        [MenuItem("Farm Fury Stampede/Environment/Import and Wire Meadow Ruins Background")]
        public static void ImportAndWireStandalone()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Environment Art", "Exit Play mode before running setup.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ImportBackgroundArt();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = GameObject.Find("LevelSystem");
            var loader = Object.FindAnyObjectByType<LevelLoader>();
            var camera = Camera.main;
            if (root == null || loader == null || camera == null)
            {
                Debug.LogError("[EnvironmentArt] LevelSystem root, LevelLoader or Main Camera missing; run Phase 5a setup first.");
                return;
            }

            var background = BuildBackground(root.transform, camera);
            var so = new SerializedObject(loader);
            so.FindProperty("background").objectReferenceValue = background;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EnvironmentArt] Meadow Ruins parallax background imported and wired.");
        }

        /// <summary>Imports every configured layer file as a smooth, opaque sprite. Idempotent; missing files warn and are skipped.</summary>
        public static void ImportBackgroundArt()
        {
            foreach (var (file, _, _) in Layers)
            {
                string path = $"{EnvironmentDir}/{file}";
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[EnvironmentArt] Missing art file {path}; that layer will be skipped.");
                    continue;
                }

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = ArtPixelsPerUnit;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = false; // opaque backdrop paintings, no transparency needed
                importer.SaveAndReimport();
            }

            string breakablePath = $"{EnvironmentDir}/{BreakableFloorFile}";
            if (File.Exists(breakablePath))
            {
                AssetDatabase.ImportAsset(breakablePath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(breakablePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = BreakableFloorPixelsPerUnit;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true; // jagged broken edges - the corners are transparent
                importer.SaveAndReimport();
            }
            else
            {
                Debug.LogWarning($"[EnvironmentArt] Missing art file {breakablePath}; BreakableTile falls back to its placeholder.");
            }
        }

        public static Sprite BreakableFloorArt() => AssetDatabase.LoadAssetAtPath<Sprite>($"{EnvironmentDir}/{BreakableFloorFile}");

        /// <summary>
        /// (Re)builds the "Background" child under <paramref name="root"/> with one ParallaxLayer per
        /// configured art layer. Safe to call repeatedly - destroys and rebuilds so re-runs stay in sync.
        /// </summary>
        public static ParallaxBackground BuildBackground(Transform root, Camera camera)
        {
            Transform existing = root.Find("Background");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(root, false);
            var background = backgroundObject.AddComponent<ParallaxBackground>();

            var layerComponents = new ParallaxLayer[Layers.Length];
            for (int i = 0; i < Layers.Length; i++)
            {
                var (file, factor, sortingOrder) = Layers[i];
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{EnvironmentDir}/{file}");
                if (sprite == null)
                {
                    Debug.LogWarning($"[EnvironmentArt] {file} did not load; that layer is left out of the background.");
                    continue;
                }

                var layerObject = new GameObject(Path.GetFileNameWithoutExtension(file));
                layerObject.transform.SetParent(backgroundObject.transform, false);
                var renderer = layerObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = sortingOrder;

                var layer = layerObject.AddComponent<ParallaxLayer>();
                var layerSo = new SerializedObject(layer);
                layerSo.FindProperty("parallaxFactor").floatValue = factor;
                layerSo.ApplyModifiedPropertiesWithoutUndo();

                layerComponents[i] = layer;
            }

            var so = new SerializedObject(background);
            so.FindProperty("targetCamera").objectReferenceValue = camera;
            so.FindProperty("baseY").floatValue = BaseY;
            SerializedProperty arr = so.FindProperty("layers");
            arr.arraySize = layerComponents.Length;
            for (int i = 0; i < layerComponents.Length; i++)
            {
                arr.GetArrayElementAtIndex(i).objectReferenceValue = layerComponents[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return background;
        }
    }
}
