using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Billy: Mud Sling (since 2026-10-17; was Charge Break, a ram that only broke cracked walls and Barrier Units, and
    /// no level has either any more). Flings a glob of mud forward in an arc, on the ground or in the air: it defeats
    /// the first robot it hits, and where it lands it leaves a mud pile that knocks out the next robot to walk into it
    /// (MudBall, MudPile). Billy holds his slinging pose for a moment. AbilityType.ChargeBreak keeps its name.
    /// </summary>
    public class MudSlingAbility : CharacterAbility
    {
        private const float SlingPoseSeconds = 0.3f;

        private float _poseTimer;

        public override AbilityType Type => AbilityType.ChargeBreak;
        public override bool IsActive => _poseTimer > 0f;

        public override bool CanActivate(CharacterController2D owner) => owner.Prefabs.mudBall != null;

        public override void Activate(CharacterController2D owner)
        {
            _poseTimer = SlingPoseSeconds;
            Vector3 origin = owner.Position + new Vector2(owner.Facing * 0.7f, 0.6f);
            var go = ObjectPool.Instance.Get(owner.Prefabs.mudBall, origin);
            go.GetComponent<MudBall>().Launch(owner.Facing);
        }

        public override void Tick(CharacterController2D owner, float dt)
        {
            _poseTimer = Mathf.Max(0f, _poseTimer - dt);
        }

        public override void Reset(CharacterController2D owner)
        {
            _poseTimer = 0f;
        }
    }
}
