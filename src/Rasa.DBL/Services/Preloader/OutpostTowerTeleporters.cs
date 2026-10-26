using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// A local teleporter on each floor of every other human outpost tower, linked both ways
    /// (Outpost_tower_teleporters), as Twin Pillars' two towers have (TwinPillarsNpcs).
    ///
    /// The tower (ArchHumOutpostTowerV03) has a teleporter pad on its ground floor and one on its
    /// second floor 13.4 m straight above, at (-0.045, 0.57) from the tower's origin, and no
    /// stairs between them. The maps place sixteen; the two in the Wilderness have their pads
    /// (984 to 987), the other fourteen had no row on either pad:
    ///  - Avernus Outpost, The Abyss and Torden Plains, one each;
    ///  - Palisades, three (two by New Cumbria and one west of it);
    ///  - Howling Maw, two;
    ///  - the Edmund Range and Proving Grounds wargame maps, four and two.
    ///
    /// Each pad's row is where the Wilderness towers' are: on the pad, 1.0 m over the tower's
    /// origin on the ground floor and 14.4 m on the second, as the GM stood on them, of the class
    /// they are (ArchHumTeleporterV02), type 1 and facing 0. One tower in Howling Maw (-710.6,
    /// -1171.2) stands sunk in its hill: the walkable ground inside it is 2.4 m over its origin,
    /// so its ground floor row is on that ground.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A row goes in only where its id is free, a link
    /// only where its pad has none and both its ends are there; Down takes out the links and rows
    /// only as Up put them.
    /// </summary>
    public static class OutpostTowerTeleporters
    {
        public const uint TeleporterClass = ThunderheadBaseTeleporters.TeleporterClass;   // ArchHumTeleporterV02
        public const uint LocalTeleporterType = 1;
        public const uint FirstTeleporter = 952, LastTeleporter = 979;

        /// <summary>A tower: its ground floor pad's row id (the second floor's is the next), and where its pads are.</summary>
        public sealed record Tower(uint Ground, uint MapContextId, string Place, double X, double GroundY, double UpperY, double Z)
        {
            public uint Upper => Ground + 1;
            public string Description(string floor) =>
                string.Format(CultureInfo.InvariantCulture, "Local Teleporter: {0} tower at {1:0}, {2:0}, {3}", Place, X, Z, floor);
        }

        public static readonly Tower[] Towers =
        {
            new(952, 2110, "Avernus Outpost", 22.4538, 529.0, 542.4, -93.4609),
            new(954, 2028, "The Abyss", 23.2167, 539.3185, 552.7185, -701.5438),
            new(956, 1764, "Torden Plains", -40.5304, 424.3534, 437.7534, -101.0676),
            new(958, 1244, "Palisades", -722.57, 139.0, 152.4, 587.955),
            new(960, 1244, "Palisades", -704.57, 139.0, 152.4, 679.955),
            new(962, 1244, "Palisades", -969.8931, 168.9102, 182.3102, 629.5889),
            new(964, 2051, "Howling Maw", -563.5909, 227.9321, 241.3321, -1211.8914),
            new(966, 2051, "Howling Maw", -710.645, 233.3, 245.31, -1170.63),
            new(968, 2374, "Edmund Range", 151.9256, 421.3588, 434.7588, 150.5345),
            new(970, 2374, "Edmund Range", -268.9546, 421.4026, 434.8026, 188.9296),
            new(972, 2374, "Edmund Range", 151.9256, 421.3588, 434.7588, -105.4651),
            new(974, 2374, "Edmund Range", -268.9546, 421.4026, 434.8026, -67.0704),
            new(976, 2361, "Proving Grounds", 151.9256, 421.3589, 434.7589, 150.5345),
            new(978, 2361, "Proving Grounds", -268.9546, 421.4026, 434.8026, 188.9296),
        };

        /// <summary>Every pad's row: (id, map, x, y, z, description).</summary>
        public static IEnumerable<(uint Id, uint MapContextId, double X, double Y, double Z, string Description)> Pads() =>
            Towers.SelectMany(t => new[]
            {
                (t.Ground, t.MapContextId, t.X, t.GroundY, t.Z, t.Description("ground floor")),
                (t.Upper, t.MapContextId, t.X, t.UpperY, t.Z, t.Description("second floor")),
            });

        /// <summary>Every link, one a way, with the map both ends are on.</summary>
        public static IEnumerable<(uint From, uint To, uint MapContextId)> Links() =>
            Towers.SelectMany(t => new[] { (t.Ground, t.Upper, t.MapContextId), (t.Upper, t.Ground, t.MapContextId) });

        public static IEnumerable<string> Up()
        {
            foreach (var p in Pads())
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

            foreach (var p in Pads())
                yield return $"DELETE FROM teleporter WHERE id = {p.Id} AND class_id = {TeleporterClass} AND type = {LocalTeleporterType} " +
                    $"AND map_context_id = {p.MapContextId} AND ABS(pos_x - {N(p.X)}) < 0.01 AND ABS(pos_z - {N(p.Z)}) < 0.01;";
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
