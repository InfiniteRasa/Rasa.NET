using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The control-point turrets on the outpost fence's own gun bases (Fence_turret_bases).
    ///
    /// The wire fence (ArchHumOutpostFence*V02: the 8 m mesh fence, CornerV04, Straight8V02 and
    /// the rest) has its gun bases built into one piece, Straight8V03 (and its copy Straight8V06,
    /// arch_hum_outpost_fence_straight_8_v06.geo): a walkway out from the fence to a round base, the
    /// Human_base_turret bone at (-3.993, 6.256, -7.895) from the piece's origin, as CornerV03/V11's
    /// is at the end of their diagonal walkway. Add_control_point_turrets put the turrets of the
    /// points with that fence on its corners and walls instead, from a wall height measured for it
    /// - on the top of the mesh, 2 m out from it or in the air beside it: Hydro Plant's eight, and
    /// the same at Research Point, Northeast and Northwest AFS, White Oasis Post, Iapyx, Ortho,
    /// Geyser Chimney Basin, Outpost Aurora, Prometheus Outpost, Charon's Crossing and Dead Zone
    /// Power Station - twenty of them over nothing at all - while every Straight8V03 base and some
    /// corners' stood empty. Afs_turret_bases moved the corner pieces' turrets onto their bases and
    /// did not know this piece had one.
    ///
    /// A point's bases are the built-in ones (Straight8V03/V06, CornerV03/V08/V11/V12) and the
    /// turret platforms the client's .map places within its turrets' reach (their farthest from the
    /// point and 15 m) and nearer it than any other point. On each:
    ///  - Moved: each turret off a base goes to the nearest empty one on its point, the AFS turret
    ///    on the bone and the Bane turret 0.38 m over it (Afs_turret_bases), both facing out from
    ///    the point.
    ///  - Removed: a point with more turrets off a base than empty bases loses the rest, AFS and
    ///    Bane pools and their links: turrets stand where the map built a place for them.
    ///  - Added: an empty base left over gets a pair of its own, the point's zone's turrets.
    /// Left as they are: the turrets on the tops of the solid wall (ArchHumOutpostFence*V01) at
    /// the Landing Zone (whose four are the assault's mounts, LandingZoneAssaultTargeting) and two
    /// of Prometheus Outpost's, which stand on the wall; and Iapyx's two empty platforms, in a wild
    /// pack's scan (Add_control_point_turrets).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A pool moves, or is removed, only from where it
    /// stands and as the creature it is; an added pool goes in only where its id is free. Down
    /// moves back only what still stands where Up put it, puts back only what is missing, and
    /// takes out only what Up added and is still where Up put it.
    /// </summary>
    public static class FenceTurretBases
    {
        public const uint Respawn = 3000;

        public sealed record Place(double X, double Y, double Z, double Rotation);

        public sealed record Move(uint Point, uint AfsPool, uint BanePool, uint Map, uint AfsCreature, uint BaneCreature,
            Place AfsFrom, Place BaneFrom, Place AfsTo, Place BaneTo);

        public sealed record Turret(uint Point, uint AfsPool, uint BanePool, uint Map, uint AfsCreature, uint BaneCreature, Place Afs, Place Bane);

        public static readonly Move[] Moves =
        {
            // 2 Hydro Plant
            new(2, 551101, 551102, 1148, 550015, 551002,
                new(-189.71, 65, 11.27, -1.031), new(-189.71, 65, 11.27, -1.031),
                new(-203.993, 63.256, -4.895, -0.6846), new(-203.993, 63.636, -4.895, -0.6846)),   // onto a Straight8V03's base
            new(2, 551103, 551104, 1148, 550015, 551002,
                new(-189.75, 65, 42.5, -1.5522), new(-189.75, 65, 42.5, -1.5522),
                new(-204.105, 63.256, 83.007, -2.3576), new(-204.105, 63.636, 83.007, -2.3576)),   // onto a Straight8V03's base
            new(2, 551105, 551106, 1148, 550015, 551002,
                new(-189.5, 64.82, 123.39, -2.5472), new(-189.5, 64.82, 123.39, -2.5472),
                new(-204.105, 63.256, 107.007, -2.5864), new(-204.105, 63.636, 107.007, -2.5864)),   // onto a Straight8V03's base
            new(2, 551109, 551110, 1148, 550015, 551002,
                new(-306.14, 65, 11.26, 1.0955), new(-306.14, 65, 11.26, 1.0955),
                new(-243.993, 63.256, -4.895, 0.0102), new(-243.993, 63.636, -4.895, 0.0102)),   // onto a Straight8V03's base
            new(2, 551111, 551112, 1148, 550015, 551002,
                new(-252.5, 64.82, 169.5, 3.0703), new(-252.5, 64.82, 169.5, 3.0703),
                new(-236.007, 63.256, 162.895, -3.0789), new(-236.007, 63.636, 162.895, -3.0789)),   // onto a Straight8V03's base
            new(2, 551115, 551116, 1148, 550015, 551002,
                new(-342.18, 65, 150.82, 2.3981), new(-342.18, 65, 150.82, 2.3981),
                new(-328.007, 63.256, 166.895, 2.5411), new(-328.007, 63.636, 166.895, 2.5411)),   // onto a Straight8V03's base
            // 10 Research Point
            new(10, 551151, 551152, 1454, 550023, 551006,
                new(-327.85, 223.82, -241.5, 3.0473), new(-327.85, 223.82, -241.5, 3.0473),
                new(-371.895, 222.256, -312.007, 1.5873), new(-371.895, 222.636, -312.007, 1.5873)),   // onto a Straight8V03's base
            new(10, 551153, 551154, 1454, 550023, 551006,
                new(-288.25, 223.81, -241.5, -2.7101), new(-288.25, 223.81, -241.5, -2.7101),
                new(-244.105, 222.256, -351.993, -1.1004), new(-244.105, 222.636, -351.993, -1.1004)),   // onto a Straight8V03's base
            new(10, 551157, 551158, 1454, 550023, 551006,
                new(-249.5, 223.81, -284.25, -1.9508), new(-249.5, 223.81, -284.25, -1.9508),
                new(-244.105, 222.256, -295.993, -1.7863), new(-244.105, 222.636, -295.993, -1.7863)),   // onto a Straight8V03's base
            // 12 Northeast AFS
            new(12, 551163, 551164, 1497, 550024, 551004,
                new(75.85, 378, 394.28, 1.0268), new(75.85, 378, 394.28, 1.0268),
                new(70.105, 376.256, 405.993, 1.3081), new(70.105, 376.636, 405.993, 1.3081)),   // onto a Straight8V03's base
            new(12, 551165, 551166, 1497, 550024, 551004,
                new(75.86, 378, 441.87, 2.1121), new(75.86, 378, 441.87, 2.1121),
                new(70.105, 376.256, 429.993, 1.827), new(70.105, 376.636, 429.993, 1.827)),   // onto a Straight8V03's base
            // 13 Northwest AFS
            new(13, 551171, 551172, 1497, 550024, 551004,
                new(-408.85, 383.82, 469.5, 2.5834), new(-408.85, 383.82, 469.5, 2.5834),
                new(-397.007, 382.256, 474.895, 2.8695), new(-397.007, 382.636, 474.895, 2.8695)),   // onto a Straight8V03's base
            new(13, 551173, 551174, 1497, 550024, 551004,
                new(-361, 383.82, 469.5, -2.5911), new(-361, 383.82, 469.5, -2.5911),
                new(-373.007, 382.256, 474.895, -2.8817), new(-373.007, 382.636, 474.895, -2.8817)),   // onto a Straight8V03's base
            new(13, 551175, 551176, 1497, 550024, 551004,
                new(-349.11, 384, 356.87, -0.4485), new(-349.11, 384, 356.87, -0.4485),
                new(-372.993, 382.256, 351.105, -0.146), new(-372.993, 382.636, 351.105, -0.146)),   // onto a Straight8V03's base
            new(13, 551177, 551178, 1497, 550024, 551004,
                new(-420.75, 384, 356.87, 0.4532), new(-420.75, 384, 356.87, 0.4532),
                new(-396.993, 382.256, 351.105, 0.1527), new(-396.993, 382.636, 351.105, 0.1527)),   // onto a Straight8V03's base
            // 17 White Oasis Post
            new(17, 551183, 551184, 1734, 550001, 551010,
                new(-438.5, 289.82, 142.14, -1.2738), new(-438.5, 289.82, 142.14, -1.2738),
                new(-433.105, 288.256, 154.007, -1.4389), new(-433.105, 288.636, 154.007, -1.4389)),   // onto a Straight8V03's base
            new(17, 551185, 551186, 1734, 550001, 551010,
                new(-438.5, 289.82, 190, -1.9105), new(-438.5, 289.82, 190, -1.9105),
                new(-433.105, 288.256, 178.007, -1.7442), new(-433.105, 288.636, 178.007, -1.7442)),   // onto a Straight8V03's base
            // 21 Ortho
            new(21, 551207, 551208, 1761, 550008, 551009,
                new(333.84, 268, 79.84, 2.251), new(333.84, 268, 79.84, 2.251),
                new(359.993, 266.256, 95.895, 2.6746), new(359.993, 266.636, 95.895, 2.6746)),   // onto a Straight8V03's base
            new(21, 551209, 551210, 1761, 550008, 551009,
                new(428.4, 267.82, 110.5, -2.699), new(428.4, 267.82, 110.5, -2.699),
                new(439.993, 266.256, 115.895, -2.6138), new(439.993, 266.636, 115.895, -2.6138)),   // onto a Straight8V03's base
            new(21, 551211, 551212, 1761, 550008, 551009,
                new(452, 267.82, 110.5, -2.4801), new(452, 267.82, 110.5, -2.4801),
                new(399.993, 266.256, 95.895, -3.0102), new(399.993, 266.636, 95.895, -3.0102)),   // onto a Straight8V03's base
            new(21, 551213, 551214, 1761, 550008, 551009,
                new(462.5, 267.82, 60.39, -1.9394), new(462.5, 267.82, 60.39, -1.9394),
                new(467.895, 266.256, 80.007, -2.1232), new(467.895, 266.636, 80.007, -2.1232)),   // onto a Straight8V03's base
            new(21, 551215, 551216, 1761, 550008, 551009,
                new(423.88, 268, -26.14, -0.4982), new(423.88, 268, -26.14, -0.4982),
                new(439.935, 266.256, -11.973, -0.8201), new(439.935, 266.636, -11.973, -0.8201)),   // onto a Straight8V03's base
            new(21, 551221, 551222, 1761, 550008, 551009,
                new(301.64, 268.09, 8.15, 1.3012), new(301.64, 268.09, 8.15, 1.3012),
                new(296.105, 266.256, 23.993, 1.4765), new(296.105, 266.636, 23.993, 1.4765)),   // onto a Straight8V03's base
            new(21, 551223, 551224, 1761, 550008, 551009,
                new(301.65, 268.1, 44, 1.692), new(301.65, 268.1, 44, 1.692),
                new(319.993, 266.256, 59.895, 1.9292), new(319.993, 266.636, 59.895, 1.9292)),   // onto a Straight8V03's base
            // 28 Outpost Aurora
            new(28, 551259, 551260, 1911, 550004, 551011,
                new(785.2, 392, -248.2, -0.8907), new(785.2, 392, -248.2, -0.8907),
                new(761.007, 390.256, -241.895, -0.6454), new(761.007, 390.636, -241.895, -0.6454)),   // onto a Straight8V03's base
            // 31 Prometheus Outpost
            new(31, 551267, 551268, 1993, 550002, 551013,
                new(353.82, 176, 359.9, 1.0268), new(353.82, 176, 359.9, 1.0268),
                new(368.007, 174.256, 344.105, 0.6329), new(368.007, 174.636, 344.105, 0.6329)),   // onto a Straight8V03's base
            new(31, 551269, 551270, 1993, 550002, 551013,
                new(446.5, 175.82, 360.14, -1.031), new(446.5, 175.82, 360.14, -1.031),
                new(432.007, 174.256, 344.105, -0.6288), new(432.007, 174.636, 344.105, -0.6288)),   // onto a Straight8V03's base
            // 33 Charon's Crossing
            new(33, 551275, 551276, 2028, 550007, 551012,
                new(-85.16, 542, -178.21, 0.9576), new(-85.16, 542, -178.21, 0.9576),
                new(-45.993, 540.256, -187.895, 0.202), new(-45.993, 540.636, -187.895, 0.202)),   // onto a Straight8V03's base
            new(33, 551277, 551278, 2028, 550007, 551012,
                new(-73.85, 541.82, -89.5, 2.5513), new(-73.85, 541.82, -89.5, 2.5513),
                new(-46.007, 540.256, -84.105, 2.9946), new(-46.007, 540.636, -84.105, 2.9946)),   // onto a Straight8V03's base
            new(33, 551279, 551280, 2028, 550007, 551012,
                new(8.5, 541.82, -171.86, -1.0282), new(8.5, 541.82, -171.86, -1.0282),
                new(-21.993, 540.256, -187.895, -0.3338), new(-21.993, 540.636, -187.895, -0.3338)),   // onto a Straight8V03's base
            new(33, 551281, 551282, 2028, 550007, 551012,
                new(-2, 541.82, -89.5, -2.5728), new(-2, 541.82, -89.5, -2.5728),
                new(-22.007, 540.256, -84.105, -2.8965), new(-22.007, 540.636, -84.105, -2.8965)),   // onto a Straight8V03's base
            // 40 Dead Zone Power Station
            new(40, 551291, 551292, 2051, 550019, 551014,
                new(-538.6, 208.9, -459.18, 0.5576), new(-538.6, 208.9, -459.18, 0.5576),
                new(-554.871, 207.256, -444.96, 0.7527), new(-554.871, 207.636, -444.96, 0.7527)),   // onto a Straight8V03's base
        };

        public static readonly Turret[] Removed =
        {
            // 2 Hydro Plant
            new(2, 551107, 551108, 1148, 550015, 551002, new(-200, 64.82, 157.5, -2.7771), new(-200, 64.82, 157.5, -2.7771)),
            new(2, 551113, 551114, 1148, 550015, 551002, new(-342.36, 65.1, 79.4, 1.9191), new(-342.36, 65.1, 79.4, 1.9191)),
            // 12 Northeast AFS
            new(12, 551167, 551168, 1497, 550024, 551004, new(126, 378.09, 383.64, -0.3004), new(126, 378.09, 383.64, -0.3004)),
            new(12, 551169, 551170, 1497, 550024, 551004, new(126, 377.82, 452.5, -2.8399), new(126, 377.82, 452.5, -2.8399)),
            // 17 White Oasis Post
            new(17, 551187, 551188, 1734, 550001, 551010, new(-469, 289.82, 220.5, -2.4984), new(-469, 289.82, 220.5, -2.4984)),
            new(17, 551189, 551190, 1734, 550001, 551010, new(-492.6, 289.82, 220.5, -2.8236), new(-492.6, 289.82, 220.5, -2.8236)),
            new(17, 551191, 551192, 1734, 550001, 551010, new(-492.61, 290.1, 111.65, -0.3371), new(-492.61, 290.1, 111.65, -0.3371)),
            new(17, 551193, 551194, 1734, 550001, 551010, new(-469.16, 290, 111.84, -0.6737), new(-469.16, 290, 111.84, -0.6737)),
            new(17, 551195, 551196, 1734, 550001, 551010, new(-535.14, 290, 177.88, 2.0831), new(-535.14, 290, 177.88, 2.0831)),
            new(17, 551197, 551198, 1734, 550001, 551010, new(-535.13, 290, 154.24, 1.1728), new(-535.13, 290, 154.24, 1.1728)),
            // 19 Iapyx
            new(19, 551199, 551200, 1759, 550009, 551008, new(634.13, 268.3, -311.83, -2.6276), new(634.13, 268.3, -311.83, -2.6276)),
            new(19, 551201, 551202, 1759, 550009, 551008, new(518.75, 268, -311.33, 2.7034), new(518.75, 268, -311.33, 2.7034)),
            // 22 Geyser Chimney Basin
            new(22, 551225, 551226, 1764, 550011, 551007, new(146.86, 440, 229.27, 1.0627), new(146.86, 440, 229.27, 1.0627)),
            new(22, 551227, 551228, 1764, 550011, 551007, new(215.5, 439.82, 229.14, -1.0268), new(215.5, 439.82, 229.14, -1.0268)),
            // 31 Prometheus Outpost
            new(31, 551271, 551272, 1993, 550002, 551013, new(352.93, 177.17, 377.36, 1.3508), new(352.93, 177.17, 377.36, 1.3508)),
            new(31, 551273, 551274, 1993, 550002, 551013, new(448.76, 176, 370.83, -1.2328), new(448.76, 176, 370.83, -1.2328)),
        };

        public static readonly Turret[] Added =
        {
            // 6 Fort Dew
            new(6, 551301, 551302, 1244, 550016, 551003, new(-96.105, 178.256, -707.993, -1.7099), new(-96.105, 178.636, -707.993, -1.7099)),   // on a Straight8V03's base
            new(6, 551303, 551304, 1244, 550016, 551003, new(-96.105, 178.256, -731.993, -1.4366), new(-96.105, 178.636, -731.993, -1.4366)),   // on a Straight8V03's base
            // 21 Ortho
            new(21, 551305, 551306, 1761, 550008, 551009, new(439.895, 266.256, 43.957, -1.7936), new(439.895, 266.636, 43.957, -1.7936)),   // on a Straight8V03's base
            // 28 Outpost Aurora
            new(28, 551307, 551308, 1911, 550004, 551011, new(704.988, 390.241, -126.112, 2.7852), new(704.988, 390.621, -126.112, 2.7852)),   // on a CornerV03's base
            new(28, 551309, 551310, 1911, 550004, 551011, new(737.007, 390.256, -241.895, -0.0455), new(737.007, 390.636, -241.895, -0.0455)),   // on a Straight8V03's base
            // 33 Charon's Crossing
            new(33, 551311, 551312, 2028, 550007, 551012, new(13.895, 540.256, -135.993, -1.7338), new(13.895, 540.636, -135.993, -1.7338)),   // on a Straight8V03's base
            new(33, 551313, 551314, 2028, 550007, 551012, new(-89.835, 540.276, -135.997, 1.7284), new(-89.835, 540.656, -135.997, 1.7284)),   // on a Straight8V03's base
            // 40 Dead Zone Power Station
            new(40, 551315, 551316, 2051, 550019, 551014, new(-399.105, 207.256, -392.993, -1.2325), new(-399.105, 207.636, -392.993, -1.2325)),   // on a Straight8V03's base
            new(40, 551317, 551318, 2051, 550019, 551014, new(-399.105, 207.256, -416.993, -0.9946), new(-399.105, 207.636, -416.993, -0.9946)),   // on a Straight8V03's base
            new(40, 551319, 551320, 2051, 550019, 551014, new(-554.871, 207.256, -380.96, 1.3573), new(-554.871, 207.636, -380.96, 1.3573)),   // on a Straight8V03's base
            new(40, 551321, 551322, 2051, 550019, 551014, new(-554.871, 207.256, -356.96, 1.6727), new(-554.871, 207.636, -356.96, 1.6727)),   // on a Straight8V03's base
        };

        public static IEnumerable<string> Up()
        {
            foreach (var m in Moves)
            {
                yield return Update(m.AfsPool, m.Map, m.AfsCreature, m.AfsFrom, m.AfsTo);
                yield return Update(m.BanePool, m.Map, m.BaneCreature, m.BaneFrom, m.BaneTo);
            }

            foreach (var t in Removed)
                foreach (var sql in Remove(t))
                    yield return sql;

            foreach (var t in Added)
                foreach (var sql in Insert(t))
                    yield return sql;
        }

        public static IEnumerable<string> Down()
        {
            foreach (var t in Added)
                foreach (var sql in Remove(t))
                    yield return sql;

            foreach (var t in Removed)
                foreach (var sql in Insert(t))
                    yield return sql;

            foreach (var m in Moves)
            {
                yield return Update(m.AfsPool, m.Map, m.AfsCreature, m.AfsTo, m.AfsFrom);
                yield return Update(m.BanePool, m.Map, m.BaneCreature, m.BaneTo, m.BaneFrom);
            }
        }

        /// <summary>The AFS pool (kind 2, mode 0) and the Bane pool (kind 1, mode 1) of a pair.</summary>
        private static IEnumerable<(uint Pool, uint Creature, Place At, int Mode, byte Kind)> Pools(Turret t)
        {
            yield return (t.AfsPool, t.AfsCreature, t.Afs, 0, ControlPointLinkEntry.KindAfsPool);
            yield return (t.BanePool, t.BaneCreature, t.Bane, 1, ControlPointLinkEntry.KindBanePool);
        }

        private static IEnumerable<string> Remove(Turret t)
        {
            foreach (var (pool, creature, at, _, kind) in Pools(t))
            {
                yield return $"DELETE FROM {ControlPointLinkEntry.TableName} WHERE control_point_id = {t.Point} AND kind = {kind} AND object_id = {pool} " +
                    $"AND EXISTS (SELECT 1 FROM spawnpool s WHERE s.{Guard(pool, t.Map, creature, at, "s.")});";
                yield return $"DELETE FROM spawnpool WHERE {Guard(pool, t.Map, creature, at, "")};";
            }
        }

        private static IEnumerable<string> Insert(Turret t)
        {
            foreach (var (pool, creature, at, mode, kind) in Pools(t))
            {
                yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"SELECT {pool}, {mode}, 0, {Respawn}, {F(at.X)}, {F(at.Y)}, {F(at.Z)}, {F(at.Rotation)}, {t.Map}, {creature}, 1, 1, " +
                    "0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 " +
                    $"FROM creature c WHERE c.id = {creature} AND EXISTS (SELECT 1 FROM {ControlPointEntry.TableName} p WHERE p.id = {t.Point}) " +
                    $"AND NOT EXISTS (SELECT 1 FROM spawnpool x WHERE x.id = {pool});";
                yield return $"INSERT INTO {ControlPointLinkEntry.TableName} (control_point_id, kind, object_id) " +
                    $"SELECT {t.Point}, {kind}, s.id FROM spawnpool s WHERE s.{Guard(pool, t.Map, creature, at, "s.")} " +
                    $"AND NOT EXISTS (SELECT 1 FROM {ControlPointLinkEntry.TableName} l WHERE l.control_point_id = {t.Point} AND l.kind = {kind} AND l.object_id = {pool});";
            }
        }

        private static string Update(uint pool, uint map, uint creature, Place from, Place to) =>
            $"UPDATE spawnpool SET pos_x = {F(to.X)}, pos_y = {F(to.Y)}, pos_z = {F(to.Z)}, rotation = {F(to.Rotation)} WHERE {Guard(pool, map, creature, from, "")};";

        /// <summary>The pool, as the creature it is, where it stands: the first column bare, so that it can follow an alias.</summary>
        private static string Guard(uint pool, uint map, uint creature, Place at, string alias) =>
            $"id = {pool} AND {alias}map_context_id = {map} AND {alias}creature_1_Id = {creature} " +
            $"AND ABS({alias}pos_x - ({F(at.X)})) < 0.001 AND ABS({alias}pos_y - ({F(at.Y)})) < 0.001 AND ABS({alias}pos_z - ({F(at.Z)})) < 0.001";

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
