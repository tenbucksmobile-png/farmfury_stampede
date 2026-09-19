using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Horace: contextual. On the ground the button is a Rear Vault, a jump noticeably taller than the shared
    /// base jump (this is the one place a character jumps higher, and it is an ability effect, not a base
    /// stat). In the air it throws a horseshoe forward that defeats one robot at range. Each use, of either
    /// kind, spends one of the level's ability uses.
    /// </summary>
    public class RearVaultThrowAbility : CharacterAbility
    {
        public const float VaultHeight = 5.5f;

        public override AbilityType Type => AbilityType.RearVaultThrow;

        public override bool CanActivate(CharacterController2D owner)
        {
            bool canVault = owner.IsGrounded || owner.CoyoteAvailable;
            return canVault || owner.Prefabs.horseshoe != null;
        }

        public override void Activate(CharacterController2D owner)
        {
            if (owner.IsGrounded || owner.CoyoteAvailable)
            {
                owner.LaunchToHeight(VaultHeight);
                return;
            }

            Vector3 origin = owner.Position + new Vector2(owner.Facing * 0.6f, 0.1f);
            var go = ObjectPool.Instance.Get(owner.Prefabs.horseshoe, origin);
            go.GetComponent<Horseshoe>().Launch(owner.Facing);
        }
    }
}
