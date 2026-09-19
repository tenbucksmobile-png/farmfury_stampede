namespace FarmFuryStampede.Data
{
    /// <summary>The enemy robot archetypes found across Stampede's levels.</summary>
    public enum RobotType
    {
        Harvester,
        Scout,
        Drone,
        BarrierUnit,
        Chaser,
        /// <summary>World boss variant (the "Robot Commander"). Appended so serialized values stay stable.</summary>
        Commander
    }
}
