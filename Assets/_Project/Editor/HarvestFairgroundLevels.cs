using System.Collections.Generic;
using FarmFuryStampede.Data;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// World 8, Harvest Fairground, a paid post-finale world: eleven levels and the Ringmaster Bot, built 2026-10-17 on
    /// the Crop Factory's validated layouts (see StampedePhase5aSetup.FairgroundAssets; all art from Kling in
    /// Sprites/UI/HarvestFairground). The county fair at night, so the twist is the rides (GDD: "hay-bale trampolines,
    /// ferris-wheel platforms, conveyor rides"):
    ///   Trampoline()  a hay bale that bounces the player high, here up to a bonus stage plank (Bounce());
    ///   FerrisWheel() gondolas going round a hub, each a level ride (levels 7, 8 and 11, in place of a lift);
    ///   Conveyor()    a conveyor ride across a pit, with the player or against them.
    /// Normal gravity; every other moving ledge is the carnival ride car. Robots: Scouts and Harvesters from 1, Drones
    /// from 2, Chasers from 4. Character-gated secrets: 3 = high ledge (double jump / Woolly), 6 = Breakable Floor
    /// (Bessie), 9 = island behind the start across a 15-unit gap (Gerald / Woolly). Secret passages (rare pellet +
    /// coins) in 1, 3, 4, 5, 7, 9 and 11 (Steps, Staircase, Pyramid, Ledges, Tunnel, Pillars, Zigzag). Every level also
    /// gets PathCorn and StandardFarm (here the fair's tents and rides), applied in CreateAll.
    /// </summary>
    internal static class HarvestFairgroundLevels
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

        // A hay-bale trampoline at x bouncing the player up beside a three-wide bonus stage plank at x+2 (its top 'top', at
        // most a jump's peak below the bounce), with two toffee apples and a coin on it.
        private static void Bounce(LevelBuilder b, int x, int top)
        {
            b.Trampoline(x, top + 1);
            b.BonusLedge(x + 2, 3, top);
            b.CropAt(x + 2.5f, top + 0.9f).BonusCoin(x + 3.5f, top).CropAt(x + 4.5f, top + 0.9f);
        }

        // ---------------------------------------------------------------- 1

        // The first conveyor ride carries the player over a pit; a trampoline up to a bonus plank; the passage sign on a perch near the end.
        private static LevelBuilder Level1()
        {
            var b = new LevelBuilder("HarvestFairground_01", "Gate Night");
            b.Flat(30);                       // [-4,26)
            b.Gap(4);                         // [26,30)
            b.Flat(26);                       // [30,56)
            b.Gap(10);                        // [56,66) a conveyor ride
            b.Flat(24);                       // [66,90)
            b.Gap(4);                         // [90,94)
            b.Flat(32);                       // [94,126)
            b.Conveyor(56, 10, 0, 2.5f);

            b.StoneBlocks(40, 3, 4);          // bonus crate stack with the coin
            b.BonusCoin(41.5f, 4);
            Bounce(b, 20, 5);
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

        // Trampolines up to bonus planks, a conveyor ride; Drones overhead.
        private static LevelBuilder Level2()
        {
            var b = new LevelBuilder("HarvestFairground_02", "Midway");
            b.Flat(34);                       // [-4,30)
            b.Gap(4);                         // [30,34)
            b.Flat(30);                       // [34,64)
            b.Gap(8);                         // [64,72) a conveyor ride
            b.Flat(30);                       // [72,102)
            b.Gap(4);                         // [102,106)
            b.Flat(22);                       // [106,128)
            b.Conveyor(64, 8, 0, 3f);

            Bounce(b, 46, 5);
            Bounce(b, 84, 5);

            b.StoneBlocks(24, 3, 4);          // bonus crate stack with the coin
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

        // Trampolines, a high secret ledge, terraces.
        private static LevelBuilder Level3()
        {
            var b = new LevelBuilder("HarvestFairground_03", "Hay Bounce");
            b.Flat(36);                       // [-4,32)
            b.Gap(4);                         // [32,36)
            b.Flat(28);                       // [36,64)
            b.Flat(18, 2);                    // [64,82) top 2
            b.Gap(4);                         // [82,86)
            b.Flat(16, 2);                    // [86,102) top 2
            b.Flat(24, 0);                    // [102,126)

            Bounce(b, 22, 5);
            Bounce(b, 94, 7);

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

        // Two ride cars slide together over a chasm, a conveyor ride against the player, trampolines and a Chaser; the passage sign on a perch just ahead of the start.
        private static LevelBuilder Level4()
        {
            var b = new LevelBuilder("HarvestFairground_04", "Bumper Lane");
            b.Flat(30);                       // [-4,26)
            b.Gap(12);                        // [26,38) two ride cars meet mid-chasm
            b.Flat(26);                       // [38,64)
            b.Gap(10);                        // [64,74) a conveyor ride
            b.Flat(20);                       // [74,94)
            b.Flat(14, 2);                    // [94,108) top 2
            b.Gap(4);                         // [108,112)
            b.Flat(16, 0);                    // [112,128)
            b.MovingLedge(26, 3, 0, 3, 0, 4f);    // [26,29) <-> [29,32)
            b.MovingLedge(35, 3, 0, -3, 0, 4f);   // [35,38) <-> [32,35): they meet at x=32
            b.Conveyor(64, 10, 0, -2.5f);

            Bounce(b, 52, 5);
            Bounce(b, 84, 5);

            b.StoneBlocks(3, 3, 4);           // perch for the passage sign, just ahead of the start
            b.SecretPassage(4.5f, 4, PassageLayout.Pyramid);

            b.Start(0).Goal(124).Checkpoint(40).Checkpoint(76).Checkpoint(96).Checkpoint(114);
            b.Scout(14, 3);
            b.Harvester(60, 2);
            b.Chaser(90, 6);                  // wakes past the second trampoline
            b.Scout(101, 3);
            b.Harvester(120, 3);
            b.Drone(46, 2.8f, 3).Drone(118, 2.8f, 3);
            return b;
        }

        // ---------------------------------------------------------------- 5

        // Two conveyor rides in a row (with, then against), trampolines up to bonus planks.
        private static LevelBuilder Level5()
        {
            var b = new LevelBuilder("HarvestFairground_05", "Prize Alley");
            b.Flat(24);                       // [-4,20)
            b.Gap(8);                         // [20,28) a conveyor ride
            b.Flat(16);                       // [28,44)
            b.Gap(8);                         // [44,52) a conveyor ride
            b.Flat(24);                       // [52,76)
            b.Gap(3);                         // [76,79) up onto the terrace
            b.Flat(21, 2);                    // [79,100) top 2
            b.Flat(26, 0);                    // [100,126)
            b.Conveyor(20, 8, 0, 3f);
            b.Conveyor(44, 8, 0, -3f);

            Bounce(b, 64, 5);
            Bounce(b, 112, 5);

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

        // A cracked prize box near the start (Bessie), trampolines, one ride car sliding across a chasm.
        private static LevelBuilder Level6()
        {
            var b = new LevelBuilder("HarvestFairground_06", "Big Top Row");
            b.Flat(30);                       // [-4,26)
            b.Gap(4);                         // [26,30)
            b.Flat(40);                       // [30,70)
            b.Gap(12);                        // [70,82) one ride car slides across
            b.Flat(22);                       // [82,104)
            b.Gap(4);                         // [104,108)
            b.Flat(20);                       // [108,128)
            b.MovingLedge(71, 3, 0, 7, 0, 4.5f);    // [71,74) <-> [78,81)

            b.BreakableFloor(16, 4);          // hollow beneath: Bessie's Ground Pound
            b.Gate(CharacterType.Bessie, "Cracked prize box at x=16..20 hides a hollow below; only Ground Pound breaks it.");
            b.SecretRow(16.5f, 19.5f, 1f, -2.5f);

            Bounce(b, 44, 5);
            Bounce(b, 92, 5);

            b.StoneBlocks(58, 3, 4);          // bonus crate stack with the coin
            b.BonusCoin(59.5f, 4);

            b.Start(0).Goal(124).Checkpoint(32).Checkpoint(84).Checkpoint(110);
            b.Scout(8, 2);
            b.Harvester(60, 3);
            b.Chaser(66, 6);                  // wakes past the trampolines, right before the chasm
            b.Scout(98, 2);
            b.Harvester(118, 3);
            b.Drone(54, 2.8f, 3).Drone(120, 2.8f, 3);
            return b;
        }

        // ---------------------------------------------------------------- 7

        // A ferris wheel over the first gap, terraces up to a conveyor ride along the top, trampolines.
        private static LevelBuilder Level7()
        {
            var b = new LevelBuilder("HarvestFairground_07", "Wheel of Fortune");
            b.Flat(28);                       // [-4,24)
            b.Gap(10);                        // [24,34) a ferris wheel
            b.Flat(26);                       // [34,60)
            b.Flat(16, 2);                    // [60,76) top 2
            b.Flat(16, 4);                    // [76,92) top 4
            b.Gap(8);                         // [92,100) a conveyor ride
            b.Flat(14, 4);                    // [100,114) top 4
            b.Flat(12, 2);                    // [114,126) top 2
            b.Flat(14, 0);                    // [126,140)
            b.FerrisWheel(29, 3, 3, 4, 9f);             // seats at deck tops 0 (bottom) to 6 (top): hop across on a low seat
            b.Conveyor(92, 8, 4, 3f);

            Bounce(b, 48, 5);
            Bounce(b, 84, 9);

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

        // A conveyor ride against the player, a ferris wheel up to the high stage, a ride along the top and trampolines either side.
        private static LevelBuilder Level8()
        {
            var b = new LevelBuilder("HarvestFairground_08", "Ferris Climb");
            b.Flat(26);                       // [-4,22)
            b.Gap(10);                        // [22,32) a conveyor ride
            b.Flat(20);                       // [32,52)
            b.Gap(8);                         // [52,60) a ferris wheel: the only way up
            b.Flat(22, 4);                    // [60,82) top 4
            b.Gap(9);                         // [82,91) a conveyor ride
            b.Flat(16, 4);                    // [91,107) top 4
            b.Flat(12, 2);                    // [107,119) top 2
            b.Flat(14, 0);                    // [119,133)
            b.Conveyor(22, 10, 0, -3f);
            b.FerrisWheel(56, 2, 2, 3, 8f);             // seats ride from deck top 0 up to 4: the only way up
            b.Conveyor(82, 9, 4, 3f);

            Bounce(b, 70, 9);
            Bounce(b, 99, 9);

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

        // An island behind the start across a 15-unit gap (Gerald / Woolly), two ride cars meeting, trampolines, terraces.
        private static LevelBuilder Level9()
        {
            // The gap's near edge is 19 behind the start, just off-screen at spawn.
            var b = new LevelBuilder("HarvestFairground_09", "Hall of Mirrors", -42);
            b.SecretFlat(8);                  // [-42,-34) island across a wide gap behind the start
            b.Gap(15);                        // [-34,-19)
            b.Flat(45);                       // [-19,26)
            b.Gap(4);                         // [26,30)
            b.Flat(30);                       // [30,60)
            b.Gap(12);                        // [60,72) two ride cars meet mid-chasm
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

            Bounce(b, 50, 5);
            Bounce(b, 84, 5);

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

        // A slider, a riser and a sinker over the widest chasm, a fast conveyor ride against the player, trampolines.
        private static LevelBuilder Level10()
        {
            var b = new LevelBuilder("HarvestFairground_10", "Rollercoaster");
            b.Flat(26);                       // [-4,22)
            b.Gap(18);                        // [22,40) the relay
            b.Flat(24);                       // [40,64)
            b.Gap(10);                        // [64,74) a fast conveyor ride
            b.Flat(20);                       // [74,94)
            b.Flat(16, 2);                    // [94,110) top 2
            b.Gap(4);                         // [110,114)
            b.Flat(22, 0);                    // [114,136)
            b.MovingLedge(23, 2, 0, 5, 0, 4f);           // slides [23,25) <-> [28,30)
            b.MovingLedge(32, 2, 0, 0, 3, 3.5f);         // rises top 0 <-> 3
            b.MovingLedge(36, 3, 3, 0, -3, 3.5f, 0.5f);  // sinks top 3 <-> 0, low while the riser is high
            b.Conveyor(64, 10, 0, -3.5f);

            Bounce(b, 56, 5);
            Bounce(b, 124, 5);

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

        // The capstone: a sliding ride car, a ferris wheel up to the high stage, two cars meeting at the top, a conveyor ride along the top, down the steps. Five checkpoints.
        private static LevelBuilder Level11()
        {
            var b = new LevelBuilder("HarvestFairground_11", "Grand Finale");
            b.Flat(24);                       // [-4,20)
            b.Gap(12);                        // [20,32) a sliding ride car
            b.Flat(18);                       // [32,50)
            b.Gap(8);                         // [50,58) a ferris wheel: the only way up
            b.Flat(20, 4);                    // [58,78) top 4
            b.Gap(14);                        // [78,92) two ride cars meeting at the top
            b.Flat(16, 4);                    // [92,108) top 4
            b.Gap(9);                         // [108,117) a conveyor ride
            b.Flat(14, 4);                    // [117,131) top 4
            b.Flat(10, 2);                    // [131,141) top 2
            b.Gap(4);                         // [141,145)
            b.Flat(22, 0);                    // [145,167)
            b.MovingLedge(21, 3, 0, 7, 0, 4.5f);        // [21,24) <-> [28,31)
            b.FerrisWheel(54, 2, 2, 3, 8f);             // seats ride from deck top 0 up to 4: the only way up
            b.MovingLedge(79, 3, 4, 4, 0, 4f);          // [79,82) <-> [83,86)
            b.MovingLedge(88, 3, 4, -2, 0, 4f);         // [88,91) <-> [86,89): they meet at x=86
            b.Conveyor(108, 9, 4, 3f);

            Bounce(b, 42, 5);
            Bounce(b, 100, 9);
            Bounce(b, 155, 5);

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

        // The Ringmaster's ring: a run-in over a conveyor ride and a trampoline, then the arena between two cover mounds.
        // Three hits; waves after hits 1 and 2. No goal: downing the Ringmaster Bot completes the level.
        private static LevelBuilder LevelBoss()
        {
            var b = new LevelBuilder("HarvestFairground_Boss", "The Ringmaster's Ring");
            b.Flat(30);                       // [-4,26) run-in
            b.Gap(8);                         // [26,34) a conveyor ride
            b.Flat(30);                       // [34,64)
            b.Gap(4);                         // [64,68)
            b.Flat(64);                       // [68,132) arena
            b.Conveyor(26, 8, 0, -2.5f);
            b.Mound(74, 3, 2);                // cover / stomp platforms either side
            b.Mound(124, 3, 2);

            Bounce(b, 46, 5);

            b.Boss();
            b.Start(0).Checkpoint(36).Checkpoint(70);
            b.Scout(12, 3).Harvester(20, 2);                         // run-in guards
            b.Commander(100, 12);                                    // the Ringmaster Bot rolls [88,112]
            b.Harvester(84, 2).Scout(116, 2);                        // arena guards (wave 0)
            b.Scout(120, 2, wave: 1).Harvester(90, 2, wave: 1).Drone(108, 2.8f, 3, wave: 1);   // after hit 1
            b.Drone(96, 2.8f, 4, wave: 2).Drone(80, 2.8f, 3, wave: 2).Scout(110, 3, wave: 2);  // after hit 2
            b.Crop(75.5f);
            b.Crop(125.5f);
            return b;
        }
    }
}
