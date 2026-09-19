using UnityEngine;

namespace FarmFuryStampede.Utilities
{
    /// <summary>Tags a GameObject as poolable and identifies which pool it returns to.</summary>
    public class PooledObject : MonoBehaviour
    {
        [SerializeField] private string poolKey;

        public string PoolKey => poolKey;

        /// <summary>True while parked in the pool. Guards against double-release (e.g. level unload after pickup).</summary>
        public bool InPool { get; internal set; }
    }
}
