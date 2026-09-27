using System.Collections.Generic;
using System.IO;
using System.Linq;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEditor;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Phase 6 setup (called from StampedePhase5aSetup.RunSetup): imports the cosmetic art copied from Farm Fury:
    /// Arcade (Sprites/Cosmetics/Hats, Trails, Machines), creates one CosmeticData per cosmetic with Arcade's ids
    /// (ScriptableObjects/Cosmetics), measures each character's head from its idle-right art for hat placement
    /// (CharacterData.hatAnchor / hatWidth), and adds IAPManager / AdManager / AnalyticsManager under GameManagers
    /// if missing - never overwriting the ad keys typed into AdManager.
    /// </summary>
    public static class StampedeCosmetics
    {
        private const string CosmeticsDir = "Assets/_Project/Sprites/Cosmetics";
        private const string UIDir = "Assets/_Project/Sprites/UI";
        private const string AssetDir = "Assets/_Project/ScriptableObjects/Cosmetics";
        private const float TrailWorldSize = 0.9f;       // a trail stamp's width in world units
        private const float MachineWorldHeight = 2.3f;   // a machine is bigger than the 1.5-unit animals
        private const float HatPivotY = 0.15f;           // the brim sinks a little onto the head

        // Arcade's file names say "Clucky" for Cluck.
        private static string ArtName(CharacterType c) => c == CharacterType.Cluck ? "Clucky" : c.ToString();

        // Per-character hat files that don't follow the pattern (Arcade's own spellings).
        private static readonly Dictionary<string, string> Renames = new()
        {
            { "Baseball_Horace_right.png", "Baseball_horace_right.png" },
            { "Cowboy_Bessie.png", "Cowboy_bessie.png" },
            { "Cowboy_Horace.png", "Cowboy_horace.png" },
        };

        private static readonly (string id, string name, string file, string icon)[] Trails =
        {
            ("trail_rainbowribbon", "Rainbow Ribbon", "RainbowRibbon.png", "Ribbon.png"),
            ("trail_sparkledust", "Sparkle Dust", "SparkleDust.png", "SparkleDust.png"),
            ("trail_cornhusk", "Corn Husk Trail", "CornHuskTrail.png", "CornHusk.png"),
            ("trail_ember", "Ember Trail", "EmberTrail.png", "Ember.png"),
            ("trail_confetti", "Confetti Trail", "ConfettiTrail.png", "Confetti.png"),
            ("trail_bubbles", "Bubbles Trail", "BubblesTrail.png", "bubbles.png"),
        };

        private static readonly (string id, string name, CharacterType owner, string right, string left, string icon)[] Machines =
        {
            ("machine_tractor_clucky", "Cluck's Tractor", CharacterType.Cluck, "Clucky_tractor_right.png", "Cluck_Tractor_left.png", "CluckyTruck.png"),
            ("machine_truck_bessie", "Bessie's Milk Tanker", CharacterType.Bessie, "Bessie_Truck_right.png", "Bessie_Truck_Left.png", "BessieTruck.png"),
            ("machine_hay_horace", "Horace's Hay Baler", CharacterType.Horace, "Horace_hay_right.png", "Horace_hay_left.png", "HoraceTruck.png"),
        };

        // ------------------------------------------------------------ art import

        public static void ImportArt()
        {
            foreach (string path in Directory.GetFiles($"{CosmeticsDir}/Hats", "*.png"))
            {
                Import(path.Replace('\\', '/'), 100f, new Vector2(0.5f, HatPivotY));
            }
            foreach (string path in Directory.GetFiles($"{CosmeticsDir}/Trails", "*.png"))
            {
                Import(path.Replace('\\', '/'), 500f / TrailWorldSize, new Vector2(0.5f, 0.5f));
            }
            foreach (string path in Directory.GetFiles($"{CosmeticsDir}/Machines", "*.png"))
            {
                Import(path.Replace('\\', '/'), 500f / MachineWorldHeight, new Vector2(0.5f, 0.02f));   // feet (wheels) pivot, like the animals
            }
            foreach (CharacterType c in System.Enum.GetValues(typeof(CharacterType)))
            {
                Import(LifeIconPath(c), 100f, new Vector2(0.5f, 0.5f));
            }
            foreach (var t in Trails) { Import($"{UIDir}/{t.icon}", 100f, new Vector2(0.5f, 0.5f)); }
            foreach (var m in Machines) { Import($"{UIDir}/{m.icon}", 100f, new Vector2(0.5f, 0.5f)); }
            foreach (string icon in new[] { "Sombrero.png", "BaseballHat.png", "CowboyHat.png", "ChefHat.png", "CrownHat.png" })
            {
                Import($"{UIDir}/{icon}", 100f, new Vector2(0.5f, 0.5f));
            }
        }

        private static void Import(string path, float pixelsPerUnit, Vector2 pivot)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[Cosmetics] Missing art {path}; skipped.");
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            StampedeUIArt.SetPivot(importer, pivot);
            importer.SaveAndReimport();
        }

        // HUD life icons: each character's thumbs up (Cluck's in Sprites/UI, the rest copied from Arcade into UI/ThumbsUp).
        private static string LifeIconPath(CharacterType c) => c switch
        {
            CharacterType.Cluck => $"{UIDir}/CluckThumbsUp.png",
            CharacterType.Percy => $"{UIDir}/ThumbsUp/PercyThumbsup.png",
            _ => $"{UIDir}/ThumbsUp/{c}ThumbsUp.png",
        };

        public static Sprite LifeIcon(CharacterType c) => AssetDatabase.LoadAssetAtPath<Sprite>(LifeIconPath(c));

        private static Sprite Hat(string file)
        {
            if (file == null) { return null; }
            if (Renames.TryGetValue(file, out var renamed)) { file = renamed; }
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{CosmeticsDir}/Hats/{file}");
        }

        private static Sprite UI(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UIDir}/{file}");

        // ------------------------------------------------------------ CosmeticData assets

        public static List<CosmeticData> CreateAssets()
        {
            Directory.CreateDirectory(AssetDir);
            var all = new List<CosmeticData>();

            var sombrero = Make(IAPManager.SombreroCosmeticId, "Sombrero", CosmeticType.Hat, UI("Sombrero.png"));
            sombrero.anyCharacter = true;
            sombrero.hatVariants = Enumerable.Range(1, 4).Select(i => Hat($"Sombrero_{i}.png")).Where(s => s != null).ToArray();
            sombrero.hatRight = sombrero.hatLeft = sombrero.hatVariants.FirstOrDefault();
            sombrero.hatScale = 1.6f;
            all.Add(sombrero);

            var chef = Make(IAPManager.ChefHatCosmeticId, "Chef Hat", CosmeticType.Hat, UI("ChefHat.png"));
            chef.anyCharacter = true;
            chef.hatRight = chef.hatLeft = Hat("ChefHat.png");
            chef.hatScale = 1.1f;
            all.Add(chef);

            var crown = Make(IAPManager.CrownCosmeticId, "Crown", CosmeticType.Hat, UI("CrownHat.png"));
            crown.anyCharacter = true;
            crown.hatRight = crown.hatLeft = Hat("Crown.png");
            crown.hatScale = 0.9f;
            all.Add(crown);

            foreach (CharacterType c in System.Enum.GetValues(typeof(CharacterType)))
            {
                string lower = c.ToString().ToLowerInvariant();
                var cap = Make($"baseball_cap_{lower}", "Baseball Cap", CosmeticType.Hat, UI("BaseballHat.png"));
                cap.character = c;
                cap.hatRight = Hat($"Baseball_{ArtName(c)}_right.png");
                cap.hatLeft = Hat($"Baseball_{ArtName(c)}_left.png");
                cap.hatScale = 1.1f;
                all.Add(cap);

                var cowboy = Make($"cowboy_hat_{lower}", "Cowboy Hat", CosmeticType.Hat, UI("CowboyHat.png"));
                cowboy.character = c;
                cowboy.hatRight = Hat($"Cowboy_{ArtName(c)}.png");
                cowboy.hatLeft = Hat($"Cowboy_{ArtName(c)}_left.png");
                cowboy.hatScale = 1.3f;
                all.Add(cowboy);
            }

            foreach (var (id, name, file, icon) in Trails)
            {
                var trail = Make(id, name, CosmeticType.Trail, UI(icon));
                trail.trailSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{CosmeticsDir}/Trails/{file}");
                all.Add(trail);
            }

            foreach (var (id, name, owner, right, left, icon) in Machines)
            {
                var machine = Make(id, name, CosmeticType.Skin, UI(icon));
                machine.character = owner;
                machine.skinRight = AssetDatabase.LoadAssetAtPath<Sprite>($"{CosmeticsDir}/Machines/{right}");
                machine.skinLeft = AssetDatabase.LoadAssetAtPath<Sprite>($"{CosmeticsDir}/Machines/{left}");
                machine.spawnsMovementSmoke = true;
                all.Add(machine);
            }

            foreach (var item in all) { EditorUtility.SetDirty(item); }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Cosmetics] {all.Count} cosmetics.");
            return all;
        }

        // Loads or creates the asset and resets it to the given basics (the rest is set by the caller).
        private static CosmeticData Make(string id, string name, CosmeticType type, Sprite preview)
        {
            string path = $"{AssetDir}/Cosmetic_{id}.asset";
            var data = AssetDatabase.LoadAssetAtPath<CosmeticData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<CosmeticData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.cosmeticId = id;
            data.displayName = name;
            data.cosmeticType = type;
            data.previewSprite = preview;
            data.anyCharacter = false;
            data.hatRight = data.hatLeft = null;
            data.hatVariants = new Sprite[0];
            data.hatScale = 1f;
            data.skinRight = data.skinLeft = null;
            data.spawnsMovementSmoke = false;
            data.trailSprite = null;
            return data;
        }

        // ------------------------------------------------------------ hat placement

        /// <summary>Finds each character's head in its idle-right frame: the top of the art, and the width near the top.</summary>
        public static void MeasureHatAnchors(IEnumerable<CharacterData> characters)
        {
            foreach (var data in characters)
            {
                var sprite = data.spriteSet != null ? (data.spriteSet.idleRight != null ? data.spriteSet.idleRight : data.spriteSet.idleLeft) : null;
                if (sprite == null || !Measure(sprite, out var anchor, out float width))
                {
                    continue;
                }

                data.hatAnchor = anchor;
                data.hatWidth = width;
                EditorUtility.SetDirty(data);
            }
        }

        private static bool Measure(Sprite sprite, out Vector2 anchor, out float width)
        {
            anchor = default;
            width = 0f;
            string path = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            var texture = new Texture2D(2, 2);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                return false;
            }

            var pixels = texture.GetPixels32();
            int w = texture.width, h = texture.height;
            int top = -1, bottom = -1;
            for (int y = h - 1; y >= 0 && top < 0; y--)
            {
                for (int x = 0; x < w; x++) { if (pixels[y * w + x].a > 128) { top = y; break; } }
            }
            for (int y = 0; y < h && bottom < 0; y++)
            {
                for (int x = 0; x < w; x++) { if (pixels[y * w + x].a > 128) { bottom = y; break; } }
            }
            if (top < 0 || bottom < 0)
            {
                Object.DestroyImmediate(texture);
                return false;
            }

            // The head: the opaque span over the top 18% of the figure.
            int bandBottom = top - Mathf.RoundToInt((top - bottom) * 0.18f);
            int minX = w, maxX = -1;
            for (int y = bandBottom; y <= top; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (pixels[y * w + x].a > 128) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); }
                }
            }
            Object.DestroyImmediate(texture);

            // Texture pixels to sprite-local units (the sprite is the whole texture, scaled to its rect).
            float scaleX = sprite.rect.width / w, scaleY = sprite.rect.height / h;
            float ppu = sprite.pixelsPerUnit;
            var pivot = sprite.pivot;
            float headTop = top - (top - bottom) * 0.06f;
            anchor = new Vector2(((minX + maxX) * 0.5f * scaleX - pivot.x) / ppu, (headTop * scaleY - pivot.y) / ppu);
            width = Mathf.Clamp((maxX - minX) * scaleX / ppu, 0.5f, 1.1f);
            return true;
        }

        // ------------------------------------------------------------ scene

        /// <summary>Gives DataManager the cosmetics and makes sure GameManagers has the three monetisation managers.</summary>
        public static void WireScene()
        {
            var cosmetics = AssetDatabase.FindAssets("t:CosmeticData", new[] { AssetDir })
                .Select(g => AssetDatabase.LoadAssetAtPath<CosmeticData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null).OrderBy(c => c.cosmeticId).ToList();

            var dataManager = Object.FindAnyObjectByType<DataManager>();
            if (dataManager != null)
            {
                var so = new SerializedObject(dataManager);
                var list = so.FindProperty("allCosmetics");
                list.arraySize = cosmetics.Count;
                for (int i = 0; i < cosmetics.Count; i++) { list.GetArrayElementAtIndex(i).objectReferenceValue = cosmetics[i]; }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var managers = dataManager != null ? dataManager.transform.parent : null;
            if (managers == null)
            {
                var root = GameObject.Find("GameManagers");
                managers = root != null ? root.transform : null;
            }
            if (managers == null)
            {
                Debug.LogError("[Cosmetics] No GameManagers object in the scene; add IAPManager, AdManager and AnalyticsManager by hand.");
                return;
            }

            Ensure<IAPManager>(managers, "IAPManager");
            Ensure<AdManager>(managers, "AdManager");
            Ensure<AnalyticsManager>(managers, "AnalyticsManager");
        }

        private static void Ensure<T>(Transform parent, string name) where T : Component
        {
            if (Object.FindAnyObjectByType<T>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<T>();
        }
    }
}
