using FarmFuryStampede.Data;

namespace FarmFuryStampede.Core
{
    /// <summary>
    /// The website demo (branch web-demo, never merged into main): Meadow Ruins only - its 11 levels and the
    /// Commander - free at farmfurygames.com/play/stampede/. Every other world shows on World Select but opens the
    /// Full Game panel (FullGameScreen) instead; no ads, purchases, shop, Locker or analytics. See WEB_DEMO_PLAN.md.
    /// </summary>
    public static class WebDemo
    {
        /// <summary>The one world the demo plays.</summary>
        public const WorldType World = WorldType.MeadowRuins;

        public static bool IsPlayable(WorldType world) => world == World;
    }
}
