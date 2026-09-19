using System.Collections.Generic;
using UnityEngine;

namespace FarmFuryStampede.Utilities
{
    /// <summary>
    /// Scene-wide pool for frequently spawned/despawned objects (crops now; robots, projectiles later).
    /// Objects are keyed by <see cref="PooledObject.PoolKey"/>. Hand-placed instances are released into
    /// the pool the same way as spawned ones, so a collected crop is parked, never destroyed.
    /// </summary>
    public class ObjectPool : MonoSingleton<ObjectPool>
    {
        private readonly Dictionary<string, Stack<GameObject>> _pools = new();
        private Transform _poolRoot;

        protected override void Awake()
        {
            base.Awake();
            _poolRoot = new GameObject("PooledObjects").transform;
            _poolRoot.SetParent(transform, false);
        }

        /// <summary>Takes an instance from the pool (or instantiates one) and places it at the position.</summary>
        public GameObject Get(GameObject prefab, Vector3 position, Transform parent = null)
        {
            if (!prefab.TryGetComponent(out PooledObject pooled))
            {
                Debug.LogError($"[ObjectPool] Prefab '{prefab.name}' has no PooledObject component.");
                return null;
            }

            GameObject instance = null;
            if (_pools.TryGetValue(pooled.PoolKey, out var stack))
            {
                while (stack.Count > 0 && instance == null)
                {
                    instance = stack.Pop();
                }
            }

            if (instance == null)
            {
                instance = Instantiate(prefab);
            }

            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            instance.SetActive(true);
            return instance;
        }

        /// <summary>Deactivates the object and parks it in the pool for reuse.</summary>
        public void Release(GameObject instance)
        {
            if (!instance.TryGetComponent(out PooledObject pooled))
            {
                Debug.LogWarning($"[ObjectPool] '{instance.name}' has no PooledObject; destroying instead.");
                Destroy(instance);
                return;
            }

            if (!_pools.TryGetValue(pooled.PoolKey, out var stack))
            {
                stack = new Stack<GameObject>();
                _pools[pooled.PoolKey] = stack;
            }

            instance.SetActive(false);
            instance.transform.SetParent(_poolRoot, false);
            stack.Push(instance);
        }

        public int PooledCount(string poolKey)
        {
            return _pools.TryGetValue(poolKey, out var stack) ? stack.Count : 0;
        }
    }
}
