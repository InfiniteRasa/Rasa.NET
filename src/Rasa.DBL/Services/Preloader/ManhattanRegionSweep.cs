using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on Manhattan (adv_earth_unitedstates_manhattan_01, 2327;
    /// 20270726000000_Manhattan_region_sweep).
    ///
    /// Subway System (region 2, interior subway sky, subway room tone, dark blue fog) had no volume.
    /// The map has two subway segments of arch_earth_corridor_subway_* pieces at floor y 184.6-190
    /// under streets at y 207+, each framed by a minimap rect: the main line (region 22 rect, x
    /// -43.5..49.5 z -66..360: tunnels x -41..-31 and 22..37, platforms between, 28th St at z -9..63,
    /// 33rd St at z 263..335) and the 23rd Street line (region 21 rect, x -339.5..-268.5 z
    /// -654.5..-419.5). The main line is navmesh-underground throughout, so it gets an underground-only
    /// box; on the 23rd St line the tunnels south of z -590 are unflagged, so that box is any-level,
    /// held under the street by y 180..206 and stopped at the E23 pit (z -644). Definite.
    ///
    /// Region 14 (a second 'Subway System', priority 10, sky war_empire_v03 only) goes on the one
    /// roofless stretch of the main line: the crater piece arch_earth_corridor_subway_endcap_8m_v04 at
    /// x -14..55 z 186..254 between the two stations, whose rubble floor is navmesh-surface. Medium: no
    /// label, but it is the only open-sky part of the subway without its own region, and an outdoor sky
    /// named Subway System above the stations' priority fits it.
    ///
    /// Region 11 (A.F.S. Womhole E23, name only) matches the POI_LABEL 'A.F.S. Wormhole E23' at
    /// -278.5,184.5,-666.5 (the region name is misspelt, so the backlog found no marker). It is the
    /// open pit at the south end of the 23rd St line (same crater mesh, x -319..-250 z -712..-644) with
    /// the wormhole catwalk platform; mission text calls it 'The Wormhole at the 23rd Street Subway
    /// Station'. Box any-level, y 178..204. Definite.
    ///
    /// Recuts: the three station rows are surface-only circles at their POI labels on the street, so
    /// they never reach the stations below. 23rd Street Station (definite): its medic, medical vendor,
    /// waypoint, cots and barricades are all at subway level, and mission text says the base is in the
    /// subway station; new box over the platform section z -556..-450, any-level under the street. 28th
    /// Street Station (probable): platform section z -66..66 with both stairways, underground-only.
    /// 33rd Street Station (probable): it sets a dark subway sky that the circle currently puts on the
    /// open street; new box z 258..358, underground-only. The streets above fall back to the map's New
    /// York City.
    ///
    /// Not placed: region 8 (sky war_empire_v02 + MUSIC_EARTH_02) and region 23 (MUSIC_EARTH_02 only)
    /// are unnamed with no label, marker or minimap rect; EARTH_02 is the Empire Sector / E34 music, so
    /// they are probably the northern part of the map, but nothing gives an extent. Region 9 is marker-
    /// only and region 1 is Molder.sky. A destroyed building's basement west of 33rd St
    /// (arch_earth_building_destroyed_closeup_v01, x -106..-39 z 264..314, navmesh y 188-192,
    /// underground) touches the station but was left out: nothing ties it to a region.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class ManhattanRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2327;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "earth_unitedstates_manhattan_01 - ";

        public static readonly Volume[] Volumes =
        {
            Of(2, -56, 40, -66, 358, 178, 206, UndergroundOnly, Map + "New York City: Subway System (main line tunnels)"),
            Of(2, -336, -268, -644, -450, 180, 206, AnyLevel, Map + "New York City: Subway System (23rd St line)"),
            Of(14, -14, 55, 186, 254, 178, 206, AnyLevel, Map + "New York City: Subway System (open crater)"),
            Of(11, -319, -250, -712, -644, 178, 204, AnyLevel, Map + "A.F.S. Womhole E23 (wormhole pit, 23rd St line)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20204, 5, 1, -302.5, 222, -519, 40, 0, 0, -2000, 2000, 2, "earth_unitedstates_manhattan_01 - 23rd Street Station",
                Of(5, -336, -268, -556, -450, 180, 206, AnyLevel, Map + "23rd Street Station (platforms and base)")),
            new(20205, 12, 1, -2.6, 209.4, -10.2, 78.6, 0, 0, -2000, 2000, 2, "earth_unitedstates_manhattan_01 - 28th Street Station",
                Of(12, -56, 40, -66, 66, 178, 206, UndergroundOnly, Map + "28th Street Station (platforms, stairs)")),
            new(20206, 13, 1, -1.8, 209.1, 302.1, 47.9, 0, 0, -2000, 2000, 2, "earth_unitedstates_manhattan_01 - 33rd Street Station",
                Of(13, -44, 40, 258, 358, 178, 206, UndergroundOnly, Map + "33rd Street Station (platforms, stairs)")),
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
