using System.Collections.Generic;
using UnityEngine;

namespace FarmFuryStampede.Utilities
{
    /// <summary>
    /// Scene-wide pool for frequently spawned/despawned objects (crops, robots, checkpoints, clouds,
    /// projectiles...). Objects are keyed by <see cref="PooledObject.PoolKey"/>. Hand-placed instances are
    /// released into the pool the same way as spawned ones, so nothing pooled is ever destroyed.
    /// </summary>
    public class ObjectPool : MonoSingleton<ObjectPool>
    {
        private readonly Dictionary<string, Stack<GameObject>> _pools = new();
        private readonly Dictionary<string, HashSet<GameObject>> _active = new();
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
                // Instantiate at the target position so physics bodies never start at the prefab's origin.
                instance = Instantiate(prefab, position, Quaternion.identity);
            }

            instance.transform.SetParent(parent, true);
            instance.transform.position = position;
            instance.GetComponent<PooledObject>().InPool = false;
            ActiveSet(pooled.PoolKey).Add(instance);
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

            if (pooled.InPool)
            {
                return;
            }

            if (!_pools.TryGetValue(pooled.PoolKey, out var stack))
            {
                stack = new Stack<GameObject>();
                _pools[pooled.PoolKey] = stack;
            }

            pooled.InPool = true;
            ActiveSet(pooled.PoolKey).Remove(instance);
            instance.SetActive(false);
            instance.transform.SetParent(_poolRoot, false);
            stack.Push(instance);
        }

        /// <summary>Returns every currently-active object with this key to the pool (e.g. leftover projectiles).</summary>
        public void ReleaseAll(string poolKey)
        {
            if (!_active.TryGetValue(poolKey, out var active))
            {
                return;
            }

            foreach (var instance in new List<GameObject>(active))
            {
                if (instance != null)
                {
                    Release(instance);
                }
            }
            active.Clear();
        }

        public int PooledCount(string poolKey)
        {
            return _pools.TryGetValue(poolKey, out var stack) ? stack.Count : 0;
        }

        public int ActiveCount(string poolKey)
        {
            return _active.TryGetValue(poolKey, out var active) ? active.Count : 0;
        }

        private HashSet<GameObject> ActiveSet(string key)
        {
            if (!_active.TryGetValue(key, out var set))
            {
                set = new HashSet<GameObject>();
                _active[key] = set;
            }
            return set;
        }
    }
}
