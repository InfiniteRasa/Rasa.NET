using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Outpost Intrepid's and Khulago Base's interiors and the east end of the cave behind the
    /// Staal Overlook on Burning Steps (adv_arieki_ligo_burningsteps, 1993;
    /// Burning_steps_interior_regions).
    ///
    /// - Region 4, "Outpost Intrepid" (sky_foreas_general_human_underground, env_human_interior,
    ///   REGION_GENERIC_ROOM_TONE_HEAVY, MUSIC_CRUCIBLE_06; priority 10), is an underground AFS
    ///   base (arch_hum_corr_room_*, arch_hum_corridor_*), all of it underground on the navmesh:
    ///   the hall with the waypoint, wormhole, shops and catwalks (x 918 to 1008, z 20 to 100,
    ///   floor 140 to 145), the east wing with the vendors and the crafting station, the
    ///   corridor south to the dropship pad, and the corridors north past the hospital (987.6,
    ///   144.9, 108) and up the ramps to the north exit at (1048, 484). Its label circle (200 m
    ///   round the label at (942, 152, 181), surface only) reached none of it and put the
    ///   underground sky and room tone on the lava fields over it. The circle becomes the hall's
    ///   box and the rest of the base gets five more, all underground-only, so the ground over
    ///   them and the exits' mouths stay outdoors.
    /// - Region 31, "Khulago Base" (sky_arieki_brann_interior_default,
    ///   env_bane_interior_tunnel_little_lights, REGION_BANE_TUNNEL_01; priority 2), is Khulago's
    ///   interior; region 7 (priority 13, the Torden Mires sky) is its outdoors. Its label circle
    ///   (the same 200 m as region 7's, surface only) changed nothing indoors and played the
    ///   tunnel ambience across the open base. It becomes a box over the base's Bane tunnels
    ///   south-west of it: the service entrance at (258, 156, -370), its ramps down, the tunnel
    ///   cross with the stacked crates and barrels and the control room at its end
    ///   (arch_bane_industrial_*, x 80 to 260, z -412 to -328, floor 140). The second Bane room
    ///   pair, x -180 to 20, z -328 to -244 at 200, with its sleep pods, extraction sensors and
    ///   bio research table, is the "Khulago Research Laboratory", "a laboratory inside Khulago
    ///   Base", that Genome Sweet Genome sends you into; no path on the navmesh reaches it from
    ///   outside. It gets a box of region 31 too. Both are underground-only.
    /// - Region 23, "Prometheus Outpost" (sky_arieki_ligo_crucible_magma_caven,
    ///   env_arieki_cave_interior; priority 29), is the cave behind the Staal Overlook
    ///   (terra_arieki_cavern_rocky_*) that leads to the Eloh door. Its minimap rectangle stops at
    ///   x 77.5; the cave's east split (x 64 to 88, z 77 to 107) runs past it. A box of region 23
    ///   takes the rest, underground-only.
    ///
    /// Not placed: region 14 (the Torden Mires sky and env map and a blank name; priority 21),
    /// which nothing ties to one place, and region 1 (Molder.sky; priority 1).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. New rows go in only where the map is loaded
    /// and no box of that region is at that spot; the circles change only while they are as
    /// seeded. Down puts them back and takes the new rows out.
    /// </summary>
    public static class BurningStepsInteriorRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>A seeded label circle (surface-only, at any height) and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, double X, double Y, double Z, double Radius, string Comment, Volume Box);

        public const uint MapContextId = 1993;
        public const uint OutpostIntrepidRegion = 4, KhulagoInteriorRegion = 31, OverlookCaveRegion = 23;
        public const byte Circle = 1, Box = 2, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "arieki_ligo_burningsteps - ";

        public static readonly Volume[] Volumes =
        {
            Of(OutpostIntrepidRegion, 1008, 1046, 38, 82, 143, 151, Map + "Outpost Intrepid (east wing)"),
            Of(OutpostIntrepidRegion, 954, 982, -32, 20, 143, 158, Map + "Outpost Intrepid (south corridor)"),
            Of(OutpostIntrepidRegion, 940, 1008, 100, 210, 143, 166, Map + "Outpost Intrepid (north corridors)"),
            Of(OutpostIntrepidRegion, 1008, 1056, 190, 310, 150, 168, Map + "Outpost Intrepid (north-east corridors)"),
            Of(OutpostIntrepidRegion, 1010, 1086, 310, 496, 158, 182, Map + "Outpost Intrepid (corridors to the north exit)"),
            Of(KhulagoInteriorRegion, -180, 20, -328, -244, 197, 230, Map + "Khulago Base (research laboratory)"),
            Of(OverlookCaveRegion, 76, 89, 76, 108, 218, 238, Map + "Prometheus Outpost (the cave's east end)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(221, OutpostIntrepidRegion, 942.0, 152.0, 180.7, 200, Map + "Outpost Intrepid",
                Of(OutpostIntrepidRegion, 918, 1008, 20, 100, 138, 163, Map + "Outpost Intrepid (hall)")),
            new(220, KhulagoInteriorRegion, 382.8, 174.0, -187.8, 200, Map + "Khulago Base",
                Of(KhulagoInteriorRegion, 80, 260, -412, -328, 136, 172, Map + "Khulago Base (Bane tunnels)")),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, UndergroundOnly, comment);

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
                    $"WHERE id = {r.RowId} AND map_context_id = {MapContextId} AND region_id = {r.RegionId} AND shape = {Circle} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01 " +
                    $"AND min_y = -2000 AND max_y = 2000 AND underground = {SurfaceOnly};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {Circle}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = 0, half_z = 0, min_y = -2000, max_y = 2000, underground = {SurfaceOnly}, comment = {S(r.Comment)} " +
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
