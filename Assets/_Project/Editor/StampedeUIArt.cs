using System.IO;
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
        // of its source resolution (89px-500px across the batch).
        private const float CropTargetWorldSize = 0.85f;
        private static readonly string[] NormalCropFiles =
        {
            "carrot.png", "cabbage.png", "Cherry.png", "CornCob.png", "CornKernel.png",
            "GrainSack.png", "MiniLoaf.png", "Red_Apple.png", "pellet_sunflower.png",
        };
        private static readonly string[] SecretCropFiles = { "RarePellets_apple.png", "RarePellets_maize.png" };

        /// <summary>Imports every file this class uses, with settings matched to how it's used. Idempotent; missing files warn and are skipped.</summary>
        public static void ImportUIArt()
        {
            ImportAnchored(CheckpointIdleFile, CheckpointPixelsPerUnit);
            ImportAnchored(CheckpointActiveFile, CheckpointPixelsPerUnit);
            ImportAnchored(GoalFile, GoalPixelsPerUnit);
            ImportCentered(ChamberBackdropFile, ChamberPixelsPerUnit);
            foreach (string file in NormalCropFiles) { ImportCropIcon(file); }
            foreach (string file in SecretCropFiles) { ImportCropIcon(file); }
        }

        public static Sprite CheckpointIdle() => Load(CheckpointIdleFile);
        public static Sprite CheckpointActive() => Load(CheckpointActiveFile);
        public static Sprite GoalFlag() => Load(GoalFile);
        public static Sprite ChamberBackdrop() => Load(ChamberBackdropFile);
        public static Sprite[] NormalCrops() => Load(NormalCropFiles);
        public static Sprite[] SecretCrops() => Load(SecretCropFiles);

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
            importer.spritePivot = new Vector2(0.5f, 0f);
            FinishImport(importer);
        }

        private static void ImportCentered(string file, float pixelsPerUnit)
        {
            var importer = BeginImport(file);
            if (importer == null) { return; }
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
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
            importer.spritePixelsPerUnit = Mathf.Max(maxDimension, 1) / CropTargetWorldSize;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
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
    }
}
