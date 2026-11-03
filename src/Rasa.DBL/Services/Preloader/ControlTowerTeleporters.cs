using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// A local teleporter on each floor of every ArchHumOutpostTowerV02, linked both ways
    /// (Control_tower_teleporters) - the towers at the control points and outposts: Landing Zone,
    /// Fort Dew, Research Point, Outpost Aurora, Stonewall's four and the rest, thirty-three on
    /// twelve maps.
    ///
    /// OutpostTowerTeleporters gave the ArchHumOutpostTowerV03 towers theirs and left these out,
    /// though they are the same tower: the V02 class draws arch_hum_outpost_tower_v05.geo, the V03
    /// arch_hum_outpost_tower_v06.geo, and the two have the same collision mesh to the triangle up
    /// to the roof (the V03 adds a mast above it) - the floor at 1.0 m and the second at 14.45 m,
    /// with no stairs between, and the pad at (-0.045, 0.57) from the origin. The original data
    /// says as much: nine of these towers already have a type 1 row on that spot, 1.0 m over the
    /// origin (537-540 Stonewall, 549 Outpost Aurora, 576 Larai Outpost, 570 Falcon Hold, 571 and
    /// 573 Research Point), and none on the second floor.
    ///
    /// Every tower gets a row on each floor where it has none - the ground floor's at 1.0 m over
    /// the origin, the second floor's at 14.4 m, as the V03 rows are, class ArchHumTeleporterV02,
    /// type 1 - and the two are linked floor to floor. New rows are 1100 to 1156.
    ///
    /// The Research Point towers' ground pads, 571 and 573, were linked to each other
    /// (LocalTeleporterLinks: the client's map markers give both the one label, "Tower
    /// Teleporter: Research Point") - 155 m apart, ground floor to ground floor, so neither
    /// tower's second floor could be reached. Each is linked to its own tower's second floor
    /// instead; Down puts the pair back.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A row goes in only where its id is free, a link
    /// only where its pad has none and both its ends are there; Down takes out the links and rows
    /// only as Up put them, and never a row it did not add.
    /// </summary>
    public static class ControlTowerTeleporters
    {
        public const uint TeleporterClass = OutpostTowerTeleporters.TeleporterClass;   // ArchHumTeleporterV02
        public const uint LocalTeleporterType = OutpostTowerTeleporters.LocalTeleporterType;
        public const uint FirstTeleporter = 1100, LastTeleporter = 1156;

        /// <summary>Where the pad is from the tower's origin, unturned, and how high each floor's row is over it.</summary>
        public const double PadX = -0.045, PadZ = 0.57, GroundRise = 1.0, UpperRise = 14.4;

        /// <summary>Research Point's two ground pads, linked to each other until now.</summary>
        public const uint ResearchPointWest = 571, ResearchPointEast = 573;

        /// <summary>A tower: its ground and second floor pads' row ids, its map, and its origin and yaw as the .map places it.</summary>
        public sealed record Tower(uint Ground, uint Upper, uint MapContextId, string Place, double X, double Y, double Z, double Yaw)
        {
            /// <summary>The pad on either floor: its spot from the origin, turned as the tower is.</summary>
            public (double X, double Z) Pad
            {
                get
                {
                    var (sin, cos) = Math.SinCos(Yaw);
                    return (X + PadX * cos + PadZ * sin, Z - PadX * sin + PadZ * cos);
                }
            }

            /// <summary>Whether the ground floor's row is one of the original data's, which Up leaves as it is.</summary>
            public bool GroundWasThere => Ground < FirstTeleporter;

            public string Description(string floor) =>
                string.Format(CultureInfo.InvariantCulture, "Local Teleporter: {0} tower at {1:0}, {2:0}, {3}", Place, X, Z, floor);
        }

        public static readonly Tower[] Towers =
        {
            new(1100, 1101, 1148, "Concordia Divide", -754.2760009765625, 178.39700317382812, 608.801025390625, -0.9424770801648622),
            new(1102, 1103, 1148, "Concordia Divide", 238.63800048828125, 89.35929870605469, -63.94279861450195, 0.10679570325273895),
            new(1104, 1105, 1220, "Concordia Wilderness", 176.14306640625, 147.7096710205078, -455.70550537109375, 2.950083484901056),
            new(1106, 1107, 1220, "Landing Zone", 239, 163.00010681152344, -94, -1.5707942406323294),
            new(1108, 1109, 1220, "Concordia Wilderness", 528.1179809570312, 231.73699951171875, 149.62600708007812, 0.7853980369569059),
            new(1110, 1111, 1244, "Fort Dew", -148, 172, -682, -3.1415925025939906),
            new(571, 1112, 1454, "Research Point", -271.8730163574219, 216.31590270996094, -346.8141174316406, 0),
            new(570, 1113, 1454, "Falcon Hold", -122, 219.39830017089844, 592, -1.5707961479809516),
            new(573, 1114, 1454, "Research Point", -116.95904541015625, 216.31590270996094, -323.71807861328125, -1.7389174493096833),
            new(1115, 1116, 1497, "Valverde Plateau", 177.70144653320312, 348.3489685058594, 133.3590850830078, 0.2870289185691894),
            new(1117, 1118, 1497, "Valverde Plateau", 242, 348, 275.5, -1.0854417995919103),
            new(1119, 1120, 1497, "Valverde Plateau", 303, 369.1412658691406, 379, 1.5707964460041932),
            new(1121, 1122, 1497, "Valverde Plateau", 400.2340087890625, 330.5090026855469, 88.42870330810547, -0.23437080067098354),
            new(539, 1123, 1734, "Stonewall Command", -63.31882858276367, 352.4285888671875, -400.54705810546875, 1.982130558558366),
            new(538, 1124, 1734, "Stonewall Operations Tower", -3.647307872772217, 338.8697509765625, -534.1763305664062, -0.047772702617850535),
            new(537, 1125, 1734, "Stonewall Artillery Control 1", 59.96290969848633, 349.5814208984375, -474.8079833984375, -1.8016963191681483),
            new(540, 1126, 1734, "Stonewall Artillery Control 2", 147.17852783203125, 336.2518615722656, -602.3430786132812, -2.356194321604962),
            new(1127, 1128, 1759, "Torden Mires", 480.25, 241.3284912109375, -328, 1.0000001537946413E-07),
            new(1129, 1130, 1759, "Torden Mires", 554, 269, -264, -3.1415925025939906),
            new(1131, 1132, 1759, "Torden Mires", 616.63427734375, 268.3371276855469, -312.60101318359375, 1.0000001537946413E-07),
            new(1133, 1134, 1761, "Torden Incline", -150.68099975585938, 283.7359924316406, 270.15399169921875, 1.5708004991202795),
            new(1135, 1136, 1761, "Torden Incline", 83.7488021850586, 245.92300415039062, -95.63770294189453, 2.5492501098512603),
            new(1137, 1138, 1764, "Torden Plains", -477, 472, 449, -1.5707961479809516),
            new(1139, 1140, 1764, "Torden Plains", -363, 438, 403, 1.0000001537946413E-07),
            new(1141, 1142, 1764, "Torden Plains", -363, 438, 496, -3.1415925025939906),
            new(1143, 1144, 1764, "Geyser Chimney Basin", 181, 434, 280, -3.1415925025939906),
            new(1145, 1146, 1764, "Torden Plains", 246, 436.3565673828125, 53, -3.1415925025939906),
            new(576, 1147, 1911, "Larai Outpost", -536, 366, -202, -3.1415925025939906),
            new(1148, 1149, 1911, "Outpost Aurora", 705.4002075195312, 384, -221.70050048828125, 1.5707942406323294),
            new(549, 1150, 1911, "Outpost Aurora", 754.9765014648438, 384, -145.28466796875, 1.5707942406323294),
            new(1151, 1152, 2047, "Valverde Descent", -304, 734.9998779296875, -529, -3.1415925025939906),
            new(1153, 1154, 2047, "Valverde Descent", -260, 671, -294, 2.2238178119022387),
            new(1155, 1156, 2051, "Howling Maw", -540.6209106445312, 199.8354949951172, -965.5240478515625, -1.5707961479809516),
        };

        /// <summary>The rows Up adds: (id, map, x, y, z, description).</summary>
        public static IEnumerable<(uint Id, uint MapContextId, double X, double Y, double Z, string Description)> NewPads() =>
            Towers.SelectMany(t =>
            {
                var pads = new List<(uint, uint, double, double, double, string)>();
                if (!t.GroundWasThere)
                    pads.Add((t.Ground, t.MapContextId, t.Pad.X, t.Y + GroundRise, t.Pad.Z, t.Description("ground floor")));
                pads.Add((t.Upper, t.MapContextId, t.Pad.X, t.Y + UpperRise, t.Pad.Z, t.Description("second floor")));
                return pads;
            });

        /// <summary>Every link, one a way, with the map both ends are on.</summary>
        public static IEnumerable<(uint From, uint To, uint MapContextId)> Links() =>
            Towers.SelectMany(t => new[] { (t.Ground, t.Upper, t.MapContextId), (t.Upper, t.Ground, t.MapContextId) });

        public static IEnumerable<string> Up()
        {
            // Research Point's ground pads go up their own towers, not to each other.
            foreach (var (from, to) in ResearchPointPair())
                yield return $"DELETE FROM {LocalTeleporterLinkEntry.TableName} WHERE from_teleporter_id = {from} AND to_teleporter_id = {to};";

            foreach (var p in NewPads())
                yield return "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
                    $"SELECT {p.Id}, {TeleporterClass}, {LocalTeleporterType}, '{p.Description}', {N(p.X)}, {N(p.Y)}, {N(p.Z)}, 0, {p.MapContextId} " +
                    $"FROM entityclass e WHERE e.id = {TeleporterClass} AND NOT EXISTS (SELECT 1 FROM teleporter x WHERE x.id = {p.Id});";

            foreach (var (from, to, map) in Links())
                yield return $"INSERT INTO {LocalTeleporterLinkEntry.TableName} (from_teleporter_id, to_teleporter_id) " +
                    $"SELECT {from}, {to} FROM teleporter a, teleporter b " +
                    $"WHERE a.id = {from} AND a.type = {LocalTeleporterType} AND a.map_context_id = {map} " +
                    $"AND b.id = {to} AND b.type = {LocalTeleporterType} AND b.map_context_id = {map} " +
                    $"AND NOT EXISTS (SELECT 1 FROM {LocalTeleporterLinkEntry.TableName} x WHERE x.from_teleporter_id = {from});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (from, to, _) in Links())
                yield return $"DELETE FROM {LocalTeleporterLinkEntry.TableName} WHERE from_teleporter_id = {from} AND to_teleporter_id = {to};";

            foreach (var p in NewPads())
                yield return $"DELETE FROM teleporter WHERE id = {p.Id} AND class_id = {TeleporterClass} AND type = {LocalTeleporterType} " +
                    $"AND map_context_id = {p.MapContextId} AND ABS(pos_x - {N(p.X)}) < 0.01 AND ABS(pos_z - {N(p.Z)}) < 0.01;";

            // Research Point's pair as LocalTeleporterLinks left it, where its pads are free again.
            foreach (var (from, to) in ResearchPointPair())
                yield return $"INSERT INTO {LocalTeleporterLinkEntry.TableName} (from_teleporter_id, to_teleporter_id) " +
                    $"SELECT {from}, {to} FROM teleporter a, teleporter b WHERE a.id = {from} AND b.id = {to} " +
                    $"AND NOT EXISTS (SELECT 1 FROM {LocalTeleporterLinkEntry.TableName} x WHERE x.from_teleporter_id = {from});";
        }

        private static IEnumerable<(uint From, uint To)> ResearchPointPair() =>
            new[] { (ResearchPointWest, ResearchPointEast), (ResearchPointEast, ResearchPointWest) };

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
