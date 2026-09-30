using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Horace: throws a horseshoe forward in a short arc, on the ground or in the air, that defeats the first
    /// robot it hits - the same launch as Cluck's egg. (Was contextual: a taller Rear Vault on the ground and a
    /// straight throw only in the air; the class and enum keep the old name so saved CharacterData still match.)
    /// </summary>
    public class RearVaultThrowAbility : CharacterAbility
    {
        private const float LaunchVisualSeconds = 0.2f;

        private float _visualTimer;

        public override AbilityType Type => AbilityType.RearVaultThrow;
        public override bool IsActive => _visualTimer > 0f;

        public override bool CanActivate(CharacterController2D owner) => owner.Prefabs.horseshoe != null;

        public override void Activate(CharacterController2D owner)
        {
            _visualTimer = LaunchVisualSeconds;
            Vector3 origin = owner.Position + new Vector2(owner.Facing * 0.6f, 0.4f);
            var go = ObjectPool.Instance.Get(owner.Prefabs.horseshoe, origin);
            go.GetComponent<Horseshoe>().Launch(owner.Facing);
        }

        public override void Tick(CharacterController2D owner, float dt)
        {
            _visualTimer = Mathf.Max(0f, _visualTimer - dt);
        }

        public override void Reset(CharacterController2D owner)
        {
            _visualTimer = 0f;
        }

        // Placeholder-art feedback: a quick squash as the horseshoe leaves.
        public override Vector2 VisualScale => _visualTimer > 0f ? new Vector2(1.08f, 0.92f) : Vector2.one;
    }
}
