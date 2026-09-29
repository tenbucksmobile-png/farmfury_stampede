namespace FarmFuryStampede.Data
{
    /// <summary>
    /// Stampede's worlds: the six free story worlds, then the three paid post-finale worlds (WorldData.purchaseRequired;
    /// unlocked by purchase alone). Values are serialized as ints, so only ever append.
    /// </summary>
    public enum WorldType
    {
        MeadowRuins,
        FrozenTundra,
        WatermillVillage,
        SkyIslands,
        SunkenCity,
        RobotMothership,
        DustbowlCanyon,
        HarvestFairground,
        CropFactory
    }
}
