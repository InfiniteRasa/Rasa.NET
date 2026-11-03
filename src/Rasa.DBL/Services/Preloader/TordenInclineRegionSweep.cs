using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Torden Incline (adv_arieki_torden_incline, 1761;
    /// 20270724030000_Torden_incline_region_sweep).
    ///
    /// Region 24 (Brann interior): the map's only Brann interior is the corridor from
    /// arch_brann_corridor_entrance_01 at (5, 233, -291) down two turning ramps to the instance door at
    /// (-43, 209, -307). It is fitted out with Brann lights, crates and carts, and its floor is flagged
    /// underground. Region 24's settings are exactly the Brann interior sky and env map. One
    /// underground-only box. Probable.
    ///
    /// Region 26 (Eloh interior, Eloh music): the two Eloh interiors on the map. The west shrine is the
    /// arch_eloh_door at (-752, 234, -282) with a ramp down to an Eloh corridor room (floor 218,
    /// obelisk, temple obstacles). The east one is an Eloh corridor tee with Logos dispensers and signs
    /// at x 1120..1191, z 380..420, at the end of the east rocky cave. Underground-only boxes.
    /// Probable.
    ///
    /// Region 22 (short-cave sky, cave env): the incline's only caves are the rocky cave-kit tunnels,
    /// all flagged underground. The main one is the east tunnel network beside Pinnacle Badlands (two
    /// boxes, probable). Two short caves end at instance doors south of Plains Post and Mires Post (one
    /// box each, medium).
    ///
    /// Region 27 (Wardenbot factory sky only): the Raksha factory entrance
    /// (arch_wardenbot_corridor_entrance_01 at (366, 237, -292)) is the only Wardenbot structure on the
    /// map. Its two entry corridors to the instance doors get one box. Medium: the region may have
    /// covered more of the approach.
    ///
    /// Region 12 (recut of minimap row 144): the underground Bane complex (tunnels, control rooms and
    /// chunnel rooms at y 246-293, flagged underground) inside the rect. The rect row applied at any
    /// level, so the open badlands above it, including Control Point: Badlands, showed the Bane
    /// interior sky, env, ambience and music. Recut to underground-only, y 240-300.
    ///
    /// Not placed: 6, 7, 8 (three identical human-interior regions, too many candidate tents), 2 (far-
    /// fog battlefield look, no place), 4 (wider Pinnacle Badlands, no extent). Regions 1 (Molder.sky)
    /// and 28 (marker-only) skipped.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class TordenInclineRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 1761;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "arieki_torden_incline - ";

        public static readonly Volume[] Volumes =
        {
            Of(24, -52, 20, -348, -279, 205, 240, UndergroundOnly, Map + "region 24 (Brann corridor down to instance door)"),
            Of(26, -880, -755, -321, -243, 214, 242, UndergroundOnly, Map + "region 26 (Eloh shrine room, west)"),
            Of(26, 1120, 1192, 379, 421, 290, 306, UndergroundOnly, Map + "region 26 (Eloh corridor at the east cave's end)"),
            Of(22, 974, 1050, 288, 500, 270, 325, UndergroundOnly, Map + "region 22 (east rocky cave tunnels)"),
            Of(22, 1048, 1122, 384, 424, 285, 310, UndergroundOnly, Map + "region 22 (east cave branch to the Eloh room)"),
            Of(22, 84, 162, -410, -376, 218, 245, UndergroundOnly, Map + "region 22 (rocky cave to instance, south)"),
            Of(22, -378, -343, -574, -498, 210, 235, UndergroundOnly, Map + "region 22 (rocky cave to instance, Mires Post)"),
            Of(27, 345, 378, -292, -250, 234, 250, AnyLevel, Map + "region 27 (Wardenbot factory entry corridor)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(144, 12, 2, 417, 304.6, 575, 0, 159, 211, -2000, 2000, 0, "arieki_torden_incline - region 12 minimap",
                Of(12, 258, 576, 364, 786, 240, 300, UndergroundOnly, Map + "region 12 (underground Bane complex)")),
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
