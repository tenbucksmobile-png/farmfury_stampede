using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Robots;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Bessie: a forced fast-fall. While pounding she cannot steer and defeats robots she touches. On landing
    /// she breaks Breakable Floor tiles within a small radius (removing the tiles and their collision, not just
    /// swapping the art) and defeats robots directly beneath / beside her feet. Only usable in the air.
    /// </summary>
    public class GroundPoundAbility : CharacterAbility
    {
        private const float PoundSpeed = 30f;
        private const float BreakRadius = 2f;

        private bool _pounding;

        public override AbilityType Type => AbilityType.GroundPound;
        public override bool IsActive => _pounding;

        public override bool CanActivate(CharacterController2D owner)
        {
            return !owner.IsGrounded && !_pounding;
        }

        public override void Activate(CharacterController2D owner)
        {
            _pounding = true;
            owner.Velocity = new Vector2(0f, -PoundSpeed);
        }

        public override void Tick(CharacterController2D owner, float dt)
        {
            if (!_pounding)
            {
                return;
            }

            owner.InputLocked = true;
            owner.ContactAttacking = true;
            owner.MaxFallSpeedOverride = PoundSpeed;
            owner.Velocity = new Vector2(0f, -PoundSpeed);
        }

        public override void OnLanded(CharacterController2D owner)
        {
            if (!_pounding)
            {
                return;
            }

            _pounding = false;
            Vector2 feet = owner.FeetPosition;
            if (owner.Prefabs.poundEffect != null)
            {
                ObjectPool.Instance.Get(owner.Prefabs.poundEffect, feet + Vector2.up * 0.3f);   // the impact ring (BessieSlam.png)
            }

            int tilesBroken = 0;
            foreach (var hit in Physics2D.OverlapCircleAll(feet, BreakRadius, owner.GroundMask))
            {
                if (hit.TryGetComponent(out BreakableFloorLayer layer))
                {
                    tilesBroken += layer.BreakInRadius(feet, BreakRadius);
                }
            }

            int robotsDefeated = 0;
            foreach (var hit in Physics2D.OverlapBoxAll(feet + Vector2.up * 0.5f, new Vector2(2.4f, 1.2f), 0f))
            {
                var robot = hit.GetComponentInParent<RobotController>();
                if (robot != null && robot.DefeatByAbility())
                {
                    robotsDefeated++;
                }
            }

            Debug.Log($"[GroundPound] Landed: broke {tilesBroken} floor tiles, defeated {robotsDefeated} robots.");
        }

        public override void Reset(CharacterController2D owner)
        {
            _pounding = false;
        }

        public override Color Tint => _pounding ? new Color(1f, 0.6f, 0.6f) : Color.white;
    }
}
