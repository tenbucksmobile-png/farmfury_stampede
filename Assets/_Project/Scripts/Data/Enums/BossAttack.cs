namespace FarmFuryStampede.Data
{
    /// <summary>
    /// The attacks a world boss may use (CommanderBoss), as flags. It opens the fight with the first one it has (in
    /// this order) and adds the next with each hit it takes. None = Meadow Ruins' gentle boss, which only patrols.
    /// </summary>
    [System.Flags]
    public enum BossAttack
    {
        None = 0,
        /// <summary>Stops, shakes, then dashes at the player across the arena: jump over it (or stomp it).</summary>
        Charge = 1,
        /// <summary>Hops and slams down, sending a shockwave along the ground both ways: jump it.</summary>
        Slam = 2,
        /// <summary>Lobs an energy orb in an arc at where the player stands: keep moving.</summary>
        Shot = 4
    }
}
