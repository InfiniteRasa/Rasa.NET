using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader.Missions
{
    /// <summary>
    /// The map each mission indicator is on (mission_indicator.map_context_id). An indicator has no map on the
    /// wire, so the client draws every indicator it is sent on the map the player is on: before this, a player
    /// on the Plateau saw Velon Hollow's, Ustor Yard's and Maligo's markers on the Plateau map, at their places
    /// in those maps (player reports, 2026-10-30: Ancient Mysteries 1978, The Better Half 1975, One Way Trip 877,
    /// Mountain Spring Toxins 1979, These Old Bones 1976; Plateau markers in Ustor Yard). The server now sends a
    /// player only the indicators on its map, or with map 0 (every map, the default for a row not set here).
    ///
    /// How each map was found, per row of the world DB as it stood (689 rows, 185 missions):
    ///  - the map of the mission's own content near the indicator (its NPCs, spawn pools, usables, map links,
    ///    areas, the content revision's zone), where that gives one map;
    ///  - else the navmesh: the maps with walkable ground within a few metres of the indicator, and of those
    ///    the one the objective text, the mission data or a guide names (for example the Trinity Bridge Logos
    ///    from their logos rows, PlateauTrinityBridgeLogosV1; Velon Hollow's missions, 1974-1979, map 2136);
    ///  - an area over other objectives (Securing Torcastra 594, objective 9: radius 140 round the four bunkers of
    ///    objectives 1, 2, 10 and 11, its centre in no walkable ground) is on their map (Torcastra Prison, 1349).
    /// A row not set here (new content) stays 0, every map, as before.
    /// The per-mission map below holds for every indicator of the mission, but for the <see cref="Exceptions"/>.
    /// Data only: Up sets the column, Down leaves it to the column's drop.
    /// </summary>
    public static class MissionIndicatorMapsV1
    {
        /// <summary>(content revision, map, missions): every indicator of these missions is on that map.</summary>
        public static readonly (string Revision, uint MapContextId, uint[] Missions)[] Missions =
        {
            ("deployment_11", 1985, new uint[]
            {
                1990, 1992, 1994, 1995, 2005
            }),
            ("devils_den_1_16_5", 1394, new uint[]
            {
                378, 379, 380, 381, 669, 670, 671, 672
            }),
            ("donn_1_16_5", 1220, new uint[]
            {
                859
            }),
            ("donn_1_16_5", 1506, new uint[]
            {
                674, 692, 693, 923, 2004
            }),
            ("minos_1_16_5", 1148, new uint[]
            {
                382, 384
            }),
            ("minos_1_16_5", 1347, new uint[]
            {
                383, 392
            }),
            ("ojasa_1_16_5", 1761, new uint[]
            {
                1491
            }),
            ("ojasa_1_16_5", 1865, new uint[]
            {
                1492, 1493, 1494, 1495
            }),
            ("plateau_1_16_5", 1220, new uint[]
            {
                2017
            }),
            ("plateau_1_16_5", 1304, new uint[]
            {
                1068, 1541
            }),
            ("plateau_1_16_5", 1416, new uint[]
            {
                1047
            }),
            ("plateau_1_16_5", 1429, new uint[]
            {
                1051
            }),
            ("plateau_1_16_5", 1454, new uint[]
            {
                1751
            }),
            ("plateau_1_16_5", 1465, new uint[]
            {
                904, 1057
            }),
            ("plateau_1_16_5", 1497, new uint[]
            {
                555, 557, 713, 719, 855, 867, 887, 895, 905, 928, 929, 930, 931, 933,
                934, 935, 936, 939, 954, 965, 970, 1014, 1015, 1017, 1027, 1034, 1040, 1044,
                1045, 1046, 1053, 1251, 1659, 1825, 1981, 1983, 2007, 2016
            }),
            ("plateau_1_16_5", 1502, new uint[]
            {
                1187, 1188, 1189, 1190
            }),
            ("plateau_1_16_5", 1700, new uint[]
            {
                1049
            }),
            ("plateau_1_16_5", 1743, new uint[]
            {
                1048
            }),
            ("plateau_1_16_5", 1830, new uint[]
            {
                876, 877, 878, 879
            }),
            ("plateau_1_16_5", 2029, new uint[]
            {
                1167, 1171, 1178, 1181
            }),
            ("plateau_1_16_5", 2136, new uint[]
            {
                1974, 1975, 1976, 1977, 1978, 1979, 1982
            }),
            ("pools_1_16_5", 1304, new uint[]
            {
                560, 561, 562, 563, 932, 937, 938, 941, 1025, 1066, 1305, 1308, 1314, 1315,
                1321, 1322, 1323, 1324, 1326, 1328, 1330, 1331, 1368, 1590
            }),
            ("pools_1_16_5", 1429, new uint[]
            {
                882, 884, 885, 886, 1079
            }),
            ("pools_1_16_5", 1465, new uint[]
            {
                896, 898, 903
            }),
            ("pools_1_16_5", 1694, new uint[]
            {
                856, 1131, 1132, 1133, 1134, 1135, 1136, 1138, 1329
            }),
            ("pravus_1_16_5", 1220, new uint[]
            {
                593, 1075
            }),
            ("pravus_1_16_5", 1430, new uint[]
            {
                321, 323, 575
            }),
            ("torcastra_1_16_5", 1349, new uint[]
            {
                356, 397, 398, 399, 594, 1860
            }),
            ("wilderness_1_16_5", 1220, new uint[]
            {
                425, 427, 428, 429, 430, 431, 432, 433, 434, 436, 442, 444, 451, 479,
                506, 508, 570, 574, 623, 665, 666, 679, 682, 695, 696, 697, 698, 700,
                701, 791, 820, 860, 908, 912, 1069, 1407, 1634, 1635
            }),
        };

        /// <summary>The indicators on another map than their mission's (mission, revision, objective, indicator, map).</summary>
        public static readonly (uint Mission, string Revision, uint Objective, uint Indicator, uint MapContextId)[] Exceptions =
        {
            (381, "devils_den_1_16_5", 2, 1000000002, 1244),
            (670, "devils_den_1_16_5", 2, 1000000002, 1244),
            (671, "devils_den_1_16_5", 7, 1000000007, 1244),
            (877, "plateau_1_16_5", 9, 1000000001, 1497),
            (877, "plateau_1_16_5", 10, 1000000003, 1502),
            (877, "plateau_1_16_5", 13, 1000000002, 1497),
            (904, "plateau_1_16_5", 1, 1000000001, 1304),
            (935, "plateau_1_16_5", 1, 1000000002, 2029),
            (1491, "ojasa_1_16_5", 2, 1000000002, 1865),
            (1975, "plateau_1_16_5", 3, 1000000003, 1497),
            (1975, "plateau_1_16_5", 4, 1000000004, 1497),
            (1978, "plateau_1_16_5", 2, 1000000001, 1497),
            (1978, "plateau_1_16_5", 3, 1000000008, 1497),
            (1978, "plateau_1_16_5", 3, 1000000009, 1497),
            (2017, "plateau_1_16_5", 1, 1000000001, 1304),
            (2017, "plateau_1_16_5", 2, 1000000002, 1454),
        };

        /// <summary>The map of one indicator, or null when this data does not set it.</summary>
        public static uint? MapOf(uint mission, string revision, uint objective, uint indicator)
        {
            foreach (var row in Exceptions)
                if (row.Mission == mission && row.Revision == revision && row.Objective == objective && row.Indicator == indicator)
                    return row.MapContextId;
            foreach (var group in Missions)
                if (group.Revision == revision && group.Missions.Contains(mission))
                    return group.MapContextId;
            return null;
        }

        public static IEnumerable<string> Up()
        {
            foreach (var group in Missions)
                yield return $"UPDATE mission_indicator SET map_context_id = {group.MapContextId} " +
                    $"WHERE content_revision = '{group.Revision}' AND mission_id IN ({string.Join(", ", group.Missions)});";
            foreach (var row in Exceptions)
                yield return $"UPDATE mission_indicator SET map_context_id = {row.MapContextId} " +
                    $"WHERE mission_id = {row.Mission} AND content_revision = '{row.Revision}' " +
                    $"AND objective_id = {row.Objective} AND indicator_id = {row.Indicator};";
        }
    }
}
