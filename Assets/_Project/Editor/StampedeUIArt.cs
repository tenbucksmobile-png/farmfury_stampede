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
        // Corn only. The other crop art (carrot, cabbage, cherry, grain sack, loaf, apple, sunflower pellet,
        // rare apple pellet) stays in Sprites/UI unused.
        private static readonly string[] NormalCropFiles = { "CornCob.png", "CornKernel.png" };
        private static readonly string[] SecretCropFiles = { "RarePellets_maize.png" };

        // Crops drawn bigger than CropTargetWorldSize. Their pivot is lowered so every crop's bottom edge sits at
        // the same height as a standard one, keeping the big icons clear of the grass too.
        private static readonly Dictionary<string, float> CropSizeOverrides = new()
        {
            { "CornCob.png", 1.2f },
            { "RarePellets_maize.png", 1.2f },
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

        // Ground obstacles (LevelBuilder.PlaceObstacles). Pivot y = where the art meets the ground (the rock has
        // transparent padding below it, the bale's straw skirt reaches almost to the bottom); pixels-per-unit set
        // so each is about the height of the 1.5-unit characters (rock ~1.4, bale ~1.5 including loose straw).
        private static readonly (string file, float pivotY, float pixelsPerUnit)[] ObstacleFiles =
        {
            ("Rock.png", 0.15f, 125.5f),
            ("Haybail.png", 0.04f, 139.5f),
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
            foreach (string file in CharacterCardFiles.Values) { ImportCentered(file, CardPixelsPerUnit); }
            foreach (var (file, pivotY, pixelsPerUnit) in ObstacleFiles)
            {
                var importer = BeginImport(file);
                if (importer == null) { continue; }
                importer.spritePixelsPerUnit = pixelsPerUnit;
                SetPivot(importer, new Vector2(0.5f, pivotY));
                FinishImport(importer);
            }

            ImportCentered(LedgeFile, 253f); // 253px tall -> 1 unit, one tile

            var barrelImporter = BeginImport(BarrelFile);
            if (barrelImporter != null)
            {
                barrelImporter.spritePixelsPerUnit = BarrelPixelsPerUnit;
                SetPivot(barrelImporter, new Vector2(0.5f, BarrelPivotY));
                FinishImport(barrelImporter);
            }
        }

        // Barrel for LevelBuilder.BarrelPyramid: 420px of barrel -> 1.55 units tall on a 1.5-tall collider, so each
        // barrel's lid tucks slightly under the one stacked on it. Pivot y = the barrel's base (34px of padding).
        private const string BarrelFile = "Wooden Barrel.png";
        private const float BarrelPixelsPerUnit = 135.5f;
        private const float BarrelPivotY = 0.068f;

        public static Sprite Barrel() => Load(BarrelFile);

        // Slab art for SecretLedge platforms (LevelBuilder.AddStoneSlabs rescales each slab to its slot).
        private const string LedgeFile = "Stone_Block.png";
        public static Sprite LedgeStone() => Load(LedgeFile);

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
