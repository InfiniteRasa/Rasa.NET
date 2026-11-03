using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Caves of Donn's places (adv_foreas_concordia_wilderness_cavesofdonn02, 1506), given
    /// the boxes the epic copy got, and the epic copy's caverns widened to their floor
    /// (adv_foreas_concordia_wilderness_cavesofdonn_epic, 2368; Caves_of_donn_regions).
    ///
    /// The two maps are the same architecture piece for piece and the same regions
    /// (CavesOfDonnEpicRegions has where each is). The epic copy's rows are its own map's, so
    /// nothing of it reached this one: Caves of Donn still had only its four label circles and
    /// the three minimap rectangles. The circles were surface-only, but this map's navmesh has
    /// from two fifths to three fifths of the floors of Spirit Cave, the Chantry cavern and
    /// Undone Fairway under the ground (the epic copy's has them above it), so there they named
    /// nothing; where they did reach, Spirit
    /// Cave's ran 20 m into the Chantry cavern and across to the west cavern's edge, the
    /// Chantry's left the north of its cavern (the way up to Spirit Cave) out, and Mar Fend's left
    /// the south of the village out.
    ///
    /// - The four circles become the epic copy's boxes, at any level: Mar Fend Village
    ///   (x -400 to -254, z -345 to -125), Spirit Cave (x -230 to -156, z -196 to -95, and its
    ///   west bay x -242 to -230, z -150 to -128), Eloh Chantry (x -236 to -118, z -370 to -200)
    ///   and Undone Fairway (x -56 to 90, z -318 to -192).
    /// - New, as on the epic copy: the Vitiated Shrine (region 4), the hidden Forean-Eloh chamber
    ///   (region 8), the Eloh corridor by the entrance (region 15), the three tunnels of region 2
    ///   and the shrine tunnel of region 16. Here the tunnels' floors are partly above the ground,
    ///   so their boxes are at any level, and the shrine tunnel's starts at the Fairway's edge
    ///   (x 90) so as not to take the cavern's mouth.
    /// - On the epic copy, the floor walked past four of its boxes' edges: Mar Fend's west (to
    ///   -400) and east (to -254) ends, Spirit Cave's east end (to -156) and its west bay, and the
    ///   Chantry's east end (to -118). Those boxes are widened and the bay is added there too.
    ///
    /// Regions 1, 10, 11 and 14 stay unplaced on both maps, and region 9's entrance rectangle
    /// stays as it is.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. New rows go in only where the map is loaded
    /// and no box of that region is at that spot; the circles and the epic boxes change only
    /// while they are as seeded or as Caves_of_donn_epic_regions left them. Down puts every one
    /// back and takes the new rows out.
    /// </summary>
    public static class CavesOfDonnRegions
    {
        public sealed record Volume(uint MapContextId, uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment);

        /// <summary>A seeded label circle (surface-only, at any height) and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, double X, double Y, double Z, double Radius, string Comment, Volume Box);

        /// <summary>An epic box as Caves_of_donn_epic_regions left it and as it becomes.</summary>
        public sealed record Resize(Volume Was, Volume Box);

        public const uint Donn = 1506, Epic = 2368;
        public const uint TunnelRegion = 2, FairwayRegion = 3, ShrineRegion = 4, ChantryRegion = 5, MarFendRegion = 6, SpiritCaveRegion = 7,
            ChamberRegion = 8, CorridorRegion = 15, ShrineTunnelRegion = 16;
        public const byte Circle = 1, Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string DonnMap = "foreas_concordia_wilderness_cavesofdonn02 - ";
        private const string EpicMap = "foreas_concordia_wilderness_cavesofdonn_epic - ";

        public static readonly Volume[] Volumes =
        {
            Of(Donn, SpiritCaveRegion, -242, -230, -150, -128, -10, 40, AnyLevel, DonnMap + "Spirit Cave (its west bay)"),
            Of(Donn, ShrineRegion, 113, 226, -355, -249, -10, 45, AnyLevel, DonnMap + "Vitiated Shrine (the round pit)"),
            Of(Donn, ChamberRegion, 224, 306, -330, -274, -5, 30, AnyLevel, DonnMap + "region 8 (the hidden Forean-Eloh chamber)"),
            Of(Donn, CorridorRegion, -330, -284, -372, -346, 18, 40, AnyLevel, DonnMap + "region 15 (the Eloh corridor by the entrance)"),
            Of(Donn, TunnelRegion, -325, -228, -127, -90, -10, 45, AnyLevel, DonnMap + "region 2 (tunnel, west cavern to Spirit Cave)"),
            Of(Donn, TunnelRegion, -212, -186, -216, -191, -10, 40, AnyLevel, DonnMap + "region 2 (tunnel, Spirit Cave to the Chantry)"),
            Of(Donn, TunnelRegion, -118, -56, -314, -270, -10, 40, AnyLevel, DonnMap + "region 2 (tunnel, Chantry to Undone Fairway)"),
            Of(Donn, ShrineTunnelRegion, 90, 116, -312, -228, -10, 40, AnyLevel, DonnMap + "region 16 (tunnel, Undone Fairway to the shrine)"),
            Of(Epic, SpiritCaveRegion, -242, -230, -150, -128, -10, 40, AnyLevel, EpicMap + "Spirit Cave (its west bay)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(121, FairwayRegion, 3.8, 21.1, -235.7, 94, DonnMap + "Undone Fairway",
                Of(Donn, FairwayRegion, -56, 90, -318, -192, -10, 45, AnyLevel, DonnMap + "Undone Fairway (the east cavern)")),
            new(122, ChantryRegion, -160.0, 36.0, -328.0, 93, DonnMap + "Eloh Chantry",
                Of(Donn, ChantryRegion, -236, -118, -370, -200, -10, 45, AnyLevel, DonnMap + "Eloh Chantry (the central cavern)")),
            new(123, SpiritCaveRegion, -191.0, 6.0, -145.0, 76, DonnMap + "Spirit Cave",
                Of(Donn, SpiritCaveRegion, -230, -156, -196, -95, -10, 40, AnyLevel, DonnMap + "Spirit Cave")),
            new(124, MarFendRegion, -326.8, 33.2, -215.0, 76, DonnMap + "Mar Fend Village",
                Of(Donn, MarFendRegion, -400, -254, -345, -125, -10, 45, AnyLevel, DonnMap + "Mar Fend Village (the west cavern)")),
        };

        public static readonly Resize[] Resizes =
        {
            new(Of(Epic, MarFendRegion, -395, -258, -345, -125, -10, 45, AnyLevel, EpicMap + "Mar Fend Village (the west cavern)"),
                Of(Epic, MarFendRegion, -400, -254, -345, -125, -10, 45, AnyLevel, EpicMap + "Mar Fend Village (the west cavern)")),
            new(Of(Epic, SpiritCaveRegion, -230, -174, -193, -95, -10, 40, AnyLevel, EpicMap + "Spirit Cave"),
                Of(Epic, SpiritCaveRegion, -230, -156, -196, -95, -10, 40, AnyLevel, EpicMap + "Spirit Cave")),
            new(Of(Epic, ChantryRegion, -236, -122, -370, -200, -10, 45, AnyLevel, EpicMap + "Eloh Chantry (the central cavern)"),
                Of(Epic, ChantryRegion, -236, -118, -370, -200, -10, 45, AnyLevel, EpicMap + "Eloh Chantry (the central cavern)")),
        };

        private static Volume Of(uint map, uint region, double west, double east, double south, double north, double floor, double roof, byte underground, string comment) =>
            new(map, region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, underground, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {v.MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {v.Underground}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {v.MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            foreach (var r in Recuts)
                yield return $"UPDATE map_region SET {Set(r.Box)}, radius = 0 " +
                    $"WHERE id = {r.RowId} AND map_context_id = {Donn} AND region_id = {r.RegionId} AND shape = {Circle} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01 " +
                    $"AND min_y = -2000 AND max_y = 2000 AND underground = {SurfaceOnly};";

            foreach (var r in Resizes)
                yield return $"UPDATE map_region SET {Set(r.Box)} WHERE {Is(r.Was)};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Resizes.Reverse())
                yield return $"UPDATE map_region SET {Set(r.Was)} WHERE {Is(r.Box)};";

            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {Circle}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = 0, half_z = 0, min_y = -2000, max_y = 2000, underground = {SurfaceOnly}, comment = {S(r.Comment)} " +
                    $"WHERE id = {r.RowId} AND {Is(r.Box)};";

            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {Is(v)};";
        }

        private static string Set(Volume v) =>
            $"shape = {Box}, pos_x = {N(v.X)}, pos_y = {N(v.Y)}, pos_z = {N(v.Z)}, half_x = {N(v.HalfX)}, half_z = {N(v.HalfZ)}, " +
            $"min_y = {N(v.MinY)}, max_y = {N(v.MaxY)}, underground = {v.Underground}, comment = {S(v.Comment)}";

        private static string Is(Volume v) =>
            $"{At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 " +
            $"AND ABS(min_y - {N(v.MinY)}) < 0.01 AND ABS(max_y - {N(v.MaxY)}) < 0.01 AND underground = {v.Underground} AND comment = {S(v.Comment)}";

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {v.MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
