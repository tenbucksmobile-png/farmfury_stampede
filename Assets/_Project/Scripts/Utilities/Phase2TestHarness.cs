using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FarmFuryStampede.Utilities
{
    /// <summary>
    /// Phase 2 playtest harness: starts the test level as Cluck, press R to restart after a win/fall,
    /// and shows a small debug overlay (state, crops, grounded, coyote/buffer timers).
    /// Replaced by real flow/HUD in Phase 3+/5.
    /// </summary>
    public class Phase2TestHarness : MonoBehaviour
    {
        [SerializeField] private CharacterController2D player;
        [SerializeField] private string levelId = "MeadowRuins_01";

        private void Start()
        {
            Time.timeScale = 1f;
            LevelData level = DataManager.Instance != null ? DataManager.Instance.GetLevelData(levelId) : null;
            GameManager.Instance.StartLevel(level, CharacterType.Cluck);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null || player == null)
            {
                return;
            }

            int pooled = ObjectPool.Instance != null ? ObjectPool.Instance.PooledCount("Crop") : 0;
            GUI.Label(new Rect(10, 10, 520, 120),
                $"State: {gm.CurrentState}   (R = restart)\n" +
                $"Crops: {gm.RunState.cropsCollectedThisRun}   Pooled: {pooled}\n" +
                $"Grounded: {player.IsGrounded}   Vel: ({player.Velocity.x:F1}, {player.Velocity.y:F1})\n" +
                $"Coyote: {player.CoyoteTimeRemaining:F2}   Jump buffer: {player.JumpBufferRemaining:F2}");
        }
#endif
    }
}
