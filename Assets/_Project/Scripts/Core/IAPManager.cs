using System;
using System.Collections.Generic;
using System.Linq;
using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Characters;
using FarmFuryStampede.Utilities;
using UnityEngine;
using UnityEngine.Purchasing;

namespace FarmFuryStampede.Core
{
    /// <summary>
    /// Real-money purchases through Unity IAP 5 (StoreController), ported from Farm Fury: Arcade's IAPManager with
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

        private StoreController _storeController;
        private readonly Dictionary<string, Product> _products = new();

        private async void Start()
        {
            _storeController = UnityIAPServices.StoreController();
            _storeController.OnStoreConnected += HandleStoreConnected;
            _storeController.OnStoreDisconnected += HandleStoreDisconnected;
            _storeController.OnProductsFetched += HandleProductsFetched;
            _storeController.OnProductsFetchFailed += HandleProductsFetchFailed;
            _storeController.OnPurchasePending += HandlePurchasePending;
            _storeController.OnPurchaseFailed += HandlePurchaseFailed;

            try
            {
                await _storeController.Connect();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[IAPManager] Store connect failed (expected until the Stampede products exist in App Store Connect / Play Console): {e.Message}");
            }
        }

        private void HandleStoreConnected()
        {
            var definitions = new List<ProductDefinition> { new(StoreProducts.RemoveAds, ProductType.NonConsumable) };
            definitions.AddRange(StoreProducts.CoinPacks.Select(id => new ProductDefinition(id, ProductType.Consumable)));
            definitions.AddRange(StoreProducts.Hats.Concat(StoreProducts.Trails).Concat(StoreProducts.Machines)
                .Select(id => new ProductDefinition(id, ProductType.NonConsumable)));
            definitions.AddRange(PurchasableWorlds().Select(w => new ProductDefinition(StoreProducts.World(w), ProductType.NonConsumable)));
            _storeController.FetchProducts(definitions);
        }

        /// <summary>Worlds sold in the shop: those marked purchaseRequired, else (until that's decided) the last three.</summary>
        public static List<WorldType> PurchasableWorlds()
        {
            var worlds = DataManager.Instance != null ? DataManager.Instance.GetAllWorlds() : new List<WorldData>();
            var gated = worlds.Where(w => w.purchaseRequired).Select(w => w.worldType).ToList();
            if (gated.Count == 0)
            {
                gated = worlds.Select(w => w.worldType).OrderBy(w => w).Skip(Mathf.Max(0, worlds.Count - 3)).ToList();
            }
            return gated;
        }

        private void HandleStoreDisconnected(StoreConnectionFailureDescription description)
        {
            Debug.LogWarning($"[IAPManager] Store disconnected: {description.message}");
        }

        private void HandleProductsFetched(List<Product> products)
        {
            foreach (var product in products)
            {
                _products[product.definition.id] = product;
            }
            IsInitialized = true;
            OnProductsReady?.Invoke();
        }

        private void HandleProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogWarning($"[IAPManager] Product fetch failed for {failure.FailedFetchProducts.Count} products: {failure.FailureReason}");
        }

        public string GetPriceString(string productId)
        {
            if (_products.TryGetValue(productId, out var product) && product.metadata != null &&
                !string.IsNullOrEmpty(product.metadata.localizedPriceString))
            {
                return product.metadata.localizedPriceString;
            }
            return FallbackPrices.TryGetValue(productId, out var fallback) ? fallback : string.Empty;
        }

        public void PurchaseProduct(string productId)
        {
            if (_storeController == null)
            {
                Debug.LogWarning("[IAPManager] Store not connected - can't purchase yet.");
                OnPurchaseFailed?.Invoke(productId, "StoreNotConnected");
                return;
            }

            try
            {
                if (_products.TryGetValue(productId, out var product))
                {
                    _storeController.PurchaseProduct(product);
                }
                else
                {
                    _storeController.PurchaseProduct(productId);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[IAPManager] PurchaseProduct({productId}) threw: {e}");
                OnPurchaseFailed?.Invoke(productId, e.Message);
            }
        }

        public void RestorePurchases(Action<bool> callback)
        {
            if (_storeController == null)
            {
                callback?.Invoke(false);
                return;
            }

            try
            {
                _storeController.RestoreTransactions((success, error) =>
                {
                    if (!success && !string.IsNullOrEmpty(error))
                    {
                        Debug.LogWarning($"[IAPManager] Restore failed: {error}");
                    }
                    callback?.Invoke(success);
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[IAPManager] RestoreTransactions threw: {e}");
                callback?.Invoke(false);
            }
        }

        private void HandlePurchasePending(PendingOrder order)
        {
            try
            {
                HandlePurchasePendingInner(order);
            }
            catch (Exception e)
            {
                Debug.LogError($"[IAPManager] HandlePurchasePending threw: {e}");
            }
        }

        private void HandlePurchasePendingInner(PendingOrder order)
        {
            var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
            if (product == null)
            {
                Debug.LogWarning("[IAPManager] Could not find product in pending order.");
                return;
            }

            string productId = product.definition.id;
            var save = SaveManager.Instance;

            if (productId == StoreProducts.RemoveAds)
            {
                if (save != null)
                {
                    save.AdsRemoved = true;
                    save.AddCoins(RemoveAdsBonusCoins);
                    save.SaveProgress();
                }
                AdManager.Instance?.HideBanner();
            }
            else if (CoinPackAmounts.TryGetValue(productId, out int amount))
            {
                save?.AddCoins(amount);
                save?.SaveProgress();
            }
            else if (TryGetWorld(productId, out var world))
            {
                save?.SetWorldPurchased(world);
            }
            else if (!GrantCosmeticEffect(productId))
            {
                Debug.LogWarning($"[IAPManager] Unrecognized product id in pending order: {productId}");
            }

            _storeController.ConfirmPurchase(order);
            AnalyticsManager.Instance?.LogPurchase(productId, GetPriceString(productId));
            OnPurchaseSucceeded?.Invoke(productId);
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

        private void HandlePurchaseFailed(FailedOrder order)
        {
            var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
            string productId = product?.definition.id ?? "unknown";
            Debug.LogWarning($"[IAPManager] Purchase failed - Product: '{productId}', Reason: {order.FailureReason}, Details: {order.Details}");
            OnPurchaseFailed?.Invoke(productId, order.FailureReason.ToString());
        }
    }
}
