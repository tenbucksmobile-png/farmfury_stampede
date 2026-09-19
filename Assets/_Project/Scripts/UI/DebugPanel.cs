using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmFuryStampede.UI
{
    /// <summary>
    /// DEBUG-ONLY cheat panel (F1 toggles; editor / development builds). Fast-forwards progress so unlock rules,
    /// world gates and boss levels can be tested without clearing 40 levels or building Worlds 2-6.
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
            var rect = new Rect(Screen.width - 330, 90, 320, 420);
            GUI.Box(rect, $"DEBUG (world: {world})");
            float y = rect.y + 28;

            Row(ref y, rect.x, $"Complete {world} levels 1-8", () => save.DebugCompleteWorldLevels(world));
            Row(ref y, rect.x, $"Flip {world} boss cleared ({save.IsWorldBossCleared(world)})", () => save.SetWorldBossCleared(world, !save.IsWorldBossCleared(world)));
            Row(ref y, rect.x, "Unlock all worlds", () => { foreach (WorldType w in System.Enum.GetValues(typeof(WorldType))) save.SetWorldBossCleared(w, true); });
            Row(ref y, rect.x, "Levels cleared +5", () => save.DebugSetCompletedLevelCount(save.CompletedLevelCount + 5));
            Row(ref y, rect.x, "Levels cleared = 40", () => save.DebugSetCompletedLevelCount(40));
            Row(ref y, rect.x, "RESET SAVE", () => save.DebugResetProgress());
            GUI.Label(new Rect(rect.x + 10, y, 300, 40), $"Levels cleared: {save.CompletedLevelCount}");
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
