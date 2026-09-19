using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Pooled robot-barrier-styled wall (1 wide x 3 tall). It sits on the Ground layer so every character is
    /// blocked by it; only Billy's Charge Break calls <see cref="Break"/>. Spawned at BreakableWallMarker
    /// positions by LevelLoader.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(PooledObject))]
    public class BreakableWall : MonoBehaviour, IChargeBreakable
    {
        public void Break()
        {
            Debug.Log($"[BreakableWall] Broken at {transform.position}.");
            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.Release(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
