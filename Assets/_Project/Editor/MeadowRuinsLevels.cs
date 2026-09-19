using System.Collections.Generic;
using FarmFuryStampede.Data;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// The eight World 1 levels, authored with <see cref="LevelBuilder"/>. Difficulty ramp:
    ///   1-2  pure movement (no robots, no checkpoints)
    ///   3-5  Harvesters, 1-2 gaps, checkpoints appear
    ///   6-7  Drones join, raised platforms and terraces, 2+ checkpoints
    ///   8    capstone: everything, widest gaps, most robots
    /// Phase 5a adds a Chaser (level 4), Scouts (levels 6 and 8), a Barrier Unit sealing level 5's secret chamber
    /// (Billy), and the boss level "Robot Commander's Fortress".
    /// Levels 1-4 carry a real character-gated secret (Phase 4): 1 = height ledge (Cluck's Flutter Jump or
    /// Woolly's Cloud Step), 2 = Breakable Floor (Bessie), 3 = Breakable Wall chamber (Billy), 4 = wide chasm
    /// (Gerald's Puff Glide, or Woolly's chained clouds). Levels 5-8 keep an open bonus cluster on a stair of
    /// mounds/platforms; World 1 needs no more gating and later worlds add their own.
    /// Objects are added after all layout so ground-relative placement sees the final geometry.
    /// </summary>
    internal static class MeadowRuinsLevels
    {
        public static List<LevelBuilder> CreateAll()
        {
            return new List<LevelBuilder> { Level1(), Level2(), Level3(), Level4(), Level5(), Level6(), Level7(), Level8(), LevelBoss() };
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

            b.SecretLedge(24, 5, 6);          // 6 above the ground: the base jump tops out at 3.5

            b.Gate(CharacterType.Cluck, "High ledge 6 units up: needs extra height (Cluck's Flutter Jump, or Woolly's Cloud Step).", CharacterType.Woolly);
            b.Start(0).Goal(112);
            b.CropRow(6, 12, 2);
            b.CropRow(15, 21, 3);
            GapArc(b, 30, 3, 0);
            b.CropRow(37, 71, 4);
            b.CropRow(76, 106, 5);
            b.SecretRow(24.5f, 28.5f, 1f, 6.5f);
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

            b.Mound(96, 3, 2);                // open stair up to the chamber's platform
            b.Floating(101, 4, 4);
            b.Floating(106, 10, 6);
            b.Chamber(110, 6, 4);             // sealed by a Breakable Wall: Billy's Charge Break

            b.Gate(CharacterType.Billy, "Sealed chamber on the high platform; its Breakable Wall only breaks to Charge Break.");
            b.Start(0).Goal(116).Checkpoint(44);
            b.Harvester(22, 4).Harvester(52, 3).Harvester(90, 4);
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
            b.SecretRow(111.5f, 114.5f, 1f, 6.5f);
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
            b.Harvester(16, 3).Harvester(44, 4).Harvester(78, 3).Harvester(108, 4);
            b.Chaser(86, 12);                 // wakes when Cluck nears the end of this stretch; stompable, and outrunnable
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
            b.Harvester(14, 3).Harvester(32, 4).Harvester(60, 3).Harvester(108, 4);
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
            b.Harvester(12, 3).Scout(74, 3);
            b.Drone(20, 2.8f, 3).Drone(56, 2.8f, 4).Drone(96, 2.8f, 4);
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
            b.Harvester(12, 3).Harvester(47, 3).Harvester(84, 3).Harvester(104, 4);
            b.Drone(30, 2.8f, 4).Drone(64, 2.6f, 3).Drone(114, 2.8f, 4);
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
            b.Harvester(13, 3).Harvester(40, 4).Scout(66, 4).Harvester(92, 4).Harvester(118, 4);
            b.Drone(46, 2.8f, 3).Drone(86, 2.6f, 3).Drone(123, 2.8f, 3);
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
            b.Harvester(16, 3).Scout(24, 3);                        // run-in guards
            b.Commander(62, 8);                                     // patrols [54,70]
            b.Harvester(50, 3).Scout(74, 3);                        // arena guards (wave 0)
            b.Scout(86, 3, wave: 1).Harvester(56, 2, wave: 1);      // reinforcements after hit 1
            b.Drone(60, 2.8f, 4, wave: 2).Scout(72, 3, wave: 2);    // reinforcements after hit 2

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
