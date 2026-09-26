namespace FarmFuryStampede.Data
{
    /// <summary>The platforming ability each character brings to a level.</summary>
    public enum AbilityType
    {
        EggLaunch,       // Cluck (was FlutterJump; same slot, so saved CharacterData keeps its value)
        GroundPound,
        RollDash,
        CloudStep,
        SkipDash,
        RearVaultThrow,
        PuffGlide,
        ChargeBreak
    }
}
