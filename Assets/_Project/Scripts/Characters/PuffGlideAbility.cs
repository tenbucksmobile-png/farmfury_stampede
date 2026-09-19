using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Gerald: inflates and slow-falls for a few seconds (or until landing), keeping full horizontal control so
    /// wide chasms can be crossed. Defeats robots on contact while inflated. Airborne only.
    /// </summary>
    public class PuffGlideAbility : CharacterAbility
    {
        private const float GlideSeconds = 3f;
        private const float GlideFallSpeed = 2f;

        private float _timeLeft;

        public override AbilityType Type => AbilityType.PuffGlide;
        public override bool IsActive => _timeLeft > 0f;

        public override bool CanActivate(CharacterController2D owner)
        {
            return !owner.IsGrounded && !IsActive;
        }

        public override void Activate(CharacterController2D owner)
        {
            _timeLeft = GlideSeconds;
        }

        public override void Tick(CharacterController2D owner, float dt)
        {
            if (!IsActive)
            {
                return;
            }

            owner.GravityScale = 0.1f;
            owner.MaxFallSpeedOverride = GlideFallSpeed;
            owner.ContactAttacking = true;
            _timeLeft -= dt;
        }

        public override void OnLanded(CharacterController2D owner)
        {
            _timeLeft = 0f;
        }

        public override void Reset(CharacterController2D owner)
        {
            _timeLeft = 0f;
        }

        public override Color Tint => IsActive ? new Color(1f, 0.95f, 0.7f) : Color.white;
        public override Vector2 VisualScale => IsActive ? new Vector2(1.4f, 1.4f) : Vector2.one;
    }
}
