using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Cluck: a brief mid-air flutter that grants a second full jump (a double jump). Once per airborne
    /// period; landing re-arms it. Each flutter spends one of the level's ability uses.
    /// </summary>
    public class FlutterJumpAbility : CharacterAbility
    {
        private const float FlutterVisualSeconds = 0.25f;

        private bool _usedThisJump;
        private float _visualTimer;

        public override AbilityType Type => AbilityType.FlutterJump;
        public override bool IsActive => _visualTimer > 0f;

        public override bool CanActivate(CharacterController2D owner)
        {
            return !owner.IsGrounded && !owner.CoyoteAvailable && !_usedThisJump;
        }

        public override void Activate(CharacterController2D owner)
        {
            _usedThisJump = true;
            _visualTimer = FlutterVisualSeconds;
            owner.Launch(owner.JumpVelocity);
        }

        public override void Tick(CharacterController2D owner, float dt)
        {
            _visualTimer = Mathf.Max(0f, _visualTimer - dt);
        }

        public override void OnLanded(CharacterController2D owner)
        {
            _usedThisJump = false;
        }

        public override void Reset(CharacterController2D owner)
        {
            _usedThisJump = false;
            _visualTimer = 0f;
        }

        public override Color Tint => _visualTimer > 0f ? new Color(1f, 1f, 0.6f) : Color.white;
    }
}
