using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Forbidden Temples (adv_foreas_concordia_palisades_elohtemples,
    /// 1803; 20270725010000_Eloh_temples_region_sweep).
    ///
    /// No todo regions remain on this map (region 2 is the editor's DefaultSky region). One placement
    /// error found and fixed.
    ///
    /// Temple of the Raging Patriarch (3) and Temple of the Proud Patriarch (4), recut: the Raging
    /// temple's three body rooms each have a spawner tee (arch_eloh_corridor_tee_32m ending in two
    /// arch_eloh_temples_spawner alcoves) on both sides, at x -611..-584 and -440..-413. The Raging box
    /// stopped at x -585..-439, so four tees showed the map default cave sky/env, and the Proud box
    /// (which reached x -586) covered the other two west tees, naming them 'Temple of the Proud
    /// Patriarch'. Raging widens to x -612..-412; Proud's main box stops at x -612, and the Proud east
    /// room (arch_eloh_corridor_room_v01, x -636..-587 z 7..57) gets its own box so it stays Proud
    /// (Proud's priority 8 beats Raging's 7 where they meet). The other temple boxes and the cave-mouth
    /// boxes cover every walkable polygon of their temples and are left alone.
    ///
    /// Not placed: 2 (DefaultSky.sky editor default).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class ElohTemplesRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 1803;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_concordia_palisades_elohtemples - ";

        public static readonly Volume[] Volumes =
        {
            Of(4, -638, -586, 5, 59, -10, 20, AnyLevel, Map + "Temple of the Proud Patriarch (east room)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20432, 3, 2, -512, -26, 124.5, 0, 73, 353.5, -26, 52, 0, "foreas_concordia_palisades_elohtemples - Temple of the Raging Patriarch (temple)",
                Of(3, -612, -412, -229, 478, -26, 52, AnyLevel, Map + "Temple of the Raging Patriarch (temple and tees)")),
            new(20433, 4, 2, -740, -10, -39.5, 0, 154, 257.5, -10, 52, 0, "foreas_concordia_palisades_elohtemples - Temple of the Proud Patriarch (temple)",
                Of(4, -894, -612, -297, 218, -10, 52, AnyLevel, Map + "Temple of the Proud Patriarch (temple)")),
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
