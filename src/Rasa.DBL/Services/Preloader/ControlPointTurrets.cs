using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Turrets on the walls of the AFS control points, the holder's own (Add_control_point_turrets):
    /// on each spot an AFS turret in the AFS's garrison and a Bane turret in the Bane's, so the
    /// side that holds the point has its turret there and the other's is not (ControlPoints).
    ///
    /// The spots, 96 on 16 points:
    ///  - The outer corners of the outpost fence (ArchHumOutpostFence*) around 15 points, on the
    ///    fence's own top, from the client's maps and their collision meshes: the catwalk at a
    ///    corner where the fence has one (8 m), the top of the wall where it has none (9.4 m),
    ///    and the round platform at the end of the diagonal walkway a CornerV03 sends out from
    ///    its corner (6 m). Where a fence ends against a cliff (Landing Zone, Prometheus Outpost)
    ///    its end is a corner too. The inside corners of a fence that steps in are not.
    ///  - The turret platforms (ArchHumanBaseTurretPlatformV01) the maps place at control points,
    ///    which Add_afs_turrets left empty for them: two at Hydro Plant, Research Point,
    ///    Geyser Chimney Basin, Lightning Fields and Iapyx, one at Outpost Aurora and Retread
    ///    Outpost; as Add_afs_turrets has them, 2.56 m over the platform. Iapyx's other two stand
    ///    in the scan of a wild pack (pool 540089) laid over the point itself, and are left out.
    ///
    /// The AFS turret is the zone's standard one (Emplacement_AFS_Turret_Standard, 5500xx). The
    /// Bane turret is the client's Bane counterpart, Emplacement_Bane_Turret_Standard, with its
    /// Weapon_Creature_Bane_Mortar_Launcher (WEAPON_GROUNDTARGET 1, as the Bane Mortar 590001):
    /// one creature row a zone, 551001 on, with the zone's AFS turret's level, health, stats
    /// and damage. Each turret faces out from the point. An AFS pool is automatic (mode 0) and a
    /// Bane pool a control point's (mode 1), as the garrisons already are; both respawn as
    /// Add_afs_turrets' turrets do (3000).
    ///
    /// Each pool is linked to its point (control_point_link: kind 2 the AFS's, kind 1 the
    /// Bane's), so a turret is part of its side's garrison: the Bane's turrets have to be
    /// destroyed with the rest of their garrison before a player can take the point, and the
    /// AFS's with theirs before the Bane do.
    /// </summary>
    public static class ControlPointTurrets
    {
        public const uint CreatureIdMin = 551001;
        public const uint CreatureIdMax = 551099;
        public const uint PoolIdMin = 551101;
        public const uint PoolIdMax = 551999;
        public const uint ActionIdMin = 73001;
        public const uint ActionIdMax = 73099;

        public const uint BaneTurretClass = 7482;           // Emplacement_Bane_Turret_Standard
        public const uint BaneMortarLauncher = 10604;       // Weapon_Creature_Bane_Mortar_Launcher
        public const uint Respawn = 3000;

        /// <summary>A zone's Bane turret, made from its AFS standard turret: level, health, body/mind/spirit/health/armour, and damage.</summary>
        public sealed record Zone(string Name, uint AfsTurret, uint BaneTurret, uint Action, uint Level, uint Health,
            uint Body, uint Mind, uint Spirit, uint StatHealth, uint Armor, uint MinDamage, uint MaxDamage);

        /// <summary>One spot: its point, where the turret stands and faces, its map, and the two pools.</summary>
        public sealed record Spot(uint Point, double X, double Y, double Z, double Rotation, uint Map, uint AfsTurret, uint BaneTurret, uint AfsPool, uint BanePool);

        public static readonly Zone[] Zones =
        {
            new("Wilderness", 550018, 551001, 73001, 15, 2892, 15, 15, 15, 2892, 130, 62, 93),
            new("Divide", 550015, 551002, 73002, 20, 4460, 15, 15, 15, 4460, 180, 95, 143),
            new("Palisades", 550016, 551003, 73003, 28, 8919, 15, 15, 15, 8919, 260, 190, 285),
            new("Plateau", 550024, 551004, 73004, 32, 12614, 15, 15, 15, 12614, 300, 269, 404),
            new("Pools", 550027, 551005, 73005, 35, 16358, 15, 15, 15, 16358, 330, 349, 523),
            new("Marshes", 550023, 551006, 73006, 38, 21213, 15, 15, 15, 21213, 360, 453, 679),
            new("Plains", 550011, 551007, 73007, 40, 25227, 15, 15, 15, 25227, 380, 538, 807),
            new("Mires", 550009, 551008, 73008, 41, 27510, 15, 15, 15, 27510, 390, 587, 880),
            new("Incline", 550008, 551009, 73009, 42, 30000, 15, 15, 15, 30000, 400, 640, 960),
            new("Ashen Desert", 550001, 551010, 73010, 44, 35676, 15, 15, 15, 35676, 420, 761, 1142),
            new("Thunderhead", 550004, 551011, 73011, 45, 38905, 15, 15, 15, 38905, 430, 830, 1245),
            new("Abyss", 550007, 551012, 73012, 46, 42427, 15, 15, 15, 42427, 440, 905, 1358),
            new("Crucible", 550002, 551013, 73013, 47, 46266, 15, 15, 15, 46266, 450, 987, 1481),
            new("Howling Maw", 550019, 551014, 73014, 48, 50454, 15, 15, 15, 50454, 460, 1076, 1615),
        };

        public static readonly Spot[] Spots =
        {
            // 2 Hydro Plant
            new(2, -189.71, 65.00, 11.27, -1.0310, 1148, 550015, 551002, 551101, 551102),   // fence corner
            new(2, -189.75, 65.00, 42.50, -1.5522, 1148, 550015, 551002, 551103, 551104),   // fence corner
            new(2, -189.50, 64.82, 123.39, -2.5472, 1148, 550015, 551002, 551105, 551106),   // fence corner
            new(2, -200.00, 64.82, 157.50, -2.7771, 1148, 550015, 551002, 551107, 551108),   // fence corner
            new(2, -306.14, 65.00, 11.26, 1.0955, 1148, 550015, 551002, 551109, 551110),   // fence corner
            new(2, -252.50, 64.82, 169.50, 3.0703, 1148, 550015, 551002, 551111, 551112),   // fence corner
            new(2, -342.36, 65.10, 79.40, 1.9191, 1148, 550015, 551002, 551113, 551114),   // fence corner
            new(2, -342.18, 65.00, 150.82, 2.3981, 1148, 550015, 551002, 551115, 551116),   // fence corner
            new(2, -180.00, 71.49, 70.00, -1.9661, 1148, 550015, 551002, 551117, 551118),   // turret platform
            new(2, -258.93, 74.21, -17.30, 0.2485, 1148, 550015, 551002, 551119, 551120),   // turret platform
            // 4 Landing Zone
            new(4, 159.20, 168.96, -28.21, 2.1629, 1220, 550018, 551001, 551121, 551122),   // corner platform
            new(4, 236.86, 168.94, -28.14, -2.1554, 1220, 550018, 551001, 551123, 551124),   // corner platform
            new(4, 145.74, 172.40, -79.10, 1.1217, 1220, 550018, 551001, 551125, 551126),   // fence corner
            new(4, 254.34, 172.40, -75.23, -1.2137, 1220, 550018, 551001, 551127, 551128),   // fence corner
            new(4, 144.70, 172.60, -127.19, 0.6269, 1220, 550018, 551001, 551129, 551130),   // fence corner
            new(4, 254.74, 172.40, -110.69, -0.7895, 1220, 550018, 551001, 551131, 551132),   // fence corner
            // 6 Fort Dew
            new(6, -222.81, 177.94, -758.81, 0.7963, 1244, 550016, 551003, 551133, 551134),   // corner platform
            new(6, -222.80, 177.96, -681.21, 2.3506, 1244, 550016, 551003, 551135, 551136),   // corner platform
            new(6, -97.21, 177.96, -758.80, -1.1497, 1244, 550016, 551003, 551137, 551138),   // corner platform
            new(6, -97.14, 177.94, -681.14, -1.9963, 1244, 550016, 551003, 551139, 551140),   // corner platform
            new(6, -170.80, 177.96, -661.21, -2.9318, 1244, 550016, 551003, 551141, 551142),   // corner platform
            new(6, -125.14, 177.94, -661.14, -2.3634, 1244, 550016, 551003, 551143, 551144),   // corner platform
            new(6, -170.43, 177.94, -778.43, -0.2186, 1244, 550016, 551003, 551145, 551146),   // corner platform
            new(6, -125.40, 177.94, -778.61, -0.7816, 1244, 550016, 551003, 551147, 551148),   // corner platform
            // 7 Retread Outpost
            new(7, 135.47, 867.55, 375.77, 2.1559, 1304, 550027, 551005, 551149, 551150),   // turret platform
            // 10 Research Point
            new(10, -327.85, 223.82, -241.50, 3.0473, 1454, 550023, 551006, 551151, 551152),   // fence corner
            new(10, -288.25, 223.81, -241.50, -2.7101, 1454, 550023, 551006, 551153, 551154),   // fence corner
            new(10, -370.80, 221.96, -269.21, 2.2913, 1454, 550023, 551006, 551155, 551156),   // corner platform
            new(10, -249.50, 223.81, -284.25, -1.9508, 1454, 550023, 551006, 551157, 551158),   // fence corner
            new(10, -336.00, 218.56, -232.00, 2.9593, 1454, 550023, 551006, 551159, 551160),   // turret platform
            new(10, -281.00, 217.56, -232.00, -2.6812, 1454, 550023, 551006, 551161, 551162),   // turret platform
            // 12 Northeast AFS
            new(12, 75.85, 378.00, 394.28, 1.0268, 1497, 550024, 551004, 551163, 551164),   // fence corner
            new(12, 75.86, 378.00, 441.87, 2.1121, 1497, 550024, 551004, 551165, 551166),   // fence corner
            new(12, 126.00, 378.09, 383.64, -0.3004, 1497, 550024, 551004, 551167, 551168),   // fence corner
            new(12, 126.00, 377.82, 452.50, -2.8399, 1497, 550024, 551004, 551169, 551170),   // fence corner
            // 13 Northwest AFS
            new(13, -408.85, 383.82, 469.50, 2.5834, 1497, 550024, 551004, 551171, 551172),   // fence corner
            new(13, -361.00, 383.82, 469.50, -2.5911, 1497, 550024, 551004, 551173, 551174),   // fence corner
            new(13, -349.11, 384.00, 356.87, -0.4485, 1497, 550024, 551004, 551175, 551176),   // fence corner
            new(13, -420.75, 384.00, 356.87, 0.4532, 1497, 550024, 551004, 551177, 551178),   // fence corner
            new(13, -435.80, 381.93, 421.79, 1.3951, 1497, 550024, 551004, 551179, 551180),   // corner platform
            new(13, -334.14, 381.91, 421.86, -1.3947, 1497, 550024, 551004, 551181, 551182),   // corner platform
            // 17 White Oasis Post
            new(17, -438.50, 289.82, 142.14, -1.2738, 1734, 550001, 551010, 551183, 551184),   // fence corner
            new(17, -438.50, 289.82, 190.00, -1.9105, 1734, 550001, 551010, 551185, 551186),   // fence corner
            new(17, -469.00, 289.82, 220.50, -2.4984, 1734, 550001, 551010, 551187, 551188),   // fence corner
            new(17, -492.60, 289.82, 220.50, -2.8236, 1734, 550001, 551010, 551189, 551190),   // fence corner
            new(17, -492.61, 290.10, 111.65, -0.3371, 1734, 550001, 551010, 551191, 551192),   // fence corner
            new(17, -469.16, 290.00, 111.84, -0.6737, 1734, 550001, 551010, 551193, 551194),   // fence corner
            new(17, -535.14, 290.00, 177.88, 2.0831, 1734, 550001, 551010, 551195, 551196),   // fence corner
            new(17, -535.13, 290.00, 154.24, 1.1728, 1734, 550001, 551010, 551197, 551198),   // fence corner
            // 19 Iapyx
            new(19, 634.13, 268.30, -311.83, -2.6276, 1759, 550009, 551008, 551199, 551200),   // fence corner
            new(19, 518.75, 268.00, -311.33, 2.7034, 1759, 550009, 551008, 551201, 551202),   // fence corner
            new(19, 534.00, 263.06, -246.50, 2.9341, 1759, 550009, 551008, 551203, 551204),   // turret platform
            new(19, 618.00, 263.06, -246.50, -2.8828, 1759, 550009, 551008, 551205, 551206),   // turret platform
            // 21 Ortho
            new(21, 333.84, 268.00, 79.84, 2.2510, 1761, 550008, 551009, 551207, 551208),   // fence corner
            new(21, 428.40, 267.82, 110.50, -2.6990, 1761, 550008, 551009, 551209, 551210),   // fence corner
            new(21, 452.00, 267.82, 110.50, -2.4801, 1761, 550008, 551009, 551211, 551212),   // fence corner
            new(21, 462.50, 267.82, 60.39, -1.9394, 1761, 550008, 551009, 551213, 551214),   // fence corner
            new(21, 423.88, 268.00, -26.14, -0.4982, 1761, 550008, 551009, 551215, 551216),   // fence corner
            new(21, 393.18, 265.94, -30.81, -0.0233, 1761, 550008, 551009, 551217, 551218),   // corner platform
            new(21, 333.18, 265.94, -18.81, 0.8457, 1761, 550008, 551009, 551219, 551220),   // corner platform
            new(21, 301.64, 268.09, 8.15, 1.3012, 1761, 550008, 551009, 551221, 551222),   // fence corner
            new(21, 301.65, 268.10, 44.00, 1.6920, 1761, 550008, 551009, 551223, 551224),   // fence corner
            // 22 Geyser Chimney Basin
            new(22, 146.86, 440.00, 229.27, 1.0627, 1764, 550011, 551007, 551225, 551226),   // fence corner
            new(22, 215.50, 439.82, 229.14, -1.0268, 1764, 550011, 551007, 551227, 551228),   // fence corner
            new(22, 219.86, 437.94, 291.86, -2.4232, 1764, 550011, 551007, 551229, 551230),   // corner platform
            new(22, 142.20, 437.96, 291.79, 2.3850, 1764, 550011, 551007, 551231, 551232),   // corner platform
            new(22, 152.62, 434.56, 209.34, 0.6441, 1764, 550011, 551007, 551233, 551234),   // turret platform
            new(22, 182.95, 433.70, 207.55, -0.0112, 1764, 550011, 551007, 551235, 551236),   // turret platform
            // 23 Lightning Fields
            new(23, -89.14, 435.94, 563.86, -2.3624, 1764, 550011, 551007, 551237, 551238),   // corner platform
            new(23, -89.21, 435.96, 470.20, -0.7796, 1764, 550011, 551007, 551239, 551240),   // corner platform
            new(23, -174.80, 435.96, 563.79, 2.4426, 1764, 550011, 551007, 551241, 551242),   // corner platform
            new(23, -174.81, 435.94, 470.19, 0.6994, 1764, 550011, 551007, 551243, 551244),   // corner platform
            new(23, -184.00, 438.86, 388.00, 0.3600, 1764, 550011, 551007, 551245, 551246),   // turret platform
            new(23, -210.92, 445.59, 594.85, 2.3719, 1764, 550011, 551007, 551247, 551248),   // turret platform
            // 28 Outpost Aurora
            new(28, 658.20, 389.96, -127.21, 2.3783, 1911, 550004, 551011, 551249, 551250),   // corner platform
            new(28, 730.20, 389.96, -127.21, 3.0765, 1911, 550004, 551011, 551251, 551252),   // corner platform
            new(28, 847.86, 389.94, -127.14, -2.1943, 1911, 550004, 551011, 551253, 551254),   // corner platform
            new(28, 847.79, 389.96, -252.80, -1.1911, 1911, 550004, 551011, 551255, 551256),   // corner platform
            new(28, 686.19, 389.94, -240.81, 0.9829, 1911, 550004, 551011, 551257, 551258),   // corner platform
            new(28, 785.20, 392.00, -248.20, -0.8907, 1911, 550004, 551011, 551259, 551260),   // fence corner
            new(28, 831.00, 422.56, -276.00, -0.9520, 1911, 550004, 551011, 551261, 551262),   // turret platform
            // 31 Prometheus Outpost
            new(31, 383.55, 177.40, 338.11, 0.3216, 1993, 550002, 551013, 551263, 551264),   // fence corner
            new(31, 416.27, 177.40, 339.56, -0.3219, 1993, 550002, 551013, 551265, 551266),   // fence corner
            new(31, 353.82, 176.00, 359.90, 1.0268, 1993, 550002, 551013, 551267, 551268),   // fence corner
            new(31, 446.50, 175.82, 360.14, -1.0310, 1993, 550002, 551013, 551269, 551270),   // fence corner
            new(31, 352.93, 177.17, 377.36, 1.3508, 1993, 550002, 551013, 551271, 551272),   // fence corner
            new(31, 448.76, 176.00, 370.83, -1.2328, 1993, 550002, 551013, 551273, 551274),   // fence corner
            // 33 Charon's Crossing
            new(33, -85.16, 542.00, -178.21, 0.9576, 2028, 550007, 551012, 551275, 551276),   // fence corner
            new(33, -73.85, 541.82, -89.50, 2.5513, 2028, 550007, 551012, 551277, 551278),   // fence corner
            new(33, 8.50, 541.82, -171.86, -1.0282, 2028, 550007, 551012, 551279, 551280),   // fence corner
            new(33, -2.00, 541.82, -89.50, -2.5728, 2028, 550007, 551012, 551281, 551282),   // fence corner
            // 40 Dead Zone Power Station
            new(40, -553.80, 206.96, -294.21, 2.3306, 2051, 550019, 551014, 551283, 551284),   // corner platform
            new(40, -448.14, 206.94, -294.14, -2.7213, 2051, 550019, 551014, 551285, 551286),   // corner platform
            new(40, -400.14, 206.94, -322.14, -2.0618, 2051, 550019, 551014, 551287, 551288),   // corner platform
            new(40, -400.21, 206.96, -467.80, -0.6563, 2051, 550019, 551014, 551289, 551290),   // corner platform
            new(40, -538.60, 208.90, -459.18, 0.5576, 2051, 550019, 551014, 551291, 551292),   // fence corner
        };

        public static void Up(MigrationBuilder migration)
        {
            foreach (var zone in Zones)
            {
                migration.Sql(
                    "insert into creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"values ({zone.Action}, 'Bane Turret {zone.Name} weapon 411/1', 411, 1, 0.0, 60.0, 1000, 0, {zone.MinDamage}, {zone.MaxDamage}, 1);");
                migration.Sql(
                    "insert into creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"values ({zone.BaneTurret}, 'Bane Turret - {zone.Name}', {BaneTurretClass}, 0, {zone.Level}, {zone.Health}, 0, 0, 0, {zone.Action}, 0, 0, 0, 0, 0, 0, 0);");
                migration.Sql(
                    "insert into creature_stat (id, body, mind, spirit, health, armor) " +
                    $"values ({zone.BaneTurret}, {zone.Body}, {zone.Mind}, {zone.Spirit}, {zone.StatHealth}, {zone.Armor});");
                migration.Sql($"insert into creature_appearance (id, slot_id, Class_id, color) values ({zone.BaneTurret}, 13, {BaneMortarLauncher}, 1);");
            }

            foreach (var spot in Spots)
            {
                Pool(migration, spot.AfsPool, 0, spot, spot.AfsTurret);
                Pool(migration, spot.BanePool, 1, spot, spot.BaneTurret);
                migration.Sql($"insert into control_point_link (control_point_id, kind, object_id) values ({spot.Point}, 2, {spot.AfsPool});");
                migration.Sql($"insert into control_point_link (control_point_id, kind, object_id) values ({spot.Point}, 1, {spot.BanePool});");
            }
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"delete from control_point_link where kind in (1, 2) and object_id between {PoolIdMin} and {PoolIdMax};");
            migration.Sql($"delete from spawnpool where id between {PoolIdMin} and {PoolIdMax};");

            foreach (var table in new[] { "creature_appearance", "creature_stat", "creature" })
                migration.Sql($"delete from {table} where id between {CreatureIdMin} and {CreatureIdMax};");

            migration.Sql($"delete from creature_action where id between {ActionIdMin} and {ActionIdMax};");
        }

        private static void Pool(MigrationBuilder migration, uint id, int mode, Spot spot, uint creature) =>
            migration.Sql(
                "insert into spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                $"values ({id}, {mode}, 0, {Respawn}, {F(spot.X)}, {F(spot.Y)}, {F(spot.Z)}, {F(spot.Rotation)}, {spot.Map}, {creature}, 1, 1, " +
                "0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.0);");

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
