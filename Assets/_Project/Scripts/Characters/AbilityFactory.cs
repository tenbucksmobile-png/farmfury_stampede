using System;
using FarmFuryStampede.Data;

namespace FarmFuryStampede.Characters
{
    /// <summary>Resolves a CharacterData's AbilityType to the ability implementation, at character spawn time.</summary>
    public static class AbilityFactory
    {
        public static CharacterAbility Create(AbilityType type)
        {
            switch (type)
            {
                case AbilityType.FlutterJump: return new FlutterJumpAbility();
                case AbilityType.GroundPound: return new GroundPoundAbility();
                case AbilityType.RollDash: return new RollDashAbility();
                case AbilityType.CloudStep: return new CloudStepAbility();
                case AbilityType.SkipDash: return new SkipDashAbility();
                case AbilityType.RearVaultThrow: return new RearVaultThrowAbility();
                case AbilityType.PuffGlide: return new PuffGlideAbility();
                case AbilityType.ChargeBreak: return new ChargeBreakAbility();
                default: throw new ArgumentOutOfRangeException(nameof(type), type, "No ability implementation.");
            }
        }
    }
}
