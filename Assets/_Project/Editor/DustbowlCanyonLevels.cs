using System.Collections.Generic;
using FarmFuryStampede.Data;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// World 7, Dustbowl Canyon, a paid post-finale world: eleven levels and the Drill Rig, built 2026-10-17 on the Crop
    /// Factory's validated layouts (see StampedePhase5aSetup.DustbowlAssets; all art from Kling in
    /// Sprites/UI/DustbowlCanyon). A drought-cracked ranch and red-rock canyon, so the twist is the desert (GDD:
    /// "sandstorms hide the path, quicksand slows, tumbleweeds roll"):
    ///   Quicksand()   a stretch of ground where everyone wades at under half speed (jumping still works);
    ///   Tumbleweeds() a lane where a tumbleweed rolls at the player every few seconds: jump it or lose a life;
    ///   Sandstorm()   a blowing haze over the level whose regular gusts hide the way ahead (5, 8, 10, 11, boss).
    /// Normal gravity; the Crop Factory's belts are rope bridges here and every moving ledge is the mine-cart lift.
    /// Robots: Scouts and Harvesters from 1, Drones from 2, Chasers from 4. Character-gated secrets: 3 = high ledge
    /// (double jump / Woolly), 6 = Breakable Floor (Bessie), 9 = island behind the start across a 15-unit gap
    /// (Gerald / Woolly). Secret passages (rare pellet + coins) in 1, 3, 4, 5, 7, 9 and 11 (Steps, Staircase, Pyramid,
    /// Ledges, Tunnel, Pillars, Zigzag). Every level also gets PathCorn and StandardFarm (here the ranch props),
    /// applied in CreateAll. Tumbleweed lanes keep clear of the start and checkpoints (validated).
    /// </summary>
    internal static class DustbowlCanyonLevels
    {
        public static List<LevelBuilder> CreateAll()
        {
            var levels = new List<LevelBuilder> { Level1(), Level2(), Level3(), Level4(), Level5(), Level6(), Level7(), Level8(),
                Level9(), Level10(), Level11(), LevelBoss() };
            foreach (var level in levels)
            {
                level.PathCorn();
                level.StandardFarm();
            }
            return levels;
        }

        // ---------------------------------------------------------------- 1

        // The first rope bridge over a gully and a patch of quicksand; the passage sign on a perch near the end.
        private static LevelBuilder Level1()
        {
            var b = new LevelBuilder("DustbowlCanyon_01", "Dry Gulch");
            b.Flat(30);                       // [-4,26)
            b.Gap(4);                         // [26,30)
            b.Flat(26);                       // [30,56)
            b.Gap(10);                        // [56,66) a rope bridge
            b.Flat(24);                       // [66,90)
            b.Gap(4);                         // [90,94)
            b.Flat(32);                       // [94,126)
            b.Bridge(56, 10, 0);
            b.Quicksand(18, 24);

            b.StoneBlocks(40, 3, 4);          // bonus block stack with the coin
            b.BonusCoin(41.5f, 4);
            b.StoneBlocks(100, 3, 4);         // perch for the passage sign
            b.SecretPassage(101.5f, 4, PassageLayout.Steps);

            b.Start(0).Goal(122).Checkpoint(32).Checkpoint(68).Checkpoint(96);
            b.Scout(12, 3);
            b.Harvester(46, 3);
            b.Scout(80, 3);
            b.Harvester(110, 2).Scout(117, 2);
            return b;
        }

        // ---------------------------------------------------------------- 2

        // Tumbleweeds roll down three stretches of trail; a bridge; Drones overhead.
        private static LevelBuilder Level2()
        {
            var b = new LevelBuilder("DustbowlCanyon_02", "Tumbleweed Trail");
            b.Flat(34);                       // [-4,30)
            b.Gap(4);                         // [30,34)
            b.Flat(30);                       // [34,64)
            b.Gap(8);                         // [64,72) a rope bridge
            b.Flat(30);                       // [72,102)
            b.Gap(4);                         // [102,106)
            b.Flat(22);                       // [106,128)
            b.Bridge(64, 8, 0);

            b.Tumbleweeds(14, 28, 4f);
            b.Tumbleweeds(40, 56, 4.5f, 0.3f);
            b.Tumbleweeds(80, 98, 4.5f, 0.6f);

            b.StoneBlocks(24, 3, 4);          // bonus block stack with the coin, over the first tumbleweed lane
            b.BonusCoin(25.5f, 4);

            b.Start(0).Goal(124).Checkpoint(36).Checkpoint(74).Checkpoint(108);
            b.Scout(12, 2);
            b.Harvester(58, 2);
            b.Scout(96, 2);
            b.Harvester(117, 3);
            b.Drone(40, 2.8f, 3).Drone(112, 2.8f, 3);
            return b;
        }

        // ---------------------------------------------------------------- 3

        // Quicksand pits along the path, a high secret ledge, terraces, tumbleweeds at the end.
        private static LevelBuilder Level3()
        {
            var b = new LevelBuilder("DustbowlCanyon_03", "Sinking Sands");
            b.Flat(36);                       // [-4,32)
            b.Gap(4);                         // [32,36)
            b.Flat(28);                       // [36,64)
            b.Flat(18, 2);                    // [64,82) top 2
            b.Gap(4);                         // [82,86)
            b.Flat(16, 2);                    // [86,102) top 2
            b.Flat(24, 0);                    // [102,126)

            b.Quicksand(16, 26);
            b.Quicksand(48, 58);
            b.Quicksand(90, 98);
            b.Tumbleweeds(108, 122, 4f);

            b.SecretLedge(42, 5, 5);          // 5 up: the double jump every character has (or Woolly's Cloud Step)
            b.Gate(CharacterType.Cluck, "Ledge 5 units up: needs extra height (the double jump every character has, or Woolly's Cloud Step).", CharacterType.Woolly);
            b.SecretRow(42.5f, 46.5f, 1f, 5.9f);

            b.StoneBlocks(105, 3, 4);         // perch for the passage sign
            b.SecretPassage(106.5f, 4, PassageLayout.Staircase);

            b.Start(0).Goal(122).Checkpoint(38).Checkpoint(66).Checkpoint(88).Checkpoint(103);
            b.Scout(12, 3);
            b.Harvester(58, 2);
            b.Scout(74, 3);
            b.Harvester(120, 2);
            b.Drone(60, 2.8f, 3).Drone(98, 2.8f, 3);
            return b;
        }

        // ---------------------------------------------------------------- 4

        // Two mine-cart lifts slide together over a chasm, a bridge, tumbleweeds, quicksand and a Chaser; the passage sign on a perch just ahead of the start.
        private static LevelBuilder Level4()
        {
            var b = new LevelBuilder("DustbowlCanyon_04", "Canyon Lifts");
            b.Flat(30);                       // [-4,26)
            b.Gap(12);                        // [26,38) two lifts meet mid-chasm
            b.Flat(26);                       // [38,64)
            b.Gap(10);                        // [64,74) a rope bridge
            b.Flat(20);                       // [74,94)
            b.Flat(14, 2);                    // [94,108) top 2
            b.Gap(4);                         // [108,112)
            b.Flat(16, 0);                    // [112,128)
            b.MovingLedge(26, 3, 0, 3, 0, 4f);    // [26,29) <-> [29,32)
            b.MovingLedge(35, 3, 0, -3, 0, 4f);   // [35,38) <-> [32,35): they meet at x=32
            b.Bridge(64, 10, 0);

            b.Tumbleweeds(44, 60, 4.5f);
            b.Quicksand(80, 90);

            b.StoneBlocks(3, 3, 4);           // perch for the passage sign, just ahead of the start
            b.SecretPassage(4.5f, 4, PassageLayout.Pyramid);

            b.Start(0).Goal(124).Checkpoint(40).Checkpoint(76).Checkpoint(96).Checkpoint(114);
            b.Scout(14, 3);
            b.Harvester(60, 2);
            b.Chaser(90, 6);                  // wakes in the quicksand
            b.Scout(101, 3);
            b.Harvester(120, 3);
            b.Drone(46, 2.8f, 3).Drone(118, 2.8f, 3);
            return b;
        }

        // ---------------------------------------------------------------- 5

        // Two bridges in a row, quicksand, tumbleweed lanes and the first sandstorm.
        private static LevelBuilder Level5()
        {
            var b = new LevelBuilder("DustbowlCanyon_05", "Dust Devil Flats");
            b.Flat(24);                       // [-4,20)
            b.Gap(8);                         // [20,28) a rope bridge
            b.Flat(16);                       // [28,44)
            b.Gap(8);                         // [44,52) a rope bridge
            b.Flat(24);                       // [52,76)
            b.Gap(3);                         // [76,79) up onto the terrace
            b.Flat(21, 2);                    // [79,100) top 2
            b.Flat(26, 0);                    // [100,126)
            b.Bridge(20, 8, 0);
            b.Bridge(44, 8, 0);

            b.Quicksand(34, 42);
            b.Tumbleweeds(60, 74, 4f, 0.5f);
            b.Tumbleweeds(86, 98, 3.5f);
            b.Quicksand(108, 118);
            b.Sandstorm();

            b.StoneBlocks(56, 3, 4);          // perch for the passage sign
            b.SecretPassage(57.5f, 4, PassageLayout.Ledges);

            b.Start(0).Goal(122).Checkpoint(30).Checkpoint(54).Checkpoint(82).Checkpoint(102);
            b.Scout(10, 3);
            b.Harvester(41, 1.5f);
            b.Scout(95, 2);
            b.Harvester(120, 3);
            b.Chaser(72, 4);
            b.Drone(60, 2.8f, 2.5f).Drone(106, 2.8f, 3);
            return b;
        }

        // ---------------------------------------------------------------- 6

        // A cracked clay floor near the start (Bessie), two tumbleweed lanes out of step, one lift sliding across a chasm.
        private static LevelBuilder Level6()
        {
            var b = new LevelBuilder("DustbowlCanyon_06", "Rattler Ridge");
            b.Flat(30);                       // [-4,26)
            b.Gap(4);                         // [26,30)
            b.Flat(40);                       // [30,70)
            b.Gap(12);                        // [70,82) one lift slides across
            b.Flat(22);                       // [82,104)
            b.Gap(4);                         // [104,108)
            b.Flat(20);                       // [108,128)
            b.MovingLedge(71, 3, 0, 7, 0, 4.5f);    // [71,74) <-> [78,81)

            b.BreakableFloor(16, 4);          // hollow beneath: Bessie's Ground Pound
            b.Gate(CharacterType.Bessie, "Cracked clay at x=16..20 hides a hollow below; only Ground Pound breaks it.");
            b.SecretRow(16.5f, 19.5f, 1f, -2.5f);

            b.Tumbleweeds(36, 52, 4.5f).Tumbleweeds(56, 68, 4f, 0.5f);   // two lanes, out of step
            b.Quicksand(88, 100);

            b.StoneBlocks(58, 3, 4);          // bonus block stack with the coin
            b.BonusCoin(59.5f, 4);

            b.Start(0).Goal(124).Checkpoint(32).Checkpoint(84).Checkpoint(110);
            b.Scout(8, 2);
            b.Harvester(60, 3);
            b.Chaser(66, 6);                  // wakes past the tumbleweed lanes, right before the chasm
            b.Scout(98, 2);
            b.Harvester(118, 3);
            b.Drone(54, 2.8f, 3).Drone(120, 2.8f, 3);
            return b;
        }

        // ---------------------------------------------------------------- 7

        // Two lifts bobbing in turn, terraces up to a bridge at the top, quicksand and tumbleweeds on the way down.
        private static LevelBuilder Level7()
        {
            var b = new LevelBuilder("DustbowlCanyon_07", "Mesa Climb");
            b.Flat(28);                       // [-4,24)
            b.Gap(10);                        // [24,34) two lifts bobbing in turn
            b.Flat(26);                       // [34,60)
            b.Flat(16, 2);                    // [60,76) top 2
            b.Flat(16, 4);                    // [76,92) top 4
            b.Gap(8);                         // [92,100) a rope bridge
            b.Flat(14, 4);                    // [100,114) top 4
            b.Flat(12, 2);                    // [114,126) top 2
            b.Flat(14, 0);                    // [126,140)
            b.MovingLedge(26, 2, 1, 0, 3, 3.5f);         // top 1 <-> 4
            b.MovingLedge(30, 2, 1, 0, 3, 3.5f, 0.5f);   // up while the first is down
            b.Bridge(92, 8, 4);

            b.Quicksand(44, 56);
            b.Tumbleweeds(64, 75, 3.5f);
            b.Tumbleweeds(104, 113, 3.5f, 0.5f);
            b.Quicksand(116, 124);

            b.StoneBlocks(40, 3, 4);          // perch for the passage sign
            b.SecretPassage(41.5f, 4, PassageLayout.Tunnel);

            b.Start(0).Goal(136).Checkpoint(36).Checkpoint(62).Checkpoint(78).Checkpoint(102).Checkpoint(128);
            b.Scout(12, 3);
            b.Harvester(54, 2);
            b.Scout(69, 3);
            b.Chaser(90, 5);
            b.Harvester(108, 2);
            b.Scout(134, 2);
            b.Drone(46, 2.8f, 3).Drone(110, 2.8f, 3);
            return b;
        }

        // ---------------------------------------------------------------- 8

        // A bridge, a lift up to the high mesa, a bridge along the top, tumbleweeds and quicksand, in a sandstorm.
        private static LevelBuilder Level8()
        {
            var b = new LevelBuilder("DustbowlCanyon_08", "Mine Shaft");
            b.Flat(26);                       // [-4,22)
            b.Gap(10);                        // [22,32) a rope bridge
            b.Flat(20);                       // [32,52)
            b.Gap(8);                         // [52,60) a lift: the only way up
            b.Flat(22, 4);                    // [60,82) top 4
            b.Gap(9);                         // [82,91) a rope bridge
            b.Flat(16, 4);                    // [91,107) top 4
            b.Flat(12, 2);                    // [107,119) top 2
            b.Flat(14, 0);                    // [119,133)
            b.Bridge(22, 10, 0);
            b.MovingLedge(54, 3, 0, 0, 4, 4f);          // top 0 <-> 4
            b.Bridge(82, 9, 4);

            b.Tumbleweeds(66, 80, 4f);
            b.Quicksand(95, 105);
            b.Tumbleweeds(123, 132, 3.5f, 0.4f);
            b.Sandstorm();

            b.StoneBlocks(38, 3, 4);          // bonus block stack with the coin
            b.BonusCoin(39.5f, 4);

            b.Start(0).Goal(130).Checkpoint(34).Checkpoint(62).Checkpoint(93).Checkpoint(121);
            b.Scout(10, 3);
            b.Harvester(44, 3);
            b.Scout(77, 2);
            b.Harvester(113, 2);
            b.Chaser(104, 5);
            b.Drone(46, 2.8f, 3).Drone(112, 2.8f, 3);
            return b;
        }

        // ---------------------------------------------------------------- 9

        // An island behind the start across a 15-unit gap (Gerald / Woolly), two lifts meeting, tumbleweeds and quicksand, terraces.
        private static LevelBuilder Level9()
        {
            // The gap's near edge is 19 behind the start, just off-screen at spawn.
            var b = new LevelBuilder("DustbowlCanyon_09", "Buzzard's Rest", -42);
            b.SecretFlat(8);                  // [-42,-34) island across a wide gap behind the start
            b.Gap(15);                        // [-34,-19)
            b.Flat(45);                       // [-19,26)
            b.Gap(4);                         // [26,30)
            b.Flat(30);                       // [30,60)
            b.Gap(12);                        // [60,72) two lifts meet mid-chasm
            b.Flat(22);                       // [72,94)
            b.Flat(16, 2);                    // [94,110) top 2
            b.Gap(4);                         // [110,114)
            b.Flat(20, 0);                    // [114,134)
            b.MovingLedge(60, 3, 0, 3, 0, 4f);    // [60,63) <-> [63,66)
            b.MovingLedge(69, 3, 0, -3, 0, 4f);   // [69,72) <-> [66,69): they meet at x=66

            b.Gate(CharacterType.Gerald, "Island across a 15-unit gap behind the start: too wide for the base jump; Puff Glide crosses it.", CharacterType.Woolly);
            b.SecretRow(-40.5f, -36.5f, 1f, 0.9f);
            b.StoneBlocks(38, 3, 4);          // perch for the passage sign past the first gap
            b.SecretPassage(39.5f, 4, PassageLayout.Pillars);

            b.Tumbleweeds(44, 58, 4.5f);
            b.Quicksand(80, 90);
            b.Tumbleweeds(118, 132, 4f, 0.5f);

            b.Start(0).Goal(130).Checkpoint(32).Checkpoint(74).Checkpoint(96).Checkpoint(116);
            b.Scout(12, 3);
            b.Scout(-10, 3);                  // guards the way back to the island's gap
            b.Harvester(56, 2);
            b.Scout(90, 2);
            b.Harvester(102, 3);
            b.Chaser(108, 5);
            b.Drone(44, 2.8f, 3).Drone(100, 2.8f, 3);
            return b;
        }

        // ---------------------------------------------------------------- 10

        // A slider, a riser and a sinker over the widest chasm, a bridge, quicksand and tumbleweeds, in a sandstorm.
        private static LevelBuilder Level10()
        {
            var b = new LevelBuilder("DustbowlCanyon_10", "Stampede Run");
            b.Flat(26);                       // [-4,22)
            b.Gap(18);                        // [22,40) the relay
            b.Flat(24);                       // [40,64)
            b.Gap(10);                        // [64,74) a rope bridge
            b.Flat(20);                       // [74,94)
            b.Flat(16, 2);                    // [94,110) top 2
            b.Gap(4);                         // [110,114)
            b.Flat(22, 0);                    // [114,136)
            b.MovingLedge(23, 2, 0, 5, 0, 4f);           // slides [23,25) <-> [28,30)
            b.MovingLedge(32, 2, 0, 0, 3, 3.5f);         // rises top 0 <-> 3
            b.MovingLedge(36, 3, 3, 0, -3, 3.5f, 0.5f);  // sinks top 3 <-> 0, low while the riser is high
            b.Bridge(64, 10, 0);

            b.Quicksand(48, 60);
            b.Tumbleweeds(80, 92, 3.5f);
            b.Quicksand(98, 106);
            b.Tumbleweeds(120, 134, 4f, 0.5f);
            b.Sandstorm();

            b.StoneBlocks(44, 3, 4);          // bonus block stack with the coin
            b.BonusCoin(45.5f, 4);

            b.Start(0).Goal(132).Checkpoint(42).Checkpoint(76).Checkpoint(96).Checkpoint(116);
            b.Scout(10, 3);
            b.Harvester(61, 1.5f);
            b.Scout(80, 1);
            b.Chaser(92, 5);
            b.Harvester(107, 2);
            b.Scout(130, 3);
            b.Drone(50, 2.8f, 3).Drone(90, 2.8f, 3).Drone(128, 2.8f, 3);
            return b;
        }

        // ---------------------------------------------------------------- 11

        // The capstone: a sliding lift, a lift up to the high mesa, two lifts meeting at the top, a bridge, down the steps; every hazard in one run, in a sandstorm. Five checkpoints.
        private static LevelBuilder Level11()
        {
            var b = new LevelBuilder("DustbowlCanyon_11", "Dust Storm Pass");
            b.Flat(24);                       // [-4,20)
            b.Gap(12);                        // [20,32) a sliding lift
            b.Flat(18);                       // [32,50)
            b.Gap(8);                         // [50,58) a lift: the only way up
            b.Flat(20, 4);                    // [58,78) top 4
            b.Gap(14);                        // [78,92) two lifts meeting at the top
            b.Flat(16, 4);                    // [92,108) top 4
            b.Gap(9);                         // [108,117) a rope bridge
            b.Flat(14, 4);                    // [117,131) top 4
            b.Flat(10, 2);                    // [131,141) top 2
            b.Gap(4);                         // [141,145)
            b.Flat(22, 0);                    // [145,167)
            b.MovingLedge(21, 3, 0, 7, 0, 4.5f);        // [21,24) <-> [28,31)
            b.MovingLedge(52, 3, 0, 0, 4, 4f);          // top 0 <-> 4
            b.MovingLedge(79, 3, 4, 4, 0, 4f);          // [79,82) <-> [83,86)
            b.MovingLedge(88, 3, 4, -2, 0, 4f);         // [88,91) <-> [86,89): they meet at x=86
            b.Bridge(108, 9, 4);

            b.Tumbleweeds(38, 49, 3.5f);
            b.Quicksand(66, 76);
            b.Tumbleweeds(96, 107, 3.5f, 0.5f);
            b.Quicksand(121, 130);
            b.Tumbleweeds(150, 165, 4.5f);
            b.Sandstorm();

            b.StoneBlocks(64, 3, 8);          // a double jump up from the high line
            b.SecretPassage(65.5f, 8, PassageLayout.Zigzag);

            b.Start(0).Goal(163).Checkpoint(34).Checkpoint(60).Checkpoint(94).Checkpoint(119).Checkpoint(147);
            b.Scout(10, 3);
            b.Harvester(46, 1.5f);
            b.Scout(67, 2);
            b.Chaser(76, 6);
            b.Harvester(105, 2);
            b.Scout(128, 2);
            b.Harvester(136, 2);
            b.Drone(40, 2.8f, 3).Drone(120, 2.8f, 2.5f).Drone(150, 2.8f, 3);
            return b;
        }

        // ---------------------------------------------------------------- boss

        // The Drill Rig's quarry: a run-in over a rope bridge, quicksand and a tumbleweed lane in a sandstorm, then the arena
        // between two cover mounds. Three hits; waves after hits 1 and 2. No goal: downing the Drill Rig completes the
        // level.
        private static LevelBuilder LevelBoss()
        {
            var b = new LevelBuilder("DustbowlCanyon_Boss", "The Drill Rig's Quarry");
            b.Flat(30);                       // [-4,26) run-in
            b.Gap(8);                         // [26,34) a rope bridge
            b.Flat(30);                       // [34,64)
            b.Gap(4);                         // [64,68)
            b.Flat(64);                       // [68,132) arena
            b.Bridge(26, 8, 0);
            b.Mound(74, 3, 2);                // cover / stomp platforms either side
            b.Mound(124, 3, 2);

            b.Quicksand(42, 50);
            b.Tumbleweeds(52, 62, 3.5f);
            b.Sandstorm();

            b.Boss();
            b.Start(0).Checkpoint(36).Checkpoint(70);
            b.Scout(12, 3).Harvester(20, 2);                         // run-in guards
            b.Commander(100, 12);                                    // the Drill Rig rolls [88,112]
            b.Harvester(84, 2).Scout(116, 2);                        // arena guards (wave 0)
            b.Scout(120, 2, wave: 1).Harvester(90, 2, wave: 1).Drone(108, 2.8f, 3, wave: 1);   // after hit 1
            b.Drone(96, 2.8f, 4, wave: 2).Drone(80, 2.8f, 3, wave: 2).Scout(110, 3, wave: 2);  // after hit 2
            b.Crop(75.5f);
            b.Crop(125.5f);
            return b;
        }
    }
}
