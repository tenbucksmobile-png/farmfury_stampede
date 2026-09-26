using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Cluck: launches an egg forward in a short arc, on the ground or in the air, that defeats the first robot it
    /// hits - a way to clear a robot without jumping on its head. Each egg spends one of the level's ability uses.
    /// </summary>
    public class EggLaunchAbility : CharacterAbility
    {
        private const float LaunchVisualSeconds = 0.2f;

        private float _visualTimer;

        public override AbilityType Type => AbilityType.EggLaunch;
        public override bool IsActive => _visualTimer > 0f;

        public override bool CanActivate(CharacterController2D owner) => owner.Prefabs.egg != null;

        public override void Activate(CharacterController2D owner)
        {
            _visualTimer = LaunchVisualSeconds;
            Vector3 origin = owner.Position + new Vector2(owner.Facing * 0.6f, 0.4f);
            var go = ObjectPool.Instance.Get(owner.Prefabs.egg, origin);
            go.GetComponent<Egg>().Launch(owner.Facing);
        }

        public override void Tick(CharacterController2D owner, float dt)
        {
            _visualTimer = Mathf.Max(0f, _visualTimer - dt);
        }

        public override void Reset(CharacterController2D owner)
        {
            _visualTimer = 0f;
        }

        // Placeholder-art feedback: a quick squash as the egg leaves.
        public override Vector2 VisualScale => _visualTimer > 0f ? new Vector2(1.08f, 0.92f) : Vector2.one;
    }
}
