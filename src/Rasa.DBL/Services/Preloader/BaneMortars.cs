using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// A Bane Mortar on every mortar base the maps leave empty (Bane_mortars).
    ///
    /// The client's maps put down 248 ArchBaneGenObjMortarlauncherBaseV01 (7478): the round pad a
    /// Bane mortar stands on, scenery with nothing on it. The mortar itself was a creature the
    /// server spawned there - Emplacement_Bane_Turret_Standard (7482) with
    /// Weapon_Creature_Bane_Mortar_Launcher (10604) - and that placement did not survive. Only the
    /// four reconstructed Wilderness mortars (630076..630079) stood on theirs. This puts one on
    /// each of the rest that has a zone: 237 on 29 maps, the open zones and their instances.
    /// Left without: the two Manhattan maps' four, which have no Bane or anything else spawning
    /// and so no zone to take a level from, and the epic Caves of Donn's two, likewise empty; and
    /// the one at Fault Lever Entrance, 13 m from its hospital (see below).
    ///
    /// A mortar is a lieutenant of its zone by the open-country rules, as the Bane Mortar
    /// (WreckCreatures, 590001) is at level 42 and which this matches there to the point: level
    /// the zone band's top less 2, health 1.75x a same-level player's base health
    /// (10 000 x 2^((level - 42) / 8)), armour 10 x level - 20, and a shot 8% of the base +-20%,
    /// Weapon_Creature_Bane_Mortar_Launcher's pair 411/1 (WEAPON_GROUNDTARGET), Physical, 0 to 60
    /// m, once a second, as the Bane Mortar and the control points' Bane turrets fire. One row a
    /// zone, with the launcher in its weapon slot (13) so the client has the gun the shell comes
    /// from; no name id, so the client names it "Bane Mortar" from its class.
    ///
    /// Each stands on its pad: on the pad's top (its bounds, 0.507 m up, along the pad's own up
    /// where the map tips it), facing as the pad does (the map's quaternion, w, x, y, z, as
    /// TurretFacings reads it), in a pool of one with no radius, respawning as the emplacements
    /// do (ControlPointTurrets, 3000 s).
    ///
    /// A pad on a control point's ground is in that point's Bane garrison: a control point's pool
    /// (mode 1) linked to the point (kind 1), so its mortar is there while the Bane hold the point
    /// and has to fall with the rest of the garrison, and is gone while the AFS do. A pad is on a
    /// point's ground within 130 m of it, the nearest, as the garrisons were linked
    /// (ControlPointSeed) - 83 - or within <see cref="FacilityReach"/> of what the point keeps
    /// apart from its marker, its hospital, waypoint or the NPCs of its post, named
    /// "&lt;point&gt; (Control Point)" - 11, by Eastern Rim, the Drill Resonator and Cuthah Scout
    /// Post. The rest are automatic (mode 0); but a mortar notices a player as any creature does,
    /// at 18 m (Creature.AggroRange), so one is not put where a player arrives or comes back to
    /// life within <see cref="SpawnPointReach"/> of it - a hospital or waypoint - or within
    /// <see cref="FriendlyReach"/> of a friendly NPC's pool, which it would never stop fighting:
    /// Fault Lever Entrance's, 13 m from both.
    ///
    /// The Wilderness requires an ordinary loot profile for every hostile creature in a spawnpool or
    /// a garrison on its map (loot_map_policy, map 1220; OrdinaryLootCoverage), and the Game server
    /// does not start while one is missing: its mortar takes the Bane mortars' profile there,
    /// wilderness_v1_mortar_5_9, as the Wilderness Bane turret does (ControlPointTurretLoot), and
    /// guarded as that is. No other map with a mortar requires one.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Down takes out every row by its range.
    /// </summary>
    public static class BaneMortars
    {
        public const uint CreatureIdMin = 592001;
        public const uint CreatureIdMax = 592099;
        public const uint PoolIdMin = 592101;
        public const uint PoolIdMax = 592999;
        public const uint ActionIdMin = 75201;
        public const uint ActionIdMax = 75299;

        public const uint PadClass = 7478;                                       // ArchBaneGenObjMortarlauncherBaseV01
        public const uint MortarClass = ControlPointTurrets.BaneTurretClass;     // Emplacement_Bane_Turret_Standard
        public const uint Launcher = ControlPointTurrets.BaneMortarLauncher;     // Weapon_Creature_Bane_Mortar_Launcher
        public const uint WeaponSlot = 13;
        public const uint Respawn = ControlPointTurrets.Respawn;
        public const double ControlPointReach = 130;
        public const double FacilityReach = 80;
        public const double SpawnPointReach = 30;
        public const double FriendlyReach = 25;
        public const uint Stat = 15;

        /// <summary>The map whose loot policy requires a profile, and the profile its mortar takes there.</summary>
        public const uint LootMap = ControlPointTurretLoot.Map;
        public const string LootProfile = ControlPointTurretLoot.Profile;
        public const string LootZone = "Wilderness";

        /// <summary>A zone's mortar: its creature and attack rows, level, health, armour and damage.</summary>
        public sealed record Zone(string Name, uint Creature, uint Action, uint Level, uint Health, uint Armor, uint MinDamage, uint MaxDamage);

        /// <summary>One pad: its pool, map, where the mortar stands and faces, its zone, the control point whose garrison it is in (0 none), and the pad's .map entity.</summary>
        public sealed record Mortar(uint Pool, uint Map, double X, double Y, double Z, double Rotation, string Zone, uint ControlPoint, ulong Pad);

        public static readonly Zone[] Zones =
        {
            new("Wilderness", 592001, 75201, 13, 1418, 110, 52, 78),
            new("Divide", 592002, 75202, 18, 2188, 160, 80, 120),
            new("Palisades", 592003, 75203, 26, 4375, 240, 160, 240),
            new("Plateau", 592004, 75204, 30, 6187, 280, 226, 339),
            new("Pools", 592005, 75205, 33, 8024, 310, 293, 440),
            new("Marshes", 592006, 75206, 36, 10406, 340, 381, 571),
            new("Descent", 592007, 75207, 38, 12374, 360, 453, 679),
            new("Plains", 592008, 75208, 38, 12374, 360, 453, 679),
            new("Mires", 592009, 75209, 39, 13494, 370, 494, 740),
            new("Incline", 592010, 75210, 40, 14716, 380, 538, 807),
            new("Ashen Desert", 592011, 75211, 42, 17500, 400, 640, 960),
            new("Thunderhead", 592012, 75212, 43, 19084, 410, 698, 1047),
            new("Abyss", 592013, 75213, 44, 20811, 420, 761, 1142),
            new("Crucible", 592014, 75214, 45, 22695, 430, 830, 1245),
            new("Howling Maw", 592015, 75215, 46, 24749, 440, 905, 1358),
        };

        public static readonly Mortar[] Mortars =
        {
            // adv_foreas_concordia_divide (1148)
            new(592101, 1148, -864.748, 134.951, -814.233, 0.0000, "Divide", 0, 132770324753503),
            new(592102, 1148, -795.561, 139.253, -887.299, 0.0000, "Divide", 0, 132770324753504),
            new(592103, 1148, -751.730, 131.127, -639.086, 0.0000, "Divide", 1, 132770324754927),   // Purgas
            new(592104, 1148, -743.107, 126.882, -603.366, 0.0000, "Divide", 1, 132770324754931),   // Purgas
            new(592105, 1148, -595.391, 122.776, -597.308, 0.0000, "Divide", 1, 132770324753501),   // Purgas
            new(592106, 1148, -589.315, 184.240, -1091.641, 0.0000, "Divide", 0, 132770324753505),
            new(592107, 1148, -583.799, 123.151, -628.145, 0.0000, "Divide", 1, 132770324753502),   // Purgas
            new(592108, 1148, -560.029, 184.240, -1089.745, 0.0000, "Divide", 0, 132770324753506),
            new(592109, 1148, -189.016, 116.973, -701.592, 0.0000, "Divide", 0, 132770324753508),
            new(592110, 1148, -175.804, 116.973, -679.622, 0.0000, "Divide", 0, 132770324753507),
            // adv_foreas_concordia_wilderness (1220)
            new(592111, 1220, -367.469, 179.251, -481.126, 0.0000, "Wilderness", 3, 133079561960590),   // Imperial Valley
            new(592112, 1220, -321.786, 179.251, -495.982, 0.0000, "Wilderness", 3, 133079561960589),   // Imperial Valley
            // adv_foreas_concordia_palisades (1244)
            new(592113, 1244, -172.650, 139.994, -223.956, 0.0000, "Palisades", 0, 133182640962677),
            new(592114, 1244, 183.061, 109.397, 381.333, -1.5708, "Palisades", 5, 133182640964331),   // River-base Krimm
            new(592115, 1244, 203.246, 109.397, 392.802, -1.5708, "Palisades", 5, 133182640964330),   // River-base Krimm
            new(592116, 1244, 238.601, 109.397, 271.888, -1.5708, "Palisades", 5, 133182640964326),   // River-base Krimm
            new(592117, 1244, 258.786, 109.397, 283.357, -1.5708, "Palisades", 5, 133182640964327),   // River-base Krimm
            new(592118, 1244, 562.129, 131.554, -584.670, -0.7854, "Palisades", 0, 133182640962675),
            new(592119, 1244, 655.328, 131.559, -561.618, -0.7854, "Palisades", 0, 133182640962676),
            // adv_foreas_valverde_pools (1304)
            new(592120, 1304, -880.962, 796.787, -4.640, 0.0000, "Pools", 0, 133436044043851),
            new(592121, 1304, -866.383, 796.379, -92.223, 0.0000, "Pools", 0, 133436044043850),
            new(592122, 1304, -861.409, 796.788, -34.905, 0.0000, "Pools", 0, 133436044043852),
            new(592123, 1304, -779.782, 796.379, 9.069, 0.0000, "Pools", 0, 133436044043849),
            new(592124, 1304, -468.949, 685.413, -351.205, -1.8850, "Pools", 8, 133436044038388),   // Bane Comm Center
            new(592125, 1304, -425.450, 685.417, -386.654, -1.0996, "Pools", 8, 133436044038386),   // Bane Comm Center
            new(592126, 1304, -207.028, 705.593, -280.997, 0.0000, "Pools", 0, 133436044043847),
            new(592127, 1304, -206.045, 701.593, -294.966, 0.0000, "Pools", 0, 133436044043848),
            new(592128, 1304, 163.613, 866.469, 368.127, 0.4268, "Pools", 7, 133436044038209),   // Retread Outpost
            new(592129, 1304, 526.715, 672.968, -97.210, 0.0000, "Pools", 9, 133436044038227),   // Bane Guard Station
            new(592130, 1304, 529.064, 675.594, -140.047, -1.5708, "Pools", 9, 133436044038228),   // Bane Guard Station
            new(592131, 1304, 612.249, 675.594, -51.992, -1.5708, "Pools", 9, 133436044038229),   // Bane Guard Station
            new(592132, 1304, 632.577, 677.101, -99.108, -1.5708, "Pools", 9, 133436044038237),   // Bane Guard Station
            // adv_foreas_concordia_divide_torcastraprison (1349)
            new(592133, 1349, -148.216, 99.700, -238.568, 0.0000, "Divide", 0, 133629317512081),
            new(592134, 1349, -104.051, 55.899, -67.773, 0.0000, "Divide", 0, 133629317512079),
            new(592135, 1349, -27.746, 56.276, -68.938, 0.0000, "Divide", 0, 133629317512078),
            new(592136, 1349, -3.353, 99.700, -268.253, 0.0000, "Divide", 0, 133629317512080),
            // adv_foreas_concordia_palisades_devilsden (1394)
            new(592137, 1394, 218.055, 155.594, 586.036, 0.0000, "Palisades", 0, 133822591019507),
            new(592138, 1394, 250.019, 155.597, 585.818, 0.0000, "Palisades", 0, 133822591019506),
            // adv_foreas_concordia_palisades_treebackcamp (1397)
            new(592139, 1397, -61.455, 160.703, 304.930, 0.0000, "Palisades", 0, 133835475920614),
            // adv_foreas_concordia_wilderness_pravusresearch (1430)
            new(592140, 1430, 65.859, 58.396, 95.819, 0.0000, "Wilderness", 0, 133981504855147),
            new(592141, 1430, 67.011, 55.396, -109.238, 0.0000, "Wilderness", 0, 133981504855148),
            new(592142, 1430, 109.030, 48.019, 179.410, 0.0000, "Wilderness", 0, 133981504855146),
            new(592143, 1430, 123.145, 43.911, 289.037, 0.0000, "Wilderness", 0, 133981504855145),
            // adv_foreas_valverde_marshes_banesupplydepot (1451)
            new(592144, 1451, -102.506, 131.414, 288.758, 0.0000, "Marshes", 0, 134071699115175),
            new(592145, 1451, -65.028, 131.320, 43.011, 0.0000, "Marshes", 0, 134071699115173),
            new(592146, 1451, -64.974, 131.320, 27.108, 0.0000, "Marshes", 0, 134071699115174),
            new(592147, 1451, -27.597, 131.564, 101.181, 0.0000, "Marshes", 0, 134071699115180),
            new(592148, 1451, -26.231, 131.414, -60.072, 0.0000, "Marshes", 0, 134071699115181),
            new(592149, 1451, -18.926, 131.695, 258.637, 0.0000, "Marshes", 0, 134071699115176),
            new(592150, 1451, 38.618, 131.695, -190.146, 0.0000, "Marshes", 0, 134071699115182),
            new(592151, 1451, 65.841, 131.789, -122.059, 0.0000, "Marshes", 0, 134071699115185),
            new(592152, 1451, 71.670, 131.508, 40.668, 0.0000, "Marshes", 0, 134071699115178),
            new(592153, 1451, 77.809, 131.789, -225.026, 0.0000, "Marshes", 0, 134071699115184),
            new(592154, 1451, 97.820, 141.635, -25.469, 0.0000, "Marshes", 0, 134071699115186),
            new(592155, 1451, 103.367, 129.695, 207.905, 0.0000, "Marshes", 0, 134071699115177),
            new(592156, 1451, 212.688, 131.789, -223.169, 0.0000, "Marshes", 0, 134071699115183),
            // adv_foreas_valverde_marshes (1454)
            new(592157, 1454, 4.811, 228.468, -364.330, 0.0000, "Marshes", 11, 134084584060102),   // Bane Assertion Camp
            new(592158, 1454, 33.941, 228.466, -351.030, 0.0000, "Marshes", 11, 134084584060103),   // Bane Assertion Camp
            new(592159, 1454, 36.486, 229.285, -649.509, 0.0000, "Marshes", 0, 134084584058336),
            new(592160, 1454, 91.253, 226.303, -290.683, 0.0000, "Marshes", 11, 134084584060200),   // Bane Assertion Camp
            new(592161, 1454, 111.398, 218.811, -350.885, 0.0000, "Marshes", 11, 134084584060100),   // Bane Assertion Camp
            new(592162, 1454, 116.018, 229.285, -628.465, 0.0000, "Marshes", 0, 134084584058339),
            new(592163, 1454, 136.262, 218.811, -353.219, 0.0000, "Marshes", 11, 134084584060101),   // Bane Assertion Camp
            new(592164, 1454, 176.586, 229.285, -554.060, 0.0000, "Marshes", 0, 134084584058340),
            new(592165, 1454, 247.697, 229.285, -467.506, 0.0000, "Marshes", 0, 134084584058343),
            new(592166, 1454, 294.206, 229.285, -411.350, 0.0000, "Marshes", 0, 134084584058344),
            new(592167, 1454, 377.255, 223.448, -269.534, 0.0000, "Marshes", 0, 134084584058534),
            new(592168, 1454, 409.053, 223.623, -193.944, 0.0000, "Marshes", 0, 134084584058532),
            new(592169, 1454, 434.858, 223.623, -121.008, 0.0000, "Marshes", 0, 134084584058543),
            new(592170, 1454, 446.902, 223.478, -50.398, 0.0000, "Marshes", 0, 134084584058530),
            new(592171, 1454, 462.255, 222.862, 78.515, 0.0000, "Marshes", 0, 134084584058527),
            // adv_foreas_valverde_plateau (1497)
            new(592172, 1497, -329.389, 380.838, -60.053, -3.1416, "Plateau", 15, 134269267733145),   // Southwest Bane
            new(592173, 1497, -329.320, 380.838, -148.011, -3.1416, "Plateau", 15, 134269267733152),   // Southwest Bane
            new(592174, 1497, -287.862, 380.838, -83.994, -3.1416, "Plateau", 15, 134269267733142),   // Southwest Bane
            new(592175, 1497, -287.766, 380.838, -123.965, -3.1416, "Plateau", 15, 134269267733140),   // Southwest Bane
            new(592176, 1497, -200.105, 442.813, -284.065, -2.5070, "Plateau", 0, 134269267664767),
            new(592177, 1497, -97.496, 427.284, -310.910, 2.3562, "Plateau", 0, 134269267733207),
            new(592178, 1497, 47.766, 376.805, -112.036, 0.0000, "Plateau", 14, 134269267733116),   // Southeast Bane
            new(592179, 1497, 47.862, 376.805, -152.006, 0.0000, "Plateau", 14, 134269267733115),   // Southeast Bane
            new(592180, 1497, 53.516, 442.546, -306.313, 2.7209, "Plateau", 0, 134269267664765),
            new(592181, 1497, 89.320, 376.805, -87.989, 0.0000, "Plateau", 14, 134269267733117),   // Southeast Bane
            new(592182, 1497, 89.389, 376.805, -175.947, 0.0000, "Plateau", 14, 134269267733118),   // Southeast Bane
            // adv_foreas_valverde_plateau_ustoryard (1502)
            new(592183, 1502, 106.198, 183.518, 47.465, 0.0000, "Plateau", 0, 134290742448608),
            new(592184, 1502, 225.014, 183.521, 95.145, 0.0000, "Plateau", 0, 134290742447246),
            // adv_foreas_concordia_wilderness_cavesofdonn02 (1506)
            new(592185, 1506, -150.309, 27.834, -239.751, 0.0000, "Wilderness", 0, 134307922318807),
            new(592186, 1506, -9.512, 21.698, -234.748, 0.0000, "Wilderness", 0, 134307922318808),
            new(592187, 1506, 30.349, 30.555, -309.971, 0.0000, "Wilderness", 0, 134307922320692),
            // adv_foreas_concordia_wilderness_clrf (1721)
            new(592188, 1721, -18.183, 102.828, -161.090, 0.0000, "Wilderness", 0, 134419591468367),
            new(592189, 1721, 66.849, 99.418, -64.638, 0.0000, "Wilderness", 0, 134419591468374),
            new(592190, 1721, 67.616, 127.288, -136.783, 0.0000, "Wilderness", 0, 134419591468372),
            // adv_arieki_ligo_ashendesert (1734)
            new(592191, 1734, -210.888, 315.288, -409.662, 0.0000, "Ashen Desert", 0, 134419591466857),
            new(592192, 1734, -195.561, 317.335, -337.739, 0.0000, "Ashen Desert", 0, 134419591466856),
            new(592193, 1734, -180.580, 310.170, -282.694, 0.0000, "Ashen Desert", 0, 134419591466858),
            new(592194, 1734, -140.300, 310.429, -285.300, 1.5707, "Ashen Desert", 0, 134419591466859),
            new(592195, 1734, 230.000, 291.716, 158.000, 0.0000, "Ashen Desert", 0, 134419591471716),
            new(592196, 1734, 346.000, 281.593, 28.000, 0.0000, "Ashen Desert", 16, 134419591471759),   // Bane Conscription Garrison
            new(592197, 1734, 399.980, 277.594, -15.850, 0.0000, "Ashen Desert", 16, 134419591472998),   // Bane Conscription Garrison
            new(592198, 1734, 399.997, 277.595, -99.033, 0.0000, "Ashen Desert", 16, 134419591472995),   // Bane Conscription Garrison
            new(592199, 1734, 440.340, 288.134, -200.983, 0.0000, "Ashen Desert", 0, 134419591467466),
            new(592200, 1734, 455.982, 277.595, -99.008, 0.0000, "Ashen Desert", 16, 134419591472996),   // Bane Conscription Garrison
            new(592201, 1734, 456.075, 277.597, -15.718, 0.0000, "Ashen Desert", 16, 134419591472997),   // Bane Conscription Garrison
            new(592202, 1734, 490.114, 281.215, -194.194, 0.0000, "Ashen Desert", 0, 134419591467465),
            new(592203, 1734, 524.666, 283.705, -170.568, 0.0000, "Ashen Desert", 0, 134419591467467),
            new(592204, 1734, 550.574, 287.546, -130.494, 0.0000, "Ashen Desert", 0, 134419591467468),
            // adv_foreas_valverde_marshes_villageruins (1743)
            new(592205, 1743, -27.206, 68.574, 207.855, 0.0000, "Marshes", 0, 134419591469152),
            new(592206, 1743, 73.800, 68.550, 207.985, 0.0000, "Marshes", 0, 134419591469151),
            // adv_arieki_torden_incline (1761)
            new(592207, 1761, -515.424, 249.369, -131.141, 0.0000, "Incline", 0, 134419591483401),
            new(592208, 1761, -464.013, 254.432, -61.024, 0.0000, "Incline", 0, 134419591484438),
            new(592209, 1761, -382.800, 236.112, -147.250, 0.0000, "Incline", 0, 134419591482897),
            new(592210, 1761, -364.723, 232.815, -151.970, 0.0000, "Incline", 0, 134419591482739),
            new(592211, 1761, -337.270, 252.930, -66.672, 0.0000, "Incline", 0, 134419591482757),
            new(592212, 1761, -335.777, 243.173, -96.371, 0.0000, "Incline", 0, 134419591482738),
            new(592213, 1761, -331.917, 227.650, -135.605, 0.0000, "Incline", 0, 134419591482949),
            new(592214, 1761, -327.335, 236.162, -107.730, 0.0000, "Incline", 0, 134419591482892),
            new(592215, 1761, -314.427, 230.096, -117.771, 0.0000, "Incline", 0, 134419591482916),
            new(592216, 1761, -313.269, 246.484, -78.382, 0.0000, "Incline", 0, 134419591482735),
            new(592217, 1761, -303.287, 241.109, -82.235, 0.0000, "Incline", 0, 134419591482736),
            new(592218, 1761, -295.267, 235.638, -90.380, 0.0000, "Incline", 0, 134419591482907),
            new(592219, 1761, 156.748, 290.359, 364.241, 0.0000, "Incline", 0, 134419591484437),
            new(592220, 1761, 212.183, 285.593, 327.890, 0.0000, "Incline", 0, 134419591484436),
            new(592221, 1761, 299.973, 285.594, 370.615, 1.4923, "Incline", 20, 134419591484941),   // Badlands
            new(592222, 1761, 324.065, 285.594, 329.092, 0.5498, "Incline", 20, 134419591484940),   // Badlands
            new(592223, 1761, 339.003, 246.522, 626.021, 0.0000, "Incline", 0, 134419591484655),
            new(592224, 1761, 339.069, 246.522, 654.226, 0.0000, "Incline", 0, 134419591484648),
            new(592225, 1761, 353.241, 246.522, 601.404, 0.0000, "Incline", 0, 134419591484659),
            new(592226, 1761, 430.779, 246.522, 601.239, 0.0000, "Incline", 0, 134419591484657),
            new(592227, 1761, 444.955, 246.522, 654.198, 0.0000, "Incline", 0, 134419591484650),
            new(592228, 1761, 445.045, 246.522, 625.922, 0.0000, "Incline", 0, 134419591484653),
            new(592229, 1761, 460.102, 285.595, 329.079, -0.5498, "Incline", 20, 134419591484938),   // Badlands
            new(592230, 1761, 484.030, 285.594, 370.647, -1.5708, "Incline", 20, 134419591484939),   // Badlands
            // adv_arieki_torden_plains (1764)
            new(592231, 1764, -996.661, 430.362, -235.573, -3.1416, "Plains", 0, 134419591468723),
            new(592232, 1764, -883.809, 445.498, -184.011, -0.3990, "Plains", 0, 134419591469667),
            new(592233, 1764, -867.283, 435.562, -293.144, -0.3990, "Plains", 0, 134419591469664),
            new(592234, 1764, -756.021, 455.994, 23.845, 0.0000, "Plains", 0, 134419591468941),
            new(592235, 1764, -744.978, 423.611, -275.871, -1.7279, "Plains", 0, 134419591471661),
            new(592236, 1764, -627.510, 461.620, 149.108, -1.2995, "Plains", 0, 134419591466213),
            new(592237, 1764, 134.255, 431.112, 577.949, 1.4750, "Plains", 0, 134419591465804),
            new(592238, 1764, 221.033, 431.112, 527.014, 0.1738, "Plains", 0, 134419591465802),
            new(592239, 1764, 266.149, 429.612, 530.756, 0.0000, "Plains", 0, 134419591465803),
            new(592240, 1764, 305.020, 431.362, 527.049, 0.0000, "Plains", 0, 134419591465801),
            new(592241, 1764, 693.839, 436.507, 565.391, 0.6283, "Plains", 24, 134419591468909),   // Irendas Support Facility
            new(592242, 1764, 694.000, 436.507, 613.000, 1.2566, "Plains", 24, 134419591468910),   // Irendas Support Facility
            new(592243, 1764, 873.000, 429.029, 550.000, -1.8850, "Plains", 24, 134419591472496),   // Irendas Support Facility
            new(592244, 1764, 873.000, 429.029, 579.000, -1.5171, "Plains", 24, 134419591472497),   // Irendas Support Facility
            // adv_foreas_concordia_divide_purgasstation2 (1806)
            new(592245, 1806, -223.532, 37.507, 68.008, -0.4712, "Divide", 0, 134419591470147),
            new(592246, 1806, -175.881, 34.115, -15.443, -0.7854, "Divide", 0, 134419591470146),
            new(592247, 1806, -161.858, 34.115, -96.275, 0.0000, "Divide", 0, 134419591470148),
            new(592248, 1806, -8.248, 10.115, -140.616, 0.0000, "Divide", 0, 134419591470150),
            new(592249, 1806, 79.206, 12.085, -58.327, 0.0000, "Divide", 0, 134419591470149),
            new(592250, 1806, 192.612, 72.407, 88.094, 0.0000, "Divide", 0, 134419591470145),
            new(592251, 1806, 272.731, 74.087, 197.536, 0.0000, "Divide", 0, 134419591470144),
            // adv_foreas_valverde_plateau_maligobasev3 (1830)
            new(592252, 1830, -190.557, -99.478, 113.039, -2.3562, "Plateau", 0, 134419591466945),
            new(592253, 1830, -73.492, -129.885, 39.880, 0.0000, "Plateau", 0, 134419591464580),
            // adv_arieki_ligo_thunderhead (1911)
            new(592254, 1911, -535.486, 385.968, 171.752, 0.0000, "Thunderhead", 26, 134419591478172),   // Western Rim
            new(592255, 1911, -511.000, 400.885, 386.000, 0.8694, "Thunderhead", 0, 134419591466932),
            new(592256, 1911, -483.810, 386.354, 171.944, 0.0000, "Thunderhead", 26, 134419591478173),   // Western Rim
            new(592257, 1911, -432.738, 382.723, 260.199, 0.0000, "Thunderhead", 26, 134419591472435),   // Western Rim
            new(592258, 1911, -430.084, 386.686, 221.702, 0.0000, "Thunderhead", 26, 134419591472434),   // Western Rim
            new(592259, 1911, -266.624, 339.840, 36.717, -0.8627, "Thunderhead", 0, 134419591464653),
            new(592260, 1911, -253.000, 396.987, 427.000, -1.6580, "Thunderhead", 0, 134419591464914),
            new(592261, 1911, -234.000, 339.340, 58.000, -0.7206, "Thunderhead", 0, 134419591464652),
            new(592262, 1911, -48.095, 335.093, -126.828, 0.0000, "Thunderhead", 29, 134419591474055),   // Fault Lever
            new(592263, 1911, -28.353, 329.200, -38.805, 0.0000, "Thunderhead", 29, 134419591468028),   // Fault Lever
            new(592264, 1911, -17.627, 378.099, 203.670, -3.1416, "Thunderhead", 0, 134419591474058),
            new(592265, 1911, 19.934, 378.099, 201.796, 1.9635, "Thunderhead", 0, 134419591474057),
            new(592266, 1911, 27.479, 328.661, -36.796, 0.0000, "Thunderhead", 29, 134419591468027),   // Fault Lever
            new(592267, 1911, 47.979, 335.044, -126.997, 0.0000, "Thunderhead", 29, 134419591474056),   // Fault Lever
            new(592268, 1911, 231.098, 314.584, -32.434, -0.4855, "Thunderhead", 0, 134419591464593),
            new(592269, 1911, 284.190, 313.834, -35.642, 0.4006, "Thunderhead", 0, 134419591464594),
            new(592270, 1911, 422.000, 387.607, 123.000, -1.6580, "Thunderhead", 0, 134419591464913),
            new(592271, 1911, 504.952, 437.594, 563.511, -2.3562, "Thunderhead", 0, 134419591469408),
            new(592272, 1911, 516.904, 437.594, 542.762, -2.3562, "Thunderhead", 0, 134419591469406),
            new(592273, 1911, 574.596, 353.343, 39.230, 0.8176, "Thunderhead", 27, 134419591463295),   // Eastern Rim (its facilities)
            new(592274, 1911, 596.221, 371.078, 166.504, 0.0000, "Thunderhead", 27, 134419591472532),   // Eastern Rim
            new(592275, 1911, 598.800, 371.349, 201.157, 0.0000, "Thunderhead", 27, 134419591472533),   // Eastern Rim
            new(592276, 1911, 630.000, 349.342, -16.000, 0.9633, "Thunderhead", 27, 134419591463296),   // Eastern Rim (its facilities)
            new(592277, 1911, 631.589, 371.089, 126.577, 0.0000, "Thunderhead", 27, 134419591472531),   // Eastern Rim
            new(592278, 1911, 682.945, 371.055, 126.262, 0.0000, "Thunderhead", 27, 134419591472530),   // Eastern Rim
            new(592279, 1911, 709.947, 365.404, 16.203, -0.0447, "Thunderhead", 0, 134419591463646),
            new(592280, 1911, 738.823, 367.982, 6.639, 0.0816, "Thunderhead", 0, 134419591463645),
            // adv_arieki_ligo_burningsteps (1993)
            new(592281, 1993, 722.992, 174.595, 504.194, 0.0000, "Crucible", 30, 134419591475328),   // Fort Intrepid
            new(592282, 1993, 722.999, 174.596, 589.918, 0.0000, "Crucible", 30, 134419591475327),   // Fort Intrepid
            new(592283, 1993, 770.983, 174.593, 504.146, 0.0000, "Crucible", 30, 134419591475329),   // Fort Intrepid
            new(592284, 1993, 770.998, 174.596, 590.081, 0.0000, "Crucible", 30, 134419591475326),   // Fort Intrepid
            // adv_arieki_torden_abyss (2028)
            new(592285, 2028, -715.172, 545.007, -113.758, 0.0000, "Abyss", 0, 134419591469877),
            new(592286, 2028, 126.042, 555.593, 423.882, 0.0000, "Abyss", 32, 134419591469570),   // Dybukkar Forward Camp
            new(592287, 2028, 161.999, 555.593, 403.054, 0.0000, "Abyss", 32, 134419591469569),   // Dybukkar Forward Camp
            new(592288, 2028, 202.045, 555.596, 402.954, 0.0000, "Abyss", 32, 134419591469568),   // Dybukkar Forward Camp
            new(592289, 2028, 238.015, 555.596, 423.742, 0.0000, "Abyss", 32, 134419591469567),   // Dybukkar Forward Camp
            // adv_foreas_valverde_descent (2047)
            new(592290, 2047, -512.076, 716.412, -28.254, 0.0000, "Descent", 35, 134419591468439),   // Virgil's Resonator
            new(592291, 2047, -508.429, 717.973, 167.368, 0.0000, "Descent", 35, 134419591468437),   // Virgil's Resonator
            new(592292, 2047, -469.482, 717.321, 167.793, 0.0000, "Descent", 35, 134419591463681),   // Virgil's Resonator
            new(592293, 2047, -462.886, 715.649, -35.182, 0.0000, "Descent", 35, 134419591468438),   // Virgil's Resonator
            new(592294, 2047, -208.328, 607.104, 754.959, 2.3992, "Descent", 0, 134419591467273),
            new(592295, 2047, -27.054, 243.545, 270.616, 1.5708, "Descent", 38, 134419591468862),   // Drill Resonator (its facilities)
            new(592296, 2047, -26.187, 244.031, 138.897, 1.5708, "Descent", 38, 134419591468865),   // Drill Resonator (its facilities)
            new(592297, 2047, -26.056, 244.045, 174.947, 1.5708, "Descent", 38, 134419591468863),   // Drill Resonator (its facilities)
            new(592298, 2047, -25.990, 242.551, 305.080, 1.5708, "Descent", 38, 134419591468864),   // Drill Resonator (its facilities)
            new(592299, 2047, -23.507, 242.099, 201.106, 1.5708, "Descent", 38, 134419591468806),   // Drill Resonator (its facilities)
            new(592300, 2047, -22.948, 242.081, 244.681, 1.5708, "Descent", 38, 134419591468805),   // Drill Resonator (its facilities)
            new(592301, 2047, 22.138, 602.624, 753.710, -3.1024, "Descent", 34, 134419591472124),   // Phi Resonator 2
            new(592302, 2047, 48.335, 603.045, 797.234, -0.1309, "Descent", 34, 134419591472120),   // Phi Resonator 2
            new(592303, 2047, 99.500, 601.947, 818.851, 2.7828, "Descent", 34, 134419591472134),   // Phi Resonator 2
            new(592304, 2047, 150.736, 602.445, 821.521, 1.0750, "Descent", 34, 134419591472129),   // Phi Resonator 2
            new(592305, 2047, 253.060, 333.361, -12.066, 0.0000, "Descent", 37, 134419591468795),   // Mal Dys Lower Resonator
            new(592306, 2047, 301.818, 331.565, -10.296, 0.0000, "Descent", 37, 134419591468796),   // Mal Dys Lower Resonator
            new(592307, 2047, 355.076, 333.320, 45.234, 0.0000, "Descent", 37, 134419591468798),   // Mal Dys Lower Resonator
            new(592308, 2047, 358.100, 333.595, 92.956, 0.0000, "Descent", 37, 134419591468797),   // Mal Dys Lower Resonator
            new(592309, 2047, 528.015, 472.138, 286.017, 0.7854, "Descent", 36, 134419591468510),   // Mal Dys Upper Resonator
            new(592310, 2047, 528.067, 473.016, 350.043, 0.0000, "Descent", 36, 134419591468508),   // Mal Dys Upper Resonator
            new(592311, 2047, 558.000, 472.983, 285.990, 0.7854, "Descent", 36, 134419591468509),   // Mal Dys Upper Resonator
            new(592312, 2047, 558.053, 473.162, 349.992, 0.0000, "Descent", 36, 134419591468507),   // Mal Dys Upper Resonator
            // adv_foreas_howlingmaw1 (2051)
            new(592313, 2051, -1152.624, 246.346, -300.692, 0.0000, "Howling Maw", 0, 134419591475375),
            new(592314, 2051, -1151.469, 245.600, -494.633, 0.0000, "Howling Maw", 39, 134419591475374),   // Cuthah Scout Post (its facilities)
            new(592315, 2051, -1131.556, 246.419, -343.795, 0.0000, "Howling Maw", 39, 134419591475376),   // Cuthah Scout Post (its facilities)
            new(592316, 2051, -1125.141, 246.715, -463.690, 0.0000, "Howling Maw", 39, 134419591475373),   // Cuthah Scout Post (its facilities)
            new(592317, 2051, -666.650, 293.230, 396.698, -3.0631, "Howling Maw", 0, 134419591471135),
            new(592318, 2051, -577.344, 298.330, 388.369, -3.0631, "Howling Maw", 0, 134419591471132),
            new(592319, 2051, 358.399, 226.431, 649.018, 0.0000, "Howling Maw", 0, 134419591475672),
            new(592320, 2051, 369.783, 238.631, 319.273, 0.0000, "Howling Maw", 0, 134419591475067),
            new(592321, 2051, 780.210, 244.067, 710.910, 0.6283, "Howling Maw", 0, 134419591476263),
            new(592322, 2051, 895.715, 245.047, 812.110, 1.6101, "Howling Maw", 0, 134419591476261),
            new(592323, 2051, 896.979, 245.562, 717.142, 1.6101, "Howling Maw", 0, 134419591473505),
            new(592324, 2051, 1202.487, 240.066, 326.034, 0.0000, "Howling Maw", 41, 134419591475881),   // Cuthah Ammo Depot
            new(592325, 2051, 1210.246, 240.581, 307.749, 0.0000, "Howling Maw", 41, 134419591475880),   // Cuthah Ammo Depot
            new(592326, 2051, 1236.354, 243.848, 305.645, 0.0000, "Howling Maw", 41, 134419591475879),   // Cuthah Ammo Depot
            new(592327, 2051, 1236.551, 244.250, 329.594, 0.0000, "Howling Maw", 41, 134419591475878),   // Cuthah Ammo Depot
            // adv_arieki_torden_mires_banefluxitemines (2115)
            new(592328, 2115, 168.558, 113.566, -72.213, -3.1416, "Mires", 0, 134419591466128),
            new(592329, 2115, 181.940, 142.507, -7.034, 0.0000, "Mires", 0, 134419591466127),
            new(592330, 2115, 183.250, 134.507, -123.500, -2.3562, "Mires", 0, 134419591468315),
            new(592331, 2115, 218.177, 106.550, 0.629, 0.7854, "Mires", 0, 134419591466126),
            new(592332, 2115, 244.500, 127.757, -92.250, 1.5708, "Mires", 0, 134419591466129),
            // adv_arieki_torden_mires_tahrendrabase (2125)
            new(592333, 2125, 109.276, 113.007, 9.711, 0.0000, "Mires", 0, 134419591465302),
            // adv_arieki_torden_abyss_dybukkar (2190)
            new(592334, 2190, -78.500, 311.007, 168.000, 0.0000, "Abyss", 0, 134419591470137),
            new(592335, 2190, -61.505, 279.700, -19.577, 0.0000, "Abyss", 0, 134419591465366),
            new(592336, 2190, 12.750, 298.757, 95.500, 0.0000, "Abyss", 0, 134419591470141),
            new(592337, 2190, 111.909, 310.747, 158.232, -2.9728, "Abyss", 0, 134419591470139),
        };

        private static readonly Dictionary<string, Zone> ZoneByName = Zones.ToDictionary(zone => zone.Name);

        public static Zone ZoneOf(Mortar mortar) => ZoneByName[mortar.Zone];

        /// <summary>The mortar row bound to a loot profile on <see cref="LootMap"/>.</summary>
        public static uint LootCreature => ZoneByName[LootZone].Creature;

        /// <summary>A same-level player's base health, the open-country rules' measure.</summary>
        public static double BaseHealth(uint level) => 10000 * System.Math.Pow(2, (level - 42.0) / 8);

        public static void Up(MigrationBuilder migration)
        {
            foreach (var zone in Zones)
            {
                migration.Sql(
                    "insert into creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage, damage_type) " +
                    $"values ({zone.Action}, 'Bane Mortar {zone.Name} weapon 411/1', 411, 1, 0.0, 60.0, 1000, 0, {zone.MinDamage}, {zone.MaxDamage}, 1);");
                migration.Sql(
                    "insert into creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"values ({zone.Creature}, 'Bane Mortar - {zone.Name}', {MortarClass}, 0, {zone.Level}, {zone.Health}, 0, 0, 0, {zone.Action}, 0, 0, 0, 0, 0, 0, 0);");
                migration.Sql(
                    "insert into creature_stat (id, body, mind, spirit, health, armor) " +
                    $"values ({zone.Creature}, {Stat}, {Stat}, {Stat}, {zone.Health}, {zone.Armor});");
                migration.Sql($"insert into creature_appearance (id, slot_id, Class_id, color) values ({zone.Creature}, {WeaponSlot}, {Launcher}, 1);");
            }

            var looted = LootCreature;
            migration.Sql(
                "INSERT INTO creature_loot_binding (map_context_id, creature_id, profile_key) " +
                $"SELECT {LootMap}, {looted}, p.profile_key FROM loot_profile p " +
                $"WHERE p.profile_key = '{LootProfile}' " +
                $"AND EXISTS (SELECT 1 FROM map_info m WHERE m.map_context_id = {LootMap}) " +
                $"AND NOT EXISTS (SELECT 1 FROM creature_loot_binding b WHERE b.map_context_id = {LootMap} AND b.creature_id = {looted});");

            foreach (var mortar in Mortars)
            {
                migration.Sql(
                    "insert into spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"values ({mortar.Pool}, {(mortar.ControlPoint == 0 ? 0 : 1)}, 0, {Respawn}, {F(mortar.X)}, {F(mortar.Y)}, {F(mortar.Z)}, {F(mortar.Rotation)}, {mortar.Map}, " +
                    $"{ZoneOf(mortar).Creature}, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.0);");

                if (mortar.ControlPoint != 0)
                    migration.Sql($"insert into control_point_link (control_point_id, kind, object_id) values ({mortar.ControlPoint}, 1, {mortar.Pool});");
            }
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql($"DELETE FROM creature_loot_binding WHERE map_context_id = {LootMap} AND creature_id = {LootCreature} AND profile_key = '{LootProfile}';");
            migration.Sql($"delete from control_point_link where kind = 1 and object_id between {PoolIdMin} and {PoolIdMax};");
            migration.Sql($"delete from spawnpool where id between {PoolIdMin} and {PoolIdMax};");

            foreach (var table in new[] { "creature_appearance", "creature_stat", "creature" })
                migration.Sql($"delete from {table} where id between {CreatureIdMin} and {CreatureIdMax};");

            migration.Sql($"delete from creature_action where id between {ActionIdMin} and {ActionIdMax};");
        }

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
