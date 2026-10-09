namespace FarmFuryStampede.Data
{
    /// <summary>The platforming ability each character brings to a level.</summary>
    public enum AbilityType
    {
        EggLaunch,       // Cluck (was FlutterJump; same slot, so saved CharacterData keeps its value)
        GroundPound,
        RollDash,
        CloudStep,
        SkipDash,        // Ducky: Water Spout since 2026-10-09 (name kept so saved CharacterData keeps its value)
        RearVaultThrow,  // Horace: Horseshoe Throw
        PuffGlide,       // Gerald: Feather Blow since 2026-10-09 (same reason)
        ChargeBreak
    }
}
