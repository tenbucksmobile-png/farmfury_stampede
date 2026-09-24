using System.Collections.Generic;
using System.IO;
using FarmFuryStampede.Data;
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
        private const string GoalFile = "LevelComplete.png"; // checkered finish flag, despite the filename
        private const float CheckpointPixelsPerUnit = 143f;  // 500px tall -> ~3.5 units, matching the old checkpoint pole+flag height
        private const float GoalPixelsPerUnit = 100f;         // 500px tall -> 5 units, matching the old goal pole height (taller, more decorative)

        private const string ChamberBackdropFile = "BarrierChamberWall.png";
        private const float ChamberPixelsPerUnit = 200f;     // arbitrary; LevelBuilder rescales it to fit each chamber exactly

        // Centred icons, scaled per-file so every crop reads at roughly the same on-screen size regardless
        // of its source resolution.
        private const float CropTargetWorldSize = 0.6f;   // well under the 1.5-unit characters/robots
        // The smiling corn kernel only: the cob was camouflaged against the cobs on the backdrop corn stalks as an
        // everyday pickup, so it is kept for the (rarer, off-path) secret crops. The other crop art (maize pellet, carrot, cabbage, cherry, grain sack, loaf, apple, sunflower pellet, rare apple
        // pellet) stays in Sprites/UI unused.
        private static readonly string[] NormalCropFiles = { "CornKernel.png" };
        // Per-marker art override (CropSpawnPoint.visualOverride): the bonus coin on a stone tower top.
        private const string CoinFile = "Collectable Coin.png";
        private static readonly string[] SecretCropFiles = { "CornCob.png" };   // the golden cob marks a gated secret

        // Crops drawn bigger than CropTargetWorldSize. Their pivot is lowered so every crop's bottom edge sits at
        // the same height as a standard one, keeping the big icons clear of the grass too.
        private static readonly Dictionary<string, float> CropSizeOverrides = new()
        {
            { "CornKernel.png", 0.9f },          // mockup size: reads clearly on the stone blocks
            { CoinFile, 0.9f },
            { "CornCob.png", 1.2f },
        };

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
            ImportAnchored(CheckpointIdleFile, CheckpointPixelsPerUnit);
            ImportAnchored(CheckpointActiveFile, CheckpointPixelsPerUnit);
            ImportAnchored(GoalFile, GoalPixelsPerUnit);
            ImportCentered(ChamberBackdropFile, ChamberPixelsPerUnit);
            foreach (string file in NormalCropFiles) { ImportCropIcon(file); }
            foreach (string file in SecretCropFiles) { ImportCropIcon(file); }
            ImportCropIcon(CoinFile);
            foreach (string file in CharacterCardFiles.Values) { ImportCentered(file, CardPixelsPerUnit); }
            foreach (var (file, pivotY, metres) in ObstacleFiles) { ImportToScale(file, pivotY, metres); }

            ImportCentered(LedgeFile, 253f); // 253px tall -> 1 unit, one tile
            ImportCentered(StoneBlockFile, 292f); // 292px tall -> 1 unit, one tile

            foreach (var (file, pivotY, metres) in SceneryFiles) { ImportToScale(file, pivotY, metres); }
            ImportToScale(BarrelFile, BarrelPivotY, BarrelMetres);
        }

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
        };
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
            float pivotY = Mathf.Min(0.5f, CropTargetWorldSize * 0.5f / Mathf.Max(worldHeight, 0.01f));
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
