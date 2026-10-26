using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Phanin Research Facility's rooms and levels (adv_arieki_torden_plains_penalresearch,
    /// 2034; Phanin_research_regions).
    ///
    /// A Brann complex in two layers: the way in, the warehouse and three rooms on floors -8 to -24,
    /// and the laboratories, the mind control rooms and Phanin's own rooms on floors -32 to -48 under
    /// them. Every named region only sets the name. The missions describe the place, not where its
    /// rooms are ("four Security Mainframes, three Research Mainframes, and two Support Mainframes";
    /// research consoles; "medical crates in the laboratories"), so the map's labels and props place
    /// them:
    ///
    /// - Security Alpha, Beta, Gamma and Delta (13 to 16): the four matching guard rooms (a room with
    ///   its control room, two stampers and catwalk stairs), one under each label: Alpha south of the
    ///   way in (floor -16), Beta north-west (-24), Gamma south-west and Delta west (-40).
    /// - Phanin Warehouse (3): the four quarter rooms in the middle (floor -24), with the crates,
    ///   lockers and storage racks, under its label.
    /// - Atta Mind Control Room (4): the two rooms with the four Atta mind control consoles and the
    ///   cages, floor -48, under "Main Laboratory" and "Mind Control Device".
    /// - Mind Control Power Plant (6): the quarter rooms with the four geothermal generators, floor
    ///   -48, at "Power Generators".
    /// - Mind Control Transmission Center (10): the north quarter rooms with the other four mind
    ///   control consoles, floor -40, at "Communications Center".
    /// - Phanin's Private Quarters (7): the dormitory beside it with its two beds and a pedestal,
    ///   under "Phanin's Quarters".
    /// - Neural Lab (5) and Neuron Processing (17): the two rooms marked "Research Laboratory".
    ///   Nothing on the map or in the missions tells them apart; the one with sixteen Atta
    ///   dissection tables in rows, floor -32, is the Neural Lab, and the one with two tables, the
    ///   specimen cages and a microscope, floor -48, is Neuron Processing.
    /// - Region 18 (an Atta cave sky and environment map, no name): the Atta cave with the two
    ///   Atta queen pieces (arch_arieki_atta_queens_obj01), x 27 to 85, z -462 to -298, apart from the facility and not joined to it.
    ///
    /// Security Center (2) is not placed: nothing names or marks it; the two guard dormitories
    /// (floors -16 and -24) have no region.
    ///
    /// The label circles of the four security rooms and the warehouse (60 m, for the surface, which
    /// the navmesh says all of this is) reached across the floors above and below and into the
    /// rooms round them; they are recut to their rooms. The two minimap layers (regions 11 and 12)
    /// stood at any height, so the lower layer's won everywhere; their rectangles fit the two layers
    /// (11 ends at Security Beta and Security Alpha, 12 reaches Security Delta and the transmission
    /// center), and they get those heights: 11 down to -28, 12 below it.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. New rows go in only where the map is loaded and
    /// no box of that region is at that spot; recut rows change only while they are as seeded.
    /// Down puts every seeded row back and takes the new ones out.
    /// </summary>
    public static class PhaninResearchRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        /// <summary>A seeded row as it is (circle or rectangle) and the volume it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ,
            double MinY, double MaxY, byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2034;
        public const uint WarehouseRegion = 3, MindControlRoomRegion = 4, NeuralLabRegion = 5, PowerPlantRegion = 6, QuartersRegion = 7,
            TransmissionRegion = 10, AlphaRegion = 13, BetaRegion = 14, GammaRegion = 15, DeltaRegion = 16, NeuronProcessingRegion = 17,
            AttaCaveRegion = 18;
        public const byte Circle = 1, Box = 2, AnyLevel = 0;

        private const string Map = "arieki_torden_plains_penalresearch - ";

        public static readonly Volume[] Volumes =
        {
            Of(MindControlRoomRegion, -122, -38, -6, 46, -50, -29, Map + "Atta Mind Control Room (two rooms)"),
            Of(PowerPlantRegion, -28, 34, 5, 66, -50, -33, Map + "Mind Control Power Plant (generator rooms)"),
            Of(TransmissionRegion, -88, -26, 62, 128, -42, -25, Map + "Mind Control Transmission Center (north rooms)"),
            Of(QuartersRegion, -117, -89, 60, 97, -41, -23, Map + "Phanin's Private Quarters (dormitory)"),
            Of(NeuralLabRegion, -48, 16, -87, -23, -34, -19, Map + "Neural Lab (dissection tables)"),
            Of(NeuronProcessingRegion, -136, -72, -103, -40, -50, -33, Map + "Neuron Processing (cages and microscope)"),
            Of(AttaCaveRegion, 22, 90, -465, -295, -30, 5, Map + "Atta cave"),
        };

        public static readonly Recut[] Recuts =
        {
            new(238, AlphaRegion, Circle, -54.7, -16.0, -100.1, 60, 0, 0, -2000, 2000, 2, Map + "Security Alpha",
                Of(AlphaRegion, -82, -30, -118, -74, -18, 4, Map + "Security Alpha (guard room)")),
            new(239, BetaRegion, Circle, -180.1, -24.0, 56.9, 60, 0, 0, -2000, 2000, 2, Map + "Security Beta",
                Of(BetaRegion, -198, -154, 30, 82, -26, -5, Map + "Security Beta (guard room)")),
            new(240, GammaRegion, Circle, -179.6, -37.2, -79.8, 60, 0, 0, -2000, 2000, 2, Map + "Security Gamma",
                Of(GammaRegion, -206, -154, -99, -54, -42, -21, Map + "Security Gamma (guard room)")),
            new(241, DeltaRegion, Circle, -223.2, -40.0, -12.0, 60, 0, 0, -2000, 2000, 2, Map + "Security Delta",
                Of(DeltaRegion, -243, -198, -38, 14, -42, -21, Map + "Security Delta (guard room)")),
            new(242, WarehouseRegion, Circle, -90.7, -23.5, -12.9, 60, 0, 0, -2000, 2000, 2, Map + "Phanin Warehouse",
                Of(WarehouseRegion, -121, -57, -43, 21, -26, -9, Map + "Phanin Warehouse (quarter rooms)")),
            Layer(236, 11, -87.0, -21.1, -21.0, 114.0, 103.0, -28, 8),
            Layer(237, 12, -107.0, -21.1, 11.0, 145.0, 116.5, -56, -28),
        };

        /// <summary>A minimap layer's seeded rectangle, kept but given its level's height.</summary>
        private static Recut Layer(uint row, uint region, double x, double y, double z, double halfX, double halfZ, double floor, double roof)
        {
            var comment = Map + $"region {region} minimap";
            return new(row, region, Box, x, y, z, 0, halfX, halfZ, -2000, 2000, 0, comment,
                new(region, x, y, z, halfX, halfZ, floor, roof, comment));
        }

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
                    $"WHERE id = {r.RowId} AND map_context_id = {MapContextId} AND region_id = {r.RegionId} AND shape = {r.Shape} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01 " +
                    $"AND ABS(half_x - {N(r.HalfX)}) < 0.01 AND ABS(half_z - {N(r.HalfZ)}) < 0.01 AND min_y = {N(r.MinY)} AND max_y = {N(r.MaxY)};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {r.Shape}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = {N(r.HalfX)}, half_z = {N(r.HalfZ)}, min_y = {N(r.MinY)}, max_y = {N(r.MaxY)}, underground = {r.Underground}, comment = {S(r.Comment)} " +
                    $"WHERE id = {r.RowId} AND {At(r.Box, "map_region")} AND ABS(half_x - {N(r.Box.HalfX)}) < 0.01 AND ABS(half_z - {N(r.Box.HalfZ)}) < 0.01 " +
                    $"AND min_y = {N(r.Box.MinY)} AND max_y = {N(r.Box.MaxY)};";

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
