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

        [Header("Pooled Prefabs (robots come from RobotData.prefab)")]
        [SerializeField] private GameObject cropPrefab;
        [SerializeField] private GameObject checkpointPrefab;
        [SerializeField] private GameObject goalPrefab;
        [SerializeField] private GameObject breakableWallPrefab;

        [SerializeField] private float respawnInvulnerability = 1.5f;

        private GameObject _levelInstance;
        private Transform _spawnedRoot;
        private readonly List<GameObject> _spawned = new();
        private readonly List<RobotSpawnPoint> _pendingWaveMarkers = new();

        public LevelData LoadedLevel { get; private set; }
        public int SpawnedCount => _spawned.Count;
        public bool IsLoaded => _levelInstance != null;
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
            Physics2D.SyncTransforms();

            var root = _levelInstance.GetComponent<LevelPrefabRoot>();
            var start = _levelInstance.GetComponentInChildren<PlayerStartPoint>();

            var run = GameManager.Instance.RunState;
            foreach (var marker in _levelInstance.GetComponentsInChildren<CropSpawnPoint>())
            {
                var crop = Spawn(cropPrefab, marker.transform.position);
                if (crop != null)
                {
                    crop.GetComponent<CropPickup>().isSecretCluster = marker.secretCluster;
                }

                if (marker.secretCluster) { run.totalSecretCrops++; } else { run.totalNormalCrops++; }
            }

            foreach (var marker in _levelInstance.GetComponentsInChildren<CheckpointMarker>())
            {
                Spawn(checkpointPrefab, marker.transform.position);
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

            if (cameraFollow != null && root != null)
            {
                cameraFollow.SetBounds(root.cameraMinX, root.cameraMaxX);
                cameraFollow.SnapToTarget();
            }

            Debug.Log($"[LevelLoader] Loaded '{data.levelId}': {_spawned.Count} spawned objects ({robots} robots).");
        }

        /// <summary>Destroys the level instance and returns everything spawned for it to the pool.</summary>
        public void UnloadLevel()
        {
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

        /// <summary>Moves the player to the given respawn point and grants brief invulnerability.</summary>
        public void RespawnPlayer(Vector2 position)
        {
            PlacePlayer(position);
            player.GrantInvulnerability(respawnInvulnerability);
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

            instance.GetComponent<RobotController>().Initialize(marker.patrolDistance);
            return true;
        }
    }
}
