using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Howling Maw (adv_foreas_howlingmaw1, 2051;
    /// 20270720000000_Howling_maw_region_sweep).
    ///
    /// Howling Maw overworld (2051).
    ///
    /// Devil's Lair (14): the region's symbol is ..._DVLCLDRN, and REGION_LABEL "Devil's Cauldron" is
    /// at (-760.7,-447.5). A surface-only box is placed around that label, clear of the Power Station
    /// and Scout Post circles.
    ///
    /// The Western Dead Zone (4) and The Eastern Dead Zone (46): two REGION_LABEL "The Dead Zone"
    /// markers sit west (-1013.7,-124.4) and east (-106.3,213.1) of the volcano, and the two regions
    /// have identical settings. Each gets a surface-only box around its label.
    ///
    /// Region 52: the map's only Eloh-temple-interior setting set goes on the three
    /// arch_eloh_corridor_room logos shrines. One is under the Dead Zone Power Station at y123, one at
    /// (232,-493) and one at (1184,-389).
    ///
    /// Recut Howling Maw (region 3, row 20141): it was a r25 circle on the Gangus dropship pad, matched
    /// from the marker "Dropship Transport: Howling Maw". It now covers the volcano crater with the
    /// Cuthah Base exterior (goo lakes, tower, landing pad). Region 3 has the same howling-maw sky as
    /// the Cuthah Base instance.
    ///
    /// Not placed: - Unnamed caves 32, 42, 40 and 41, and the env-only region 51: no tie to a single
    /// cave. - Outdoor regions with no marker: Yul Fissure, Lake Divinus, Gangus Ridge, Devil's
    /// Sanctuary, Ingruo Valley, Strider Staging Area. - Molder region 1, and marker-only region 48.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class HowlingMawRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2051;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_howlingmaw1 - ";

        public static readonly Volume[] Volumes =
        {
            Of(14, -900, -620, -570, -330, 150, 330, SurfaceOnly, Map + "Devil's Lair (label 'Devil's Cauldron', plain)"),
            Of(4, -1164, -864, -200, 50, 150, 330, SurfaceOnly, Map + "The Western Dead Zone (west 'The Dead Zone' label)"),
            Of(46, -256, 44, 63, 363, 150, 330, SurfaceOnly, Map + "The Eastern Dead Zone (east 'The Dead Zone' label)"),
            Of(52, -508, -456, -558, -449, 115, 145, AnyLevel, Map + "region 52 (Eloh logos room under Power Station)"),
            Of(52, 206, 258, -521, -443, 244, 270, AnyLevel, Map + "region 52 (Eloh logos room south of Dia Yul)"),
            Of(52, 1158, 1227, -415, -345, 290, 315, AnyLevel, Map + "region 52 (Eloh logos room east of Elohvale)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20141, 3, 1, -618.5, 204, -1051.4, 25, 0, 0, -2000, 2000, 2, "foreas_howlingmaw1 - Howling Maw",
                Of(3, -665, -375, 95, 360, 180, 320, AnyLevel, Map + "Howling Maw (volcano crater, Cuthah Base exterior)")),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, byte underground, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, underground, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {v.Underground}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            foreach (var r in Recuts)
                yield return $"UPDATE map_region SET shape = {Box}, pos_x = {N(r.Box.X)}, pos_y = {N(r.Box.Y)}, pos_z = {N(r.Box.Z)}, radius = 0, " +
                    $"half_x = {N(r.Box.HalfX)}, half_z = {N(r.Box.HalfZ)}, min_y = {N(r.Box.MinY)}, max_y = {N(r.Box.MaxY)}, underground = {r.Box.Underground}, " +
                    $"comment = {S(r.Box.Comment)} " +
                    $"WHERE id = {r.RowId} AND map_context_id = {MapContextId} AND region_id = {r.RegionId} AND shape = {r.Shape} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01 " +
                    $"AND ABS(half_x - {N(r.HalfX)}) < 0.01 AND ABS(half_z - {N(r.HalfZ)}) < 0.01 " +
                    $"AND ABS(min_y - {N(r.MinY)}) < 0.01 AND ABS(max_y - {N(r.MaxY)}) < 0.01 AND underground = {r.Underground};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {r.Shape}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = {N(r.HalfX)}, half_z = {N(r.HalfZ)}, min_y = {N(r.MinY)}, max_y = {N(r.MaxY)}, underground = {r.Underground}, comment = {S(r.Comment)} " +
                    $"WHERE id = {r.RowId} AND {Is(r.Box)};";

            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {Is(v)};";
        }

        private static string Is(Volume v) =>
            $"{At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 " +
            $"AND ABS(min_y - {N(v.MinY)}) < 0.01 AND ABS(max_y - {N(v.MaxY)}) < 0.01 AND underground = {v.Underground} AND comment = {S(v.Comment)}";

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
