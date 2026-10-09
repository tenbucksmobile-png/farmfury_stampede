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
    /// (ScriptableObjects/Cosmetics), sets each character's hat placement per art frame from hand-measured head
    /// points (CharacterData.hatPlacements), and adds IAPManager / AdManager / AnalyticsManager under GameManagers
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

        // The top of each head on each character frame, read off the art (Sprites/Characters, all 500x500) on a pixel
        // grid on 2026-10-09: x, y in source pixels from the frame's top-left, where the hat's brim sits (on the skull,
        // between the horns / ears, under Cluck's crest - not the art's highest point, which is a tail fan, mane or
        // horn on half the cast), and the head's width in pixels (what a hat's hatScale multiplies). The automatic
        // measurement this replaces put hats on Gerald's tail, Horace's back and Woolly's fleece.
        // A frame not listed (ability poses, Cluck's defeat) uses its side's idle frame's placement.
        // To tune: edit a row, re-run setup, then Farm Fury Stampede > Debug > Render Cosmetic Preview Sheets.
        private static readonly Dictionary<string, (int x, int y, int width)> HeadPoints = new()
        {
            { "Cluck_right.png", (290, 78, 165) }, { "Cluck_right1.png", (290, 78, 165) }, { "Cluck_right2.png", (295, 78, 165) },
            { "Cluck_Right_Jump.png", (250, 95, 165) },
            { "Clucky_Left_Stand.png", (205, 95, 165) }, { "Clucky_Left1.png", (210, 110, 165) }, { "Clucky_Left2.png", (225, 100, 165) },
            { "Clucky_left_Jump.png", (240, 98, 165) },
            { "Bessie_right.png", (330, 55, 160) }, { "Bessie_right2.png", (325, 55, 160) },
            { "Bessie_left3.png", (185, 50, 160) }, { "Bessie_left.png", (150, 50, 160) }, { "Bessie_left2.png", (160, 45, 160) },
            { "Right1.png", (300, 35, 190) }, { "Right2.png", (305, 35, 190) },
            { "Flat1.png", (190, 35, 190) }, { "Flat2.png", (175, 30, 190) },
            { "Wooly_right.png", (350, 60, 195) }, { "Wooly_left.png", (100, 60, 195) },
            { "Ducky_right.png", (270, 25, 200) }, { "Ducky_left.png", (215, 25, 200) },
            { "Horace_right.png", (370, 60, 150) }, { "Horace_right1.png", (365, 60, 150) },
            { "Horace_left.png", (115, 60, 150) }, { "Horace_left1.png", (112, 65, 150) },
            { "Gerald_right.png", (390, 20, 130) }, { "Gerald_left.png", (105, 35, 125) }, { "Gerald_left1.png", (85, 20, 130) },
            { "Billy_right.png", (380, 60, 150) }, { "Billy_right1.png", (375, 60, 150) },
            { "Billy_left.png", (115, 60, 150) }, { "Billy_left1.png", (120, 60, 150) },
        };

        /// <summary>
        /// Converts each character frame's hand-measured head point (HeadPoints) into a HatPlacement on its
        /// CharacterData, in the Visual's local units (from that sprite's own pivot and pixels-per-unit, so jump frames
        /// imported larger still line up), and sets the per-side fallbacks from the idle frames.
        /// </summary>
        public static void ApplyHatPlacements(IEnumerable<CharacterData> characters)
        {
            foreach (var data in characters)
            {
                var set = data.spriteSet;
                if (set == null)
                {
                    continue;
                }

                var frames = new[] { set.idleRight, set.jumpRight, set.abilityRight, set.idleLeft, set.jumpLeft, set.abilityLeft, set.defeat }
                    .Concat(set.runRight ?? new Sprite[0]).Concat(set.runLeft ?? new Sprite[0])
                    .Where(f => f != null).Distinct();
                var placements = new List<HatPlacement>();
                foreach (var frame in frames)
                {
                    if (HeadPoints.TryGetValue(Path.GetFileName(AssetDatabase.GetAssetPath(frame)), out var point))
                    {
                        placements.Add(Placement(frame, point));
                    }
                }
                if (placements.Count == 0)
                {
                    Debug.LogWarning($"[Cosmetics] No head points for {data.characterType}'s frames; its hats keep the old placement.");
                    continue;
                }

                data.hatPlacements = placements.ToArray();
                var right = placements.FirstOrDefault(p => p.frame == set.idleRight);
                var left = placements.FirstOrDefault(p => p.frame == set.idleLeft);
                if (right.frame != null)
                {
                    data.hatAnchor = right.anchor;
                    data.hatWidth = right.width;
                }
                if (left.frame != null)
                {
                    data.hatAnchorLeft = left.anchor;
                }
                EditorUtility.SetDirty(data);
            }
        }

        private static HatPlacement Placement(Sprite frame, (int x, int y, int width) point)
        {
            // Source pixels to sprite-local units: the sprite is the whole texture (possibly imported smaller than the
            // file), pivot in rect pixels from bottom-left.
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(frame));
            importer.GetSourceTextureWidthAndHeight(out _, out int sourceHeight);
            float ppu = frame.pixelsPerUnit;
            float scale = frame.rect.height / sourceHeight;
            var pivot = frame.pivot;
            return new HatPlacement
            {
                frame = frame,
                anchor = new Vector2((point.x * scale - pivot.x) / ppu, ((sourceHeight - point.y) * scale - pivot.y) / ppu),
                width = point.width * scale / ppu,
            };
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
