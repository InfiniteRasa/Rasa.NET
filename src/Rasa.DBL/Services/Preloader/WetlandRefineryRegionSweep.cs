using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Wetland Refinery (adv_foreas_valverde_marshes_wetlandrefinery,
    /// 1429; 20270719010000_Wetland_refinery_region_sweep).
    ///
    /// Turpis Refinery is an underground Bane plant (all interior navmesh flagged underground) under a
    /// surface terrain sheet at y 158, with one open clearing to the south-west.
    ///
    /// Chemical Processing (19): the seeded circle at the west label was surface-only, so it never
    /// applied in the flagged interior. Recut to an underground box over the west refinery dome
    /// (arch_bane_industrial_chunnel_room_refinery_v01, x -352..-224, z -24..130) and added the mirror
    /// east dome (x 40..168, z -56..98), where the region's second label sits.
    ///
    /// Processing Power Supply (8, 9): both rows were surface-only r200 circles at the east label; they
    /// missed the interior and covered the terrain above. Region 8 now covers the east control room
    /// (arch_bane_industrial_room_control_24m_v07 at 104,-96 plus tunnel caps), region 9 the mirror
    /// west room (-288,-64) where the second label is.
    ///
    /// Region 27 (Eloh sky/env, Eloh ruins ambient and music): the Eloh corridor room with the Logos
    /// dispenser (x 4..53, z -249..-199) and its coupler; mission text places a Logos shrine inside
    /// Turpis.
    ///
    /// Experimentation Control Room (15): the 'Experimentation' map label is in the control room v05
    /// with the information-extraction room at (-96,176); the box stops before the extraction-chair
    /// exam rooms.
    ///
    /// Refinery Barracks (10): the two chunnels full of sleep pods, upper (x -141..-83, y 122..146) and
    /// lower (x -109..-49, y 91..104).
    ///
    /// LZ X-Ray (7): outdoor settings; the only open-air area is the tree-filled clearing with the AFS
    /// tent, medical unit and Turpis Base Camp label, matching the zone text's wilderness clearing
    /// where the AFS team set down. Two boxes cover it, excluding the cave tunnel and the terrain
    /// above.
    ///
    /// Region 16 (cave sky, Fithik cave tunnel ambient, same music as LZ X-Ray): the
    /// terra_foreas_cavern tunnel off the clearing's north-west corner, underground-only. Medium
    /// confidence: the Eloh cave passage is the other cave on the map.
    ///
    /// Not placed: 11 (third copy of the power-supply label; its dead circle left as is), 4 Toxin
    /// Processing, 5 Chemical Research, 6 Chemical Storage, 12, 13, 14, 2, 3, Bane Entrance, the Exam
    /// Room and Observation name-only regions (rooms identified but numbering unknown), and 1
    /// (DefaultSky).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class WetlandRefineryRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 1429;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_valverde_marshes_wetlandrefinery - ";

        public static readonly Volume[] Volumes =
        {
            Of(19, 40, 168, -56, 98, 90, 150, UndergroundOnly, Map + "Chemical Processing (east refinery dome)"),
            Of(27, -24, 54, -250, -198, 105, 130, UndergroundOnly, Map + "region 27 (Eloh Logos shrine room)"),
            Of(15, -137, -80, 142, 210, 95, 128, UndergroundOnly, Map + "Experimentation Control Room (north control room)"),
            Of(10, -141, -83, -41, 57, 120, 147, UndergroundOnly, Map + "Refinery Barracks (upper sleep-pod chunnel)"),
            Of(10, -109, -49, -73, 25, 88, 104, UndergroundOnly, Map + "Refinery Barracks (lower sleep-pod chunnel)"),
            Of(7, -300, -130, -310, -207, 100, 150, AnyLevel, Map + "LZ X-Ray (open clearing, south part)"),
            Of(7, -231, -130, -207, -145, 100, 150, AnyLevel, Map + "LZ X-Ray (open clearing, north part)"),
            Of(16, -306, -232, -207, -148, 105, 165, UndergroundOnly, Map + "region 16 (cave tunnel off the clearing)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(82, 19, 1, -286, 111.1, 61.3, 200, 0, 0, 60.4, 149.4, 2, "foreas_valverde_marshes_wetlandrefinery - Chemical Processing",
                Of(19, -352, -224, -24, 130, 90, 150, UndergroundOnly, Map + "Chemical Processing (west refinery dome)")),
            new(79, 8, 1, 103.5, 106.9, -96.4, 200, 0, 0, -2000, 2000, 2, "foreas_valverde_marshes_wetlandrefinery - Processing Power Suppl",
                Of(8, 56, 152, -144, -56, 100, 135, UndergroundOnly, Map + "Processing Power Supply (east control room)")),
            new(80, 9, 1, 103.5, 106.9, -96.4, 200, 0, 0, -2000, 2000, 2, "foreas_valverde_marshes_wetlandrefinery - Processing Power Suppl",
                Of(9, -336, -240, -112, -24, 100, 135, UndergroundOnly, Map + "Processing Power Supply (west control room)")),
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
