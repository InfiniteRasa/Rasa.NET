using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Valverde Plateau (adv_foreas_valverde_plateau, 1497;
    /// 20270722000000_Valverde_plateau_region_sweep).
    ///
    /// Fort Defiance (region 1): mission text puts the fort underground in caverns; it is a cavern base
    /// (fort HQ, hospital, barracks, shops, tavern, auction house) under a 500 x 280 cavern roof, with
    /// the label and bunkers on the surface above the gate. The seeded surface-only circle (r 200 round
    /// the label) missed the navmesh-underground gate passage and west wall, the north annex rooms and
    /// everything beyond 200 (the Criminal Investigations Division office, both ends), which showed
    /// 'Valverde Plateau'. Added four boxes: the cavern (385..445), the gate passage, the west cave
    /// tunnel in and the north annex; the circle stays for the surface.
    ///
    /// Region 21 (unnamed, Fort Defiance's sky with the large-open human base ambient, one priority
    /// above Fort Defiance): the cavern hall of the fort, same box as the cavern; the surface above
    /// keeps Fort Defiance's distant-battle ambient.
    ///
    /// Region 38 (Eloh interior sky/env with cave echo and MUSIC_ELOH_01): the two Eloh corridor
    /// shrines inside caves, at the end of the cave west of Fort Defiance (corridor cross with a logos
    /// dispenser) and under the Maligo front (corridor straight with a logos dispenser). Region 39
    /// (cave_short, id next to 38, medium): the cave passages leading to those two shrines.
    ///
    /// Maligo Creek (region 16): the outdoor stretches of the creek on the floor of Martyr's Canyon
    /// (river pieces, pools and a swamp pond at 286..342), either side of the creek's cave (region 19).
    /// Below Martyr's Canyon's terraces and label.
    ///
    /// Derak Prison (region 4, recut): its minimap rect had no height and put the Bane interior sky
    /// over the open ground above the underground prison, and stopped short of the entrance tunnel
    /// where its label stands. Limited to y 300..338 and extended to the entrance.
    ///
    /// Region 2 (recut): the minimap rect of the cave under Velon Das Ruins did not reach its two
    /// tunnel ends; extended to both, still underground-only, below the village.
    ///
    /// Not placed: region 25 (cave_far; candidate caves listed), regions 13, 14 and 29 (outdoor
    /// battlefield zones with no identifiable extent).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class ValverdePlateauRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 1497;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_valverde_plateau - ";

        public static readonly Volume[] Volumes =
        {
            Of(1, -340, 85, 757, 991, 385, 445, AnyLevel, Map + "Fort Defiance (cavern base)"),
            Of(1, -166, -74, 720, 757, 385, 435, AnyLevel, Map + "Fort Defiance (main gate passage)"),
            Of(1, -195, -122, 991, 1076, 390, 430, AnyLevel, Map + "Fort Defiance (north annex rooms)"),
            Of(1, -250, -184, 722, 757, 400, 445, UndergroundOnly, Map + "Fort Defiance (west cave tunnel into the fort)"),
            Of(21, -340, 85, 757, 991, 385, 445, AnyLevel, Map + "region 21 (Fort Defiance cavern hall ambient)"),
            Of(38, -465, -420, 821, 888, 372, 395, UndergroundOnly, Map + "region 38 (Eloh shrine, cave west of the fort)"),
            Of(38, -133, -107, -369, -318, 400, 422, UndergroundOnly, Map + "region 38 (Eloh shrine under Maligo front)"),
            Of(39, -468, -418, 708, 821, 365, 402, UndergroundOnly, Map + "region 39 (cave to the west Eloh shrine)"),
            Of(39, -130, -108, -396, -369, 396, 420, UndergroundOnly, Map + "region 39 (cave ramp to the Maligo shrine)"),
            Of(16, -640, 215, -850, -690, 280, 342, AnyLevel, Map + "Maligo Creek (creek bed, Martyr's Canyon floor)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(101, 4, 2, 159, 0, -228, 0, 78.5, 38, -2000, 2000, 0, "foreas_valverde_plateau - Derak Prison",
                Of(4, 80, 260, -266, -190, 300, 338, AnyLevel, Map + "Derak Prison (chunnel prison, not ground above)")),
            new(100, 2, 2, -500, 334.1, 44, 0, 67, 49, -2000, 2000, 1, "foreas_valverde_plateau - region 2 minimap",
                Of(2, -595, -430, -32, 122, 265, 386, UndergroundOnly, Map + "region 2 (cave under Velon Das, both mouths)")),
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
