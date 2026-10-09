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

            var save = SaveManager.Instance;
            var world = GameFlow.Instance.CurrentWorld;
            var rect = new Rect(Screen.width - 330, 90, 320, 712);
            GUI.Box(rect, $"DEBUG (world: {world})");
            float y = rect.y + 28;

            Row(ref y, rect.x, $"Complete {world} levels 1-8", () => save.DebugCompleteWorldLevels(world));
            Row(ref y, rect.x, $"Flip {world} boss cleared ({save.IsWorldBossCleared(world)})", () => save.SetWorldBossCleared(world, !save.IsWorldBossCleared(world)));
            Row(ref y, rect.x, "Unlock all worlds", () => { foreach (WorldType w in System.Enum.GetValues(typeof(WorldType))) save.SetWorldBossCleared(w, true); });
            Row(ref y, rect.x, "Levels cleared +5", () => save.DebugSetCompletedLevelCount(save.CompletedLevelCount + 5));
            Row(ref y, rect.x, "Levels cleared = 40", () => save.DebugSetCompletedLevelCount(40));
            Row(ref y, rect.x, $"Coins +1000 ({save.CoinBalance})", () => save.DebugAddCoins(1000));
            Row(ref y, rect.x, $"Rare pellet +1 ({save.RarePelletCount})", () => save.DebugAddRarePellet());
            Row(ref y, rect.x, "RESET SAVE", () => save.DebugResetProgress());
            GUI.Label(new Rect(rect.x + 10, y, 300, 22), $"Levels cleared: {save.CompletedLevelCount}");
            y += 26;

            CosmeticRows(ref y, rect.x, save);
        }

        // ------------------------------------------------------------ cosmetics

        private static void CosmeticRows(ref float y, float x, SaveManager save)
        {
            var dm = DataManager.Instance;
            if (dm == null)
            {
                return;
            }

            var all = dm.GetAllCosmetics();
            var player = LevelLoader.Instance != null ? LevelLoader.Instance.Player : null;
            CharacterType who = player != null && player.Data != null ? player.Data.characterType : save.LastCharacter;
            GUI.Label(new Rect(x + 10, y, 300, 22), $"COSMETICS - {who}");
            y += 24;

            var hats = all.Where(c => c.cosmeticType == CosmeticType.Hat && c.FitsCharacter(who)).ToList();
            string hat = save.GetEquippedCosmetic(CosmeticType.Hat, who);
            Row(ref y, x, $"Hat: {NameOf(dm, hat)}  >", () =>
            {
                var next = Next(hats, hat);
                save.SetEquippedCosmetic(CosmeticType.Skin, who, string.Empty);   // a machine hides the hat
                Wear(save, next, id => save.SetEquippedCosmetic(CosmeticType.Hat, who, id));
            });

            var machines = all.Where(c => c.cosmeticType == CosmeticType.Skin && c.FitsCharacter(who)).ToList();
            string machine = save.GetEquippedCosmetic(CosmeticType.Skin, who);
            Row(ref y, x, machines.Count == 0 ? $"Machine: none made for {who}" : $"Machine: {NameOf(dm, machine)}  >", () =>
            {
                if (machines.Count > 0)
                {
                    Wear(save, Next(machines, machine), id => save.SetEquippedCosmetic(CosmeticType.Skin, who, id));
                }
            });

            var trails = all.Where(c => c.cosmeticType == CosmeticType.Trail).ToList();
            string trail = save.GetEquippedTrail();
            Row(ref y, x, $"Trail: {NameOf(dm, trail)}  >", () => Wear(save, Next(trails, trail), save.SetEquippedTrail));

            Row(ref y, x, $"Own all cosmetics ({all.Count(c => save.IsCosmeticOwned(c.cosmeticId))}/{all.Count})", () =>
            {
                foreach (var item in all) save.DebugSetCosmeticOwned(item, true);
            });
            Row(ref y, x, "Remove all cosmetics", () =>
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

        private static void Row(ref float y, float x, string label, System.Action action)
        {
            if (GUI.Button(new Rect(x + 10, y, 300, 30), label))
            {
                action();
                if (GameFlow.Instance != null)
                {
                    GameFlow.Instance.RefreshCurrentScreen();
                }
            }
            y += 36;
        }
#endif
    }
}
