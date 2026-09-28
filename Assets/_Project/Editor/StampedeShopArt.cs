using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Imports the Settings / Shop screen art and fills GameFlow's ShopArt (see FarmFuryStampede.UI.ShopArt).
    /// Most files were dropped into Sprites/UI; the ones Stampede didn't have (the Cash icon, the world price sign,
    /// the cosmetics banners and price plaques, the owned badge) were copied from Farm Fury: Arcade into
    /// Sprites/UI/Shop. Paths are relative to Sprites/UI. A missing file leaves its slot null, which the screens
    /// draw as a plain labelled button.
    /// </summary>
    public static class StampedeShopArt
    {
        private const string UIDir = "Assets/_Project/Sprites/UI";
        private const string PlaqueFile = "Btn_plaque.png";
        // Arcade's 9-slice border for the 485x256 plaque (left, bottom, right, top), keeping the rounded ends.
        private static readonly Vector4 PlaqueBorder = new(90f, 70f, 90f, 70f);

        private static readonly Dictionary<string, string> Singles = new()
        {
            { "settingsSign", "SettingsSign.png" },
            { "shopSign", "ShopBanner.png" },
            { "merchBanner", "MerchBanner.png" },
            { "legalSign", "Legal.png" },
            { "leaderboardSign", "Leaderboard.png" },
            { "musicIcon", "Btn_music-remove.png" },
            { "leaderboardIcon", "Btn_LeaderBoard.png" },
            { "characterStoryIcon", "Btn_CharacterStory.png" },
            { "policiesIcon", "Policies.png" },
            { "cashIcon", "Shop/Shop.png" },
            { "worldsIcon", "WorldMaze.png" },
            { "removeAdsIcon", "Ads.png" },
            { "cosmeticsIcon", "Cosmetics_Icon.png" },
            { "plaque", PlaqueFile },
            { "worldPrice", "Shop/3.99.png" },
            { "hatsBanner", "Shop/Hats&Caps.png" },
            { "trailsBanner", "Shop/Trails.png" },
            { "machinesBanner", "Shop/machine.png" },
            { "ownedBadge", "Shop/EquippedBadge_Icon.png" },
            { "purchaseComplete", "PurchaseComplete.png" },
            { "coinIcon", "Shop/Coin_UI.png" },
            { "yesButton", "Shop/Yes.png" },
            { "noButton", "Shop/No.png" },
            { "revivePanel", "Shop/Revive Prompt panel background.png" },
            { "watchAd", "WatchAd.png" },
            { "doubleCoins", "DoubleCoins.png" },
            { "useCoinsSign", "UseCoins.png" },
            { "lockerIcon", "Locker.png" },
            { "lockerBanner", "LockerBanner.png" },
            { "lockerSuggestion", "LockerAd.png" },
            { "cardFrame", "Shop/PurchaseCardFrame.png" },
            { "moveLeftButton", "left.png" },
            { "moveRightButton", "right.png" },
            { "jumpButton", "up.png" },
        };

        // Array order matches StoreProducts (CoinPacks, Hats, Trails, Machines, Cosmetics) and CharacterType.
        private static readonly Dictionary<string, string[]> Arrays = new()
        {
            { "coinPacks", new[] { "100.png", "500.png", "5000.png", "15000.png" } },
            { "hatItems", new[] { "Shop/sombrero_price.png", "Shop/baseball_price.png", "Shop/cowboy_price.png", "Shop/ChefHat_price.png", "Crown_price.png" } },
            { "trailItems", new[] { "Shop/RainbowRibbon_price.png", "Shop/SparkleDust_Price.png", "Shop/CornHusk_price.png",
                "Shop/EmberTrail_price.png", "Shop/Confetti_price.png", "Shop/Bubble_price.png" } },
            { "machineItems", new[] { "Shop/Price_Clucky_truck.png", "Shop/Price_Bessie_truck.png", "Shop/Price_Horace_truck.png" } },
            { "abilityIcons", new[] { "Cluck_ability.png", "Bessie_ability.png", "Percy_ability.png", "Woolly_ability.png",
                "Ducky_ability.png", "Horace_ability.png", "Gerald_ability.png", "Billy_ability.png" } },
            { "cosmeticIcons", new[] { "Sombrero.png", "BaseballHat.png", "CowboyHat.png", "ChefHat.png", "CrownHat.png",
                "Ribbon.png", "SparkleDust.png", "CornHusk.png", "Ember.png", "Confetti.png", "bubbles.png",
                "CluckyTruck.png", "BessieTruck.png", "HoraceTruck.png" } },
        };

        public static void ImportShopArt()
        {
            foreach (string file in AllFiles())
            {
                Import(file);
            }
        }

        /// <summary>Assigns every sprite to the ShopArt field at <paramref name="property"/> (e.g. "shopArt").</summary>
        public static void Wire(SerializedObject so, string property)
        {
            foreach (var (field, file) in Singles)
            {
                so.FindProperty($"{property}.{field}").objectReferenceValue = Load(file);
            }

            foreach (var (field, files) in Arrays)
            {
                var array = so.FindProperty($"{property}.{field}");
                array.arraySize = files.Length;
                for (int i = 0; i < files.Length; i++)
                {
                    array.GetArrayElementAtIndex(i).objectReferenceValue = Load(files[i]);
                }
            }
        }

        private static IEnumerable<string> AllFiles()
        {
            foreach (string file in Singles.Values) { yield return file; }
            foreach (string[] files in Arrays.Values)
            {
                foreach (string file in files) { yield return file; }
            }
        }

        private static Sprite Load(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UIDir}/{file}");

        private static void Import(string file)
        {
            string path = $"{UIDir}/{file}";
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[ShopArt] Missing art {path}; that slot keeps its plain look.");
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 100f;
            StampedeUIArt.SetPivot(importer, new Vector2(0.5f, 0.5f));
            if (file == PlaqueFile)
            {
                // A sliced Image needs the border and a full-rect mesh.
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.spriteBorder = PlaqueBorder;
            }
            importer.SaveAndReimport();
        }
    }
}
