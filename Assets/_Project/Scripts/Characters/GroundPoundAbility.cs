using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Robots;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Bessie's slam. On the ground she stamps her foot at once; in the air it is a forced fast-fall (she cannot
    /// steer and defeats robots she touches) that slams when she lands. Either way the slam breaks Breakable Floor
    /// tiles within a small radius (removing the tiles and their collision, not just swapping the art), defeats
    /// robots right at her feet, and sends an earthquake running along the ground the way she faces: a line of
    /// small dust bursts (the BessieSlam art, drawn small) that follows the ground, stops at a wall, a pit or the
    /// end of its range, and erupts full-size under the first robot it reaches, flattening it.
    /// </summary>
    public class GroundPoundAbility : CharacterAbility
    {
        private const float PoundSpeed = 30f;
        private const float BreakRadius = 2f;

        // The quake.
        private const float QuakeSpeed = 16f;         // units per second along the ground
        private const float QuakeRange = 10f;
        private const float QuakePuffSpacing = 0.8f;  // one small burst this often along the way
        private const float QuakePuffSize = 0.45f;    // relative to the full-size impact
        private const float QuakeHitSize = 1.3f;      // the eruption under the robot
        private const float QuakeStepUp = 0.6f;       // climbs steps up to this high; anything taller stops it
        private const float SlamVisualSeconds = 0.2f;

        private bool _pounding;
        private bool _quake;
        private Vector2 _quakePosition;
        private int _quakeDirection;
        private float _quakeLeft;
        private float _quakeToNextPuff;
        private float _slamVisual;

        public override AbilityType Type => AbilityType.GroundPound;
        public override bool IsActive => _pounding;

        public override bool CanActivate(CharacterController2D owner)
        {
            return !_pounding && !_quake;
        }

        public override void Activate(CharacterController2D owner)
        {
            if (owner.IsGrounded)
            {
                Slam(owner);
                return;
            }

            _pounding = true;
            owner.Velocity = new Vector2(0f, -PoundSpeed);
        }

        public override void Tick(CharacterController2D owner, float dt)
        {
            _slamVisual = Mathf.Max(0f, _slamVisual - dt);
            if (_quake)
            {
                AdvanceQuake(owner, dt);
            }

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
            Slam(owner);
        }

        // The foot hits the ground: impact burst, floor break, robots at her feet, and the quake sets off.
        private void Slam(CharacterController2D owner)
        {
            _slamVisual = SlamVisualSeconds;
            Vector2 feet = owner.FeetPosition;
            Burst(owner, feet, 1f);

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

            _quake = true;
            _quakePosition = feet;
            _quakeDirection = owner.Facing >= 0 ? 1 : -1;
            _quakeLeft = QuakeRange;
            _quakeToNextPuff = QuakePuffSpacing;

            Debug.Log($"[GroundPound] Slam: broke {tilesBroken} floor tiles, defeated {robotsDefeated} robots.");
        }

        // Moves the quake front along the ground; it ends at a wall, a pit, its range, or the first robot it flattens.
        private void AdvanceQuake(CharacterController2D owner, float dt)
        {
            float step = Mathf.Min(QuakeSpeed * dt, _quakeLeft);
            _quakeLeft -= step;
            _quakePosition.x += _quakeDirection * step;

            // A wall (anything solid more than a small step above the ground line) stops it; so does a pit.
            Vector2 probe = _quakePosition + Vector2.up * QuakeStepUp;
            if (Physics2D.OverlapPoint(probe, owner.GroundMask) != null)
            {
                _quake = false;
                return;
            }
            var ground = Physics2D.Raycast(probe, Vector2.down, QuakeStepUp + 0.8f, owner.GroundMask);
            if (ground.collider == null)
            {
                _quake = false;
                return;
            }
            _quakePosition.y = ground.point.y;

            foreach (var hit in Physics2D.OverlapBoxAll(_quakePosition + Vector2.up * 0.75f, new Vector2(0.9f, 1.5f), 0f))
            {
                var robot = hit.GetComponentInParent<RobotController>();
                if (robot != null && robot.DefeatByAbility())
                {
                    Burst(owner, _quakePosition, QuakeHitSize);
                    _quake = false;
                    return;
                }
            }

            _quakeToNextPuff -= step;
            if (_quakeToNextPuff <= 0f)
            {
                _quakeToNextPuff += QuakePuffSpacing;
                Burst(owner, _quakePosition, QuakePuffSize);
            }

            if (_quakeLeft <= 0f)
            {
                _quake = false;
            }
        }

        // The BessieSlam art at a point on the ground, 'size' times its normal size (its base kept on the ground).
        private static void Burst(CharacterController2D owner, Vector2 groundPoint, float size)
        {
            if (owner.Prefabs.poundEffect == null || ObjectPool.Instance == null)
            {
                return;
            }

            var go = ObjectPool.Instance.Get(owner.Prefabs.poundEffect, groundPoint + Vector2.up * (0.3f * size));
            if (go.TryGetComponent(out FadeEffect effect))
            {
                effect.Play(size);
            }
        }

        public override void Reset(CharacterController2D owner)
        {
            _pounding = false;
            _quake = false;
            _slamVisual = 0f;
        }

        public override Color Tint => _pounding ? new Color(1f, 0.6f, 0.6f) : Color.white;

        // A quick squash as the foot comes down.
        public override Vector2 VisualScale => _slamVisual > 0f ? new Vector2(1.1f, 0.9f) : Vector2.one;
    }
}
