using System.Collections.Generic;
using FarmFuryStampede.Data;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// The eleven World 1 levels and the boss, authored with <see cref="LevelBuilder"/>. The layouts ramp up (more
    /// and wider gaps, then terraces, then 5-wide gaps and five checkpoints) and so do the robots, one new type at a
    /// time with every level adding pressure:
    ///   1-3   Scouts only: 4, 6, 7 - guarding take-offs, landings and the ground under stairs
    ///   4-7   Harvesters join (2, 3, 3, 4) beside 6-7 Scouts
    ///   8-11  the Chaser (DriftRobot art) and Drones join: 1/2 Chasers, 2-4 Drones, 3-4 Harvesters, 6-8 Scouts
    ///   12    the boss: Commander with a Chaser on the run-in and Drones in the reinforcement waves
    /// Props to climb recur: hay pyramids (2, 3, 4), stone-block stairs with the coin on top (1, 2, 3), Level 10's
    /// barrel pyramid and hay stack. A Barrier Unit seals level 5's secret chamber (Billy).
    /// Character-gated secrets (Phase 4): 1 = height ledge (the double jump every character has, or Woolly's Cloud Step),
    /// 2 = Breakable Floor (Bessie), 4 = wide chasm (Gerald's Puff Glide, or Woolly's chained clouds), 5 = Barrier
    /// Unit chamber (Billy). Level 3 has no secret (its Billy chamber was removed: Billy unlocks far too late for a
    /// level-3 player); its stair leads to an open bonus platform instead. Levels 6-8 keep an open bonus cluster on
    /// a stair of mounds/platforms. Levels 9-11 return to real gates: 9 = Breakable Floor (Bessie), 10 = chasm island behind
    /// the start (Gerald / Woolly), 11 = Breakable Wall chamber on the highest platform (Billy).
    /// Objects are added after all layout so ground-relative placement sees the final geometry.
    /// Every level also gets PathCorn (a continuous kernel line start to goal) and, except Level 1, StandardFarm
    /// (Level 1's farm scenery laid out automatically); both are applied in CreateAll and laid out at build time.
    /// </summary>
    internal static class MeadowRuinsLevels
    {
        public static List<LevelBuilder> CreateAll()
        {
            var levels = new List<LevelBuilder> { Level1(), Level2(), Level3(), Level4(), Level5(), Level6(), Level7(), Level8(),
                Level9(), Level10(), Level11(), LevelBoss() };

            // World-wide look: an unbroken line of kernels along every level's path, and Level 1's farm scenery
            // (farmstead, oak, gnarled tree, fenced corn, biplane) behind every other level - Level 1 hand-places its own.
            foreach (var level in levels)
            {
                level.PathCorn();
                if (level != levels[0]) { level.StandardFarm(); }
            }
            return levels;
        }

        // Arc of crops hinting the jump path over a gap.
        private static void GapArc(LevelBuilder b, int gapStart, int width, int groundTop)
        {
            b.CropArc(gapStart + 0.5f, gapStart + width - 0.5f, Mathf.Max(3, width), groundTop + 1.2f, 1.4f);
        }

        // ---------------------------------------------------------------- 1

        private static LevelBuilder Level1()
        {
            var b = new LevelBuilder("MeadowRuins_01", "First Steps");
            b.Flat(34);                       // [-4,30)
            b.Gap(3);                         // [30,33)
            b.Flat(85);                       // [33,118)

            b.SecretLedge(24, 5, 4);          // 4 above the ground: out of a single jump's reach, easy with the double jump

            b.Gate(CharacterType.Cluck, "High ledge 4 units up: needs extra height (the double jump every character has, or Woolly's Cloud Step).", CharacterType.Woolly);
            b.Start(0).Goal(112);
            b.ManualScenery();                // every obstacle and backdrop piece below is placed by hand (the mockups)
            b.SecretRow(24.5f, 28.5f, 1f, 4.5f);

            // Everything is to the shared world scale (StampedeUIArt.UnitsPerMetre). The farmstead appears once, mid-level,
            // and each landmark (oak, gnarled tree, barn, silo, windmill, water wheel) only once; the rest is corn fields.

            // Opening field [-4,30): fenced corn from the start, the first Scout in front of it, and the stone
            // secret ledge above the far end of the corn.
            b.CornField(3f, 29f).Fence(3f, 29f);
            b.Scout(18, 3);                   // robots introduced one type per level: the pink Scout first
            b.CropRow(6, 12, 2);
            b.CropRow(15, 21, 3);
            GapArc(b, 30, 3, 0);

            // Mockup 1 [33,57): a hay stack to climb, stone blocks stepping up over a fenced corn field with kernels
            // on them and a coin on the top block, two Scouts patrolling in front of the fence, the gnarled tree.
            b.HayStack(37.5f);
            b.StoneBlocks(39, 3, 3).StoneBlocks(43, 1, 4).StoneBlocks(45, 1, 6).StoneBlocks(47, 2, 3);
            b.CropAt(39.4f, 3.9f).CropAt(40.65f, 3.9f).CropAt(41.9f, 3.9f);
            b.CropAt(43.5f, 4.9f);
            b.BonusCoin(45.5f, 6);
            b.CropAt(47.4f, 3.9f).CropAt(48.65f, 3.9f);
            b.Scout(42, 2).Scout(51, 2);
            b.CornField(40f, 54f).Fence(40f, 54f);
            b.Backdrop(FarmProp.GnarledTree, 55.5f);

            // Mockup 2 [58,92): the one farmstead the chicken runs past - water wheel, windmill and cart together,
            // the silo tucked behind the barn, a fenced corn patch with the oak behind it, then the barrel pyramid.
            b.Backdrop(FarmProp.WaterWheel, 60f).Backdrop(FarmProp.Windmill, 63f).Backdrop(FarmProp.Cart, 66.3f);
            b.Backdrop(FarmProp.Silo, 69.5f).Backdrop(FarmProp.Barn, 74.5f);
            b.CornField(79f, 86f).Fence(79f, 86.5f);
            b.Backdrop(FarmProp.Oak, 83f);
            b.BarrelPyramid(89).Scout(93.5f, 1.5f);      // barrel pyramid to climb over, with a Scout waiting on the landing side

            // Fields to the goal [92,118): a second stone-block run over fenced corn, kernels in front of the last stretch.
            b.StoneBlocks(97, 3, 3).StoneBlocks(101, 1, 5).StoneBlocks(103, 2, 3);
            b.CropAt(97.4f, 3.9f).CropAt(98.65f, 3.9f).CropAt(99.9f, 3.9f);
            b.CropAt(101.5f, 5.9f);
            b.CropAt(103.4f, 3.9f).CropAt(104.65f, 3.9f);
            b.Scout(102, 3);
            b.CornField(95f, 117f).Fence(95f, 117f);
            b.CropRow(106, 110, 3);

            b.Biplane(8.5f);
            return b;
        }

        // ---------------------------------------------------------------- 2

        private static LevelBuilder Level2()
        {
            var b = new LevelBuilder("MeadowRuins_02", "Stone Stairs");
            b.Flat(30);                       // [-4,26)
            b.Gap(3);                         // [26,29)
            b.Flat(22);                       // [29,51)
            b.Gap(4);                         // [51,55)
            b.Flat(30);                       // [55,85)
            b.Gap(4);                         // [85,89)
            b.Flat(31);                       // [89,120)

            b.Mound(34, 3, 2);
            b.Floating(38, 4, 4);
            b.Mound(60, 3, 2);
            b.Floating(64, 4, 4);
            b.Floating(69, 4, 5);
            b.BreakableFloor(74, 4);          // hollow beneath: Bessie's Ground Pound

            b.Gate(CharacterType.Bessie, "Cracked Breakable Floor at x=74..78 hides a chamber below; only Ground Pound breaks it.");
            b.Start(0).Goal(114);
            b.HayPyramid(22);                 // 3-2-1 bales, 4.5 high, on the run-up to the first pit: climb it, leap off the top
            b.HaybalesAsBarrels(1);           // the first random hay bale (x=9.5, before the pyramid) is a wooden barrel
            b.CropRow(6, 22, 2);
            GapArc(b, 26, 3, 0);
            b.Crop(35.5f);
            b.CropAt(39, 4.5f).CropAt(40, 4.5f).CropAt(41, 4.5f);
            b.CropRow(44, 48, 2);
            GapArc(b, 51, 4, 0);
            b.CropRow(57, 58, 1);
            b.CropAt(65, 4.5f).CropAt(66, 4.5f).CropAt(67, 4.5f);
            b.CropAt(70, 5.5f).CropAt(71, 5.5f).CropAt(72, 5.5f);
            b.CropRow(79, 83, 2);
            GapArc(b, 85, 4, 0);
            b.CropRow(92, 108, 4);

            // Six Scouts, spaced to ramp up: one alone on each of the first three stretches (the first well clear of
            // the start, the third under the stepping platforms), one squeezed between the breakable floor and the
            // third pit, then a pair 10 apart on the run to the goal.
            b.Scout(15, 3).Scout(45, 3).Scout(67, 2.5f).Scout(81.5f, 2).Scout(96, 3).Scout(106, 3);

            // Level 1's stone-block stair on the final run: kernels up the steps and the coin on the top block,
            // the fifth Scout patrolling underneath.
            b.StoneBlocks(92, 3, 3).StoneBlocks(96, 1, 4).StoneBlocks(98, 1, 6).StoneBlocks(100, 2, 3);
            b.CropAt(92.4f, 3.9f).CropAt(93.65f, 3.9f).CropAt(94.9f, 3.9f);
            b.CropAt(96.5f, 4.9f);
            b.BonusCoin(98.5f, 6);
            b.CropAt(100.4f, 3.9f).CropAt(101.65f, 3.9f);
            b.SecretRow(74.5f, 77.5f, 1f, -2.5f);
            return b;
        }

        // ---------------------------------------------------------------- 3

        private static LevelBuilder Level3()
        {
            var b = new LevelBuilder("MeadowRuins_03", "Rusty Patrol");
            b.Flat(40);                       // [-4,36)
            b.Gap(3);                         // [36,39)
            b.Flat(30);                       // [39,69)
            b.Gap(4);                         // [69,73)
            b.Flat(45);                       // [73,118)

            b.Mound(96, 3, 2);                // open stair up to a bonus platform over the finish
            b.Floating(101, 4, 4);
            b.Floating(106, 10, 6);

            b.Start(0).Goal(116).Checkpoint(44);
            // Seven Scouts: three on the opening run (the last at the first pit's take-off), two under the stone
            // stair, one past the hay pyramid, one at the foot of the stair up to the chamber.
            b.Scout(10, 3).Scout(24, 3).Scout(32, 2).Scout(53, 3).Scout(63, 3).Scout(85, 2.5f).Scout(91, 3);
            b.HayPyramid(77);                 // 3-2-1 bales on the last stretch, clear of the pit landing at 73
            b.StoneBlocks(55, 3, 3).StoneBlocks(59, 1, 4).StoneBlocks(61, 1, 6).StoneBlocks(63, 2, 3);
            b.CropAt(55.4f, 3.9f).CropAt(56.65f, 3.9f).CropAt(57.9f, 3.9f);
            b.CropAt(59.5f, 4.9f);
            b.BonusCoin(61.5f, 6);
            b.CropAt(63.4f, 3.9f).CropAt(64.65f, 3.9f);
            b.CropRow(4, 14, 5);
            b.CropRow(28, 34, 3);
            GapArc(b, 36, 3, 0);
            b.CropRow(41, 43, 2);
            b.CropRow(47, 65, 4);
            GapArc(b, 69, 4, 0);
            b.CropRow(75, 84, 3);
            b.CropRow(93, 95, 2);
            b.Crop(97.5f);
            b.CropAt(102, 4.5f).CropAt(103, 4.5f);
            b.CropAt(107.5f, 6.9f).CropAt(110f, 6.9f).CropAt(112.5f, 6.9f).CropAt(115f, 6.9f);   // bonus kernels on the top platform
            return b;
        }

        // ---------------------------------------------------------------- 4

        private static LevelBuilder Level4()
        {
            var b = new LevelBuilder("MeadowRuins_04", "Broken Bridge", -32);
            b.SecretFlat(8);                  // [-32,-24) island across a wide chasm behind the start
            b.Gap(15);                        // [-24,-9)
            b.Flat(35);                       // [-9,26)
            b.Gap(4);                         // [26,30)
            b.Flat(28);                       // [30,58)
            b.Gap(4);                         // [58,62)
            b.Flat(26);                       // [62,88)
            b.Gap(3);                         // [88,91)
            b.Flat(30);                       // [91,121)

            b.Gate(CharacterType.Gerald, "Island 15 units left of the start: too wide for the base jump; Puff Glide crosses it.", CharacterType.Woolly);
            b.Start(0).Goal(116).Checkpoint(34).Checkpoint(66);
            // The Harvester arrives: one guarding the first pit's take-off, one mid-way through the third stretch,
            // with six Scouts around them, paired up on the two middle stretches and the run to the goal.
            b.HayPyramid(16);                 // 3-2-1 bales on the opening run
            b.Scout(7, 2).Harvester(22.5f, 2);
            b.Scout(44, 3).Scout(52, 3);
            b.Harvester(76, 3).Scout(83, 2.5f);
            b.Scout(100, 3).Scout(108, 3);
            b.CropRow(4, 10, 3);
            b.CropRow(20, 24, 2);
            GapArc(b, 26, 4, 0);
            b.CropRow(32, 38, 3);
            b.CropRow(40, 48, 4);
            GapArc(b, 58, 4, 0);
            b.CropRow(64, 70, 3);
            b.CropRow(82, 86, 2);
            GapArc(b, 88, 3, 0);
            b.CropRow(94, 102, 4);
            b.SecretRow(-30.5f, -26.5f, 1f, 0.5f);
            return b;
        }

        // ---------------------------------------------------------------- 5

        private static LevelBuilder Level5()
        {
            var b = new LevelBuilder("MeadowRuins_05", "Hilltop Harvest");
            b.Flat(26);                       // [-4,22) top 0
            b.Flat(20, 2);                    // [22,42) top 2
            b.Gap(3);                         // [42,45)
            b.Flat(24, 3);                    // [45,69) top 3
            b.Flat(20, 1);                    // [69,89) top 1
            b.Gap(4);                         // [89,93)
            b.Flat(28, 2);                    // [93,121) top 2

            b.Mound(52, 3, 2);                // open stair on the top-3 terrace up to a high platform
            b.Floating(56, 4, 7);
            b.Floating(61, 10, 9);
            b.Chamber(65, 9, 4, sealWithBarrierUnit: true);   // sealed by a Barrier Unit: only Charge Break clears it

            b.Gate(CharacterType.Billy, "Chamber on the high platform sealed by a Barrier Unit; only Charge Break clears it.");
            b.Start(0).Goal(116).Checkpoint(24).Checkpoint(72);
            // One robot per terrace section, two on the long ones: 3 Harvesters, 6 Scouts.
            b.Scout(10, 3).Harvester(17, 2.5f);
            b.Scout(31, 3).Scout(38, 2.5f);
            b.Harvester(48.5f, 2).Scout(63, 3);
            b.Harvester(80, 3);
            b.Scout(100, 3).Scout(109, 3);
            b.CropRow(4, 10, 3);
            b.CropRow(17, 20, 3);
            b.CropRow(26, 30, 2);
            b.CropRow(38, 40, 2);
            GapArc(b, 42, 3, 2);
            b.CropRow(47, 49, 2);
            b.Crop(53.5f);
            b.CropAt(57, 7.5f).CropAt(58, 7.5f);
            b.CropRow(65, 67, 2);
            b.CropRow(74, 86, 4);
            GapArc(b, 89, 4, 1);
            b.CropRow(96, 104, 4);
            b.CropRow(112, 118, 3);
            b.SecretRow(66.5f, 69.5f, 1f, 9.5f);
            return b;
        }

        // ---------------------------------------------------------------- 6

        private static LevelBuilder Level6()
        {
            var b = new LevelBuilder("MeadowRuins_06", "Buzzing Skies");
            b.Flat(30);                       // [-4,26)
            b.Gap(4);                         // [26,30)
            b.Flat(32);                       // [30,62)
            b.Gap(3);                         // [62,65)
            b.Flat(40);                       // [65,105)

            b.Mound(40, 3, 2);                // raised platforms mid-level
            b.Floating(45, 4, 4);
            b.Floating(50, 4, 5);
            b.Mound(78, 3, 2);                // secret stair
            b.Floating(82, 4, 4);
            b.Floating(87, 4, 6);

            b.Start(0).Goal(103).Checkpoint(33).Checkpoint(68);
            // 3 Harvesters, 7 Scouts: Harvesters under both platform stairs and on the opening run, Scouts at every
            // take-off, the checkpoint approaches and the finish.
            b.Scout(9, 3).Harvester(17, 3).Scout(23, 2);
            b.Scout(37.5f, 1.5f).Harvester(47, 3).Scout(57, 3);
            b.Scout(73, 3).Harvester(86, 3);
            b.Scout(95, 2.5f).Scout(101, 1.5f);
            b.CropRow(4, 8, 2);
            b.CropRow(15, 24, 3);
            GapArc(b, 26, 4, 0);
            b.CropRow(35, 38, 3);
            b.Crop(41.5f);
            b.CropAt(46, 4.5f).CropAt(47, 4.5f).CropAt(51, 5.5f).CropAt(52, 5.5f);
            b.CropRow(58, 60, 2);
            GapArc(b, 62, 3, 0);
            b.CropRow(70, 72, 2);
            b.CropRow(79, 80, 1);
            b.CropAt(83, 4.5f).CropAt(84, 4.5f);
            b.CropRow(92, 100, 4);
            b.SecretRow(87.5f, 90.5f, 1f, 6.5f);
            return b;
        }

        // ---------------------------------------------------------------- 7

        private static LevelBuilder Level7()
        {
            var b = new LevelBuilder("MeadowRuins_07", "Ruined Terraces");
            b.Flat(24);                       // [-4,20) top 0
            b.Flat(18, 2);                    // [20,38) top 2
            b.Flat(18, 4);                    // [38,56) top 4
            b.Gap(3);                         // [56,59)
            b.Flat(16, 5);                    // [59,75) top 5
            b.Flat(16, 2);                    // [75,91) top 2
            b.Gap(4);                         // [91,95)
            b.Flat(30, 0);                    // [95,125) top 0

            b.Mound(69, 3, 2);                // secret stair on the high terrace
            b.Floating(73, 3, 9);

            b.Start(0).Goal(121).Checkpoint(22).Checkpoint(41).Checkpoint(78);
            // 4 Harvesters, 7 Scouts, two robots on most terraces so each climb meets a pair.
            b.Scout(9, 3).Harvester(15.5f, 2);
            b.Scout(29, 3).Harvester(34.5f, 1.5f);
            b.Scout(47, 3).Scout(53, 1.5f);
            b.Harvester(64, 3);
            b.Scout(84, 3);
            b.Harvester(100, 3).Scout(108, 3).Scout(115, 2.5f);
            b.CropRow(4, 10, 3);
            b.CropRow(15, 18, 3);
            b.CropRow(26, 36, 5);
            b.CropRow(44, 54, 5);
            GapArc(b, 56, 3, 4);
            b.CropRow(61, 66, 3);
            b.CropRow(80, 90, 5);
            GapArc(b, 91, 4, 2);
            b.CropRow(97, 100, 3);
            b.CropRow(108, 112, 4);
            b.SecretRow(73.5f, 75.5f, 1f, 9.5f);
            return b;
        }

        // ---------------------------------------------------------------- 8

        private static LevelBuilder Level8()
        {
            var b = new LevelBuilder("MeadowRuins_08", "The Overlord's Gate");
            b.Flat(28);                       // [-4,24) top 0
            b.Gap(3);                         // [24,27)
            b.Flat(23, 2);                    // [27,50) top 2
            b.Gap(5);                         // [50,55)
            b.Flat(20, 2);                    // [55,75) top 2
            b.Gap(3);                         // [75,78)
            b.Flat(25, 4);                    // [78,103) top 4
            b.Gap(5);                         // [103,108)
            b.Flat(22, 0);                    // [108,130) top 0

            b.Mound(98, 3, 2);                // secret stair over the last big gap
            b.Floating(102, 4, 8);

            b.Start(0).Goal(128).Checkpoint(30).Checkpoint(58).Checkpoint(82).Checkpoint(111);
            // The Chaser and the Drone arrive: one Chaser waking on the top-4 terrace, Drones over the widest gap's
            // run-up and the finish; 3 Harvesters and 6 Scouts hold the ground between.
            b.Scout(9, 3).Harvester(18, 3);
            b.Scout(38, 3).Scout(45, 2.5f);
            b.Harvester(65, 3).Scout(71, 2);
            b.Scout(87, 2);
            b.Chaser(95, 9);                  // wakes as the player passes x=86; stopped by the mound at 98
            b.Harvester(118, 3).Scout(125, 2);
            b.Drone(46, 2.8f, 3).Drone(123, 2.8f, 3);
            b.CropRow(4, 8, 2);
            b.CropRow(16, 22, 3);
            GapArc(b, 24, 3, 0);
            b.CropRow(32, 36, 2);
            GapArc(b, 50, 5, 2);
            b.CropRow(57, 60, 3);
            b.CropRow(70, 73, 3);
            GapArc(b, 75, 3, 2);
            b.CropRow(80, 86, 3);
            b.Crop(99.5f);
            GapArc(b, 103, 5, 4);
            b.CropRow(113, 116, 3);
            b.CropRow(122, 127, 5);
            b.SecretRow(102.5f, 105.5f, 1f, 8.5f);
            return b;
        }

        // ---------------------------------------------------------------- 9

        // Terraces climbing to a windy top-4 plateau under Drones, a perch stair on each rise, then back down.
        private static LevelBuilder Level9()
        {
            var b = new LevelBuilder("MeadowRuins_09", "Windmill Heights");
            b.Flat(26);                       // [-4,22) top 0
            b.Gap(3);                         // [22,25)
            b.Flat(20, 2);                    // [25,45) top 2
            b.Flat(18, 4);                    // [45,63) top 4
            b.Gap(4);                         // [63,67)
            b.Flat(22, 4);                    // [67,89) top 4
            b.Gap(4);                         // [89,93)
            b.Flat(16, 2);                    // [93,109) top 2
            b.Flat(24, 0);                    // [109,133) top 0

            b.Mound(10, 3, 2);                // opening perch: mound up to a platform
            b.Floating(15, 4, 4);
            b.Mound(55, 2, 2);                // plateau perch
            b.Floating(59, 4, 8);
            b.BreakableFloor(78, 4);          // hollow beneath: Bessie's Ground Pound

            b.Gate(CharacterType.Bessie, "Cracked Breakable Floor at x=78..82 on the plateau hides a hollow; only Ground Pound breaks it.");
            b.Start(0).Goal(128).Checkpoint(27).Checkpoint(47).Checkpoint(84).Checkpoint(111);
            // 1 Chaser, 3 Drones, 3 Harvesters, 7 Scouts: every terrace and perch is guarded, the Chaser waits on
            // the way down.
            b.Scout(7, 2).Harvester(17.5f, 2.5f);
            b.Scout(35, 3).Scout(41.5f, 2);
            b.Harvester(51.5f, 2.5f).Scout(60, 2);
            b.Scout(72, 3);
            b.Scout(99, 3);
            b.Chaser(106, 10);                // wakes at x=96 on the top-2 step down
            b.Harvester(116, 3).Scout(123, 2.5f);
            b.Drone(34, 2.8f, 3).Drone(50, 2.8f, 3).Drone(100, 2.8f, 3);
            b.CropRow(4, 8, 2);
            b.Crop(11.5f);
            b.CropAt(16, 4.5f).CropAt(17, 4.5f).CropAt(18, 4.5f);
            GapArc(b, 22, 3, 0);
            b.CropRow(28, 33, 3);
            b.CropRow(40, 44, 2);
            b.CropRow(46, 53, 3);
            b.Crop(56f);
            b.CropAt(60, 8.5f).CropAt(61, 8.5f).CropAt(62, 8.5f);
            GapArc(b, 63, 4, 4);
            b.CropRow(69, 76, 3);
            b.CropRow(84, 88, 2);
            GapArc(b, 89, 4, 4);
            b.CropRow(96, 106, 4);
            b.CropRow(112, 126, 4);
            b.SecretRow(78.5f, 81.5f, 1f, 1.5f);
            return b;
        }

        // ---------------------------------------------------------------- 10

        // A long flat run with a Chaser waking mid-way, props to climb (barrel pyramid, hay stack, bonus blocks), then
        // rising terraces. The secret island sits behind the start across a chasm, as in level 4.
        private static LevelBuilder Level10()
        {
            var b = new LevelBuilder("MeadowRuins_10", "Scarecrow Pass", -32);
            b.SecretFlat(8);                  // [-32,-24) island across a wide chasm behind the start
            b.Gap(15);                        // [-24,-9)
            b.Flat(35);                       // [-9,26) top 0
            b.Gap(4);                         // [26,30)
            b.Flat(40);                       // [30,70) top 0
            b.Gap(4);                         // [70,74)
            b.Flat(24, 1);                    // [74,98) top 1
            b.Gap(3);                         // [98,101)
            b.Flat(20, 3);                    // [101,121) top 3
            b.Flat(16, 0);                    // [121,137) top 0

            b.BarrelPyramid(20);
            b.HayStack(40);
            b.StoneBlocks(52, 3, 4);          // bonus perch over the Chaser's stretch

            b.Gate(CharacterType.Gerald, "Island 15 units left of the start: too wide for the base jump; Puff Glide crosses it.", CharacterType.Woolly);
            b.Start(0).Goal(133).Checkpoint(32).Checkpoint(76).Checkpoint(103);
            // 2 Chasers, 3 Drones, 3 Harvesters, 7 Scouts.
            b.Scout(10, 3).Scout(24.5f, 1);   // the second guards the first pit's take-off
            b.Harvester(48, 3).Scout(54, 2).Harvester(60, 2);
            b.Chaser(66, 12);                 // wakes as the player crosses the hay stack; stompable, and outrunnable
            b.Scout(81, 2.5f).Harvester(88, 3).Scout(94.5f, 2.5f);
            b.Scout(108, 3);
            b.Chaser(118, 10);                // wakes on the top-3 terrace
            b.Scout(126, 3);
            b.Drone(64, 2.8f, 3).Drone(90, 2.8f, 3).Drone(128, 2.8f, 3);
            b.CropRow(4, 9, 2);
            b.CropRow(23, 25, 2);
            GapArc(b, 26, 4, 0);
            b.CropRow(32, 36, 2);
            b.CropRow(44, 50, 3);
            b.CropAt(52.4f, 4.9f).CropAt(53.65f, 4.9f).CropAt(54.9f, 4.9f);
            b.CropRow(58, 68, 4);
            GapArc(b, 70, 4, 0);
            b.CropRow(78, 96, 4);
            GapArc(b, 98, 3, 1);
            b.CropRow(104, 119, 5);
            b.CropRow(124, 131, 3);
            b.SecretRow(-30.5f, -26.5f, 1f, 0.5f);
            return b;
        }

        // ---------------------------------------------------------------- 11

        // Capstone before the boss: five terraces, three 5-wide gaps, every ordinary robot type, five checkpoints,
        // and a sealed chamber on the highest platform of the level.
        private static LevelBuilder Level11()
        {
            var b = new LevelBuilder("MeadowRuins_11", "Commander's Approach");
            b.Flat(24);                       // [-4,20) top 0
            b.Gap(3);                         // [20,23)
            b.Flat(21, 2);                    // [23,44) top 2
            b.Gap(5);                         // [44,49)
            b.Flat(20, 2);                    // [49,69) top 2
            b.Flat(16, 4);                    // [69,85) top 4
            b.Gap(5);                         // [85,90)
            b.Flat(18, 4);                    // [90,108) top 4
            b.Gap(4);                         // [108,112)
            b.Flat(22, 2);                    // [112,134) top 2
            b.Gap(5);                         // [134,139)
            b.Flat(24, 0);                    // [139,163) top 0

            b.Mound(52, 3, 2);                // perch after the first wide gap
            b.Floating(56, 4, 6);
            b.Mound(93, 3, 2);                // open stair up to the chamber's platform
            b.Floating(97, 4, 8);
            b.Floating(102, 10, 10);
            b.Chamber(106, 10, 4);            // sealed by a Breakable Wall: Billy's Charge Break

            b.Gate(CharacterType.Billy, "Sealed chamber on the highest platform, above the last gap; its Breakable Wall only breaks to Charge Break.");
            b.Start(0).Goal(158).Checkpoint(26).Checkpoint(51).Checkpoint(91).Checkpoint(114).Checkpoint(141);
            // The capstone: 2 Chasers, 4 Drones, 4 Harvesters, 8 Scouts.
            b.Scout(8, 3).Harvester(15.5f, 2.5f);
            b.Scout(33, 3).Scout(40, 2.5f);
            b.Harvester(62, 3).Scout(66.5f, 1.5f);
            b.Scout(74, 3);
            b.Chaser(80, 10);                 // wakes on the top-4 terrace, right before the second wide gap
            b.Scout(102, 3);
            b.Scout(119, 2.5f).Harvester(126, 3);
            b.Chaser(131, 8);                 // wakes at x=123, just before the last wide gap
            b.Scout(146, 3).Harvester(153, 3);
            b.Drone(40, 3.2f, 3).Drone(82, 2.8f, 3).Drone(120, 2.8f, 4).Drone(150, 2.8f, 3);
            b.CropRow(4, 7, 2);
            b.CropRow(10, 18, 4);
            GapArc(b, 20, 3, 0);
            b.CropRow(28, 40, 4);
            GapArc(b, 44, 5, 2);
            b.Crop(53.5f);
            b.CropAt(57, 6.5f).CropAt(58, 6.5f);
            b.CropRow(62, 67, 5);
            b.CropRow(71, 83, 4);
            GapArc(b, 85, 5, 4);
            b.Crop(94.5f);
            b.CropAt(98, 8.5f).CropAt(99, 8.5f);
            b.CropAt(103, 10.5f).CropAt(104, 10.5f);
            GapArc(b, 108, 4, 4);
            b.CropRow(116, 132, 4);
            GapArc(b, 134, 5, 2);
            b.CropRow(143, 156, 4);
            b.SecretRow(107.5f, 110.5f, 1f, 10.5f);
            return b;
        }

        // ---------------------------------------------------------------- boss

        // The Robot Commander's fortress: a run-in guarded by a Harvester and a Scout, a checkpoint at the gate, then
        // an arena with two cover mounds. The Commander needs three hits; reinforcement waves arrive after hits 1
        // and 2. No goal marker: defeating the Commander completes the level. (See CommanderBoss for the pattern.)
        private static LevelBuilder LevelBoss()
        {
            var b = new LevelBuilder("MeadowRuins_Boss", "Robot Commander's Fortress");
            b.Flat(34);                       // [-4,30) run-in
            b.Gap(3);                         // [30,33)
            b.Flat(60);                       // [33,93) arena
            b.Mound(44, 3, 2);                // cover / stomp platforms
            b.Mound(80, 3, 2);

            b.Boss();
            b.Start(0).Checkpoint(38);
            b.Scout(10, 3).Harvester(18, 2);                        // run-in guards
            b.Chaser(26, 8);                                        // wakes at x=18, right before the gap to the gate
            b.Commander(62, 8);                                     // patrols [54,70]
            b.Harvester(50, 3).Scout(74, 3);                        // arena guards (wave 0)
            b.Scout(86, 3, wave: 1).Harvester(56, 2, wave: 1).Drone(76, 2.8f, 3, wave: 1);   // after hit 1
            b.Drone(60, 2.8f, 4, wave: 2).Drone(48, 2.8f, 3, wave: 2).Scout(72, 3, wave: 2); // after hit 2

            b.CropRow(4, 26, 3);
            GapArc(b, 30, 3, 0);
            b.CropRow(35, 42, 3);
            b.Crop(45.5f);
            b.CropRow(50, 76, 6);
            b.Crop(81.5f);
            b.CropRow(86, 91, 3);
            return b;
        }
    }
}
