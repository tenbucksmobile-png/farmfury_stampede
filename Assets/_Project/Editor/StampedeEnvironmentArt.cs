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
        public static float FloorTileScaleY(Sprite variant) => variant.rect.width / Mathf.Max(1f, variant.pivot.y * 2f);   // pivot = dirt centre

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

        // ---- Frozen Tundra (World 2) ----------------------------------------------------------------------------
        // Parallax, far to near: the aurora sky over distant peaks, then the full scene (mountains and the snow plain).
        // Both paintings are opaque, so (as with Meadow Ruins' three) only the nearer one shows; true depth needs a
        // near layer with a transparent sky. They replace the background layers per level (LevelPrefabRoot.parallaxLayers).
        private static readonly string[] TundraLayers = { "FT_Paralax_mid (1).png", "FT_Paralax_Far.png" };
        // The frosted-grass floor strip: four blocks inside transparent margins (opaque x 39-641, y 77-326 from the top
        // of the 666x375 image); each block's dirt body is its bottom 160 rows.
        private const string TundraFloorPath = "Assets/_Project/Sprites/UI/FrozenTundra/FT_Ground.png";
        private static readonly RectInt TundraFloorArea = new(40, 49, 600, 249);   // bottom-up pixel rows
        private const int TundraFloorVariants = 4, TundraFloorDirtPixels = 160;

        // ---- Watermill Village (World 3) ------------------------------------------------------------------------
        // Parallax, far to near: the sky and river valley (opaque), the mill village and the reed bank. The user's
        // originals are in Sprites/UI/WatermillVillage; a script (2026-10-03) cut the white background out of the middle
        // and near paintings and repeated each three times side by side into WV_Parallax_Mid/Near.png (4096 wide): the
        // parallax scales a layer to the level's width, so a single copy drew the village and reeds ~3x too big.
        // The near reed-bank layer (WV_Parallax_Near.png) is left out (2026-10-03): stretched to the level's width the
        // reeds still drew several units tall, far out of proportion with the characters.
        private static readonly string[] WatermillLayers = { "WV_Parallax_Far.png", "WV_Parallax_Mid.png" };
        // The riverside grass floor strip: three blocks inside transparent margins (opaque x 35-583, top-down rows
        // 89-285 of the 666x375 image). The grass is thick (the top ~60% of a block), so the "dirt" body counted for
        // the cell is the bottom 150 rows: the block is stretched ~1.2x and the grass overhangs the cell ~0.3 units.
        private const string WatermillFloorPath = "Assets/_Project/Sprites/UI/WatermillVillage/GroundStrip.png";
        private static readonly RectInt WatermillFloorArea = new(35, 90, 548, 196);   // bottom-up pixel rows
        private const int WatermillFloorVariants = 3, WatermillFloorDirtPixels = 150;

        // ---- Sky Islands (World 4): one opaque layer, the sky over a sea of clouds (a copy of the user's
        // Sprites/UI/SkyIsland/SkyIslands.png).
        private static readonly string[] SkyLayers = { "SI_Parallax_Far.png" };
        // ---- Sunken City (World 5): one opaque layer, the drowned ruins (a copy of UI/SunkenCity/SunkenCity.png).
        private static readonly string[] SunkenLayers = { "SC_Parallax_Far.png" };
        // ---- Robot Mothership (World 6): one opaque layer, space over the planet (a copy of UI/MotherShip/RobotMothership.png).
        private static readonly string[] MothershipLayers = { "RM_Parallax_Far.png" };

        /// <summary>The Mothership's parallax layers, far to near (missing ones left out).</summary>
        public static Sprite[] MothershipParallax() => MothershipLayers
            .Select(f => AssetDatabase.LoadAssetAtPath<Sprite>($"{EnvironmentDir}/{f}"))
            .Where(s => s != null)
            .ToArray();

        // The Mothership's deck: generated like Sunken City's seabed (a script, 2026-10-04): DeckStrip.png is a 768x320
        // strip, a seamless hull-panel body (bottom 256 rows) under a steel deck plate with a glowing strip that
        // overhangs the cell by ~0.23. Three one-cell variants.
        private const string MothershipFloorPath = "Assets/_Project/Sprites/UI/MotherShip/DeckStrip.png";
        private static readonly RectInt MothershipFloorArea = new(0, 0, 768, 320);   // bottom-up pixel rows
        private const int MothershipFloorVariants = 3, MothershipFloorDirtPixels = 256;

        /// <summary>The Mothership's deck variants, left to right; empty if the art is missing.</summary>
        public static Sprite[] MothershipFloorArt() => AssetDatabase.LoadAllAssetsAtPath(MothershipFloorPath)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        /// <summary>Sunken City's parallax layers, far to near (missing ones left out).</summary>
        public static Sprite[] SunkenParallax() => SunkenLayers
            .Select(f => AssetDatabase.LoadAssetAtPath<Sprite>($"{EnvironmentDir}/{f}"))
            .Where(s => s != null)
            .ToArray();

        // Sunken City's seabed: no ground art came with the world, so SeabedStrip.png was generated (a script,
        // 2026-10-04): a 768x320 strip whose bottom 256 rows are a seamless rock body and whose top is a wavy sand cap
        // overhanging the cell by ~0.25 (like the grass). Three one-cell variants, consecutive columns join seamlessly.
        private const string SunkenFloorPath = "Assets/_Project/Sprites/UI/SunkenCity/SeabedStrip.png";
        private static readonly RectInt SunkenFloorArea = new(0, 0, 768, 320);   // bottom-up pixel rows
        private const int SunkenFloorVariants = 3, SunkenFloorDirtPixels = 256;

        /// <summary>Sunken City's sand-topped seabed variants, left to right; empty if the art is missing.</summary>
        public static Sprite[] SunkenFloorArt() => AssetDatabase.LoadAllAssetsAtPath(SunkenFloorPath)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        /// <summary>Sky Islands' parallax layers, far to near (missing ones left out).</summary>
        public static Sprite[] SkyParallax() => SkyLayers
            .Select(f => AssetDatabase.LoadAssetAtPath<Sprite>($"{EnvironmentDir}/{f}"))
            .Where(s => s != null)
            .ToArray();

        /// <summary>Watermill Village's parallax layers, far to near (missing ones left out).</summary>
        public static Sprite[] WatermillParallax() => WatermillLayers
            .Select(f => AssetDatabase.LoadAssetAtPath<Sprite>($"{EnvironmentDir}/{f}"))
            .Where(s => s != null)
            .ToArray();

        /// <summary>Watermill Village's sliced grass floor variants, left to right; empty if the art is missing.</summary>
        public static Sprite[] WatermillFloorArt() => AssetDatabase.LoadAllAssetsAtPath(WatermillFloorPath)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        /// <summary>Frozen Tundra's parallax layers, far to near (missing ones left out).</summary>
        public static Sprite[] TundraParallax() => TundraLayers
            .Select(f => AssetDatabase.LoadAssetAtPath<Sprite>($"{EnvironmentDir}/{f}"))
            .Where(s => s != null)
            .ToArray();

        /// <summary>Frozen Tundra's sliced frosted-grass floor variants, left to right; empty if the art is missing.</summary>
        public static Sprite[] TundraFloorArt() => AssetDatabase.LoadAllAssetsAtPath(TundraFloorPath)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        /// <summary>Imports every configured layer file as a smooth, opaque sprite. Idempotent; missing files warn and are skipped.</summary>
        public static void ImportBackgroundArt()
        {
            foreach (string file in Layers.Select(l => l.file).Concat(TundraLayers).Concat(WatermillLayers).Concat(SkyLayers).Concat(SunkenLayers).Concat(MothershipLayers))
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
                // Most are opaque paintings; Watermill's middle and near layers have a transparent sky.
                importer.alphaIsTransparency = WatermillLayers.Contains(file);
                importer.maxTextureSize = 4096;   // the repeated Watermill layers are 4096 wide
                importer.SaveAndReimport();
            }

            string floorPath = $"{EnvironmentDir}/{FloorTileFile}";
            if (File.Exists(floorPath))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(floorPath);
                importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                SliceFloorStrip(floorPath, "FloorTile", new RectInt(0, 0, width, height), FloorVariants, FloorDirtPixels);
            }
            else
            {
                Debug.LogWarning($"[EnvironmentArt] Missing art file {floorPath}; the ground surface falls back to the procedural GroundTile.");
            }
            SliceFloorStrip(TundraFloorPath, "FT_Ground", TundraFloorArea, TundraFloorVariants, TundraFloorDirtPixels);
            SliceFloorStrip(WatermillFloorPath, "WV_Ground", WatermillFloorArea, WatermillFloorVariants, WatermillFloorDirtPixels);
            SliceCloudStrip(SkyFloorPath, "SI_Cloud", SkyFloorArea, SkyFloorVariants);
            SliceFloorStrip(SunkenFloorPath, "SC_Ground", SunkenFloorArea, SunkenFloorVariants, SunkenFloorDirtPixels);
            SliceFloorStrip(MothershipFloorPath, "RM_Deck", MothershipFloorArea, MothershipFloorVariants, MothershipFloorDirtPixels);
        }

        // ---- Sky Islands' ground: there is no grass strip up here. The ground is cloud: CloudTile.png is two copies of
        // one seamless flat-topped cloud (933px each); one copy is cut into SkyFloorVariants one-cell slices, and
        // PaintSurface uses variant = column % count, so consecutive columns rebuild the cloud and it repeats without a
        // seam. Each slice keeps its aspect (~1.6 cells tall), pivoted so the flat top is the cell's top and the puffs
        // hang down over the cloud fill below (CloudFill.png, StampedePhase5aSetup.SkyGroundTileName).
        private const string SkyFloorPath = "Assets/_Project/Sprites/UI/SkyIsland/CloudTile.png";
        private static readonly RectInt SkyFloorArea = new(0, 2, 933, 252);   // bottom-up pixel rows
        private const int SkyFloorVariants = 6;

        /// <summary>Sky Islands' cloud surface slices, left to right; empty if the art is missing.</summary>
        public static Sprite[] SkyFloorArt() => AssetDatabase.LoadAllAssetsAtPath(SkyFloorPath)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        // Like SliceFloorStrip, but each slice hangs from the cell's top instead of filling it from the bottom.
        private static void SliceCloudStrip(string path, string prefix, RectInt area, int variants)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[EnvironmentArt] Missing cloud strip {path}; Sky Islands' ground falls back to the grass.");
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            int sliceWidth = area.width / variants;
            int height = area.height;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = sliceWidth; // one slice = one cell wide
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

            var rects = new SpriteRect[variants];
            for (int i = 0; i < variants; i++)
            {
                string spriteName = $"{prefix}_{i}";
                var previous = System.Array.Find(existing, r => r.name == spriteName);
                rects[i] = new SpriteRect
                {
                    name = spriteName,
                    rect = new Rect(area.x + i * sliceWidth, area.y, sliceWidth, height),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 1f - sliceWidth * 0.5f / height),   // the cell centre, half a cell under the top
                    spriteID = previous != null ? previous.spriteID : GUID.Generate(),
                };
            }
            provider.SetSpriteRects(rects);
            var nameIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameIds?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        // Slices a strip of grass-topped dirt blocks (inside 'area', bottom-up pixels) into equal-width sprites named
        // <prefix>_0..n-1, one cell wide, each pivoted on the centre of its dirt body (its bottom dirtPixels rows) -
        // FloorTileScaleY reads the dirt height back from that pivot.
        private static void SliceFloorStrip(string path, string prefix, RectInt area, int variants, int dirtPixels)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[EnvironmentArt] Missing floor strip {path}; that world's ground surface falls back to plain dirt.");
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            int sliceWidth = area.width / variants;
            int height = area.height;
            int dirt = Mathf.Min(dirtPixels, height);

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

            var rects = new SpriteRect[variants];
            for (int i = 0; i < variants; i++)
            {
                string spriteName = $"{prefix}_{i}";
                var previous = System.Array.Find(existing, r => r.name == spriteName);
                rects[i] = new SpriteRect
                {
                    name = spriteName,
                    rect = new Rect(area.x + i * sliceWidth, area.y, sliceWidth, height),
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
