using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The four Eloh Temples (adv_foreas_concordia_palisades_elohtemples, 1803;
    /// Eloh_temples_regions).
    ///
    /// The map is four separate Eloh temples, each reached from Palisades by its own map link
    /// through a short cave mouth (terra_foreas_cavern_instance) and an Eloh door into an entry
    /// room. Each temple has an interior region (an Eloh temple sky) and a cave region
    /// (sky_foreas_cave_tunnel); which temple is which the hospitals say, each in its temple's
    /// entry room and named "Temple of the ... Patriarch Entrance":
    ///
    /// - Temple of the Raging Patriarch (east, hospital at (-530.8, 27.7, -209.8)): the entry room,
    ///   then three body rooms (arch_eloh_temples_room_body) down a stair of ramps from y 8 to
    ///   -24, to the end room at z 428: x -584 to -440, z -228 to 477. Interior region 3.
    /// - Temple of the Proud Patriarch (middle, hospital at (-760.2, 27.2, -272.4)): the entry
    ///   room, two mind rooms (arch_eloh_temples_room_mind) and the three side rooms off their
    ///   tees, with the logos signs: x -893 to -587, z -296 to 217. Interior region 4.
    /// - Temple of the Bowed Patriarch (west, hospital at (-1012.2, 27.4, -251.1)): the entry room,
    ///   the spirit room (arch_eloh_temples_room_spirit, with its floor plates and the map's one
    ///   temple fog, arch_eloh_temples_room_spirit_fog) and the end room past it. The temple has
    ///   two interior regions: 11, with the far-fog sky, is the spirit room; 5 the rest.
    /// - Temple of the Patriarch Protector (north-west, the fourth map link, no hospital): one room
    ///   (arch_eloh_corridor_room_v04 at (-992, 24, 274), with its projector). Interior region 6.
    ///
    /// The cave regions (7, 8, 9 and 10) are the cave mouths from each map link to the Eloh door.
    /// The first three had 120 m circles from MapRegionMarkerPreloader, at every height, round
    /// the hospitals; those become the cave mouths. The Protector's (10) had none.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is
    /// loaded and no box of that region is at that spot; a recut row changes only while it is
    /// the circle as seeded. Down takes out what went in and gives the circles back.
    /// </summary>
    public static class ElohTemplesRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        /// <summary>A seeded circle and the box it becomes.</summary>
        public sealed record Recut(uint RowId, double X, double Y, double Z, double Radius, string Comment, Volume Box);

        public const uint MapContextId = 1803;
        public const uint RagingRegion = 3, ProudRegion = 4, BowedRegion = 5, ProtectorRegion = 6,
            RagingCaveRegion = 7, ProudCaveRegion = 8, BowedCaveRegion = 9, ProtectorCaveRegion = 10, BowedSpiritRegion = 11;
        public const byte Circle = 1, Box = 2, AnyLevel = 0;

        private const string Map = "foreas_concordia_palisades_elohtemples - ";

        public static readonly Volume[] Volumes =
        {
            Of(RagingRegion, -585, -439, -229, 478, -26, 52, Map + "Temple of the Raging Patriarch (temple)"),
            Of(ProudRegion, -894, -586, -297, 218, -10, 52, Map + "Temple of the Proud Patriarch (temple)"),
            Of(BowedRegion, -1018, -966, -277, -182, 5, 45, Map + "Temple of the Bowed Patriarch (entry room)"),
            Of(BowedRegion, -1018, -966, -35, 62, -12, 15, Map + "Temple of the Bowed Patriarch (end room)"),
            Of(BowedSpiritRegion, -1066, -918, -182, -35, -18, 53, Map + "Temple of the Bowed Patriarch (spirit room)"),
            Of(ProtectorRegion, -1037, -947, 194, 279, 20, 48, Map + "Temple of the Patriarch Protector (temple)"),
            Of(ProtectorCaveRegion, -1009, -981, 132, 195, 18, 36, Map + "Temple of the Patriarch Protector (cave mouth)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20101, -530.8, 27.7, -209.8, 120.0, Map + "Temple of the Raging Patriarch",
                Of(RagingCaveRegion, -524, -500, -266, -228, 18, 36, Map + "Temple of the Raging Patriarch (cave mouth)")),
            new(20102, -760.2, 27.2, -272.4, 118.9, Map + "Temple of the Proud Patriarch",
                Of(ProudCaveRegion, -752, -728, -334, -296, 18, 36, Map + "Temple of the Proud Patriarch (cave mouth)")),
            new(20103, -1012.2, 27.4, -251.1, 120.0, Map + "Temple of the Bowed Patriarch",
                Of(BowedCaveRegion, -1004, -980, -314, -276, 18, 36, Map + "Temple of the Bowed Patriarch (cave mouth)")),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {AnyLevel}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            foreach (var r in Recuts)
                yield return $"UPDATE map_region SET shape = {Box}, pos_x = {N(r.Box.X)}, pos_y = {N(r.Box.Y)}, pos_z = {N(r.Box.Z)}, radius = 0, " +
                    $"half_x = {N(r.Box.HalfX)}, half_z = {N(r.Box.HalfZ)}, min_y = {N(r.Box.MinY)}, max_y = {N(r.Box.MaxY)}, underground = {AnyLevel}, comment = {S(r.Box.Comment)} " +
                    $"WHERE id = {r.RowId} AND map_context_id = {MapContextId} AND region_id = {r.Box.RegionId} AND shape = {Circle} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01;";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {Circle}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = 0, half_z = 0, min_y = -2000, max_y = 2000, underground = {AnyLevel}, comment = {S(r.Comment)} " +
                    $"WHERE id = {r.RowId} AND {At(r.Box, "map_region")} AND ABS(half_x - {N(r.Box.HalfX)}) < 0.01 AND ABS(half_z - {N(r.Box.HalfZ)}) < 0.01;";

            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)};";
        }

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
