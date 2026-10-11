using System.Collections.Generic;
using FarmFuryStampede.Data;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// The eleven World 2 (Frozen Tundra) levels and the boss, authored with <see cref="LevelBuilder"/> and built with
    /// the Tundra kit (StampedePhase5aSetup.TundraAssets: frosted-grass ground, snow-slab platforms, ice-block steps,
    /// ice boulders, the Tundra props in the shared scenery layout, the aurora parallax and the ice robots).
    ///
    /// The world's twist is ice (GDD: reduced traction): IceFlat() stretches are slippery - slow to get going, slower
    /// to stop or turn - each marked by a THIN ICE sign. It ramps like World 1 but starts where World 1 left off:
    ///   1-3   ice on open flats first, then between robots; Scouts and Harvesters (the tracked Ice Harvester) from
    ///         level 1, a Drone from level 3
    /// Since 2026-10-11 it is at least as hard as World 1 level for level: moving ledges over widened pits, taken off
    /// from ice, in 2, 3, 4, 5, 7, 9, 10 and 11 (as well as 6 and 8), bonus block stairs in 1-4, more robots in 1-5,
    /// and Meadow's long approach and five-ledge gauntlet before the boss arena.
    ///   4-7   ice at pit take-offs and on terraces; Chasers and Drones join
    ///   8-11  5-wide gaps taken off from ice, ice terraces, every robot type, four and five checkpoints
    ///   12    the boss: the Glacier Harvester on an arena whose middle is ice, with waves after hits 1 and 2
    /// The layouts of 4-11 follow their World 1 counterparts' proven geometry (the same gates and the same robot
    /// clearances), re-surfaced with ice and made busier.
    /// Character-gated secrets: 1 = high snow ledge (double jump / Woolly), 2 and 9 = Breakable Floor (Bessie),
    /// 4 and 10 = chasm island behind the start (Gerald / Woolly); 3, 5 (its Barrier chamber removed 2026-09-29),
    /// 6-8 and 11 (its Breakable Wall chamber removed 2026-10-03) have no gated secret.
    /// Secret passages (rare pellet + coins, see LevelBuilder.SecretPassage) in 2, 5, 8, 9, 10 and 11.
    /// Every level also gets PathCorn and StandardFarm (here the Tundra props), applied in CreateAll.
    /// </summary>
    internal static class FrozenTundraLevels
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

        private static void GapArc(LevelBuilder b, int gapStart, int width, int groundTop)
        {
            b.CropArc(gapStart + 0.5f, gapStart + width - 0.5f, Mathf.Max(3, width), groundTop + 1.2f, 1.4f);
        }

        // ---------------------------------------------------------------- 1

        // First ice: two open frozen flats with a robot on each, a snow ledge secret, a rope bridge over a chasm with a
        // frozen waterfall below, an ice-block stair with the coin.
        private static LevelBuilder Level1()
        {
            var b = new LevelBuilder("FrozenTundra_01", "Frost Fields");
            b.Flat(30);                       // [-4,26)
            b.IceFlat(14);                    // [26,40) first ice
            b.Flat(8);                        // [40,48)
            b.Gap(7);                         // [48,55) chasm, too wide to jump: the rope bridge carries the path
            b.Flat(20);                       // [55,75)
            b.Bridge(48, 7, 0);
            b.IceFlat(16);                    // [75,91)
            b.Flat(29);                       // [91,120)

            b.SecretLedge(16, 5, 4);          // 4 up: the double jump every character has (or Woolly's Cloud Step)
            b.Gate(CharacterType.Cluck, "Snow ledge 4 units up: needs extra height (the double jump every character has, or Woolly's Cloud Step).", CharacterType.Woolly);
            b.SecretRow(16.5f, 20.5f, 1f, 4.5f);

            b.StoneBlocks(67, 3, 4);          // bonus perch over the Harvester (2026-10-11)
            b.CropAt(67.4f, 4.9f).CropAt(68.65f, 4.9f).CropAt(69.9f, 4.9f);

            b.Start(0).Goal(114).Checkpoint(57);
            b.Scout(10, 2);
            b.Scout(34, 2);                   // on the first ice
            b.Scout(44, 1.5f);                // between the ice and the bridge
            b.Harvester(64, 3);
            b.Harvester(83, 2.5f);            // on the second ice
            b.StoneBlocks(95, 3, 3).StoneBlocks(99, 1, 4).StoneBlocks(101, 1, 6).StoneBlocks(103, 2, 3);
            b.CropAt(95.4f, 3.9f).CropAt(96.65f, 3.9f).CropAt(97.9f, 3.9f);
            b.CropAt(99.5f, 4.9f);
            b.BonusCoin(101.5f, 6);
            b.CropAt(103.4f, 3.9f).CropAt(104.65f, 3.9f);
            b.Scout(100, 3);
            b.CropRow(4, 8, 2);
            return b;
        }

        // ---------------------------------------------------------------- 2

        // Ice after every pit: land and slide (the first ice runs straight onto a rope bridge over a chasm). A stair of
        // snow slabs up to the passage sign, Bessie's cracked floor.
        private static LevelBuilder Level2()
        {
            var b = new LevelBuilder("FrozenTundra_02", "Glacier Steps");
            b.Flat(24);                       // [-4,20)
            b.Gap(3);                         // [20,23)
            b.IceFlat(20);                    // [23,43)
            b.Gap(7);                         // [43,50) chasm under a rope bridge, a frozen waterfall below
            b.Flat(23);                       // [50,73)
            b.Bridge(43, 7, 0);
            b.IceFlat(12);                    // [73,85)
            b.Gap(9);                         // [85,94) a ledge slides across, taken off from the ice (2026-10-11)
            b.Flat(26);                       // [94,120)
            b.MovingLedge(85, 3, 0, 6, 0, 4f);    // [85,88) <-> [91,94), flush with both banks

            b.Mound(52, 3, 2);
            b.Floating(56, 4, 4);
            b.Floating(61, 4, 5);
            b.BreakableFloor(66, 4);          // hollow beneath: Bessie's Ground Pound
            b.Gate(CharacterType.Bessie, "Cracked wooden floor at x=66..70 hides a hollow below; only Ground Pound breaks it.");
            b.SecretRow(66.5f, 69.5f, 1f, -2.5f);
            b.SecretPassage(64f, 5, PassageLayout.Zigzag);     // secret passage (rare pellet + coins): sign on the top snow slab (back up on the same spot)

            b.Start(0).Goal(114).Checkpoint(51);
            b.Scout(12, 3);
            b.Scout(33, 3);                   // waits on the ice after the first pit
            b.Harvester(39, 2);               // and another at the chasm's take-off
            b.Harvester(79, 2);               // on the second ice
            b.Scout(101, 3).Scout(109, 2);
            b.StoneBlocks(99, 2, 3).StoneBlocks(102, 1, 5).StoneBlocks(104, 2, 3);   // bonus stair with the coin
            b.CropAt(99.4f, 3.9f).CropAt(100.65f, 3.9f);
            b.BonusCoin(102.5f, 5);
            b.CropAt(104.4f, 3.9f).CropAt(105.65f, 3.9f);
            b.CropRow(4, 16, 3);
            GapArc(b, 20, 3, 0);
            b.CropAt(57, 4.9f).CropAt(58.5f, 4.9f);
            return b;
        }

        // ---------------------------------------------------------------- 3

        // A long frozen middle under a Drone, an ice-barrel pyramid, and a bonus route over the finish (the 2026-10-03
        // mockup): double jump up to a snow ledge, a rope bridge to a second ledge, then brick steps back down.
        private static LevelBuilder Level3()
        {
            var b = new LevelBuilder("FrozenTundra_03", "Snowbound Ruins");
            b.Flat(30);                       // [-4,26)
            b.Gap(3);                         // [26,29)
            b.Flat(20);                       // [29,49)
            b.IceFlat(18);                    // [49,67)
            b.Gap(10);                        // [67,77) a ledge slides across, taken off from the ice (2026-10-11)
            b.Flat(8);                        // [77,85)
            b.IceFlat(10);                    // [85,95)
            b.Flat(25);                       // [95,120)
            b.MovingLedge(67, 3, 0, 7, 0, 4.5f);  // [67,70) <-> [74,77)

            b.BarrelPyramid(40);
            b.BonusLedge(97, 3, 5);           // 5 up: the double jump
            b.Bridge(100, 6, 5, bonus: true);
            b.BonusLedge(106, 3, 5);
            b.BrickBlocks(109, 2, 4).BrickBlocks(112, 1, 3);   // stepping back down

            b.StoneBlocks(50, 2, 4).StoneBlocks(53, 2, 6);   // bonus steps over the start of the ice
            b.CropAt(50.4f, 4.9f).CropAt(53.4f, 6.9f).CropAt(54.65f, 6.9f);

            b.Start(0).Goal(116).Checkpoint(46).Checkpoint(78);
            b.Scout(10, 3).Scout(20, 3);
            b.Harvester(34, 2);
            b.Scout(57, 3);                   // on the long ice
            b.Scout(62.5f, 2);                // and at its far end, by the slide
            b.Scout(82, 1.5f);
            b.Harvester(90, 3);               // on the second ice
            b.Drone(60, 2.8f, 3);
            b.CropRow(4, 14, 5);
            GapArc(b, 26, 3, 0);
            b.CropAt(98.5f, 5.9f);
            b.CropAt(101f, 5.9f).BonusCoin(103f, 5).CropAt(105f, 5.9f);
            b.CropAt(107.5f, 5.9f);
            b.CropAt(110f, 4.9f).CropAt(112.5f, 3.9f);
            return b;
        }

        // ---------------------------------------------------------------- 4

        // Ice runs up to three of the four pits; the secret island sits behind the start across a crevasse.
        private static LevelBuilder Level4()
        {
            var b = new LevelBuilder("FrozenTundra_04", "Crevasse Crossing", -42);
            b.SecretFlat(8);                  // [-42,-34) island across a wide crevasse behind the start
            b.Gap(15);                        // [-34,-19)
            b.Flat(40);                       // [-19,21)
            b.Gap(4);                         // [21,25)
            b.IceFlat(24);                    // [25,49)
            b.Gap(4);                         // [49,53)
            b.Flat(26);                       // [53,79)
            b.Gap(10);                        // [79,89) two ledges bobbing in turn (2026-10-11; was a plain 5-wide gap)
            b.Flat(31);                       // [89,120)
            b.MovingLedge(80, 2, 0, 0, 3, 3.5f);         // top 0 <-> 3
            b.MovingLedge(85, 2, 0, 0, 3, 3.5f, 0.5f);   // up while the first is down
            b.StoneBlocks(65, 3, 4).StoneBlocks(69, 2, 6);   // bonus stair over the third stretch
            b.CropAt(65.4f, 4.9f).CropAt(66.65f, 4.9f).CropAt(69.4f, 6.9f);
            b.BonusCoin(70.5f, 6);

            b.Gate(CharacterType.Gerald, "Island across a 15-unit crevasse behind the start: too wide for the base jump; Puff Glide crosses it.", CharacterType.Woolly);
            b.SecretRow(-40.5f, -36.5f, 1f, 0.5f);

            b.Start(0).Goal(116).Checkpoint(28).Checkpoint(56).Checkpoint(91);
            b.Scout(7, 2).Harvester(15, 2);
            b.Scout(33, 3).Harvester(43, 2); // the ice stretch, its end at the second pit's take-off
            b.Scout(62, 3).Harvester(72, 2);
            b.Scout(98, 3).Scout(108, 3);
            b.Drone(90, 2.8f, 3);
            b.CropRow(4, 12, 4);
            GapArc(b, 21, 4, 0);
            GapArc(b, 49, 4, 0);
            b.CropRow(98, 104, 3);
            return b;
        }

        // ---------------------------------------------------------------- 5

        // Terraces with a frozen top step; the passage sign on a block top level above the high platform.
        private static LevelBuilder Level5()
        {
            var b = new LevelBuilder("FrozenTundra_05", "Frozen Keep");
            b.Flat(26);                       // [-4,22) top 0
            b.Flat(20, 2);                    // [22,42) top 2
            b.Gap(3);                         // [42,45)
            b.IceFlat(24, 3);                 // [45,69) top 3, frozen
            b.Flat(20, 1);                    // [69,89) top 1
            b.Gap(9);                         // [89,98) a ledge rides up and across (2026-10-11; was 4 wide)
            b.Flat(23, 2);                    // [98,121) top 2
            b.MovingLedge(89, 3, 1, 6, 1, 4.5f);  // [89,92) top 1 <-> [95,98) top 2

            b.Mound(52, 3, 2);                // stair on the frozen terrace up to a high platform
            b.Floating(56, 4, 7);
            b.Floating(61, 10, 9);
            // The Barrier-sealed chamber was removed (2026-09-29, as in Meadow Ruins 5): its roof is now a block top
            // level, a double jump up from the platform, carrying the passage sign.
            b.StoneBlocks(65, 6, 13);
            b.CropAt(66.5f, 9.5f).CropAt(69f, 9.5f);
            b.SecretPassage(68f, 13, PassageLayout.Tunnel);  // secret passage (rare pellet + coins): sign on the top level (back up on the same spot)

            b.Start(0).Goal(116).Checkpoint(24).Checkpoint(72);
            b.Scout(10, 3).Harvester(17, 2.5f);
            b.Scout(31, 3).Scout(38, 2.5f);
            b.Harvester(48.5f, 2).Scout(63, 3);
            b.Harvester(80, 3);
            b.Scout(103, 2).Scout(109, 3);
            b.Drone(30, 2.8f, 3).Drone(84, 2.8f, 3);
            b.CropRow(4, 8, 2);
            b.CropRow(26, 30, 2);
            GapArc(b, 42, 3, 2);
            b.Crop(53.5f);
            b.CropAt(57, 7.9f).CropAt(58.5f, 7.9f);
            return b;
        }

        // ---------------------------------------------------------------- 6

        // A frozen middle stretch under two slab stairs, sliding straight to a chasm crossed on two moving ledges that
        // meet in the middle; the Chaser arrives, Drones over both ends.
        private static LevelBuilder Level6()
        {
            var b = new LevelBuilder("FrozenTundra_06", "Aurora Ridge");
            b.Flat(30);                       // [-4,26)
            b.Gap(4);                         // [26,30)
            b.IceFlat(32);                    // [30,62) frozen, right up to the chasm
            b.Gap(16);                        // [62,78) chasm: two moving ledges slide together mid-way and apart
            b.Flat(27);                       // [78,105)
            b.MovingLedge(63, 3, 0, 4, 0, 4f);        // [63,66) <-> [67,70)
            b.MovingLedge(74, 3, 0, -4, 0, 4f);       // [74,77) <-> [70,73): they meet in the middle

            b.Mound(40, 3, 2);                // raised platforms mid-level
            b.Floating(45, 4, 4);
            b.Floating(50, 4, 5);
            b.Mound(80, 3, 2);                // bonus stair
            b.Floating(84, 4, 4);
            b.Floating(89, 4, 6);

            b.Start(0).Goal(103).Checkpoint(33).Checkpoint(79);
            b.Scout(9, 3).Harvester(17, 3).Scout(23, 2);
            b.Scout(37.5f, 1.5f).Harvester(47, 3).Scout(57, 3);
            b.Harvester(86, 3);
            b.Scout(95, 2.5f).Scout(101, 1.5f);
            b.Chaser(58, 7);                  // wakes on the ice as the player comes off the stair
            b.Drone(20, 2.8f, 3).Drone(92, 2.8f, 3);
            b.CropRow(4, 8, 2);
            GapArc(b, 26, 4, 0);
            b.Crop(41.5f);
            b.CropAt(46, 4.9f).CropAt(47.5f, 4.9f).CropAt(51, 5.9f).CropAt(52.5f, 5.9f);
            b.CropAt(85, 4.9f).CropAt(86.5f, 4.9f);
            b.SecretRow(89.5f, 92.5f, 1f, 6.5f);
            return b;
        }

        // ---------------------------------------------------------------- 7

        // Climbing terraces, two of them frozen, up to a high stair; a Chaser on the top terrace.
        private static LevelBuilder Level7()
        {
            var b = new LevelBuilder("FrozenTundra_07", "Icefall Terraces");
            b.Flat(24);                       // [-4,20) top 0
            b.IceFlat(18, 2);                 // [20,38) top 2, frozen
            b.Flat(18, 4);                    // [38,56) top 4
            b.Gap(3);                         // [56,59)
            b.IceFlat(16, 5);                 // [59,75) top 5, frozen
            b.Flat(16, 2);                    // [75,91) top 2
            b.Gap(10);                        // [91,101) a ledge rides across and down (2026-10-11; was 4 wide)
            b.Flat(24, 0);                    // [101,125) top 0
            b.MovingLedge(91, 3, 2, 7, -2, 4.5f); // [91,94) top 2 <-> [98,101) top 0

            b.Mound(69, 3, 2);                // bonus stair on the high frozen terrace
            b.Floating(73, 3, 9);

            b.Start(0).Goal(121).Checkpoint(22).Checkpoint(41).Checkpoint(78);
            b.Scout(9, 3).Harvester(15.5f, 2);
            b.Scout(29, 3).Harvester(34.5f, 1.5f);
            b.Scout(47, 3).Scout(53, 1.5f);
            b.Harvester(64, 3);
            b.Scout(84, 3);
            b.Harvester(106, 2.5f).Scout(112, 2).Scout(117, 2);
            b.Chaser(52, 8);                  // wakes on the top-4 terrace
            b.Drone(30, 2.8f, 3).Drone(104, 2.8f, 3);
            b.CropRow(4, 8, 2);
            GapArc(b, 56, 3, 4);
            b.SecretRow(73.5f, 75.5f, 1f, 9.5f);
            return b;
        }

        // ---------------------------------------------------------------- 8

        // A 5-wide gap taken off from ice, an 11-wide one crossed on two ledges bobbing up and down in turn, and a stair
        // over the last gap up to the passage sign.
        private static LevelBuilder Level8()
        {
            var b = new LevelBuilder("FrozenTundra_08", "Blizzard Gate");
            b.Flat(28);                       // [-4,24) top 0
            b.Gap(3);                         // [24,27)
            b.IceFlat(23, 2);                 // [27,50) top 2, frozen up to a 5-wide gap
            b.Gap(11);                        // [50,61) gap with two ledges bobbing up and down in turn
            b.Flat(14, 2);                    // [61,75) top 2
            b.MovingLedge(52, 2, 1, 0, 3, 3.5f);         // top 1 <-> 4
            b.MovingLedge(56, 2, 1, 0, 3, 3.5f, 0.5f);   // starts at the top: up while the first is down
            b.Gap(3);                         // [75,78)
            b.IceFlat(25, 4);                 // [78,103) top 4, frozen up to another 5-wide gap
            b.Gap(5);                         // [103,108)
            b.Flat(22, 0);                    // [108,130) top 0

            b.Mound(98, 3, 2);                // stair over the last big gap
            b.Floating(102, 4, 8);
            b.SecretPassage(104.5f, 8, PassageLayout.Ledges);   // secret passage (rare pellet + coins): sign on the platform over the gap

            b.Start(0).Goal(128).Checkpoint(30).Checkpoint(62).Checkpoint(82).Checkpoint(111);
            b.Scout(9, 3).Harvester(18, 3);
            b.Scout(38, 3).Scout(45, 2.5f);
            b.Harvester(67, 2).Scout(71, 2);
            b.Scout(87, 2);
            b.Chaser(95, 9);                  // wakes as the player passes x=86; stopped by the mound at 98
            b.Harvester(118, 3).Scout(125, 2);
            b.Drone(46, 2.8f, 3).Drone(88, 2.8f, 3).Drone(123, 2.8f, 3);
            b.CropRow(4, 8, 2);
            GapArc(b, 24, 3, 0);
            GapArc(b, 75, 3, 2);
            b.Crop(99.5f);
            GapArc(b, 103, 5, 4);
            b.SecretRow(102.5f, 103.5f, 1f, 8.5f);
            return b;
        }

        // ---------------------------------------------------------------- 9

        // Terraces up to a windswept plateau with Bessie's cracked floor, a frozen step back down.
        private static LevelBuilder Level9()
        {
            var b = new LevelBuilder("FrozenTundra_09", "Cracked Glacier");
            b.Flat(26);                       // [-4,22) top 0
            b.Gap(3);                         // [22,25)
            b.Flat(20, 2);                    // [25,45) top 2
            b.IceFlat(18, 4);                 // [45,63) top 4, frozen
            b.Gap(4);                         // [63,67)
            b.Flat(22, 4);                    // [67,89) top 4
            b.Gap(10);                        // [89,99) a ledge rides across and down (2026-10-11; was 4 wide)
            b.IceFlat(10, 2);                 // [99,109) top 2, frozen
            b.MovingLedge(89, 3, 4, 7, -2, 4.5f); // [89,92) top 4 <-> [96,99) top 2
            b.Flat(24, 0);                    // [109,133) top 0

            b.Mound(10, 3, 2);                // opening perch
            b.Floating(15, 4, 4);
            b.Mound(55, 2, 2);                // plateau perch
            b.Floating(59, 4, 8);
            b.BreakableFloor(78, 4);          // hollow beneath: Bessie's Ground Pound
            b.Gate(CharacterType.Bessie, "Cracked wooden floor at x=78..82 on the plateau hides a hollow; only Ground Pound breaks it.");
            b.SecretRow(78.5f, 81.5f, 1f, 1.5f);
            b.SecretPassage(62f, 8, PassageLayout.Staircase);   // secret passage (rare pellet + coins): sign on the plateau perch (back up on the same spot)

            b.Start(0).Goal(128).Checkpoint(27).Checkpoint(47).Checkpoint(84).Checkpoint(111);
            b.Scout(7, 2).Harvester(17.5f, 2.5f);
            b.Scout(35, 3).Scout(41.5f, 2);
            b.Harvester(51.5f, 2.5f).Scout(60, 2);
            b.Scout(72, 3);
            b.Scout(103, 2);
            b.Chaser(106, 10);                // wakes as the player rides down to the frozen step
            b.Harvester(116, 3).Scout(123, 2.5f);
            b.Drone(34, 2.8f, 3).Drone(50, 2.8f, 3).Drone(100, 2.8f, 3);
            b.CropRow(4, 8, 2);
            b.CropAt(16, 4.9f).CropAt(17.5f, 4.9f);
            GapArc(b, 22, 3, 0);
            GapArc(b, 63, 4, 4);
            return b;
        }

        // ---------------------------------------------------------------- 10

        // A long frozen run with a Chaser waking mid-way, an ice-barrel pyramid, rising terraces; the secret island
        // behind the start as in level 4, and the passage perch just ahead of the start.
        private static LevelBuilder Level10()
        {
            var b = new LevelBuilder("FrozenTundra_10", "Whiteout Pass", -42);
            b.SecretFlat(8);                  // [-42,-34) island across a wide crevasse behind the start
            b.Gap(15);                        // [-34,-19)
            b.Flat(45);                       // [-19,26) top 0
            b.Gap(4);                         // [26,30)
            b.IceFlat(40);                    // [30,70) top 0, frozen
            b.Gap(12);                        // [70,82) two ledges meet mid-chasm, taken off from the ice (2026-10-11)
            b.Flat(16, 1);                    // [82,98) top 1
            b.MovingLedge(70, 3, 1, 3, 0, 4f);    // [70,73) <-> [73,76)
            b.MovingLedge(79, 3, 1, -3, 0, 4f);   // [79,82) <-> [76,79): they meet at x=76
            b.Gap(3);                         // [98,101)
            b.IceFlat(20, 3);                 // [101,121) top 3, frozen
            b.Flat(16, 0);                    // [121,137) top 0

            b.BarrelPyramid(20);
            b.StoneBlocks(52, 3, 4);          // bonus perch over the Chaser's stretch
            // Secret passage (rare pellet + coins): its sign on an ice-block perch just ahead of the start, in view
            // from spawn and a double jump up; back up on the same perch.
            b.StoneBlocks(3, 3, 4);
            b.SecretPassage(4.5f, 4, PassageLayout.Pyramid);
            b.Gate(CharacterType.Gerald, "Island across a 15-unit crevasse behind the start: too wide for the base jump; Puff Glide crosses it.", CharacterType.Woolly);
            b.SecretRow(-40.5f, -36.5f, 1f, 0.5f);

            b.Start(0).Goal(133).Checkpoint(32).Checkpoint(84).Checkpoint(103);
            b.Scout(10, 3).Scout(24.5f, 1);
            b.Harvester(48, 3).Scout(54, 2).Harvester(60, 2);
            b.Chaser(66, 12);                 // wakes as the player crosses the perch; stompable, and outrunnable
            b.Harvester(89, 2.5f).Scout(94.5f, 2.5f);
            b.Scout(108, 3);
            b.Chaser(118, 10);                // wakes on the frozen top-3 terrace
            b.Scout(126, 3);
            b.Drone(64, 2.8f, 3).Drone(90, 2.8f, 3).Drone(128, 2.8f, 3);
            b.CropRow(4, 9, 2);
            GapArc(b, 26, 4, 0);
            b.CropAt(52.4f, 4.9f).CropAt(53.65f, 4.9f).CropAt(54.9f, 4.9f);
            GapArc(b, 98, 3, 1);
            return b;
        }

        // ---------------------------------------------------------------- 11

        // Capstone before the boss: five terraces, three of them frozen, three 5-wide gaps, five checkpoints, and the
        // passage sign on a block top level above the highest platform.
        private static LevelBuilder Level11()
        {
            var b = new LevelBuilder("FrozenTundra_11", "Overlord's Icecap");
            b.Flat(24);                       // [-4,20) top 0
            b.Gap(3);                         // [20,23)
            b.IceFlat(21, 2);                 // [23,44) top 2, frozen up to a 5-wide gap
            b.Gap(5);                         // [44,49)
            b.Flat(20, 2);                    // [49,69) top 2
            b.IceFlat(16, 4);                 // [69,85) top 4, frozen up to a 5-wide gap
            b.Gap(5);                         // [85,90)
            b.Flat(18, 4);                    // [90,108) top 4
            b.Gap(4);                         // [108,112)
            b.IceFlat(22, 2);                 // [112,134) top 2, frozen up to the last 5-wide gap
            b.Gap(5);                         // [134,139)
            b.Flat(24, 0);                    // [139,163) top 0

            b.Mound(52, 3, 2);                // perch after the first wide gap
            b.Floating(56, 4, 6);
            b.Mound(93, 3, 2);                // open stair up to the high platform
            b.Floating(97, 4, 8);
            b.Floating(102, 10, 10);
            // The Breakable Wall chamber was removed (2026-10-03, as in Meadow Ruins 11): its roof is now a block top
            // level, a double jump up from the platform, carrying the passage sign.
            b.StoneBlocks(106, 6, 13);
            b.MovingLedge(46, 2, 2, 0, 5, 4.5f); // in the first wide gap: bobs top 2 (level with the banks) <-> 7 (2026-10-11)
            b.StoneBlocks(43, 2, 8);             // bonus perch over the gap's edge: a hop off the ledge at the top
            b.BonusCoin(44f, 8);
            b.SecretPassage(109f, 13, PassageLayout.Pillars);   // secret passage (rare pellet + coins): sign on the top level

            b.Start(0).Goal(158).Checkpoint(26).Checkpoint(51).Checkpoint(91).Checkpoint(114).Checkpoint(141);
            b.Scout(8, 3).Harvester(15.5f, 2.5f);
            b.Scout(33, 3).Scout(40, 2.5f);
            b.Harvester(62, 3).Scout(66.5f, 1.5f);
            b.Scout(74, 3);
            b.Chaser(80, 10);                 // wakes on the frozen top-4 terrace, right before the second wide gap
            b.Scout(102, 3);
            b.Scout(119, 2.5f).Harvester(126, 3);
            b.Chaser(131, 8);                 // wakes on the last ice, just before the last wide gap
            b.Scout(146, 3).Harvester(153, 3);
            b.Drone(40, 3.2f, 3).Drone(82, 2.8f, 3).Drone(120, 2.8f, 4).Drone(150, 2.8f, 3);
            b.CropRow(4, 7, 2);
            GapArc(b, 20, 3, 0);
            GapArc(b, 44, 5, 2);
            b.Crop(53.5f);
            b.CropAt(57, 6.9f).CropAt(58.5f, 6.9f);
            GapArc(b, 85, 5, 4);
            b.Crop(94.5f);
            b.CropAt(98, 8.9f).CropAt(99.5f, 8.9f);
            GapArc(b, 108, 4, 4);
            GapArc(b, 134, 5, 2);
            return b;
        }

        // ---------------------------------------------------------------- boss

        // The Glacier Harvester's ice fortress (2026-10-11: as long and hard as Meadow Ruins' boss level). A long
        // approach - terraces up to the fortress wall and back down, two of them frozen, four gaps, an ice-barrel
        // pyramid, a Chaser on the wall - then Meadow's moving-ledge gauntlet over three pits (two ledges sliding
        // together, a bobbing pair after a checkpoint island, one long slide), taken off from ice, then the arena
        // whose middle is ice between two cover mounds. Three hits; waves after hits 1 and 2. No goal: defeating
        // the boss completes the level.
        private static LevelBuilder LevelBoss()
        {
            var b = new LevelBuilder("FrozenTundra_Boss", "Commander's Ice Fortress");
            b.Flat(26);                       // [-4,22) top 0
            b.Gap(3);                         // [22,25)
            b.Flat(22, 2);                    // [25,47) top 2
            b.Gap(4);                         // [47,51)
            b.IceFlat(20, 2);                 // [51,71) top 2, frozen
            b.Flat(14, 4);                    // [71,85) top 4: the fortress wall
            b.Gap(4);                         // [85,89)
            b.IceFlat(18, 2);                 // [89,107) top 2, frozen right up to the gauntlet
            b.Gap(12);                        // [107,119) two ledges sliding together
            b.Flat(6, 2);                     // [119,125) checkpoint island
            b.Gap(10);                        // [125,135) two ledges bobbing in turn
            b.Flat(5, 2);                     // [135,140)
            b.Gap(12);                        // [140,152) one long slide
            b.Flat(14, 0);                    // [152,166) arena, solid footing by the gate
            b.IceFlat(30);                    // [166,196) frozen arena floor
            b.Flat(16, 0);                    // [196,212) arena, solid footing at the far wall
            b.MovingLedge(107, 3, 2, 3, 0, 3.5f);       // [107,110) <-> [110,113)
            b.MovingLedge(116, 3, 2, -3, 0, 3.5f);      // [116,119) <-> [113,116): they meet at x=113
            b.MovingLedge(127, 2, 1, 0, 3, 3f);         // top 1 <-> 4
            b.MovingLedge(131, 2, 1, 0, 3, 3f, 0.5f);   // up while the first is down
            b.MovingLedge(140, 3, 2, 9, 0, 4.5f);       // [140,143) <-> [149,152), docks at each bank
            b.BarrelPyramid(39);
            b.Mound(161, 3, 2);               // cover / stomp platforms on the solid ground either side
            b.Mound(199, 3, 2);

            b.Boss();
            b.Start(0).Checkpoint(27).Checkpoint(53).Checkpoint(91).Checkpoint(121).Checkpoint(156);
            // The approach: 1 Chaser, 2 Drones, 3 Harvesters, 4 Scouts.
            b.Scout(10, 3).Harvester(17, 2);
            b.Scout(32, 2.5f).Harvester(44, 1.5f);
            b.Scout(64, 2.5f);                // on the first ice
            b.Chaser(80, 10);                 // wakes as the player climbs onto the fortress wall
            b.Scout(98, 3).Harvester(103, 2); // on the ice before the gauntlet
            b.Drone(58, 3.2f, 3).Drone(96, 2.8f, 3);
            // The arena.
            b.Commander(181, 8);                                       // patrols [173,189] on the ice
            b.Harvester(169, 2).Scout(192, 3);                         // arena guards (wave 0)
            b.Scout(205, 3, wave: 1).Harvester(175, 2, wave: 1).Drone(195, 2.8f, 3, wave: 1);   // after hit 1
            b.Drone(179, 2.8f, 4, wave: 2).Drone(167, 2.8f, 3, wave: 2).Scout(187, 3, wave: 2); // after hit 2

            b.CropRow(4, 20, 3);
            GapArc(b, 22, 3, 0);
            GapArc(b, 47, 4, 2);
            GapArc(b, 85, 4, 4);
            b.Crop(162.5f);
            b.Crop(200.5f);
            return b;
        }
    }
}
