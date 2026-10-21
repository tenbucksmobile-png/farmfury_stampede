using System;
using System.Collections.Generic;
using System.Linq;
using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Characters;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Core
{
    /// <summary>
    /// WEB DEMO (branch web-demo): Unity IAP is removed in this branch, so nothing can be bought for money - every
    /// purchase fails at once and the shop is not reachable. The coin and cosmetic helpers below are the main game's.
    ///
    /// Main game: real-money purchases through Unity IAP 5 (StoreController), ported from Farm Fury: Arcade's IAPManager with
    /// the same catalog, prices and grant rules (GDD Section 11): Remove Ads ($4.99, +100 bonus coins), four coin
    /// packs, hats and trails ($1.99, or 2500 coins), machines ($3.99) and purchase-gated worlds ($3.99). What
    /// Stampede changes: worlds are WorldTypes, and "the active character" (who a new hat is put on) is the one
    /// last played (GameManager.CurrentCharacter). Grants re-dress the player immediately via
    /// CharacterCosmeticRenderer.
    ///
    /// Store setup still to do (not code): create the Stampede products with these ids in App Store Connect and
    /// the Play Console, and link the Unity project to its Unity Gaming Services project. Until then the connect
    /// or fetch fails with a logged warning; in the Editor, Unity IAP's fake store completes purchases for testing.
    /// </summary>
    public class IAPManager : MonoSingleton<IAPManager>
    {
        public const string SombreroCosmeticId = "sombrero_hat";
        public const string ChefHatCosmeticId = "chef_hat";
        public const string CrownCosmeticId = "crown";
        public const int RemoveAdsBonusCoins = 100;
        public const int CosmeticCoinCost = 2500;

        private static readonly Dictionary<string, int> CoinPackAmounts = new()
        {
            { StoreProducts.CoinPacks[0], 100 },
            { StoreProducts.CoinPacks[1], 500 },
            { StoreProducts.CoinPacks[2], 5000 },
            { StoreProducts.CoinPacks[3], 15000 },
        };

        // Hats and trails can also be bought with coins (Arcade: 2500 each). Machines and worlds are IAP only.
        public static bool TryGetCoinCost(string productId, out int coinCost)
        {
            bool coinBuyable = StoreProducts.Hats.Contains(productId) || StoreProducts.Trails.Contains(productId);
            coinCost = coinBuyable ? CosmeticCoinCost : 0;
            return coinBuyable;
        }

        /// <summary>Shown until the store returns localized prices.</summary>
        private static readonly Dictionary<string, string> FallbackPrices = BuildFallbackPrices();

        private static Dictionary<string, string> BuildFallbackPrices()
        {
            var prices = new Dictionary<string, string>
            {
                { StoreProducts.RemoveAds, "$4.99" },
                { StoreProducts.CoinPacks[0], "$0.99" },
                { StoreProducts.CoinPacks[1], "$3.99" },
                { StoreProducts.CoinPacks[2], "$9.99" },
                { StoreProducts.CoinPacks[3], "$19.99" },
            };
            foreach (string id in StoreProducts.Hats.Concat(StoreProducts.Trails)) { prices[id] = "$1.99"; }
            foreach (string id in StoreProducts.Machines) { prices[id] = "$3.99"; }
            foreach (WorldType world in Enum.GetValues(typeof(WorldType))) { prices[StoreProducts.World(world)] = "$3.99"; }
            return prices;
        }

        public bool IsInitialized { get; private set; }

        public event Action<string> OnPurchaseSucceeded;
        public event Action<string, string> OnPurchaseFailed;
        public event Action OnProductsReady;

        /// <summary>
        /// Worlds sold in the shop: only those marked purchaseRequired. The six story worlds are all free (decided
        /// 2026-09-29, replacing the GDD's paid Worlds 4-6); the paid worlds will be new ones added after them.
        /// </summary>
        public static List<WorldType> PurchasableWorlds()
        {
            var worlds = DataManager.Instance != null ? DataManager.Instance.GetAllWorlds() : new List<WorldData>();
            return worlds.Where(w => w.purchaseRequired).Select(w => w.worldType).OrderBy(w => w).ToList();
        }

        public string GetPriceString(string productId)
        {
            return FallbackPrices.TryGetValue(productId, out var fallback) ? fallback : string.Empty;
        }

        public void PurchaseProduct(string productId)
        {
            OnPurchaseFailed?.Invoke(productId, "WebDemo");
        }

        public void RestorePurchases(Action<bool> callback)
        {
            callback?.Invoke(false);
        }

        private static bool TryGetWorld(string productId, out WorldType world)
        {
            foreach (WorldType candidate in Enum.GetValues(typeof(WorldType)))
            {
                if (StoreProducts.World(candidate) == productId)
                {
                    world = candidate;
                    return true;
                }
            }
            world = default;
            return false;
        }

        /// <summary>Hats and trails bought with coins instead of money. Refunds if the grant fails.</summary>
        public bool PurchaseProductWithCoins(string productId)
        {
            if (!TryGetCoinCost(productId, out int cost) || SaveManager.Instance == null || !SaveManager.Instance.SpendCoins(cost))
            {
                return false;
            }

            if (!GrantCosmeticEffect(productId))
            {
                SaveManager.Instance.AddCoins(cost);
                return false;
            }

            SaveManager.Instance.SaveProgress();
            OnPurchaseSucceeded?.Invoke(productId);
            return true;
        }

        // ------------------------------------------------------------ cosmetic grants (Arcade's rules)

        private static CharacterType ActiveCharacter =>
            GameManager.Instance != null ? GameManager.Instance.CurrentCharacter : CharacterType.Cluck;

        /// <summary>The cosmetic id a product grants for a character (caps and cowboy hats come per character).</summary>
        public static string CosmeticIdFor(string productId, CharacterType character)
        {
            string c = character.ToString().ToLowerInvariant();
            if (productId == StoreProducts.Hats[0]) { return SombreroCosmeticId; }
            if (productId == StoreProducts.Hats[1]) { return $"baseball_cap_{c}"; }
            if (productId == StoreProducts.Hats[2]) { return $"cowboy_hat_{c}"; }
            if (productId == StoreProducts.Hats[3]) { return ChefHatCosmeticId; }
            if (productId == StoreProducts.Hats[4]) { return CrownCosmeticId; }
            return productId;   // trails and machines use the product id as the cosmetic id
        }

        /// <summary>Whether a product is already owned (for the active character, for per-character hats).</summary>
        public static bool IsProductOwned(string productId)
        {
            var save = SaveManager.Instance;
            if (save == null)
            {
                return false;
            }
            if (productId == StoreProducts.RemoveAds)
            {
                return save.AdsRemoved;
            }
            if (TryGetWorld(productId, out var world))
            {
                return save.IsWorldPurchased(world);
            }
            if (CoinPackAmounts.ContainsKey(productId))
            {
                return false;
            }
            return save.IsCosmeticOwned(CosmeticIdFor(productId, ActiveCharacter));
        }

        private bool GrantCosmeticEffect(string productId)
        {
            if (productId == StoreProducts.Hats[1] || productId == StoreProducts.Hats[2])
            {
                // Baseball cap / cowboy hat: a set, one made for each character; the active one puts theirs on.
                foreach (CharacterType character in Enum.GetValues(typeof(CharacterType)))
                {
                    SaveManager.Instance?.SetCosmeticOwned(CosmeticIdFor(productId, character));
                }
                GrantAndEquip(CosmeticType.Hat, CosmeticIdFor(productId, ActiveCharacter), ActiveCharacter);
            }
            else if (StoreProducts.Hats.Contains(productId))
            {
                GrantAndEquip(CosmeticType.Hat, CosmeticIdFor(productId, ActiveCharacter), ActiveCharacter);
            }
            else if (StoreProducts.Trails.Contains(productId))
            {
                GrantAndEquip(CosmeticType.Trail, productId, ActiveCharacter);
            }
            else if (productId == StoreProducts.Machines[0])
            {
                GrantAndEquip(CosmeticType.Skin, productId, CharacterType.Cluck);
            }
            else if (productId == StoreProducts.Machines[1])
            {
                GrantAndEquip(CosmeticType.Skin, productId, CharacterType.Bessie);
            }
            else if (productId == StoreProducts.Machines[2])
            {
                GrantAndEquip(CosmeticType.Skin, productId, CharacterType.Horace);
            }
            else
            {
                return false;
            }
            return true;
        }

        private static void GrantAndEquip(CosmeticType type, string cosmeticId, CharacterType character)
        {
            var save = SaveManager.Instance;
            if (save == null)
            {
                return;
            }

            save.SetCosmeticOwned(cosmeticId);
            if (type == CosmeticType.Trail)
            {
                save.SetEquippedTrail(cosmeticId);
            }
            else
            {
                save.SetEquippedCosmetic(type, character, cosmeticId);
            }
            RefreshPlayerCosmetics();
        }

        /// <summary>Re-dresses the player (after a purchase or a Locker equip).</summary>
        public static void RefreshPlayerCosmetics()
        {
            var player = LevelLoader.Instance != null ? LevelLoader.Instance.Player : null;
            if (player != null)
            {
                var renderer = player.GetComponentInChildren<CharacterCosmeticRenderer>(true);
                if (renderer != null)
                {
                    renderer.Refresh();
                }
            }
        }

    }
}
