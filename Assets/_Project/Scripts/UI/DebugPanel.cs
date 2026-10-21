using FarmFuryStampede.Core;
using System.Collections.Generic;
using System.Linq;
using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// DEBUG-ONLY cheat panel (F1 toggles; editor / development builds). Fast-forwards progress so unlock rules,
    /// world gates and boss levels can be tested without clearing 40 levels or building Worlds 2-6. The COSMETICS
    /// rows dress whoever is being played (in a level; else the character the next level starts as): each press
    /// moves to the next hat / machine / trail that fits, granting it first, so every item can be seen in play
    /// without buying it; "Own all" / "Remove all" set up the Locker and the shop pages.
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        private bool _visible;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                _visible = !_visible;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!_visible || SaveManager.Instance == null || GameFlow.Instance == null)
            {
                GUI.Label(new Rect(Screen.width - 150, Screen.height - 24, 150, 20), "F1: debug panel");
                return;
            }

            var rect = new Rect(Screen.width - 330, 90, 320, 784);
            GUI.Box(rect, $"DEBUG (world: {GameFlow.Instance.CurrentWorld})");
            float y = rect.y + 28;
            DrawRows(
                (text, action) => { if (GUI.Button(new Rect(rect.x + 10, y, 300, 30), text)) { Run(action); } y += 36; },
                text => { GUI.Label(new Rect(rect.x + 10, y, 300, 22), text); y += 24; });
        }

        /// <summary>
        /// Every debug control, shared by the F1 overlay and the editor's Test Panel tab (Farm Fury Stampede > Debug >
        /// Test Panel): row(text, action) draws a button, label(text) a heading. Needs Play mode (the save and data
        /// managers). Each action is run through <see cref="Run"/> by the caller so the current screen refreshes.
        /// </summary>
        public static void DrawRows(System.Action<string, System.Action> row, System.Action<string> label)
        {
            var save = SaveManager.Instance;
            if (save == null || GameFlow.Instance == null) { return; }
            var world = GameFlow.Instance.CurrentWorld;

            row($"Complete {world} levels 1-8", () => save.DebugCompleteWorldLevels(world));
            row($"Flip {world} boss cleared ({save.IsWorldBossCleared(world)})", () => save.SetWorldBossCleared(world, !save.IsWorldBossCleared(world)));
            row("Unlock all worlds", () => { foreach (WorldType w in System.Enum.GetValues(typeof(WorldType))) save.SetWorldBossCleared(w, true); });
            // Paid worlds: all bought <-> none bought, to test the world shop's purchase again.
            var paid = System.Enum.GetValues(typeof(WorldType)).Cast<WorldType>()
                .Where(w => DataManager.Instance != null && DataManager.Instance.GetWorldData(w) is { purchaseRequired: true }).ToList();
            bool allBought = paid.Count > 0 && paid.All(save.IsWorldPurchased);
            row(allBought ? "Paid worlds: BOUGHT (tap to re-lock)" : "Paid worlds: locked (tap to unlock)", () =>
            {
                foreach (var w in paid) { save.DebugSetWorldPurchased(w, !allBought); }
            });
            row("Levels cleared +5", () => save.DebugSetCompletedLevelCount(save.CompletedLevelCount + 5));
            row("Levels cleared = 40", () => save.DebugSetCompletedLevelCount(40));
            row($"Coins +1000 ({save.CoinBalance})", () => save.DebugAddCoins(1000));
            row($"Rare pellet +1 ({save.RarePelletCount})", () => save.DebugAddRarePellet());
            row($"Cherry power ({CherryPickup.PowerSeconds:0}s)", () =>
            {
                var p = LevelLoader.Instance != null ? LevelLoader.Instance.Player : null;
                if (p != null) { p.StartPowerUp(CherryPickup.PowerSeconds); }
            });
            row("RESET SAVE", () => save.DebugResetProgress());
            label($"Levels cleared: {save.CompletedLevelCount}");

            CosmeticRows(row, label, save);
        }

        // ------------------------------------------------------------ cosmetics

        private static void CosmeticRows(System.Action<string, System.Action> row, System.Action<string> label, SaveManager save)
        {
            var dm = DataManager.Instance;
            if (dm == null)
            {
                return;
            }

            var all = dm.GetAllCosmetics();
            var player = LevelLoader.Instance != null ? LevelLoader.Instance.Player : null;
            CharacterType who = player != null && player.Data != null ? player.Data.characterType : save.LastCharacter;
            label($"COSMETICS - {who}");

            var hats = all.Where(c => c.cosmeticType == CosmeticType.Hat && c.FitsCharacter(who)).ToList();
            string hat = save.GetEquippedCosmetic(CosmeticType.Hat, who);
            row($"Hat: {NameOf(dm, hat)}  >", () =>
            {
                var next = Next(hats, hat);
                save.SetEquippedCosmetic(CosmeticType.Skin, who, string.Empty);   // a machine hides the hat
                Wear(save, next, id => save.SetEquippedCosmetic(CosmeticType.Hat, who, id));
            });

            var machines = all.Where(c => c.cosmeticType == CosmeticType.Skin && c.FitsCharacter(who)).ToList();
            string machine = save.GetEquippedCosmetic(CosmeticType.Skin, who);
            row(machines.Count == 0 ? $"Machine: none made for {who}" : $"Machine: {NameOf(dm, machine)}  >", () =>
            {
                if (machines.Count > 0)
                {
                    Wear(save, Next(machines, machine), id => save.SetEquippedCosmetic(CosmeticType.Skin, who, id));
                }
            });

            var trails = all.Where(c => c.cosmeticType == CosmeticType.Trail).ToList();
            string trail = save.GetEquippedTrail();
            row($"Trail: {NameOf(dm, trail)}  >", () => Wear(save, Next(trails, trail), save.SetEquippedTrail));

            row($"Own all cosmetics ({all.Count(c => save.IsCosmeticOwned(c.cosmeticId))}/{all.Count})", () =>
            {
                foreach (var item in all) save.DebugSetCosmeticOwned(item, true);
            });
            row("Remove all cosmetics", () =>
            {
                foreach (var item in all) save.DebugSetCosmeticOwned(item, false);
            });
        }

        // The item after 'currentId' in the list, then "none" (null), then the first again.
        private static CosmeticData Next(List<CosmeticData> items, string currentId)
        {
            int index = items.FindIndex(c => c.cosmeticId == currentId);
            return index + 1 < items.Count ? items[index + 1] : null;
        }

        private static void Wear(SaveManager save, CosmeticData item, System.Action<string> equip)
        {
            if (item != null)
            {
                save.DebugSetCosmeticOwned(item, true);
            }
            equip(item != null ? item.cosmeticId : string.Empty);
            IAPManager.RefreshPlayerCosmetics();
        }

        private static string NameOf(DataManager dm, string id)
        {
            var item = dm.GetCosmeticData(id);
            return item != null ? (string.IsNullOrEmpty(item.displayName) ? item.cosmeticId : item.displayName) : "none";
        }

        /// <summary>Runs a debug action, then refreshes whatever screen is showing.</summary>
        public static void Run(System.Action action)
        {
            action();
            if (GameFlow.Instance != null)
            {
                GameFlow.Instance.RefreshCurrentScreen();
            }
        }
#endif
    }
}
