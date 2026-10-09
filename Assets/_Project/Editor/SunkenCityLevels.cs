using System.Collections.Generic;
using FarmFuryStampede.Data;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// World 5, Sunken City: eleven levels and the boss, built 2026-10-04 (see StampedePhase5aSetup.SunkenAssets). The
    /// whole world is underwater (LevelBuilder.Underwater()): jumps reach the usual height but float ~1.35x as long, so
    /// the gaps are wider (6 units is the everyday jump here, validated against 6.5) and falls sink slowly. Every moving
    /// ledge is a submarine. Pearls are the crops, clams the secret crops, jellyfish take the Drones' place and the Deep
    /// Dredger is the boss.
    ///   1-3   Scouts and Harvesters; floaty gaps, a sliding sub (1), bobbing subs (2), a bridge (1, 3); jellyfish from 2
    ///   4-7   Chasers join; two subs meeting (4), a trench of floating stones (5), a long slide (6), three bobbers and a
    ///         terrace climb (7)
    ///   8-11  a sub lift (8, 11), the relay (10) and the capstone (11)
    ///   12    the boss: the Deep Dredger's trench, waves after hits 1 and 2
    /// Character-gated secrets: 3 = high ledge (double jump / Woolly), 6 = Breakable Floor (Bessie), 9 = island behind
    /// the start across a 17-unit gap (wider than in other worlds, as underwater jumps carry further; Gerald / Woolly).
    /// Secret passages (rare pellet + coins) in 1, 3, 4, 7, 9 and 11 (Steps, Staircase, Pyramid, Ledges, Pillars,
    /// Tunnel). Every level also gets PathCorn and StandardFarm (here the ruins and reef props), applied in CreateAll.
    /// Since 2026-10-09 every level 1-11 also has an air vent (AirVent: its bubbles carry the player up to a bonus rock
    /// with two pearls and a coin, at the Robot Mothership's validated gravity-lift spots), every ledge is floating rock
    /// and the submarine only sails across the background.
    /// </summary>
    internal static class SunkenCityLevels
    {
        public static List<LevelBuilder> CreateAll()
        {
            var levels = new List<LevelBuilder> { Level1(), Level2(), Level3(), Level4(), Level5(), Level6(), Level7(), Level8(),
                Level9(), Level10(), Level11(), LevelBoss() };
            foreach (var level in levels)
            {
                level.Underwater();
                level.PathCorn();
                level.StandardFarm();
            }
            return levels;
        }

        // An air vent at x whose bubbles carry the player 'height' up beside a three-wide bonus rock at x0 (top = the
        // seabed under the vent + height), with two pearls and a coin on it. The spots are the Robot Mothership's
        // gravity lifts (its levels are these layouts), validated the same way.
        private static void AirVent(LevelBuilder b, float x, int height, int x0, int top)
        {
            b.Updraft(x, height);
            b.BonusLedge(x0, 3, top);
            b.CropAt(x0 + 0.5f, top + 0.9f).BonusCoin(x0 + 1.5f, top).CropAt(x0 + 2.5f, top + 0.9f);
        }

        // ---------------------------------------------------------------- 1

        // Coral Gate: the first floaty jump, a submarine sliding over a trench, a bridge, the passage sign on a perch.
        private static LevelBuilder Level1()
        {
            var b = new LevelBuilder("SunkenCity_01", "Coral Gate");
            b.Flat(30);                       // [-4,26)
            b.Gap(6);                         // [26,32) a floaty jump
            b.Flat(20);                       // [32,52)
            b.Gap(12);                        // [52,64) a sub slides across
            b.Flat(24);                       // [64,88)
            b.Gap(8);                         // [88,96) under a rope bridge
            b.Flat(30);                       // [96,126)
            b.MovingLedge(53, 3, 0, 7, 0, 4.5f);        // [53,56) <-> [60,63)
            b.Bridge(88, 8, 0);

            b.StoneBlocks(100, 3, 4);         // perch for the passage sign past the bridge
            b.SecretPassage(101.5f, 4, PassageLayout.Steps);

            b.Start(0).Goal(122).Checkpoint(34).Checkpoint(66).Checkpoint(98);
            b.Scout(12, 3);
            b.Harvester(42, 3);
            b.Scout(76, 3);
            b.Harvester(108, 2).Scout(117, 2);
            AirVent(b, 20f, 6, 22, 6);
            return b;
        }

        // ---------------------------------------------------------------- 2

        // Kelp Forest: two subs bobbing in turn over a gap, up onto a terrace and down to the finish.
        private static LevelBuilder Level2()
        {
            var b = new LevelBuilder("SunkenCity_02", "Kelp Forest");
            b.Flat(26);                       // [-4,22)
            b.Gap(6);                         // [22,28)
            b.Flat(18);                       // [28,46)
            b.Gap(10);                        // [46,56) two bobbing subs
            b.Flat(20, 2);                    // [56,76) top 2
            b.Flat(16, 0);                    // [76,92)
            b.Gap(6);                         // [92,98)
            b.Flat(26);                       // [98,124)
            b.MovingLedge(48, 2, 1, 0, 3, 3.5f);         // top 1 <-> 4
            b.MovingLedge(52, 2, 1, 0, 3, 3.5f, 0.5f);   // up while the first is down

            b.StoneBlocks(104, 3, 4);         // bonus perch with the coin
            b.BonusCoin(105.5f, 4);

            b.Start(0).Goal(120).Checkpoint(30).Checkpoint(58).Checkpoint(100);
            b.Scout(10, 3);
            b.Harvester(38, 3);
            b.Scout(66, 3);
            b.Harvester(84, 2);
            b.Scout(112, 3);
            b.Drone(38, 2.8f, 3).Drone(112, 2.8f, 3);
            AirVent(b, 43f, 6, 39, 6);
            return b;
        }

        // ---------------------------------------------------------------- 3

        // Statue Garden: a bridge, a high secret ledge past it, a terrace, a floaty jump down to the finish.
        private static LevelBuilder Level3()
        {
            var b = new LevelBuilder("SunkenCity_03", "Statue Garden");
            b.Flat(32);                       // [-4,28)
            b.Gap(9);                         // [28,37) under a rope bridge
            b.Flat(24);                       // [37,61)
            b.Flat(18, 2);                    // [61,79) top 2
            b.Gap(6);                         // [79,85)
            b.Flat(36, 0);                    // [85,121)
            b.Bridge(28, 9, 0);

            b.SecretLedge(46, 5, 5);          // 5 up: the double jump every character has (or Woolly's Cloud Step)
            b.Gate(CharacterType.Cluck, "Ledge 5 units up: needs extra height (the double jump every character has, or Woolly's Cloud Step).", CharacterType.Woolly);
            b.SecretRow(46.5f, 50.5f, 1f, 5.9f);

            b.StoneBlocks(97, 3, 4);
            b.SecretPassage(98.5f, 4, PassageLayout.Staircase);

            b.Start(0).Goal(117).Checkpoint(39).Checkpoint(63).Checkpoint(87);
            b.Scout(12, 3).Harvester(20, 2);
            b.Scout(54, 3);
            b.Harvester(70, 3);
            b.Scout(107, 3).Harvester(113, 2);
            b.Drone(66, 2.8f, 3);
            AirVent(b, 90f, 6, 92, 6);
            return b;
        }

        // ---------------------------------------------------------------- 4

        // Twin Subs: two subs slide together over a wide trench and apart again; a Chaser on the terrace; a bridge.
        private static LevelBuilder Level4()
        {
            var b = new LevelBuilder("SunkenCity_04", "Twin Subs");
            b.Flat(30);                       // [-4,26)
            b.Gap(16);                        // [26,42) two subs meeting mid-way
            b.Flat(24);                       // [42,66)
            b.Flat(16, 2);                    // [66,82) top 2
            b.Gap(10);                        // [82,92) rope bridge at terrace height
            b.Flat(16, 2);                    // [92,108) top 2
            b.Flat(18, 0);                    // [108,126)
            b.MovingLedge(27, 3, 0, 4, 0, 4f);          // [27,30) <-> [31,34)
            b.MovingLedge(38, 3, 0, -4, 0, 4f);         // [38,41) <-> [34,37): they meet in the middle
            b.Bridge(82, 10, 2);

            b.StoneBlocks(70, 3, 6);          // a double jump up from the terrace
            b.SecretPassage(71.5f, 6, PassageLayout.Pyramid);

            b.Start(0).Goal(122).Checkpoint(44).Checkpoint(68).Checkpoint(94).Checkpoint(110);
            b.Scout(10, 3).Harvester(18, 3);
            b.Scout(53, 3).Harvester(60, 2);
            b.Chaser(78, 8);                  // wakes on the terrace before the bridge
            b.Scout(100, 3);
            b.Harvester(116, 3);
            b.Drone(56, 2.8f, 3).Drone(100, 2.8f, 3);
            AirVent(b, 47f, 7, 49, 7);
            return b;
        }

        // ---------------------------------------------------------------- 5

        // Treasure Trench: a 20-wide trench crossed on two floating stones (floaty 5-unit hops), a long ridge, then
        // the drop to the finish.
        private static LevelBuilder Level5()
        {
            var b = new LevelBuilder("SunkenCity_05", "Treasure Trench");
            b.Flat(24);                       // [-4,20)
            b.Gap(20);                        // [20,40) the trench
            b.Flat(22, 2);                    // [40,62) top 2
            b.Flat(18, 0);                    // [62,80)
            b.Gap(6);                         // [80,86)
            b.Flat(14, 0);                    // [86,100)
            b.Flat(10, 2);                    // [100,110) top 2
            b.Flat(16, 0);                    // [110,126)
            b.Floating(25, 3, 1);             // [25,28) top 1: 5 out from the edge
            b.Floating(33, 3, 2);             // [33,36) top 2: 5 on, then 4 to the ridge

            b.StoneBlocks(90, 3, 4);          // bonus perch with the coin
            b.BonusCoin(91.5f, 4);

            b.Start(0).Goal(122).Checkpoint(42).Checkpoint(64).Checkpoint(88).Checkpoint(112);
            b.Scout(10, 3);
            b.Harvester(52, 3);
            b.Scout(71, 3);
            b.Chaser(96, 5);
            b.Harvester(105, 2);
            b.Scout(119, 1);
            b.Drone(56, 2.8f, 3);
            AirVent(b, 66f, 6, 68, 6);
            return b;
        }

        // ---------------------------------------------------------------- 6

        // Sunken Plaza: a cracked floor (Bessie), one sub sliding all the way across a wide trench.
        private static LevelBuilder Level6()
        {
            var b = new LevelBuilder("SunkenCity_06", "Sunken Plaza");
            b.Flat(26);                       // [-4,22)
            b.Gap(6);                         // [22,28)
            b.Flat(30);                       // [28,58)
            b.Gap(14);                        // [58,72) one sub slides all the way across
            b.Flat(24);                       // [72,96)
            b.Gap(6);                         // [96,102)
            b.Flat(24);                       // [102,126)
            b.MovingLedge(59, 3, 0, 9, 0, 5f);          // [59,62) <-> [68,71)

            b.BreakableFloor(39, 4);          // hollow beneath: Bessie's Ground Pound
            b.Gate(CharacterType.Bessie, "Cracked floor at x=39..43 hides a hollow below; only Ground Pound breaks it.");
            b.SecretRow(39.5f, 42.5f, 1f, -2.5f);

            b.Start(0).Goal(122).Checkpoint(30).Checkpoint(74).Checkpoint(104);
            b.Scout(10, 3);
            b.Harvester(50, 3);
            b.Chaser(56, 6);                  // wakes just before the trench
            b.Scout(82, 3).Harvester(90, 2);
            b.Scout(112, 3).Harvester(118, 1);
            b.Drone(46, 3.2f, 3).Drone(112, 2.8f, 3);
            AirVent(b, 33f, 7, 35, 7);
            return b;
        }

        // ---------------------------------------------------------------- 7

        // Archway Climb: three subs bobbing in turn over a wide trench, then terraces climbing to a high ridge and
        // stepping back down.
        private static LevelBuilder Level7()
        {
            var b = new LevelBuilder("SunkenCity_07", "Archway Climb");
            b.Flat(28);                       // [-4,24)
            b.Gap(14);                        // [24,38) three bobbing subs
            b.Flat(26);                       // [38,64)
            b.Flat(18, 2);                    // [64,82) top 2
            b.Flat(14, 4);                    // [82,96) top 4
            b.Gap(6);                         // [96,102) a floaty jump along the ridge
            b.Flat(16, 4);                    // [102,118) top 4
            b.Flat(12, 2);                    // [118,130) top 2
            b.Flat(10, 0);                    // [130,140)
            b.MovingLedge(26, 2, 1, 0, 3, 3.5f);
            b.MovingLedge(30, 2, 1, 0, 3, 3.5f, 0.5f);
            b.MovingLedge(34, 2, 1, 0, 3, 3.5f);

            b.StoneBlocks(46, 3, 4);
            b.SecretPassage(47.5f, 4, PassageLayout.Ledges);

            b.Start(0).Goal(136).Checkpoint(40).Checkpoint(66).Checkpoint(84).Checkpoint(104).Checkpoint(120);
            b.Scout(10, 3).Harvester(18, 2);
            b.Scout(56, 3);
            b.Harvester(73, 3);
            b.Chaser(92, 4);
            b.Scout(110, 3);
            b.Harvester(125, 1);
            b.Drone(58, 2.8f, 3).Drone(110, 2.8f, 3);
            AirVent(b, 61f, 7, 62, 7);
            return b;
        }

        // ---------------------------------------------------------------- 8

        // Wreck Alley: a sliding sub, a sub lift up to the high ridge, a bridge at the top and the long way down.
        private static LevelBuilder Level8()
        {
            var b = new LevelBuilder("SunkenCity_08", "Wreck Alley");
            b.Flat(26);                       // [-4,22)
            b.Gap(10);                        // [22,32) a sliding sub
            b.Flat(20);                       // [32,52)
            b.Gap(8);                         // [52,60) a sub lift
            b.Flat(22, 4);                    // [60,82) top 4
            b.Gap(9);                         // [82,91) rope bridge at the top
            b.Flat(16, 4);                    // [91,107) top 4
            b.Flat(12, 2);                    // [107,119) top 2
            b.Flat(14, 0);                    // [119,133)
            b.MovingLedge(23, 3, 0, 5, 0, 4f);          // [23,26) <-> [28,31)
            b.MovingLedge(54, 3, 0, 0, 4, 4f);          // top 0 <-> 4: the only way up
            b.Bridge(82, 9, 4);

            b.Start(0).Goal(129).Checkpoint(34).Checkpoint(62).Checkpoint(93).Checkpoint(121);
            b.Scout(10, 3).Harvester(15, 2);
            b.Scout(42, 3).Harvester(48, 2);
            b.Scout(71, 2);
            b.Chaser(78, 6);
            b.Harvester(100, 2);
            b.Scout(113, 2);
            b.Drone(44, 2.8f, 3).Drone(96, 2.8f, 3);
            AirVent(b, 65f, 6, 61, 10);
            return b;
        }

        // ---------------------------------------------------------------- 9

        // Drowned Isle: an island behind the start across a 17-unit gap (Gerald / Woolly), a floaty jump, terraces,
        // the passage sign on a perch.
        private static LevelBuilder Level9()
        {
            // The gap's near edge is 21 behind the start, off-screen at spawn.
            var b = new LevelBuilder("SunkenCity_09", "Drowned Isle", -46);
            b.SecretFlat(8);                  // [-46,-38) island across a wide gap behind the start
            b.Gap(17);                        // [-38,-21)
            b.Flat(47);                       // [-21,26)
            b.Gap(6);                         // [26,32)
            b.Flat(22);                       // [32,54)
            b.Flat(16, 2);                    // [54,70) top 2
            b.Flat(16, 4);                    // [70,86) top 4
            b.Gap(6);                         // [86,92)
            b.Flat(14, 2);                    // [92,106) top 2
            b.Flat(20, 0);                    // [106,126)

            b.Gate(CharacterType.Gerald, "Island across a 17-unit gap behind the start: too wide even for a floaty underwater jump; Puff Glide crosses it.", CharacterType.Woolly);
            b.SecretRow(-44.5f, -40.5f, 1f, 0.5f);
            b.StoneBlocks(38, 3, 4);          // perch past the first gap
            b.SecretPassage(39.5f, 4, PassageLayout.Pillars);

            b.Start(0).Goal(122).Checkpoint(34).Checkpoint(56).Checkpoint(72).Checkpoint(94).Checkpoint(108);
            b.Scout(12, 3);
            b.Scout(46, 3);
            b.Scout(62, 3);
            b.Chaser(80, 6);
            b.Scout(99, 2);
            b.Harvester(116, 3);
            b.Drone(44, 2.8f, 3).Drone(100, 2.8f, 3);
            AirVent(b, -10f, 7, -7, 7);
            return b;
        }

        // ---------------------------------------------------------------- 10

        // Sub Relay: a slider, a riser and a sinker in a row over the widest trench, a bridge, the climb out.
        private static LevelBuilder Level10()
        {
            var b = new LevelBuilder("SunkenCity_10", "Sub Relay");
            b.Flat(26);                       // [-4,22)
            b.Gap(18);                        // [22,40) the relay
            b.Flat(24);                       // [40,64)
            b.Gap(10);                        // [64,74) rope bridge
            b.Flat(20);                       // [74,94)
            b.Flat(16, 2);                    // [94,110) top 2
            b.Gap(6);                         // [110,116)
            b.Flat(20, 0);                    // [116,136)
            b.MovingLedge(23, 2, 0, 5, 0, 4f);           // slides [23,25) <-> [28,30)
            b.MovingLedge(32, 2, 0, 0, 3, 3.5f);         // rises top 0 <-> 3
            b.MovingLedge(36, 3, 3, 0, -3, 3.5f, 0.5f);  // sinks top 3 <-> 0, low while the riser is high
            b.Bridge(64, 10, 0);

            b.StoneBlocks(48, 3, 4);          // bonus perch with the coin
            b.BonusCoin(49.5f, 4);

            b.Start(0).Goal(132).Checkpoint(42).Checkpoint(76).Checkpoint(96).Checkpoint(118);
            b.Scout(10, 3).Harvester(16, 2);
            b.Scout(52, 2).Harvester(58, 2);
            b.Scout(82, 3);
            b.Chaser(90, 6);
            b.Harvester(102, 2);
            b.Scout(125, 3);
            b.Drone(52, 2.8f, 3).Drone(86, 2.8f, 3).Drone(126, 2.8f, 3);
            AirVent(b, 78f, 7, 80, 7);
            return b;
        }

        // ---------------------------------------------------------------- 11

        // Abyss Run, the capstone: a sliding sub, a sub lift to the high ridge, two subs meeting over a trench at the
        // top, a bridge, down the steps and one last floaty jump to the gate. Five checkpoints.
        private static LevelBuilder Level11()
        {
            var b = new LevelBuilder("SunkenCity_11", "Abyss Run");
            b.Flat(24);                       // [-4,20)
            b.Gap(12);                        // [20,32) a sliding sub
            b.Flat(18);                       // [32,50)
            b.Gap(8);                         // [50,58) a sub lift
            b.Flat(20, 4);                    // [58,78) top 4
            b.Gap(14);                        // [78,92) two subs meeting at the top
            b.Flat(16, 4);                    // [92,108) top 4
            b.Gap(9);                         // [108,117) rope bridge
            b.Flat(14, 4);                    // [117,131) top 4
            b.Flat(10, 2);                    // [131,141) top 2
            b.Gap(6);                         // [141,147)
            b.Flat(20, 0);                    // [147,167)
            b.MovingLedge(21, 3, 0, 7, 0, 4.5f);        // [21,24) <-> [28,31)
            b.MovingLedge(52, 3, 0, 0, 4, 4f);          // top 0 <-> 4: the only way up
            b.MovingLedge(79, 3, 4, 4, 0, 4f);          // [79,82) <-> [83,86)
            b.MovingLedge(88, 3, 4, -2, 0, 4f);         // [88,91) <-> [86,89): they meet at x=86
            b.Bridge(108, 9, 4);

            b.StoneBlocks(64, 3, 8);          // a double jump up from the high ridge
            b.SecretPassage(65.5f, 8, PassageLayout.Tunnel);

            b.Start(0).Goal(163).Checkpoint(34).Checkpoint(60).Checkpoint(94).Checkpoint(119).Checkpoint(149);
            b.Scout(10, 3);
            b.Scout(42, 3);
            b.Harvester(70, 3);
            b.Chaser(76, 6);
            b.Scout(100, 3);
            b.Harvester(125, 2);
            b.Scout(136, 2);
            b.Harvester(156, 3);
            b.Drone(44, 2.8f, 3).Drone(100, 2.8f, 3).Drone(156, 2.8f, 3);
            AirVent(b, 151f, 7, 152, 7);
            return b;
        }

        // ---------------------------------------------------------------- boss

        // The Deep Dredger's trench: a run-in guarded by a Harvester, a Scout and a Chaser, a floaty jump to the gate
        // and its checkpoint, then the arena between two cover mounds. Three hits; waves after hits 1 and 2
        // (jellyfish among them). No goal: downing the Deep Dredger completes the level.
        private static LevelBuilder LevelBoss()
        {
            var b = new LevelBuilder("SunkenCity_Boss", "Deep Dredger's Trench");
            b.Flat(34);                       // [-4,30) run-in
            b.Gap(5);                         // [30,35)
            b.Flat(60);                       // [35,95) arena
            b.Mound(42, 3, 2);                // cover / stomp platforms either side
            b.Mound(86, 3, 2);

            b.Boss();
            b.Start(0).Checkpoint(38);
            b.Scout(10, 3).Harvester(18, 2);                        // run-in guards
            b.Chaser(26, 8);                                        // wakes at x=18, right before the gap to the gate
            b.Commander(64, 8);                                     // the Deep Dredger patrols [56,72]
            b.Harvester(52, 2).Scout(76, 2);                        // arena guards (wave 0)
            b.Scout(91, 2, wave: 1).Harvester(58, 2, wave: 1).Drone(78, 2.8f, 3, wave: 1);   // after hit 1
            b.Drone(62, 2.8f, 4, wave: 2).Drone(50, 2.8f, 3, wave: 2).Scout(72, 3, wave: 2); // after hit 2
            b.Crop(43.5f);
            b.Crop(87.5f);
            return b;
        }
    }
}
