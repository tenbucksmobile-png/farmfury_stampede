using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Billy: charges forward, breaking any Breakable Wall or Barrier Unit in the way (both are IChargeBreakable). Those are the only things this
    /// affects; no other character or ability can break them, and the charge does not break Bessie's floor tiles.
    /// </summary>
    public class ChargeBreakAbility : CharacterAbility
    {
        private const float ChargeSpeed = 15f;
        private const float ChargeSeconds = 0.5f;

        private float _timeLeft;
        private int _direction = 1;

        public override AbilityType Type => AbilityType.ChargeBreak;
        public override bool IsActive => _timeLeft > 0f;

        public override bool CanActivate(CharacterController2D owner)
        {
            return !IsActive;
        }

        public override void Activate(CharacterController2D owner)
        {
            _timeLeft = ChargeSeconds;
            _direction = owner.Facing;
        }

        public override void Tick(CharacterController2D owner, float dt)
        {
            if (!IsActive)
            {
                return;
            }

            owner.InputLocked = true;
            owner.Velocity = new Vector2(_direction * ChargeSpeed, owner.Velocity.y);

            // Look a step ahead so the wall breaks before the controller's own collision stops the charge against it.
            Bounds b = owner.ColliderBounds;
            var hit = Physics2D.BoxCast(b.center, b.size * 0.9f, 0f, new Vector2(_direction, 0f), ChargeSpeed * dt + 0.4f, owner.GroundMask);
            if (hit.collider != null && hit.collider.TryGetComponent(out IChargeBreakable breakable))
            {
                breakable.Break();
            }

            _timeLeft -= dt;
            if (_timeLeft <= 0f)
            {
                _timeLeft = 0f;
                owner.Velocity = new Vector2(_direction * owner.MoveSpeed, owner.Velocity.y);
            }
        }

        public override void Reset(CharacterController2D owner)
        {
            _timeLeft = 0f;
        }

        public override Color Tint => IsActive ? new Color(1f, 0.8f, 0.6f) : Color.white;
    }
}
