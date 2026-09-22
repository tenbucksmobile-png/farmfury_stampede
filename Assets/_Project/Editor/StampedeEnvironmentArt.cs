using System.IO;
using System.Linq;
using FarmFuryStampede.LevelSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
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

        // A 456x184 strip of three grass-topped dirt blocks (152px wide each). Sliced into three variants that
        // LevelBuilder puts on every exposed top cell of the ground; the procedural GroundTile fills below.
        // Each block's dirt body (the bottom FloorDirtPixels rows) is stretched to exactly one cell and the
        // grass (the rows above it) overhangs into the empty cell above - visual only, the tiles use Grid colliders.
        private const string FloorTileFile = "FloorTile.png";
        private const int FloorVariants = 3;
        private const int FloorDirtPixels = 126;

        /// <summary>Tile transform Y scale that stretches a floor variant's dirt body to exactly one cell tall.</summary>
        public static float FloorTileScaleY(Sprite variant) => variant.rect.width / Mathf.Min(FloorDirtPixels, variant.rect.height);

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

            ImportFloorTileArt();
        }

        // Slices FloorTile.png into FloorVariants equal-width sprites, each pivoted on the centre of its dirt body.
        private static void ImportFloorTileArt()
        {
            string path = $"{EnvironmentDir}/{FloorTileFile}";
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[EnvironmentArt] Missing art file {path}; the ground surface falls back to the procedural GroundTile.");
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            int sliceWidth = width / FloorVariants;
            int dirt = Mathf.Min(FloorDirtPixels, height);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = sliceWidth; // one block = one cell wide
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existing = provider.GetSpriteRects();

            var rects = new SpriteRect[FloorVariants];
            for (int i = 0; i < FloorVariants; i++)
            {
                string spriteName = $"FloorTile_{i}";
                var previous = System.Array.Find(existing, r => r.name == spriteName);
                rects[i] = new SpriteRect
                {
                    name = spriteName,
                    rect = new Rect(i * sliceWidth, 0, sliceWidth, height),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, dirt * 0.5f / height),
                    spriteID = previous != null ? previous.spriteID : GUID.Generate(),
                };
            }
            provider.SetSpriteRects(rects);
            var nameIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameIds?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        /// <summary>The sliced grass-topped floor variants, in left-to-right order; empty if the art is missing.</summary>
        public static Sprite[] FloorTileArt() => AssetDatabase.LoadAllAssetsAtPath($"{EnvironmentDir}/{FloorTileFile}")
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

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
