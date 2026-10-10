using System.Collections.Generic;
using FarmFuryStampede.Core;
using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Robots;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Loads levels from data: instantiates a LevelData's prefab into the CurrentLevel container, reads its
    /// marker objects, and spawns the pooled gameplay objects (crops, robots, checkpoints, goal) at each
    /// marker. UnloadLevel destroys the level instance and returns every spawned object to its pool.
    /// </summary>
    public class LevelLoader : MonoSingleton<LevelLoader>
    {
        [Header("Scene References")]
        [Tooltip("Parent for the loaded level prefab instance and spawned objects.")]
        [SerializeField] private Transform levelContainer;
        [SerializeField] private CharacterController2D player;
        [SerializeField] private CameraFollow2D cameraFollow;
        [SerializeField] private ParallaxBackground background;

        [Header("Pooled Prefabs (robots come from RobotData.prefab)")]
        [SerializeField] private GameObject cropPrefab;
        [SerializeField] private GameObject checkpointPrefab;
        [SerializeField] private GameObject goalPrefab;
        [SerializeField] private GameObject breakableWallPrefab;

        [SerializeField] private float respawnInvulnerability = 1.5f;
        [Tooltip("How long the defeat pose shows at the spot of death before the respawn.")]
        [SerializeField] private float defeatPoseSeconds = 0.6f;

        private Coroutine _respawnRoutine;

        private GameObject _levelInstance;
        private Transform _spawnedRoot;
        private readonly List<GameObject> _spawned = new();
        private readonly List<RobotSpawnPoint> _pendingWaveMarkers = new();

        public LevelData LoadedLevel { get; private set; }
        public int SpawnedCount => _spawned.Count;
        public bool IsLoaded => _levelInstance != null;
        public bool IsRespawning => _respawnRoutine != null;
        public CharacterController2D Player => player;

        /// <summary>Spawned objects currently active (not collected, defeated or pooled).</summary>
        public int ActiveSpawnedCount
        {
            get
            {
                int count = 0;
                foreach (var go in _spawned)
                {
                    if (go != null && go.activeInHierarchy)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            _spawnedRoot = new GameObject("Spawned").transform;
            _spawnedRoot.SetParent(levelContainer, false);
        }

        /// <summary>Loads the level and turns the player into the given character (fresh ability uses).</summary>
        public void LoadLevel(LevelData data, CharacterType character = CharacterType.Cluck)
        {
            UnloadLevel();

            if (data == null || data.levelPrefab == null)
            {
                Debug.LogError($"[LevelLoader] Cannot load '{data?.levelId}': no LevelData or levelPrefab assigned.");
                return;
            }

            player.gameObject.SetActive(true); // menus hide the player between attempts

            _levelInstance = Instantiate(data.levelPrefab, levelContainer);
            _levelInstance.name = data.levelId;
            LoadedLevel = data;
            AudioManager.PlayWorldMusic(data.worldType);
            Physics2D.SyncTransforms();

            var root = _levelInstance.GetComponent<LevelPrefabRoot>();
            var start = _levelInstance.GetComponentInChildren<PlayerStartPoint>();

            var run = GameManager.Instance.RunState;
            var save = SaveManager.Instance;
            int passageCoinIndex = 0;
            foreach (var marker in _levelInstance.GetComponentsInChildren<CropSpawnPoint>())
            {
                // Secret-passage coins pay once: one already taken (in any earlier attempt) isn't put back.
                int coinIndex = marker.passageCoin ? passageCoinIndex++ : -1;
                if (marker.passageCoin && save != null && save.IsPassageCoinCollected(data.levelId, coinIndex))
                {
                    continue;
                }

                var crop = Spawn(cropPrefab, marker.transform.position);
                if (crop != null)
                {
                    var pickup = crop.GetComponent<CropPickup>();
                    pickup.visualOverride = marker.visualOverride;
                    pickup.coinValue = marker.coinValue;
                    pickup.isSecretCluster = marker.secretCluster;
                    pickup.isPassageCoin = marker.passageCoin;
                    pickup.passageCoinIndex = coinIndex;
                }

                if (marker.passageCoin) { continue; }   // passage coins pay coins, they aren't crops
                if (marker.secretCluster) { run.totalSecretCrops++; } else { run.totalNormalCrops++; }
            }

            foreach (var marker in _levelInstance.GetComponentsInChildren<CheckpointMarker>())
            {
                var checkpoint = Spawn(checkpointPrefab, marker.transform.position);
                if (checkpoint != null && checkpoint.TryGetComponent(out Checkpoint cp)) { cp.SetWorldArt(marker.worldArt); }
            }

            foreach (var marker in _levelInstance.GetComponentsInChildren<GoalMarker>())
            {
                Spawn(goalPrefab, marker.transform.position);
            }

            foreach (var marker in _levelInstance.GetComponentsInChildren<BreakableWallMarker>())
            {
                Spawn(breakableWallPrefab, marker.transform.position);
            }

            int robots = 0;
            foreach (var marker in _levelInstance.GetComponentsInChildren<RobotSpawnPoint>())
            {
                if (marker.wave > 0)
                {
                    _pendingWaveMarkers.Add(marker);   // boss reinforcements: spawned later by SpawnWave
                }
                else if (SpawnRobot(marker))
                {
                    robots++;
                }
            }

            if (start == null)
            {
                Debug.LogError($"[LevelLoader] Level '{data.levelId}' has no PlayerStartPoint.");
            }
            else
            {
                GameManager.Instance.RunState.SetLevelStart(start.transform.position);
                player.SetCharacter(character);
                PlacePlayer(start.transform.position);
            }

            player.SetUnderwater(root != null && root.underwater);   // every load sets it, so it never carries over

            if (root != null)
            {
                _levelMinX = root.cameraMinX;
                _levelMaxX = root.cameraMaxX;
            }

            if (cameraFollow != null && root != null)
            {
                cameraFollow.SetBounds(root.cameraMinX, root.cameraMaxX);
                cameraFollow.SnapToTarget();
                background?.Configure(root.cameraMinX, root.cameraMaxX, root.parallaxLayers);
            }

            Debug.Log($"[LevelLoader] Loaded '{data.levelId}': {_spawned.Count} spawned objects ({robots} robots).");
        }

        /// <summary>Destroys the level instance and returns everything spawned for it to the pool.</summary>
        public void UnloadLevel()
        {
            if (_respawnRoutine != null)
            {
                StopCoroutine(_respawnRoutine);
                _respawnRoutine = null;
            }
            CancelPassageTravel();

            foreach (var go in _spawned)
            {
                if (go != null && ObjectPool.Instance != null)
                {
                    ObjectPool.Instance.Release(go);
                }
            }
            _spawned.Clear();
            _pendingWaveMarkers.Clear();

            // Ability-spawned objects belong to the attempt too: nothing may leak into the next load.
            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.ReleaseAll("CloudPlatform");
                ObjectPool.Instance.ReleaseAll("Horseshoe");
            }

            if (_levelInstance != null)
            {
                // Deactivate first so its colliders vanish immediately; Destroy only takes effect at end of frame.
                _levelInstance.SetActive(false);
                Destroy(_levelInstance);
                _levelInstance = null;
            }

            LoadedLevel = null;
        }

        /// <summary>Spawns the reinforcement robots marked with this wave number (called by the boss as it takes hits).</summary>
        public void SpawnWave(int wave)
        {
            int spawned = 0;
            foreach (var marker in _pendingWaveMarkers.ToArray())
            {
                if (marker != null && marker.wave == wave)
                {
                    _pendingWaveMarkers.Remove(marker);
                    if (SpawnRobot(marker))
                    {
                        spawned++;
                    }
                }
            }

            Debug.Log($"[LevelLoader] Wave {wave}: spawned {spawned} robots.");
        }

        /// <summary>Defeats every robot in the level and cancels the waves still to come (the boss has fallen).</summary>
        public void DefeatAllRobots()
        {
            _pendingWaveMarkers.Clear();
            foreach (var go in _spawned.ToArray())
            {
                if (go != null && go.activeInHierarchy && go.TryGetComponent(out RobotController robot))
                {
                    robot.ForceDefeat();
                }
            }
        }

        /// <summary>Moves the player to the given respawn point and grants brief invulnerability.</summary>
        public void RespawnPlayer(Vector2 position)
        {
            if (_respawnRoutine == null)
            {
                _respawnRoutine = StartCoroutine(RespawnAfterDefeatPose(position));
            }
        }

        private System.Collections.IEnumerator RespawnAfterDefeatPose(Vector2 position)
        {
            CancelPassageTravel();
            player.BeginDeath(defeatPoseSeconds);
            yield return new WaitForSeconds(defeatPoseSeconds);
            _respawnRoutine = null;
            RestoreLevelCamera();   // respawn points are always up on the level, never in a passage room
            PlacePlayer(position);
            player.GrantInvulnerability(respawnInvulnerability);
        }

        /// <summary>Freezes the player in the defeat pose for good (the level is over). Used when the last life is lost.</summary>
        public void FreezePlayerDefeated()
        {
            player.BeginDeath(60f);
        }

        // ------------------------------------------------------------ secret passages

        private const float PassageFadeSeconds = 0.25f;
        private float _levelMinX, _levelMaxX;
        private Coroutine _passageRoutine;
        private SpriteRenderer _fade;

        /// <summary>True during a secret-passage transition (the fade to black and back).</summary>
        public bool IsTravelling => _passageRoutine != null;

        /// <summary>
        /// A SecretPassageDoor was walked into: fade to black, move the player to the other end, set the camera's
        /// limits for where they arrive (the room, or the level again), fade back in. Ignored mid-death, mid-transition
        /// and outside play.
        /// </summary>
        public void TravelThroughPassage(SecretPassageDoor door)
        {
            if (door == null || _passageRoutine != null || _respawnRoutine != null || player.IsDying
                || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            _passageRoutine = StartCoroutine(PassageTransition(door.destination, door.returnsToLevel, door.cameraMinX, door.cameraMaxX, door.entrance));
        }

        private System.Collections.IEnumerator PassageTransition(Vector2 destination, bool toLevel, float minX, float maxX, SecretPassageDoor returnTo)
        {
            var fade = FadeRenderer();
            for (float t = 0f; t < PassageFadeSeconds; t += Time.deltaTime)
            {
                SetFade(fade, t / PassageFadeSeconds);
                yield return null;
            }
            SetFade(fade, 1f);

            if (toLevel)
            {
                RestoreLevelCamera();
                if (returnTo != null) { returnTo.HoldUntilLeft(); }   // back up on the entrance: don't drop straight back in
            }
            else if (cameraFollow != null)
            {
                cameraFollow.SetBounds(minX, maxX);
            }
            PlacePlayer(destination);

            for (float t = 0f; t < PassageFadeSeconds; t += Time.deltaTime)
            {
                SetFade(fade, 1f - t / PassageFadeSeconds);
                yield return null;
            }
            SetFade(fade, 0f);
            _passageRoutine = null;
        }

        private void CancelPassageTravel()
        {
            if (_passageRoutine != null)
            {
                StopCoroutine(_passageRoutine);
                _passageRoutine = null;
            }
            if (_fade != null)
            {
                SetFade(_fade, 0f);
            }
        }

        private void RestoreLevelCamera()
        {
            if (cameraFollow != null && _levelMaxX > _levelMinX)
            {
                cameraFollow.SetBounds(_levelMinX, _levelMaxX);
            }
        }

        // A black square in front of everything the camera draws, parented to the camera (made on first use).
        private SpriteRenderer FadeRenderer()
        {
            if (_fade != null || cameraFollow == null)
            {
                return _fade;
            }

            var go = new GameObject("PassageFade");
            go.transform.SetParent(cameraFollow.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 1f);
            go.transform.localScale = new Vector3(200f, 200f, 1f);
            _fade = go.AddComponent<SpriteRenderer>();
            var texture = Texture2D.whiteTexture;
            _fade.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
            _fade.sortingOrder = short.MaxValue;
            SetFade(_fade, 0f);
            return _fade;
        }

        private static void SetFade(SpriteRenderer fade, float alpha)
        {
            if (fade == null)
            {
                return;
            }
            fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
            fade.enabled = alpha > 0f;
        }

        private void PlacePlayer(Vector2 position)
        {
            player.Teleport(position);
            if (cameraFollow != null)
            {
                cameraFollow.SnapToTarget();
            }
        }

        private GameObject Spawn(GameObject prefab, Vector3 position)
        {
            if (prefab == null)
            {
                Debug.LogError("[LevelLoader] A pooled prefab reference is missing on LevelLoader.");
                return null;
            }

            var instance = ObjectPool.Instance.Get(prefab, position, _spawnedRoot);
            _spawned.Add(instance);
            return instance;
        }

        private bool SpawnRobot(RobotSpawnPoint marker)
        {
            var robotData = DataManager.Instance.GetRobotData(marker.robotType);
            if (robotData == null || robotData.prefab == null)
            {
                Debug.LogError($"[LevelLoader] No RobotData/prefab for {marker.robotType}; skipping spawn at {marker.transform.position}.");
                return false;
            }

            var instance = Spawn(robotData.prefab, marker.transform.position);
            if (instance == null)
            {
                return false;
            }

            var robot = instance.GetComponent<RobotController>();
            robot.SetWorldArt(marker.artRight, marker.artLeft, marker.artDefeat, marker.artScale);   // this world's look (or the prefab's)
            robot.Initialize(marker.patrolDistance);
            return true;
        }
    }
}
