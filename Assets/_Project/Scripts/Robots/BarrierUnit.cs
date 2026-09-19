using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Robots
{
    /// <summary>
    /// Stationary Barrier Unit (1 wide x 3 tall). A solid object on the Ground layer, so every character just
    /// stops against it; it has no stomp or hurt zone and refuses every hit (stomps, dashes, the pound shockwave,
    /// horseshoes), so nothing defeats it. Only Billy's Charge Break clears it, through
    /// <see cref="IChargeBreakable"/>. Design note: it can be jumped over on open ground (the base jump is 3.5), so
    /// use it only where the passage is capped (a chamber entrance), never on a main path.
    /// </summary>
    public class BarrierUnit : RobotController, IChargeBreakable
    {
        protected override void Tick(float dt)
        {
        }

        protected override bool TakeHit()
        {
            return false;
        }

        public void Break()
        {
            Debug.Log($"[BarrierUnit] Broken at {transform.position}.");
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
