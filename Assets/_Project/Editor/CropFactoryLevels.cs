using System.Collections.Generic;
using FarmFuryStampede.Data;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// World 9, Crop Factory, the paid post-finale world: eleven levels and Harvester Prime, built 2026-10-17 (see
    /// StampedePhase5aSetup.CropFactoryAssets; all art from Kling in Sprites/UI/CropFactory). The robots' processing
    /// plant, so the twist is the machinery (GDD: "conveyors, rhythm crushers, laser gates"):
    ///   Conveyor()  a belt across a pit carrying the player with it (faster) or against them (slower);
    ///   Crusher()   a press that slams onto the path in a steady rhythm: run under it between slams;
    ///   LaserGate() a beam straight up from the floor, on for half its cycle: pass while it is off.
    /// Normal gravity (as Meadow Ruins), so plain gaps stay at 4; wider ones are crossed on belts or conveyor lifts
    /// (every moving ledge is the hovering conveyor lift).
    ///   1     belts only (with the player); 2 crushers join; 3 laser gates join
    ///   4-7   lifts meeting over a chasm (4), the belt gauntlet (5), the crusher row (6), the overhead line (7)
    ///   8-11  the lift shaft (8), the recycling pit (9), the relay (10), the capstone (11)
    ///   12    Harvester Prime's furnace, waves after hits 1 and 2
    /// Robots: Scouts and Harvesters from 1, Drones from 2, Chasers from 4. Character-gated secrets: 3 = high ledge
    /// (double jump / Woolly), 6 = Breakable Floor (Bessie), 9 = island behind the start across a 15-unit gap
    /// (Gerald / Woolly). Secret passages (rare pellet + coins) in 1, 3, 4, 5, 7, 9 and 11 (Steps, Staircase,
    /// Pyramid, Ledges, Tunnel, Pillars, Zigzag). Every level also gets PathCorn and StandardFarm (here the factory
    /// props), applied in CreateAll. Hazards keep clear of the start, checkpoints and robot patrols (validated).
    /// </summary>
    internal static class CropFactoryLevels
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

        // Loading Dock: the first belt carries the player over a pit; the passage sign on a perch near the end.
        private static LevelBuilder Level1()
        {
            var b = new LevelBuilder("CropFactory_01", "Loading Dock");
            b.Flat(30);                       // [-4,26)
            b.Gap(4);                         // [26,30)
            b.Flat(26);                       // [30,56)
            b.Gap(10);                        // [56,66) a belt, running with the player
            b.Flat(24);                       // [66,90)
            b.Gap(4);                         // [90,94)
            b.Flat(32);                       // [94,126)
            b.Conveyor(56, 10, 0, 2.5f);

            b.StoneBlocks(40, 3, 4);          // bonus crate stack with the coin
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

        // Press Line: crushers slam onto the path, a pair in turn; a belt; Drones overhead.
        private static LevelBuilder Level2()
        {
            var b = new LevelBuilder("CropFactory_02", "Press Line");
            b.Flat(34);                       // [-4,30)
            b.Gap(4);                         // [30,34)
            b.Flat(30);                       // [34,64)
            b.Gap(8);                         // [64,72) a belt with the player
            b.Flat(30);                       // [72,102)
            b.Gap(4);                         // [102,106)
            b.Flat(22);                       // [106,128)
            b.Conveyor(64, 8, 0, 3f);

            b.Crusher(20f);
            b.Crusher(46f).Crusher(50f, 3.2f, 0.5f);   // a pair slamming in turn
            b.Crusher(84f, 3f).Crusher(88f, 3f, 0.33f);

            b.StoneBlocks(24, 3, 4);          // bonus crate stack with the coin, over the first crusher's run-out
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

        // Laser Hall: laser gates pulse across the path, a high secret ledge, terraces, a crusher at the end.
        private static LevelBuilder Level3()
        {
            var b = new LevelBuilder("CropFactory_03", "Laser Hall");
            b.Flat(36);                       // [-4,32)
            b.Gap(4);                         // [32,36)
            b.Flat(28);                       // [36,64)
            b.Flat(18, 2);                    // [64,82) top 2
            b.Gap(4);                         // [82,86)
            b.Flat(16, 2);                    // [86,102) top 2
            b.Flat(24, 0);                    // [102,126)

            b.LaserGate(22f);
            b.LaserGate(52f, 3f, 0.5f);
            b.LaserGate(94f, 2.6f);
            b.Crusher(114f);

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

        // Twin Lifts: two conveyor lifts slide together over a chasm, a belt running against the player, a Chaser,
        // crushers; the passage sign on a perch just ahead of the start.
        private static LevelBuilder Level4()
        {
            var b = new LevelBuilder("CropFactory_04", "Twin Lifts");
            b.Flat(30);                       // [-4,26)
            b.Gap(12);                        // [26,38) two lifts meet mid-chasm
            b.Flat(26);                       // [38,64)
            b.Gap(10);                        // [64,74) a belt running against the player
            b.Flat(20);                       // [74,94)
            b.Flat(14, 2);                    // [94,108) top 2
            b.Gap(4);                         // [108,112)
            b.Flat(16, 0);                    // [112,128)
            b.MovingLedge(26, 3, 0, 3, 0, 4f);    // [26,29) <-> [29,32)
            b.MovingLedge(35, 3, 0, -3, 0, 4f);   // [35,38) <-> [32,35): they meet at x=32
            b.Conveyor(64, 10, 0, -2.5f);

            b.Crusher(52f);
            b.Crusher(84f, 3f);

            b.StoneBlocks(3, 3, 4);           // perch for the passage sign, just ahead of the start
            b.SecretPassage(4.5f, 4, PassageLayout.Pyramid);

            b.Start(0).Goal(124).Checkpoint(40).Checkpoint(76).Checkpoint(96).Checkpoint(114);
            b.Scout(14, 3);
            b.Harvester(60, 2);
            b.Chaser(90, 6);                  // wakes past the second crusher
            b.Scout(101, 3);
            b.Harvester(120, 3);
            b.Drone(46, 2.8f, 3).Drone(118, 2.8f, 3);
            return b;
        }

        // ---------------------------------------------------------------- 5

        // Sorting Floor: two belts in a row (with, then against), a pair of lasers pulsing in turn, a terrace crusher.
        private static LevelBuilder Level5()
        {
            var b = new LevelBuilder("CropFactory_05", "Sorting Floor");
            b.Flat(24);                       // [-4,20)
            b.Gap(8);                         // [20,28) a belt with the player
            b.Flat(16);                       // [28,44)
            b.Gap(8);                         // [44,52) a belt against the player
            b.Flat(24);                       // [52,76)
            b.Gap(3);                         // [76,79) up onto the terrace
            b.Flat(21, 2);                    // [79,100) top 2
            b.Flat(26, 0);                    // [100,126)
            b.Conveyor(20, 8, 0, 3f);
            b.Conveyor(44, 8, 0, -3f);

            b.LaserGate(36f, 2.6f);
            b.LaserGate(64f).LaserGate(68f, 3f, 0.5f);   // never both off: wait between them
            b.Crusher(90f);
            b.Crusher(112f, 3f);

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

        // Crusher Row: a cracked grate near the start (Bessie), three crushers slamming in sequence, one conveyor lift
        // sliding across a chasm.
        private static LevelBuilder Level6()
        {
            var b = new LevelBuilder("CropFactory_06", "Crusher Row");
            b.Flat(30);                       // [-4,26)
            b.Gap(4);                         // [26,30)
            b.Flat(40);                       // [30,70)
            b.Gap(12);                        // [70,82) one lift slides across
            b.Flat(22);                       // [82,104)
            b.Gap(4);                         // [104,108)
            b.Flat(20);                       // [108,128)
            b.MovingLedge(71, 3, 0, 7, 0, 4.5f);    // [71,74) <-> [78,81)

            b.BreakableFloor(16, 4);          // hollow beneath: Bessie's Ground Pound
            b.Gate(CharacterType.Bessie, "Cracked grate at x=16..20 hides a hollow below; only Ground Pound breaks it.");
            b.SecretRow(16.5f, 19.5f, 1f, -2.5f);

            b.Crusher(40f).Crusher(44f, 3.2f, 0.33f).Crusher(48f, 3.2f, 0.66f);   // a wave rolling along the row
            b.Crusher(92f, 2.8f);

            b.StoneBlocks(58, 3, 4);          // bonus crate stack with the coin
            b.BonusCoin(59.5f, 4);

            b.Start(0).Goal(124).Checkpoint(32).Checkpoint(84).Checkpoint(110);
            b.Scout(8, 2);
            b.Harvester(60, 3);
            b.Chaser(66, 6);                  // wakes past the crusher row, right before the chasm
            b.Scout(98, 2);
            b.Harvester(118, 3);
            b.Drone(54, 2.8f, 3).Drone(120, 2.8f, 3);
            return b;
        }

        // ---------------------------------------------------------------- 7

        // Overhead Line: two lifts bobbing in turn, terraces up to a belt at the top, lasers on the way, down to the end.
        private static LevelBuilder Level7()
        {
            var b = new LevelBuilder("CropFactory_07", "Overhead Line");
            b.Flat(28);                       // [-4,24)
            b.Gap(10);                        // [24,34) two lifts bobbing in turn
            b.Flat(26);                       // [34,60)
            b.Flat(16, 2);                    // [60,76) top 2
            b.Flat(16, 4);                    // [76,92) top 4
            b.Gap(8);                         // [92,100) a belt along the top
            b.Flat(14, 4);                    // [100,114) top 4
            b.Flat(12, 2);                    // [114,126) top 2
            b.Flat(14, 0);                    // [126,140)
            b.MovingLedge(26, 2, 1, 0, 3, 3.5f);         // top 1 <-> 4
            b.MovingLedge(30, 2, 1, 0, 3, 3.5f, 0.5f);   // up while the first is down
            b.Conveyor(92, 8, 4, 3f);

            b.LaserGate(48f);
            b.LaserGate(84f, 2.6f);
            b.Crusher(120f);

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

        // Lift Shaft: a belt against the player, a conveyor lift up to the high line, a belt along the top with
        // crushers either side, and down past a laser to the end.
        private static LevelBuilder Level8()
        {
            var b = new LevelBuilder("CropFactory_08", "Lift Shaft");
            b.Flat(26);                       // [-4,22)
            b.Gap(10);                        // [22,32) a belt against the player
            b.Flat(20);                       // [32,52)
            b.Gap(8);                         // [52,60) a lift: the only way up
            b.Flat(22, 4);                    // [60,82) top 4
            b.Gap(9);                         // [82,91) a belt along the top
            b.Flat(16, 4);                    // [91,107) top 4
            b.Flat(12, 2);                    // [107,119) top 2
            b.Flat(14, 0);                    // [119,133)
            b.Conveyor(22, 10, 0, -3f);
            b.MovingLedge(54, 3, 0, 0, 4, 4f);          // top 0 <-> 4
            b.Conveyor(82, 9, 4, 3f);

            b.Crusher(70f);
            b.Crusher(99f, 3f);
            b.LaserGate(126f, 2.6f);

            b.StoneBlocks(38, 3, 4);          // bonus crate stack with the coin
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

        // Recycling Pit: an island behind the start across a 15-unit gap (Gerald / Woolly), two lifts meeting, crushers
        // and a laser, terraces.
        private static LevelBuilder Level9()
        {
            // The gap's near edge is 19 behind the start, just off-screen at spawn.
            var b = new LevelBuilder("CropFactory_09", "Recycling Pit", -42);
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

            b.Crusher(50f);
            b.LaserGate(84f);
            b.Crusher(124f, 3f);

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

        // Conveyor Maze: a slider, a riser and a sinker in a row over the widest chasm, a long belt against the player,
        // laser pairs and a crusher pair, terraces.
        private static LevelBuilder Level10()
        {
            var b = new LevelBuilder("CropFactory_10", "Conveyor Maze");
            b.Flat(26);                       // [-4,22)
            b.Gap(18);                        // [22,40) the relay
            b.Flat(24);                       // [40,64)
            b.Gap(10);                        // [64,74) a fast belt against the player
            b.Flat(20);                       // [74,94)
            b.Flat(16, 2);                    // [94,110) top 2
            b.Gap(4);                         // [110,114)
            b.Flat(22, 0);                    // [114,136)
            b.MovingLedge(23, 2, 0, 5, 0, 4f);           // slides [23,25) <-> [28,30)
            b.MovingLedge(32, 2, 0, 0, 3, 3.5f);         // rises top 0 <-> 3
            b.MovingLedge(36, 3, 3, 0, -3, 3.5f, 0.5f);  // sinks top 3 <-> 0, low while the riser is high
            b.Conveyor(64, 10, 0, -3.5f);

            b.LaserGate(52f).LaserGate(56f, 3f, 0.5f);
            b.Crusher(84f, 3f).Crusher(88f, 3f, 0.5f);
            b.LaserGate(102f, 2.6f);
            b.Crusher(124f);

            b.StoneBlocks(44, 3, 4);          // bonus crate stack with the coin
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

        // Final Assembly, the capstone: a sliding lift, a lift up to the high line, two lifts meeting at the top, a belt,
        // down the steps and a crusher pair before the gate. Every hazard in one run; five checkpoints.
        private static LevelBuilder Level11()
        {
            var b = new LevelBuilder("CropFactory_11", "Final Assembly");
            b.Flat(24);                       // [-4,20)
            b.Gap(12);                        // [20,32) a sliding lift
            b.Flat(18);                       // [32,50)
            b.Gap(8);                         // [50,58) a lift: the only way up
            b.Flat(20, 4);                    // [58,78) top 4
            b.Gap(14);                        // [78,92) two lifts meeting at the top
            b.Flat(16, 4);                    // [92,108) top 4
            b.Gap(9);                         // [108,117) a belt along the top
            b.Flat(14, 4);                    // [117,131) top 4
            b.Flat(10, 2);                    // [131,141) top 2
            b.Gap(4);                         // [141,145)
            b.Flat(22, 0);                    // [145,167)
            b.MovingLedge(21, 3, 0, 7, 0, 4.5f);        // [21,24) <-> [28,31)
            b.MovingLedge(52, 3, 0, 0, 4, 4f);          // top 0 <-> 4
            b.MovingLedge(79, 3, 4, 4, 0, 4f);          // [79,82) <-> [83,86)
            b.MovingLedge(88, 3, 4, -2, 0, 4f);         // [88,91) <-> [86,89): they meet at x=86
            b.Conveyor(108, 9, 4, 3f);

            b.Crusher(42f);
            b.LaserGate(72f);
            b.Crusher(100f, 3f);
            b.LaserGate(124f, 2.6f);
            b.Crusher(155f).Crusher(159f, 3.2f, 0.5f);

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

        // Harvester Prime's furnace: a run-in with a belt against the player, a crusher and a laser, then the arena
        // between two cover mounds. Three hits; waves after hits 1 and 2. No goal: downing Harvester Prime completes
        // the level (and the game's true final boss).
        private static LevelBuilder LevelBoss()
        {
            var b = new LevelBuilder("CropFactory_Boss", "Harvester Prime's Furnace");
            b.Flat(30);                       // [-4,26) run-in
            b.Gap(8);                         // [26,34) a belt against the player
            b.Flat(30);                       // [34,64)
            b.Gap(4);                         // [64,68)
            b.Flat(64);                       // [68,132) arena
            b.Conveyor(26, 8, 0, -2.5f);
            b.Mound(74, 3, 2);                // cover / stomp platforms either side
            b.Mound(124, 3, 2);

            b.Crusher(46f);
            b.LaserGate(54f);

            b.Boss();
            b.Start(0).Checkpoint(36).Checkpoint(70);
            b.Scout(12, 3).Harvester(20, 2);                         // run-in guards
            b.Commander(100, 12);                                    // Harvester Prime rolls [88,112]
            b.Harvester(84, 2).Scout(116, 2);                        // arena guards (wave 0)
            b.Scout(120, 2, wave: 1).Harvester(90, 2, wave: 1).Drone(108, 2.8f, 3, wave: 1);   // after hit 1
            b.Drone(96, 2.8f, 4, wave: 2).Drone(80, 2.8f, 3, wave: 2).Scout(110, 3, wave: 2);  // after hit 2
            b.Crop(75.5f);
            b.Crop(125.5f);
            return b;
        }
    }
}
