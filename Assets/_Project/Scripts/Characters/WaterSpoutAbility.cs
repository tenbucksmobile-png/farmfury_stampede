using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Ducky: rolls a ring of water out along the ground the way she faces, on the ground or in the air (it drops
    /// to the ground first), that bursts on and defeats the first robot it reaches (see <see cref="WaterSpout"/>).
    /// Ducky is also the only character immune to the Water tile type's slowdown and drowning (GDD: "the only
    /// character who doesn't sink or take damage from water tiles"). Replaced Skip Dash on 2026-10-09.
    /// </summary>
    public class WaterSpoutAbility : CharacterAbility
    {
        private const float LaunchVisualSeconds = 0.2f;

        private float _visualTimer;

        public override AbilityType Type => AbilityType.SkipDash;
        public override bool IsActive => _visualTimer > 0f;

        public override bool CanActivate(CharacterController2D owner) => owner.Prefabs.waterSpout != null;

        public override void Activate(CharacterController2D owner)
        {
            _visualTimer = LaunchVisualSeconds;
            Vector3 origin = owner.FeetPosition + new Vector2(owner.Facing * 0.7f, WaterSpout.Radius);
            var go = ObjectPool.Instance.Get(owner.Prefabs.waterSpout, origin);
            go.GetComponent<WaterSpout>().Launch(owner.Facing, owner.Prefabs.splashEffect);
        }

        public override void Tick(CharacterController2D owner, float dt)
        {
            owner.WaterImmune = true;
            _visualTimer = Mathf.Max(0f, _visualTimer - dt);
        }

        public override void Reset(CharacterController2D owner)
        {
            _visualTimer = 0f;
        }

        // Placeholder-art feedback: a quick lean into the push as the spout leaves.
        public override Vector2 VisualScale => _visualTimer > 0f ? new Vector2(1.08f, 0.92f) : Vector2.one;
    }
}
