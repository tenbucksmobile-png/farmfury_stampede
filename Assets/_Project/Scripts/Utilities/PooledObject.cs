using UnityEngine;

namespace FarmFuryStampede.Utilities
{
    /// <summary>Tags a GameObject as poolable and identifies which pool it returns to.</summary>
    public class PooledObject : MonoBehaviour
    {
        [SerializeField] private string poolKey;

        public string PoolKey => poolKey;
    }
}
