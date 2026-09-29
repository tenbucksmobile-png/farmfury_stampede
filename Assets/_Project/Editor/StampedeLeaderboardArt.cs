using System.Collections.Generic;
using System.IO;
using System.Linq;
using FarmFuryStampede.Data;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Imports the Leaderboard screens' art (see FarmFuryStampede.UI.LeaderboardArt) and fills GameFlow's
    /// leaderboardArt. The backdrop is Environment/Settings_Canvas.png, widened like the results art
    /// (Settings_Canvas_Wide.png; edit the original). The world-name lettering (Sprites/UI/&lt;World&gt;.png, 500x500
    /// with wide transparent margins) is imported as one sprite trimmed to its opaque pixels, so every name fills
    /// its slot the same way whatever padding the file has. The LeaderBoard sign, plaque, kernel and apple come
    /// from the Shop / menu art already imported elsewhere.
    /// </summary>
    public static class StampedeLeaderboardArt
    {
        private const string UIDir = "Assets/_Project/Sprites/UI";
        private const string EnvironmentDir = "Assets/_Project/Sprites/Environment";
        private const string BackgroundSource = EnvironmentDir + "/Settings_Canvas.png";
        private const string BackgroundPath = EnvironmentDir + "/Settings_Canvas_Wide.png";
        private const int TrimPadding = 4;

        // In WorldType order (the files' spellings differ from the enum: SkyIslands, MotherShip).
        private static readonly Dictionary<WorldType, string> WorldNameFiles = new()
        {
            { WorldType.MeadowRuins, "MeadowRuins.png" },
            { WorldType.FrozenTundra, "FrozenTundra.png" },
            { WorldType.WatermillVillage, "WatermillVillage.png" },
            { WorldType.SkyIslands, "SkyIslands.png" },
            { WorldType.SunkenCity, "SunkenCity.png" },
            { WorldType.RobotMothership, "MotherShip.png" },
            { WorldType.DustbowlCanyon, "DustbowlCanyon.png" },
            { WorldType.HarvestFairground, "HarvestFairground.png" },
            { WorldType.CropFactory, "CropFactory.png" },
        };

        private static readonly Dictionary<string, string> Singles = new()
        {
            { "logo", "FF_stampedelogo.png" },
            { "bestLabel", "Best.png" },
            { "highScoreLabel", "HighScore.png" },
            { "fastestTimeLabel", "FastestTime.png" },
            { "coin", "Collectable Coin.png" },   // already imported as the bonus-coin pickup; only loaded here
        };

        public static void ImportLeaderboardArt()
        {
            StampedeUIArt.BuildWideResultsArt(BackgroundSource, BackgroundPath);
            ImportPlain(BackgroundPath);
            foreach (var (field, file) in Singles)
            {
                if (field != "coin") { ImportPlain($"{UIDir}/{file}"); }
            }
            foreach (string file in WorldNameFiles.Values) { ImportTrimmed($"{UIDir}/{file}"); }
        }

        /// <summary>Assigns every sprite to the LeaderboardArt field at <paramref name="property"/> (e.g. "leaderboardArt").</summary>
        public static void Wire(SerializedObject so, string property)
        {
            so.FindProperty($"{property}.background").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            foreach (var (field, file) in Singles)
            {
                so.FindProperty($"{property}.{field}").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIDir}/{file}");
            }

            var names = so.FindProperty($"{property}.worldNames");
            var worlds = (WorldType[])System.Enum.GetValues(typeof(WorldType));
            names.arraySize = worlds.Length;
            for (int i = 0; i < worlds.Length; i++)
            {
                names.GetArrayElementAtIndex(i).objectReferenceValue = WorldNameFiles.TryGetValue(worlds[i], out string file)
                    ? AssetDatabase.LoadAllAssetsAtPath($"{UIDir}/{file}").OfType<Sprite>().FirstOrDefault()
                    : null;
            }
        }

        /// <summary>The same backdrop for every Settings / Shop page (ShopArt.pageBackground).</summary>
        public static void WirePageBackground(SerializedObject so, string propertyPath)
        {
            so.FindProperty(propertyPath).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        }

        private static TextureImporter BeginImport(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[LeaderboardArt] Missing art {path}; that part of the Leaderboard keeps its plain look.");
                return null;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 100f;
            importer.maxTextureSize = 4096;
            return importer;
        }

        private static void ImportPlain(string path)
        {
            var importer = BeginImport(path);
            if (importer == null) { return; }
            importer.spriteImportMode = SpriteImportMode.Single;
            StampedeUIArt.SetPivot(importer, new Vector2(0.5f, 0.5f));
            importer.SaveAndReimport();
        }

        // One sprite covering only the opaque pixels (plus a little padding for the lettering's soft shadow).
        private static void ImportTrimmed(string path)
        {
            var importer = BeginImport(path);
            if (importer == null) { return; }
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();

            var bounds = OpaqueBounds(path);
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            string spriteName = Path.GetFileNameWithoutExtension(path);
            var previous = System.Array.Find(provider.GetSpriteRects(), r => r.name == spriteName);
            var rect = new SpriteRect
            {
                name = spriteName,
                rect = bounds,
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = previous != null ? previous.spriteID : GUID.Generate(),
            };
            provider.SetSpriteRects(new[] { rect });
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?
                .SetNameFileIdPairs(new[] { new SpriteNameFileIdPair(rect.name, rect.spriteID) });
            provider.Apply();
            importer.SaveAndReimport();
        }

        // Bounding box of the pixels over ~8% alpha, read from the source PNG, padded and clamped to the texture.
        private static Rect OpaqueBounds(string path)
        {
            var texture = new Texture2D(2, 2);
            try
            {
                texture.LoadImage(File.ReadAllBytes(path));
                Color32[] pixels = texture.GetPixels32();
                int w = texture.width, h = texture.height;
                int minX = w, minY = h, maxX = -1, maxY = -1;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (pixels[y * w + x].a <= 20) { continue; }
                        if (x < minX) { minX = x; }
                        if (x > maxX) { maxX = x; }
                        if (y < minY) { minY = y; }
                        if (y > maxY) { maxY = y; }
                    }
                }
                if (maxX < 0) { return new Rect(0, 0, w, h); }

                minX = Mathf.Max(0, minX - TrimPadding);
                minY = Mathf.Max(0, minY - TrimPadding);
                maxX = Mathf.Min(w - 1, maxX + TrimPadding);
                maxY = Mathf.Min(h - 1, maxY + TrimPadding);
                return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
