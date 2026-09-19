using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Woolly: spawns a temporary wool platform just under her feet while airborne, once per jump. Landing on
    /// anything (including the cloud) re-arms it, so a chain of clouds is possible but each one spends a use.
    /// </summary>
    public class CloudStepAbility : CharacterAbility
    {
        // Gap between the feet and the cloud's top surface, so the cloud never overlaps the player's collider.
        private const float CloudGap = 0.03f;

        private bool _usedThisJump;

        public override AbilityType Type => AbilityType.CloudStep;

        public override bool CanActivate(CharacterController2D owner)
        {
            return !owner.IsGrounded && !owner.CoyoteAvailable && !_usedThisJump && owner.Prefabs.cloudPlatform != null;
        }

        public override void Activate(CharacterController2D owner)
        {
            _usedThisJump = true;

            Vector2 feet = owner.FeetPosition;
            var cloud = ObjectPool.Instance.Get(owner.Prefabs.cloudPlatform, new Vector3(feet.x, feet.y - CloudGap - CloudPlatform.Thickness * 0.5f, 0f));
            cloud.GetComponent<CloudPlatform>().Begin();
        }

        public override void OnLanded(CharacterController2D owner)
        {
            _usedThisJump = false;
        }

        public override void Reset(CharacterController2D owner)
        {
            _usedThisJump = false;
        }
    }
}
