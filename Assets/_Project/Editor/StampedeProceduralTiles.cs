using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Hand-built fallback ground/platform tile art. Two rounds of AI generation (Kling, then Leonardo)
    /// only ever produced isometric block icons despite explicit "side elevation, not isometric" prompts -
    /// this generates a flat, correctly-projected tile directly instead of continuing to fight the model.
    ///
    /// GroundTile deliberately has NO grass cap: it fills the buried ground mass (GroundDepth = 6 tiles deep,
    /// plus the tall level-boundary walls), where a cap would stripe down every column. The grass-topped look
    /// comes from Sprites/Environment/FloorTile.png instead, which LevelBuilder.PaintSurface puts only on the
    /// exposed top cell of each column.
    /// PlatformTile IS always exactly one tile thick (Floating surfaces are a single row), so its plank art
    /// has no equivalent stacking concern.
    /// </summary>
    public static class StampedeProceduralTiles
    {
        private const string SpritesDir = "Assets/_Project/Sprites/Placeholder";
        private const int Size = 64;

        public static Sprite CreateGroundTileSprite() => CreateSprite("GroundTileArt", GroundPixel);
        public static Sprite CreatePlatformTileSprite() => CreateSprite("PlatformTileArt", PlatformPixel);

        // Fixed rock positions, kept well clear of every edge so nothing is cut off at a tile seam.
        private static readonly (int cx, int cy, int r)[] Rocks =
        {
            (16, 44, 4), (42, 40, 3), (26, 20, 4), (50, 16, 3), (12, 14, 3),
        };

        private static Color32 GroundPixel(int x, int y, int size)
        {
            foreach (var (cx, cy, r) in Rocks)
            {
                int dx = x - cx, dy = y - cy;
                if (dx * dx + dy * dy <= r * r)
                {
                    bool rim = dx * dx + dy * dy >= (r - 1) * (r - 1);
                    return rim ? new Color32(90, 90, 100, 255) : new Color32(150, 152, 160, 255);
                }
            }

            // Deterministic (not random) fleck noise, so re-running setup always produces the same texture.
            int hash = (x * 928371 + y * 123457) & 0xFF;
            bool fleck = hash < 40;
            return fleck ? new Color32(96, 64, 40, 255) : new Color32(124, 86, 54, 255);
        }

        private static Color32 PlatformPixel(int x, int y, int size)
        {
            // Bands and grain repeat on a period that divides Size evenly, so left/right edges tile seamlessly.
            int band = y % 16;
            bool seam = band == 0 || band == 15;
            bool grain = (x + y * 3) % 21 < 2;

            if (seam) { return new Color32(120, 86, 48, 255); }
            if (grain) { return new Color32(150, 112, 66, 255); }
            return new Color32(178, 138, 84, 255);
        }

        private static Sprite CreateSprite(string spriteName, Func<int, int, int, Color32> pixel)
        {
            string path = $"{SpritesDir}/{spriteName}.png";

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    pixels[y * Size + x] = pixel(x, y, Size);
                }
            }
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Size; // 1 tile = 1 world unit, matching the Grid's default cell size
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
