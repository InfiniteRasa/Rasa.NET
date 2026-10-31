using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Fault Lever's three sectors, its main generator hall, operations room and transfer
    /// station (adv_arieki_ligo_thunderhead_faultlever, 2103; Fault_lever_sector_regions).
    ///
    /// The facility's regions come in three groups by priority, each from a wing's name at the
    /// bottom up through the rooms in it: Main Generators, Operations, North Sector, Service
    /// Concourse C and the Transfer Station (11 to 15); East Sector, its security yard, Service
    /// Concourse B, Hammer Monitoring Station B, Maintenance Level B and Power Transmitters B1
    /// and B2 (16 to 22); West Sector, its yard, Service Concourse A, Hammer Monitoring Station
    /// A, Maintenance Level A and Power Transmitters A1 and A2 (23 to 29). Fault_lever_regions
    /// found the A side to the west and the B side to the east from the monitoring stations'
    /// labels. All of the facility's rooms are underground on the navmesh.
    ///
    /// - West Sector (14), East Sector (8) and North Sector (3), REGION_BANE_TUNNEL_03 and a
    ///   name, each the lowest of its group, are the wings: the west wing (x -425 to -128,
    ///   z 130 to 500, its chunnels at 310, 334 and 350), the east wing (x 112 to 418, z 22 to
    ///   284, at 286, 310 and 326) and the north complex (x -118 to 98, z 318 to 568, at 344).
    ///   Each gets boxes over its wing, underground-only, that leave out the places under
    ///   lower-priority regions they would take over: the Atta pockets (Hidden Cave, 10) in the
    ///   west and north, and the generator hall and operations room in the north. The rooms
    ///   above them in priority take their own places over them as before.
    /// - Main Generators (2, REGION_BANE_INTERIOR_02): the hall at its label (-50, 347.5,
    ///   499.5), the wide chunnel (arch_bane_industrial_chunnel_straight_01) from x -83 to -13,
    ///   z 448 to 564, floor 344, that the north complex's chunnels open into. Its label circle
    ///   (148 m, surface only) reached none of it; it becomes the hall's box.
    /// - Operations (4, REGION_BANE_INTERIOR_01, the room ambience of the two Hammer Monitoring
    ///   Stations' galleries): the north complex's command gallery, the overlook over the hall's
    ///   east side with its banks of consoles and command chairs (x -29 to 16, z 433 to 499,
    ///   floor 352), as the monitoring stations are the wings' galleries of consoles and chairs.
    /// - Transfer Station (24, REGION_BANE_INTERIOR_01, the north group's highest): the hub the
    ///   map link opens into (arch_bane_industrial_tunnel_cross_01 at (1, 324, 130), x -32 to
    ///   33, z 118 to 180), with its console pedestals and thermal vents, which the map's
    ///   minimap gives a level of its own (region 25's rectangle).
    ///
    /// Not placed: Service Concourses A, B and C and Maintenance Levels A and B. Each wing has
    /// service corridors (arch_bane_industrial_service_*) and three chunnel levels, but nothing
    /// says which of them a concourse or a maintenance level is.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. New rows go in only where the map is loaded
    /// and no box of that region is at that spot; the circle changes only while it is as
    /// seeded. Down puts it back and takes the new rows out.
    /// </summary>
    public static class FaultLeverSectorRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>A seeded label circle (surface-only, at any height) and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, double X, double Y, double Z, double Radius, string Comment, Volume Box);

        public const uint MapContextId = 2103;
        public const uint MainGeneratorsRegion = 2, NorthSectorRegion = 3, OperationsRegion = 4, EastSectorRegion = 8,
            WestSectorRegion = 14, TransferStationRegion = 24;
        public const byte Circle = 1, Box = 2, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "arieki_ligo_thunderhead_faultlever - ";

        public static readonly Volume[] Volumes =
        {
            Of(WestSectorRegion, -425, -321, 130, 500, 295, 378, Map + "West Sector (west wing, west)"),
            Of(WestSectorRegion, -321, -263, 130, 270, 295, 378, Map + "West Sector (west wing, south-west)"),
            Of(WestSectorRegion, -321, -305, 361, 500, 295, 378, Map + "West Sector (west wing, north-west)"),
            Of(WestSectorRegion, -269, -189, 218, 358, 295, 378, Map + "West Sector (west wing, middle)"),
            Of(WestSectorRegion, -263, -189, 130, 153, 295, 378, Map + "West Sector (west wing, south)"),
            Of(WestSectorRegion, -229, -189, 153, 218, 295, 378, Map + "West Sector (west wing, south-east)"),
            Of(WestSectorRegion, -189, -128, 130, 500, 295, 378, Map + "West Sector (west wing, east)"),
            Of(EastSectorRegion, 112, 418, 22, 284, 280, 345, Map + "East Sector (east wing)"),
            Of(NorthSectorRegion, -118, -83, 318, 568, 336, 372, Map + "North Sector (west side)"),
            Of(NorthSectorRegion, -83, 98, 318, 404, 336, 372, Map + "North Sector (south chunnels)"),
            Of(NorthSectorRegion, -83, -29, 404, 448, 336, 372, Map + "North Sector (by the hall's doors)"),
            Of(NorthSectorRegion, -29, 27, 404, 433, 336, 372, Map + "North Sector (under the operations room)"),
            Of(NorthSectorRegion, 16, 27, 433, 568, 336, 372, Map + "North Sector (east of the operations room)"),
            Of(NorthSectorRegion, -13, 16, 499, 568, 336, 372, Map + "North Sector (behind the operations room)"),
            Of(NorthSectorRegion, 27, 98, 495, 568, 336, 372, Map + "North Sector (north-east)"),
            Of(NorthSectorRegion, 79, 98, 404, 495, 336, 372, Map + "North Sector (east)"),
            Of(OperationsRegion, -29, 16, 433, 499, 351, 362, Map + "Operations (command gallery over the hall)"),
            Of(TransferStationRegion, -32, 33, 118, 180, 322, 342, Map + "Transfer Station (the hub at the way in)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20162, MainGeneratorsRegion, -50.0, 347.5, 499.5, 148, Map + "Main Generators",
                Of(MainGeneratorsRegion, -83, -13, 448, 564, 342, 369, Map + "Main Generators (the hall)")),
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
