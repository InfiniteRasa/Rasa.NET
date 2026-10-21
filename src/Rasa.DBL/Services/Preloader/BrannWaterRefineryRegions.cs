using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Brann Water Refinery's rooms (adv_arieki_torden_plains_brannwaterrefinery, 2093;
    /// Brann_water_refinery_regions).
    ///
    /// The refinery is all under the terrain (underground on the navmesh), so the five label
    /// circles seeded for it (Central Operations, Sediment Separation Area, Skybridge, Greenhouse,
    /// Reservoir Tanks: surface only) applied nowhere; each becomes a box of the room its label
    /// stands in. The rest of its named regions, most of them its name and an ambient only, had
    /// no volumes; those whose rooms are plain from the .map get theirs:
    ///
    /// - Main Access (13): the way in, from the entrance dome off Torden Plains
    ///   (arch_brann_dome_01 at (-34, 439, -65)) down the ramp past the instance doors and the
    ///   hospital (-34.9, 423.5, -85.4) to the airlock at (2, 415, -121).
    /// - Water Collection 1 and 2 (3 and 4): the two great ore-chamber halls beyond it at
    ///   y 408 (arch_brann_corridor_room_01_orechamber, x -62 to -14), the first reached
    ///   (z -160 to -114) and the second (z -232 to -186), each under a ring of alarm lights.
    /// - Central Operations (2): the operations room at its label (x -118 to -90, z -182 to
    ///   -124), sandbagged round.
    /// - Sediment Separation Area (5): the two oval settling rooms with their stampers at its
    ///   label (arch_brann_corridor_room_01_v02, x -78 to 2, z -281 to -233, floor y 391).
    /// - Final Processing (21): the next step down: the grated cross-corridor floor at y 391
    ///   (x -138 to -74, z -321 to -273) over the machinery hall below it at y 377, its rows of
    ///   relay towers and the stampers driving through it.
    /// - Concourse Level (22): the long corridor at y 384 to 396 down its west side
    ///   (x -154 to -138, z -417 to -305), from Final Processing to the reservoir and the
    ///   south-west pump room.
    /// - Reservoir Tanks (20): the nine quarter rooms at y 383 at its label, with the Brann
    ///   reservoir tanks (x -130 to -34, z -424 to -330).
    /// - PumpStation 1 and 2 (17 and 18, REGION_HUMAN_BASE_COMMAND): the refinery's two
    ///   operations control rooms (arch_brann_corridor_room_01_operations_v02): the one over
    ///   Final Processing (x -98 to -74, z -341 to -309, y 408) and the one at the south-west
    ///   end of the Concourse (x -186 to -154, z -397 to -373, y 392).
    /// - Living Quarters (10): the two dorm rooms (arch_brann_corridor_room_01_dorm_v01) at
    ///   x -206 to -182, y 408: the north one (z -189 to -157) with its beds and the Bane sleep
    ///   pods now in it, the south one (z -325 to -293) with its beds, toilets and sinks.
    /// - Skybridge (9): the Brann bridge over the chasm at its label (x -186 to -122).
    /// - Greenhouse (11): the greenhouse at its label (x -262 to -214), Bane consoles round its
    ///   command chair.
    /// - Bane Entrypoint (25, the Bane interior sky): the Bane chunnel the Bane broke in by at
    ///   the far west (x -351 to -299, z -238 to -189), with its machines.
    ///
    /// Left without volumes: Maintenance, Water Regulation and the two Procurement Sectors, which
    /// nothing in the .map picks out, and the map's unnamed regions.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is
    /// loaded and no box of that region is at that spot; a recut row changes only while it is
    /// the circle as seeded. Down takes out what went in and gives the circles back.
    /// </summary>
    public static class BrannWaterRefineryRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        /// <summary>A seeded label circle and the box it becomes.</summary>
        public sealed record Recut(uint RowId, double X, double Y, double Z, double Radius, string Comment, Volume Box);

        public const uint MapContextId = 2093;
        public const uint CentralOperationsRegion = 2, WaterCollection1Region = 3, WaterCollection2Region = 4, SedimentRegion = 5, SkybridgeRegion = 9,
            LivingQuartersRegion = 10, GreenhouseRegion = 11, MainAccessRegion = 13, PumpStation1Region = 17, PumpStation2Region = 18,
            ReservoirTanksRegion = 20, FinalProcessingRegion = 21, ConcourseLevelRegion = 22, BaneEntrypointRegion = 25;
        public const byte Circle = 1, Box = 2;

        private const string Map = "arieki_torden_plains_brannwaterrefinery - ";

        public static readonly Volume[] Volumes =
        {
            Of(MainAccessRegion, -52, 12, -130, -40, 412, 470, Map + "Main Access (entrance dome to the airlock)"),
            Of(WaterCollection1Region, -63, -13, -161, -113, 404, 430, Map + "Water Collection 1 (north hall)"),
            Of(WaterCollection2Region, -63, -13, -233, -185, 404, 430, Map + "Water Collection 2 (south hall)"),
            Of(FinalProcessingRegion, -139, -73, -322, -272, 370, 400, Map + "Final Processing (grated floor and machinery hall)"),
            Of(ConcourseLevelRegion, -155, -137, -418, -300, 380, 406, Map + "Concourse Level (west corridor)"),
            Of(PumpStation1Region, -99, -73, -342, -308, 404, 419, Map + "PumpStation 1 (control room over Final Processing)"),
            Of(PumpStation2Region, -187, -153, -398, -372, 388, 403, Map + "PumpStation 2 (control room south-west)"),
            Of(LivingQuartersRegion, -207, -181, -190, -156, 405, 422, Map + "Living Quarters (north dorm)"),
            Of(LivingQuartersRegion, -207, -181, -326, -292, 405, 422, Map + "Living Quarters (south dorm)"),
            Of(BaneEntrypointRegion, -352, -296, -240, -185, 396, 425, Map + "Bane Entrypoint (chunnel)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20157, -101.5, 407.8, -153.0, 63.7, Map + "Central Operations", Of(CentralOperationsRegion, -120, -88, -184, -122, 395, 427, Map + "Central Operations (room)")),
            new(20158, -37.8, 391.2, -257.0, 58.6, Map + "Sediment Separation Area", Of(SedimentRegion, -79, 3, -282, -232, 388, 410, Map + "Sediment Separation Area (settling rooms)")),
            new(20159, -154.0, 399.5, -241.0, 42.0, Map + "Skybridge", Of(SkybridgeRegion, -186, -122, -255, -227, 395, 412, Map + "Skybridge (bridge)")),
            new(20160, -238.0, 399.2, -241.0, 42.0, Map + "Greenhouse", Of(GreenhouseRegion, -263, -213, -266, -216, 395, 420, Map + "Greenhouse (room)")),
            new(20161, -81.8, 383.5, -376.8, 63.8, Map + "Reservoir Tanks", Of(ReservoirTanksRegion, -131, -33, -425, -329, 378, 400, Map + "Reservoir Tanks (quarter rooms)")),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, 0, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            foreach (var r in Recuts)
                yield return $"UPDATE map_region SET shape = {Box}, pos_x = {N(r.Box.X)}, pos_y = {N(r.Box.Y)}, pos_z = {N(r.Box.Z)}, radius = 0, " +
                    $"half_x = {N(r.Box.HalfX)}, half_z = {N(r.Box.HalfZ)}, min_y = {N(r.Box.MinY)}, max_y = {N(r.Box.MaxY)}, underground = 0, comment = {S(r.Box.Comment)} " +
                    $"WHERE id = {r.RowId} AND map_context_id = {MapContextId} AND region_id = {r.Box.RegionId} AND shape = {Circle} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01;";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {Circle}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = 0, half_z = 0, min_y = -2000, max_y = 2000, underground = 2, comment = {S(r.Comment)} " +
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
