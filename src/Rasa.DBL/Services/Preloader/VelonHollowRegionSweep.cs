using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on Velon Hollow (the Death Burrow)
    /// (adv_foreas_howlingmaw_deathburrow, 2136; 20270720020000_Velon_hollow_region_sweep).
    ///
    /// Velon Hollow (2136) is the Howling Maw Death Burrow instance. The entry tunnel with the AFS
    /// hospital and portable waypoint is at about (170,-500). Cavern tunnels (terra_foreas_cavern_*,
    /// navmesh underground) lead from it to three Eloh temple caverns and a Bane outpost cavern with a
    /// Bane lab. The temple and outpost cavern floors are NOT flagged underground. Above everything is
    /// a flag-1 roof surface at y 90+.
    ///
    /// Bane Chemical Laboratory (regions 3 and 14, identical settings) covers the only two Bane rooms.
    /// Region 3 gets the entry chunnel, the tunnels, the upper room_control_24m_v07 and the ramp.
    /// Region 14 gets the lower room_control_24m_v14. Their POI sits between the two rooms.
    ///
    /// The four Paths share the cave-hall set. The Path of the Beholden, the Path of the Penitent and
    /// the Path of Renewal are placed on the tunnel legs that lead only to the Mausoleum of the
    /// Beholden, the Shrine of the Penitent and the Sanctuary of Renewal. They are underground-only
    /// boxes.
    ///
    /// Recuts (all were broken seeded circles): - Velon Hollow Outpost (name id _BO = Bane Outpost,
    /// Bane sky/ambient/music) had a surface-only circle on the AFS hospital in an underground tunnel.
    /// It is moved to the Bane Outpost cavern around its POI. - The Mausoleum, Shrine and Sanctuary
    /// circles were underground-only over caverns whose floors are not underground, so they never named
    /// their own halls. Each is now a ug-0 box over its cavern, kept under the roof surface. - The
    /// Velon Hollow circle at the entry is left as is.
    ///
    /// Not placed: Path of Death (south infected-pool branch vs north tunnel to the outpost, no tie-
    /// breaker) and region 1 (Molder.sky).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class VelonHollowRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2136;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_howlingmaw_deathburrow - ";

        public static readonly Volume[] Volumes =
        {
            Of(3, 334, 437, 27, 105, 30, 52, AnyLevel, Map + "Bane Chemical Laboratory (entry chunnel, tunnels)"),
            Of(3, 328, 423, 89, 186, 62, 95, AnyLevel, Map + "Bane Chemical Laboratory (upper control room)"),
            Of(3, 405, 437, 121, 153, 30, 82, AnyLevel, Map + "Bane Chemical Laboratory (ramp between rooms)"),
            Of(14, 428, 492, 67, 141, 30, 62, AnyLevel, Map + "Bane Chemical Laboratory (lower control room)"),
            Of(8, -50, 30, -500, -455, 50, 80, UndergroundOnly, Map + "Path of the Beholden (tunnel to Mausoleum)"),
            Of(9, -30, 55, -452, -340, 55, 95, UndergroundOnly, Map + "Path of the Penitent (tunnel to Shrine, south)"),
            Of(9, -30, 10, -340, -285, 55, 95, UndergroundOnly, Map + "Path of the Penitent (tunnel to Shrine, north)"),
            Of(11, 488, 556, -362, -322, 45, 70, UndergroundOnly, Map + "Path of Renewal (tunnel to Sanctuary)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20175, 4, 1, 180, 71, -496.4, 120, 0, 0, -2000, 2000, 2, "foreas_howlingmaw_deathburrow - Velon Hollow Outpost ",
                Of(4, 150, 392, -95, 165, 20, 88, AnyLevel, Map + "Velon Hollow Outpost (Bane outpost cavern)")),
            new(20176, 5, 1, -115.2, 60.3, -488.8, 80, 0, 0, -2000, 2000, 1, "foreas_howlingmaw_deathburrow - Mausoleum of the Beholden",
                Of(5, -222, -48, -580, -372, 40, 82, AnyLevel, Map + "Mausoleum of the Beholden (temple cavern)")),
            new(20178, 6, 1, -119.3, 81.8, -279.5, 80, 0, 0, -2000, 2000, 1, "foreas_howlingmaw_deathburrow - Shrine of the Penitent",
                Of(6, -216, -28, -366, -205, 65, 89, AnyLevel, Map + "Shrine of the Penitent (temple cavern)")),
            new(20179, 7, 1, 631.6, 49.8, -343.2, 80, 0, 0, -2000, 2000, 1, "foreas_howlingmaw_deathburrow - Sanctuary of Renewal",
                Of(7, 552, 732, -432, -262, 35, 78, AnyLevel, Map + "Sanctuary of Renewal (temple cavern)")),
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
