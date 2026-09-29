using UnityEngine;

namespace FarmFuryStampede.LevelSystem
{
    /// <summary>
    /// Marks a collider as slippery ice (Frozen Tundra's reduced traction, GDD Section 7). A character standing on it
    /// speeds up, slows down and turns round far more gradually (CharacterController2D multiplies its ground
    /// acceleration and deceleration by <see cref="traction"/>), so it slides on past where it let go. LevelBuilder
    /// puts it on the level's Ice tilemap (LevelBuilder.IceFlat).
    /// </summary>
    public class IceSurface : MonoBehaviour
    {
        [Tooltip("Fraction of the normal ground acceleration/deceleration while standing on this ice.")]
        [Range(0.02f, 1f)] public float traction = 0.18f;
    }
}
