using UnityEngine;
using FarmFuryStampede.Movement;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Shared behaviour of the two horizontal dashes (Percy's Roll Dash, Ducky's Skip Dash): for a short time
    /// the owner cannot steer and moves at a fixed speed in the facing direction, with gravity scaled down
    /// so a mid-air dash carries across gaps. Subclasses set the numbers.
    /// </summary>
    public abstract class DashAbility : CharacterAbility
    {
        protected abstract float DashSpeed { get; }
        protected abstract float DashSeconds { get; }
        protected abstract float DashGravityScale { get; }
        protected abstract bool DefeatsRobots { get; }

        private float _timeLeft;
        private int _direction = 1;
        private bool _usedInAir;

        public override bool IsActive => _timeLeft > 0f;

        // One dash per airborne period (like Flutter and Cloud Step), so a dash crosses a small gap but
        // three chained mid-air dashes cannot turn it into a chasm-crosser. Landing re-arms it.
        public override bool CanActivate(CharacterController2D owner)
        {
            return !IsActive && (owner.IsGrounded || !_usedInAir);
        }

        public override void Activate(CharacterController2D owner)
        {
            _timeLeft = DashSeconds;
            _direction = owner.Facing;
            _usedInAir = !owner.IsGrounded;
        }

        public override void OnLanded(CharacterController2D owner)
        {
            _usedInAir = false;
        }

        public override void Tick(CharacterController2D owner, float dt)
        {
            if (!IsActive)
            {
                return;
            }

            owner.InputLocked = true;
            owner.GravityScale = DashGravityScale;
            owner.ContactAttacking = DefeatsRobots;
            owner.Velocity = new Vector2(_direction * DashSpeed, DashGravityScale <= 0f ? 0f : owner.Velocity.y);

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
            _usedInAir = false;
        }
    }

    /// <summary>Percy: curls into a ball and dashes forward, defeating robots on contact and crossing small gaps.</summary>
    public class RollDashAbility : DashAbility
    {
        public override Data.AbilityType Type => Data.AbilityType.RollDash;
        protected override float DashSpeed => 18f;
        protected override float DashSeconds => 0.35f;
        protected override float DashGravityScale => 0f;
        protected override bool DefeatsRobots => true;

        public override Color Tint => IsActive ? new Color(1f, 0.75f, 0.75f) : Color.white;
        public override Vector2 VisualScale => IsActive ? new Vector2(1f, 0.6f) : Vector2.one;
    }

    /// <summary>
    /// Ducky: skims forward just above the surface. Ducky is also the only character immune to the Water tile
    /// type's slowdown and drowning, dash or not (GDD: "the only character who doesn't sink or take damage
    /// from water tiles").
    /// </summary>
    public class SkipDashAbility : DashAbility
    {
        public override Data.AbilityType Type => Data.AbilityType.SkipDash;
        protected override float DashSpeed => 11f;
        protected override float DashSeconds => 0.5f;
        protected override float DashGravityScale => 0.15f;
        protected override bool DefeatsRobots => false;

        public override void Tick(CharacterController2D owner, float dt)
        {
            owner.WaterImmune = true;
            base.Tick(owner, dt);
        }

        public override Color Tint => IsActive ? new Color(0.7f, 0.9f, 1f) : Color.white;
    }
}
