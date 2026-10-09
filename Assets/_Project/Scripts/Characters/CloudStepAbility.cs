using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Woolly: fires a tuft of wool straight ahead that defeats the first robot it reaches (a <see cref="FeatherGust"/>
    /// with the wool art; on the ground or in the air, since 2026-10-09), and while airborne also spawns a
    /// temporary wool platform just under her feet, once per jump. Landing on anything (including the cloud)
    /// re-arms the platform, so a chain of clouds is possible but each one spends a use.
    /// </summary>
    public class CloudStepAbility : CharacterAbility
    {
        // Gap between the feet and the cloud's top surface, so the cloud never overlaps the player's collider.
        private const float CloudGap = 0.03f;

        private bool _usedThisJump;

        public override AbilityType Type => AbilityType.CloudStep;

        public override bool CanActivate(CharacterController2D owner)
        {
            return CanStep(owner) || owner.Prefabs.woolTuft != null;
        }

        private bool CanStep(CharacterController2D owner)
        {
            return !owner.IsGrounded && !owner.CoyoteAvailable && !_usedThisJump && owner.Prefabs.cloudPlatform != null;
        }

        public override void Activate(CharacterController2D owner)
        {
            if (owner.Prefabs.woolTuft != null)
            {
                Vector3 origin = owner.Position + new Vector2(owner.Facing * 0.8f, 0.3f);
                var tuft = ObjectPool.Instance.Get(owner.Prefabs.woolTuft, origin);
                tuft.GetComponent<FeatherGust>().Launch(owner.Facing, owner.Prefabs.woolEffect);
            }

            if (!CanStep(owner))
            {
                return;
            }

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
