using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using UnityEngine;

namespace FarmFuryStampede.Utilities
{
    /// <summary>
    /// Phase 1 verification harness. Attach anywhere in Game.unity and press Play; it exercises
    /// each manager's public API and logs pass/fail per check. Delete once Phase 2 introduces
    /// real gameplay-driven tests.
    /// </summary>
    public class Phase1Test : MonoBehaviour
    {
        [SerializeField] private AudioClip testSfxClip;

        private const string TestLevelId = "Phase1Test_Level";

        private void Start()
        {
            Debug.Log("===== Phase 1 Verification =====");

            RunDataManagerCheck();
            RunGameManagerCheck();
            RunSaveManagerCheck();
            RunAudioManagerCheck();

            Debug.Log("===== Phase 1 Verification Complete =====");
        }

        private void RunDataManagerCheck()
        {
            if (DataManager.Instance == null)
            {
                Debug.LogError("[Phase1Test] FAIL: DataManager.Instance is null.");
                return;
            }

            var cluck = DataManager.Instance.GetCharacterData(CharacterType.Cluck);
            var world = DataManager.Instance.GetWorldData(WorldType.MeadowRuins);

            Debug.Log(cluck != null
                ? $"[Phase1Test] PASS: CharacterData_Cluck loaded ({cluck.displayName}, ability={cluck.abilityType})."
                : "[Phase1Test] FAIL: CharacterData for Cluck not found. Assign it on the DataManager component.");

            Debug.Log(world != null
                ? $"[Phase1Test] PASS: WorldData_MeadowRuins loaded ({world.displayName})."
                : "[Phase1Test] FAIL: WorldData for MeadowRuins not found. Assign it on the DataManager component.");
        }

        private void RunGameManagerCheck()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[Phase1Test] FAIL: GameManager.Instance is null.");
                return;
            }

            LevelData level = DataManager.Instance != null
                ? DataManager.Instance.GetLevelData("MeadowRuins_01")
                : null;

            GameManager.Instance.StartLevel(level, CharacterType.Cluck);
            bool startedPlaying = GameManager.Instance.CurrentState == GameState.Playing;

            Debug.Log(startedPlaying
                ? "[Phase1Test] PASS: GameManager.StartLevel() set state to Playing."
                : "[Phase1Test] FAIL: GameManager.StartLevel() did not set state to Playing.");

            GameManager.Instance.EndLevel(completed: true, stars: 3);
            bool endedComplete = GameManager.Instance.CurrentState == GameState.LevelComplete;

            Debug.Log(endedComplete
                ? "[Phase1Test] PASS: GameManager.EndLevel(true, ...) set state to LevelComplete."
                : "[Phase1Test] FAIL: GameManager.EndLevel(true, ...) did not set state to LevelComplete.");

            GameManager.Instance.EndLevel(completed: false, stars: 0);
            bool endedFailed = GameManager.Instance.CurrentState == GameState.LevelFailed;

            Debug.Log(endedFailed
                ? "[Phase1Test] PASS: GameManager.EndLevel(false, ...) set state to LevelFailed."
                : "[Phase1Test] FAIL: GameManager.EndLevel(false, ...) did not set state to LevelFailed.");
        }

        private void RunSaveManagerCheck()
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogError("[Phase1Test] FAIL: SaveManager.Instance is null.");
                return;
            }

            const int testStars = 3;
            SaveManager.Instance.SetLevelStars(TestLevelId, testStars);
            SaveManager.Instance.SaveProgress();

            bool roundTripOk = SaveManager.Instance.GetLevelStars(TestLevelId) == testStars;

            Debug.Log(roundTripOk
                ? "[Phase1Test] PASS: SaveManager wrote and read back level stars correctly."
                : "[Phase1Test] FAIL: SaveManager level stars did not round-trip.");

            bool cluckUnlocked = SaveManager.Instance.IsCharacterUnlocked(CharacterType.Cluck);
            bool bessieUnlocked = SaveManager.Instance.IsCharacterUnlocked(CharacterType.Bessie);

            Debug.Log(cluckUnlocked && bessieUnlocked
                ? "[Phase1Test] PASS: Cluck and Bessie are both unlocked by default."
                : "[Phase1Test] FAIL: Cluck and Bessie should both be unlocked on a fresh save.");
        }

        private void RunAudioManagerCheck()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogError("[Phase1Test] FAIL: AudioManager.Instance is null.");
                return;
            }

            AudioManager.Instance.PlaySfx(null);
            Debug.Log("[Phase1Test] PASS: AudioManager.PlaySfx(null) did not throw.");

            if (testSfxClip == null)
            {
                Debug.LogWarning("[Phase1Test] SKIP: AudioManager.PlaySfx() with a real clip not tested — assign a Test Sfx Clip on Phase1Test.");
                return;
            }

            AudioManager.Instance.PlaySfx(testSfxClip);
            Debug.Log("[Phase1Test] PASS: AudioManager.PlaySfx() called without error.");
        }
    }
}
