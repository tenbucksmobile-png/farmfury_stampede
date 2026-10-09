using FarmFuryStampede.Data;
using FarmFuryStampede.LevelSystem;
using FarmFuryStampede.Movement;
using FarmFuryStampede.Utilities;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Gerald: puffs up and blows a flurry of feathers straight ahead, on the ground or in the air, that defeats
    /// the first robot it reaches - Drones included, at his height (see <see cref="FeatherGust"/>). Replaced
    /// Puff Glide on 2026-10-09, so the crevasse-island secrets once marked Gerald/Woolly are Woolly's alone.
    /// </summary>
    public class FeatherBlowAbility : CharacterAbility
    {
        private const float BlowVisualSeconds = 0.3f;

        private float _visualTimer;

        public override AbilityType Type => AbilityType.PuffGlide;
        public override bool IsActive => _visualTimer > 0f;

        public override bool CanActivate(CharacterController2D owner) => owner.Prefabs.featherGust != null;

        public override void Activate(CharacterController2D owner)
        {
            _visualTimer = BlowVisualSeconds;
            Vector3 origin = owner.Position + new Vector2(owner.Facing * 0.9f, 0.3f);
            var go = ObjectPool.Instance.Get(owner.Prefabs.featherGust, origin);
            go.GetComponent<FeatherGust>().Launch(owner.Facing, owner.Prefabs.featherEffect);
        }

        public override void Tick(CharacterController2D owner, float dt)
        {
            _visualTimer = Mathf.Max(0f, _visualTimer - dt);
        }

        public override void Reset(CharacterController2D owner)
        {
            _visualTimer = 0f;
        }

        // Placeholder-art feedback: he swells as he draws breath, then lets it out.
        public override Vector2 VisualScale
        {
            get
            {
                if (_visualTimer <= 0f)
                {
                    return Vector2.one;
                }
                float puff = 1f + 0.15f * Mathf.Sin(_visualTimer / BlowVisualSeconds * Mathf.PI);
                return new Vector2(puff, puff);
            }
        }
    }
}
