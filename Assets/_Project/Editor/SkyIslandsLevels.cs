using System.Collections.Generic;
using FarmFuryStampede.Data;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// World 4, Sky Islands: eleven levels and the boss, built 2026-10-04 on Watermill Village's validated layouts,
    /// re-dressed for the sky (see StampedePhase5aSetup.SkyAssets): every ledge and moving ledge is a flat-topped
    /// cloud, the lifts are hot-air balloons (Balloon()), lanterns hang under the rope bridges, plums are the crops,
    /// storm clouds (StormCloud(), scenery only since 2026-10-08) float high over each Drone, and the Storm Baron airship is
    /// the boss. The world's twist is the wind:
    /// every level has an Updraft() that floats the player up to a bonus cloud (plums and a coin) too high to jump to.
    ///   1-3   Scouts and Harvesters; a sliding cloud (1), bobbing clouds (2), bridges (1, 3)
    ///   4-7   Chasers join; two clouds meeting mid-gap (4), a balloon up to a terrace (5), a long slide (6), three bobbers (7)
    ///   8-11  every trick in one level, a cloud relay (10), and the capstone (11) with all of them
    ///   12    the boss: the Storm Baron's sky dock, waves after hits 1 and 2
    /// Character-gated secrets: 3 = high ledge (double jump / Woolly), 6 = Breakable Floor (Bessie), 9 = island behind
    /// the start (Gerald / Woolly). Secret passages (rare pellet + coins) in 1, 3, 4, 7, 9 and 11, each a different room
    /// (Zigzag, Tunnel, Ledges, Staircase, Pyramid, Pillars). Every level also gets PathCorn and StandardFarm (here
    /// the Sky props), applied in CreateAll.
    /// </summary>
    internal static class SkyIslandsLevels
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

        // An updraft at x carrying the player 'height' up beside a three-wide bonus cloud at x0 (top = the ground under
        // the updraft + height), with two plums and a coin on it.
        private static void WindCloud(LevelBuilder b, float x, int height, int x0, int top)
        {
            b.Updraft(x, height);
            b.BonusLedge(x0, 3, top);
            b.CropAt(x0 + 0.5f, top + 0.9f).BonusCoin(x0 + 1.5f, top).CropAt(x0 + 2.5f, top + 0.9f);
        }

        // ---------------------------------------------------------------- 1

        // Cloudbank Meadow: an updraft to the first bonus cloud, a cloud sliding to and fro over the first big gap, a
        // lantern-hung rope bridge, then a perch with the passage sign.
        private static LevelBuilder Level1()
        {
            var b = new LevelBuilder("SkyIslands_01", "Cloudbank Meadow");
            b.Flat(30);                       // [-4,26)
            b.Gap(3);                         // [26,29)
            b.Flat(20);                       // [29,49)
            b.Gap(12);                        // [49,61) one cloud slides across
            b.Flat(22);                       // [61,83)
            b.Gap(8);                         // [83,91) under a rope bridge
            b.Flat(30);                       // [91,121)
            b.MovingLedge(50, 3, 0, 7, 0, 4f);          // [50,53) <-> [57,60)
            b.Bridge(83, 8, 0);

            WindCloud(b, 20f, 6, 22, 6);      // the first updraft, by the start

            b.StoneBlocks(95, 3, 4);          // perch for the passage sign past the bridge (a double jump up)
            b.SecretPassage(96.5f, 4, PassageLayout.Zigzag);

            b.Start(0).Goal(117).Checkpoint(31).Checkpoint(63).Checkpoint(93);
            b.Scout(10, 3);
            b.Harvester(40, 3);
            b.Scout(72, 3);
            b.Harvester(102, 3).Scout(112, 2);
            return b;
        }

        // ---------------------------------------------------------------- 2

        // Puffball Pass: two clouds bobbing up and down in turn over a gap, up onto a terrace and down to the finish.
        private static LevelBuilder Level2()
        {
            var b = new LevelBuilder("SkyIslands_02", "Puffball Pass");
            b.Flat(26);                       // [-4,22)
            b.Gap(4);                         // [22,26)
            b.Flat(18);                       // [26,44)
            b.Gap(10);                        // [44,54) two bobbing clouds
            b.Flat(20, 2);                    // [54,74) top 2
            b.Flat(16, 0);                    // [74,90)
            b.Gap(5);                         // [90,95)
            b.Flat(25);                       // [95,120)
            b.MovingLedge(46, 2, 1, 0, 3, 3.5f);         // top 1 <-> 4
            b.MovingLedge(50, 2, 1, 0, 3, 3.5f, 0.5f);   // up while the first is down

            WindCloud(b, 40.5f, 6, 36, 6);

            b.StoneBlocks(100, 3, 4);         // bonus perch with the coin
            b.BonusCoin(101.5f, 4);

            b.Start(0).Goal(116).Checkpoint(28).Checkpoint(56).Checkpoint(97);
            b.Scout(10, 3);
            b.Harvester(35, 3);
            b.Scout(64, 3);
            b.Harvester(82, 2);
            b.Scout(108, 3);
            b.Drone(36, 2.8f, 3).StormCloud(36).Drone(110, 2.8f, 3).StormCloud(110);
            return b;
        }

        // ---------------------------------------------------------------- 3

        // Lantern Bridge: a bridge with a lantern beneath, a high secret ledge, a terrace, and an updraft near the end.
        private static LevelBuilder Level3()
        {
            var b = new LevelBuilder("SkyIslands_03", "Lantern Bridge");
            b.Flat(32);                       // [-4,28)
            b.Gap(9);                         // [28,37) under a rope bridge
            b.Flat(24);                       // [37,61)
            b.Flat(18, 2);                    // [61,79) top 2
            b.Gap(4);                         // [79,83)
            b.Flat(36, 0);                    // [83,119)
            b.Bridge(28, 9, 0);

            b.SecretLedge(46, 5, 5);          // 5 up: the double jump every character has (or Woolly's Cloud Step)
            b.Gate(CharacterType.Cluck, "Ledge 5 units up: needs extra height (the double jump every character has, or Woolly's Cloud Step).", CharacterType.Woolly);
            b.SecretRow(46.5f, 50.5f, 1f, 5.9f);

            WindCloud(b, 87f, 6, 89, 6);      // well away from the secret ledge

            b.StoneBlocks(95, 3, 4);
            b.SecretPassage(96.5f, 4, PassageLayout.Tunnel);

            b.Start(0).Goal(115).Checkpoint(39).Checkpoint(63).Checkpoint(85);
            b.Scout(12, 3).Harvester(20, 2);
            b.Scout(54, 3);
            b.Harvester(70, 3);
            b.Scout(105, 3).Harvester(111, 2);
            b.Drone(66, 2.8f, 3).StormCloud(66);
            return b;
        }

        // ---------------------------------------------------------------- 4

        // Twin Clouds: two clouds slide together over a wide gap and apart again; a Chaser on the terrace; a bridge.
        private static LevelBuilder Level4()
        {
            var b = new LevelBuilder("SkyIslands_04", "Twin Clouds");
            b.Flat(30);                       // [-4,26)
            b.Gap(16);                        // [26,42) two clouds meeting mid-way
            b.Flat(24);                       // [42,66)
            b.Flat(16, 2);                    // [66,82) top 2
            b.Gap(10);                        // [82,92) rope bridge at terrace height
            b.Flat(16, 2);                    // [92,108) top 2
            b.Flat(18, 0);                    // [108,126)
            b.MovingLedge(27, 3, 0, 4, 0, 4f);          // [27,30) <-> [31,34)
            b.MovingLedge(38, 3, 0, -4, 0, 4f);         // [38,41) <-> [34,37): they meet in the middle
            b.Bridge(82, 10, 2);

            WindCloud(b, 47f, 7, 49, 7);

            b.StoneBlocks(70, 3, 6);          // a double jump up from the terrace
            b.SecretPassage(71.5f, 6, PassageLayout.Ledges);

            b.Start(0).Goal(122).Checkpoint(44).Checkpoint(68).Checkpoint(94).Checkpoint(110);
            b.Scout(10, 3).Harvester(18, 3);
            b.Scout(53, 3).Harvester(60, 2);
            b.Chaser(78, 8);                  // wakes on the terrace before the bridge
            b.Scout(100, 3);
            b.Harvester(116, 3);
            b.Drone(56, 2.8f, 3).StormCloud(56).Drone(100, 2.8f, 3).StormCloud(100);
            return b;
        }

        // ---------------------------------------------------------------- 5

        // Balloon Rise: a hot-air balloon lifts the player to the high terrace, down the steps, and a bonus route over
        // the finish (two clouds joined by a rope bridge, then puffs back down).
        private static LevelBuilder Level5()
        {
            var b = new LevelBuilder("SkyIslands_05", "Balloon Rise");
            b.Flat(28);                       // [-4,24)
            b.Gap(8);                         // [24,32) a balloon rises to the terrace
            b.Flat(20, 4);                    // [32,52) top 4
            b.Flat(16, 2);                    // [52,68) top 2
            b.Gap(5);                         // [68,73)
            b.Flat(20, 2);                    // [73,93) top 2
            b.Flat(30, 0);                    // [93,123)
            b.Balloon(27, 0, 4, 4.5f);        // deck [27,29), top 0 <-> 4: the only way up

            WindCloud(b, 21f, 6, 16, 6);

            b.BonusLedge(97, 3, 5);           // 5 up: the double jump
            b.Bridge(100, 6, 5, bonus: true);
            b.BonusLedge(106, 3, 5);
            b.StoneBlocks(109, 2, 4).StoneBlocks(112, 1, 3);   // stepping back down
            b.CropAt(98.5f, 5.9f);
            b.CropAt(101f, 5.9f).BonusCoin(103f, 5).CropAt(105f, 5.9f);
            b.CropAt(107.5f, 5.9f);
            b.CropAt(110f, 4.9f).CropAt(112.5f, 3.9f);

            b.Start(0).Goal(119).Checkpoint(34).Checkpoint(54).Checkpoint(75).Checkpoint(95);
            b.Scout(10, 3).Harvester(15, 1);
            b.Scout(42, 3);
            b.Harvester(60, 3);
            b.Scout(83, 3);
            b.Chaser(90, 6);
            b.Harvester(117, 1);
            b.Drone(62, 2.8f, 3).StormCloud(62);
            return b;
        }

        // ---------------------------------------------------------------- 6

        // Windmill Race: a cracked floor (Bessie), one cloud sliding all the way across a wide gap.
        private static LevelBuilder Level6()
        {
            var b = new LevelBuilder("SkyIslands_06", "Windvane Race");
            b.Flat(26);                       // [-4,22)
            b.Gap(4);                         // [22,26)
            b.Flat(30);                       // [26,56)
            b.Gap(14);                        // [56,70) one cloud slides all the way across
            b.Flat(24);                       // [70,94)
            b.Gap(4);                         // [94,98)
            b.Flat(26);                       // [98,124)
            b.MovingLedge(57, 3, 0, 9, 0, 5f);          // [57,60) <-> [66,69)

            b.BreakableFloor(37, 4);          // hollow beneath: Bessie's Ground Pound
            b.Gate(CharacterType.Bessie, "Cracked floor at x=37..41 hides a hollow below; only Ground Pound breaks it.");
            b.SecretRow(37.5f, 40.5f, 1f, -2.5f);

            WindCloud(b, 32f, 7, 34, 7);

            b.Start(0).Goal(120).Checkpoint(28).Checkpoint(72).Checkpoint(100);
            b.Scout(10, 3);
            b.Harvester(48, 3);
            b.Chaser(54, 6);                  // wakes just before the race
            b.Scout(80, 3).Harvester(88, 2);
            b.Scout(108, 3).Harvester(116, 2);
            b.Drone(44, 3.2f, 3).StormCloud(44).Drone(110, 2.8f, 3).StormCloud(110);
            return b;
        }

        // ---------------------------------------------------------------- 7

        // Temple Steps: three clouds bobbing in turn over a wide gap, the passage sign on a perch, a terrace bridge.
        private static LevelBuilder Level7()
        {
            var b = new LevelBuilder("SkyIslands_07", "Temple Steps");
            b.Flat(28);                       // [-4,24)
            b.Gap(14);                        // [24,38) three bobbing clouds
            b.Flat(26);                       // [38,64)
            b.Flat(18, 2);                    // [64,82) top 2
            b.Gap(10);                        // [82,92) rope bridge at terrace height
            b.Flat(14, 2);                    // [92,106) top 2
            b.Flat(20, 0);                    // [106,126)
            b.MovingLedge(26, 2, 1, 0, 3, 3.5f);
            b.MovingLedge(30, 2, 1, 0, 3, 3.5f, 0.5f);
            b.MovingLedge(34, 2, 1, 0, 3, 3.5f);
            b.Bridge(82, 10, 2);

            WindCloud(b, 60f, 7, 62, 7);

            b.StoneBlocks(46, 3, 4);
            b.SecretPassage(47.5f, 4, PassageLayout.Staircase);

            b.Start(0).Goal(122).Checkpoint(40).Checkpoint(66).Checkpoint(94).Checkpoint(108);
            b.Scout(10, 3).Harvester(18, 2);
            b.Scout(55, 2);
            b.Harvester(74, 3);
            b.Chaser(100, 6);
            b.Scout(116, 3);
            b.Drone(58, 2.8f, 3).StormCloud(58).Drone(116, 2.8f, 3).StormCloud(116);
            return b;
        }

        // ---------------------------------------------------------------- 8

        // High Winds: a sliding cloud, a balloon up to the high terrace, an updraft higher still, a bridge at the top
        // and the long way down.
        private static LevelBuilder Level8()
        {
            var b = new LevelBuilder("SkyIslands_08", "High Winds");
            b.Flat(26);                       // [-4,22)
            b.Gap(10);                        // [22,32) a sliding cloud
            b.Flat(20);                       // [32,52)
            b.Gap(8);                         // [52,60) a balloon up
            b.Flat(22, 4);                    // [60,82) top 4
            b.Gap(9);                         // [82,91) rope bridge at the top
            b.Flat(16, 4);                    // [91,107) top 4
            b.Flat(12, 2);                    // [107,119) top 2
            b.Flat(14, 0);                    // [119,133)
            b.MovingLedge(23, 3, 0, 5, 0, 4f);          // [23,26) <-> [28,31)
            b.Balloon(55, 0, 4, 4.5f);                  // deck [55,57), top 0 <-> 4: the only way up
            b.Bridge(82, 9, 4);

            WindCloud(b, 65f, 6, 61, 10);     // from the high terrace (top 4) up to 10

            b.Start(0).Goal(129).Checkpoint(34).Checkpoint(62).Checkpoint(93).Checkpoint(121);
            b.Scout(10, 3).Harvester(15, 2);
            b.Scout(42, 3).Harvester(48, 2);
            b.Scout(71, 2);
            b.Chaser(78, 6);
            b.Harvester(100, 2);
            b.Scout(113, 2);
            b.Drone(44, 2.8f, 3).StormCloud(44).Drone(96, 2.8f, 3).StormCloud(96);
            return b;
        }

        // ---------------------------------------------------------------- 9

        // Drifting Isle: an island behind the start across a wide gap (Gerald / Woolly), two clouds meeting over a gap,
        // the passage sign on a perch past it, climbing terraces.
        private static LevelBuilder Level9()
        {
            // The gap's near edge is 19 behind the start, just off-screen at spawn (as Meadow Ruins 10).
            var b = new LevelBuilder("SkyIslands_09", "Drifting Isle", -42);
            b.SecretFlat(8);                  // [-42,-34) island across a wide gap behind the start
            b.Gap(15);                        // [-34,-19)
            b.Flat(45);                       // [-19,26)
            b.Gap(12);                        // [26,38) two clouds meeting mid-way
            b.Flat(24);                       // [38,62)
            b.Flat(16, 2);                    // [62,78) top 2
            b.Flat(16, 4);                    // [78,94) top 4
            b.Gap(5);                         // [94,99)
            b.Flat(14, 2);                    // [99,113) top 2
            b.Flat(20, 0);                    // [113,133)
            b.MovingLedge(27, 2, 0, 3, 0, 3.5f);        // [27,29) <-> [30,32)
            b.MovingLedge(36, 2, 0, -3, 0, 3.5f);       // [36,38) <-> [33,35)

            b.Gate(CharacterType.Gerald, "Island across a 15-unit gap behind the start: too wide for the base jump; Puff Glide crosses it.", CharacterType.Woolly);
            b.SecretRow(-40.5f, -36.5f, 1f, 0.5f);
            b.StoneBlocks(43, 3, 4);          // perch past the clouds
            b.SecretPassage(44.5f, 4, PassageLayout.Pyramid);

            WindCloud(b, -10f, 7, -7, 7);     // behind the start, facing away from the island

            b.Start(0).Goal(129).Checkpoint(40).Checkpoint(64).Checkpoint(80).Checkpoint(101).Checkpoint(115);
            b.Scout(12, 3).Harvester(20, 2);
            b.Scout(48, 3).Harvester(56, 2);
            b.Scout(70, 3);
            b.Chaser(90, 6);
            b.Scout(106, 2);
            b.Harvester(122, 3);
            b.Drone(50, 2.8f, 3).StormCloud(50).Drone(108, 2.8f, 3).StormCloud(108);
            return b;
        }

        // ---------------------------------------------------------------- 10

        // Cloud Relay: a slider, a riser and a sinker in a row over the widest gap, a lantern bridge, the climb out.
        private static LevelBuilder Level10()
        {
            var b = new LevelBuilder("SkyIslands_10", "Cloud Relay");
            b.Flat(26);                       // [-4,22)
            b.Gap(18);                        // [22,40) the relay
            b.Flat(24);                       // [40,64)
            b.Gap(10);                        // [64,74) rope bridge
            b.Flat(20);                       // [74,94)
            b.Flat(16, 2);                    // [94,110) top 2
            b.Gap(5);                         // [110,115)
            b.Flat(20, 0);                    // [115,135)
            b.MovingLedge(23, 2, 0, 5, 0, 4f);           // slides [23,25) <-> [28,30)
            b.MovingLedge(32, 2, 0, 0, 3, 3.5f);         // rises top 0 <-> 3
            b.MovingLedge(36, 3, 3, 0, -3, 3.5f, 0.5f);  // sinks top 3 <-> 0, low while the riser is high
            b.Bridge(64, 10, 0);

            WindCloud(b, 46f, 7, 48, 7);

            b.Start(0).Goal(131).Checkpoint(42).Checkpoint(76).Checkpoint(96).Checkpoint(117);
            b.Scout(10, 3).Harvester(16, 2);
            b.Scout(52, 2).Harvester(58, 2);
            b.Scout(82, 3);
            b.Chaser(90, 6);
            b.Harvester(102, 2);
            b.Scout(124, 3);
            b.Drone(52, 2.8f, 3).StormCloud(52).Drone(86, 2.8f, 3).StormCloud(86).Drone(126, 2.8f, 3).StormCloud(126);
            return b;
        }

        // ---------------------------------------------------------------- 11

        // Storm Front, the capstone: a sliding cloud, a balloon up to the high islands, two clouds meeting over a gap
        // at the top, a lantern bridge, and down to the gate. Five checkpoints.
        private static LevelBuilder Level11()
        {
            var b = new LevelBuilder("SkyIslands_11", "Storm Front");
            b.Flat(24);                       // [-4,20)
            b.Gap(12);                        // [20,32) a sliding cloud
            b.Flat(18);                       // [32,50)
            b.Gap(8);                         // [50,58) a balloon up
            b.Flat(20, 4);                    // [58,78) top 4
            b.Gap(14);                        // [78,92) two clouds meeting at the top
            b.Flat(16, 4);                    // [92,108) top 4
            b.Gap(9);                         // [108,117) rope bridge
            b.Flat(14, 4);                    // [117,131) top 4
            b.Flat(10, 2);                    // [131,141) top 2
            b.Flat(20, 0);                    // [141,161)
            b.MovingLedge(21, 3, 0, 7, 0, 4.5f);        // [21,24) <-> [28,31)
            b.Balloon(53, 0, 4, 4.5f);                  // deck [53,55), top 0 <-> 4: the only way up
            b.MovingLedge(79, 3, 4, 4, 0, 4f);          // [79,82) <-> [83,86)
            b.MovingLedge(88, 3, 4, -2, 0, 4f);         // [88,91) <-> [86,89): they meet at x=86
            b.Bridge(108, 9, 4);

            b.StoneBlocks(64, 3, 8);          // a double jump up from the high terrace
            b.SecretPassage(65.5f, 8, PassageLayout.Pillars);

            WindCloud(b, 147f, 7, 142, 7);

            b.Start(0).Goal(157).Checkpoint(34).Checkpoint(60).Checkpoint(94).Checkpoint(119).Checkpoint(143);
            b.Scout(10, 3);
            b.Scout(42, 3);
            b.Harvester(70, 3);
            b.Chaser(76, 6);
            b.Scout(100, 3);
            b.Harvester(125, 2);
            b.Scout(136, 2);
            b.Harvester(152, 3);
            b.Drone(44, 2.8f, 3).StormCloud(44).Drone(100, 2.8f, 3).StormCloud(100).Drone(150, 2.8f, 3).StormCloud(150);
            return b;
        }

        // ---------------------------------------------------------------- boss

        // The Storm Baron's sky dock: a run-in guarded by a Harvester, a Scout and a Chaser, a checkpoint at the gate,
        // then the arena between two cover mounds. Three hits; waves after hits 1 and 2 (storm clouds among them). No
        // goal: downing the Storm Baron completes the level.
        private static LevelBuilder LevelBoss()
        {
            var b = new LevelBuilder("SkyIslands_Boss", "Storm Baron's Sky Dock");
            b.Flat(34);                       // [-4,30) run-in
            b.Gap(3);                         // [30,33)
            b.Flat(60);                       // [33,93) arena
            b.Mound(40, 3, 2);                // cover / stomp platforms either side
            b.Mound(84, 3, 2);

            b.Boss();
            b.Start(0).Checkpoint(36);
            b.Scout(10, 3).Harvester(18, 2);                        // run-in guards
            b.Chaser(26, 8);                                        // wakes at x=18, right before the gap to the gate
            b.Commander(62, 8);                                     // the Storm Baron patrols [54,70]
            b.Harvester(50, 2).Scout(74, 2);                        // arena guards (wave 0)
            b.Scout(89, 2, wave: 1).Harvester(56, 2, wave: 1).Drone(76, 2.8f, 3, wave: 1).StormCloud(76);   // after hit 1
            b.Drone(60, 2.8f, 4, wave: 2).StormCloud(60).Drone(48, 2.8f, 3, wave: 2).StormCloud(48).Scout(70, 3, wave: 2); // after hit 2
            b.Crop(41.5f);
            b.Crop(85.5f);
            return b;
        }
    }
}
