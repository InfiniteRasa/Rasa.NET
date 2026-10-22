using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Bane Fluxite Mines' places (adv_arieki_torden_mires_banefluxitemines, 2115;
    /// Fluxite_mines_regions).
    ///
    /// From the way in at the west (tunnel_jamb_instance at (-36, 143, -20), the field medic
    /// beside it) a chunnel runs east at y 144 into the mine; a warren of service corridors at
    /// y 135 to 158 round it leads to Deep Planet Scan, Flight Control and out to the dropship
    /// pads. The mine itself is one great cavern under terra_arieki_cavern_mine_roof (x 32 to 261,
    /// z 27 to 626); east of it the lava of Vapor Chasm, and at its north-east end the Ore Prep
    /// Chamber and the ventilation beyond. The navmesh calls some of each underground and some
    /// not, so the label circles that were surface or underground only fell short.
    ///
    /// - Main Passage (10, REGION_BANE_TUNNEL_03): the chunnel from the way in to the mine, past
    ///   the tee by the medic, along z 20 and round to its entrance on the mine (x -38 to 119,
    ///   z -25 to 73).
    /// - Maintenance (12, REGION_BANE_TUNNEL_01, the service corridors' ambience on every Bane
    ///   map): the service corridors round the Main Passage, from the rooms south of it to the
    ///   service entrance on the dropship yard (x -6 to 197, z -54 to 128).
    /// - Flight Control (9, the Mires sky through its window): "the Flight Control Room" whose
    ///   console radios the dropships: the room at the exterior window over the dropship pads
    ///   (arch_bane_industrial_overlook_exterior_window at (124, 151, -60)) with its command chair
    ///   and 27 consoles.
    /// - Deep Planet Scan (3): its circle becomes its control room
    ///   (arch_bane_industrial_room_control_24m_v17 at (-12, 143, 124)), clear of the corridors.
    /// - Mining Chamber (14): its 80 m underground-only circle missed its own label; it becomes
    ///   the mine cavern under its roof, with the fluxite piles, carts, drills and conveyors,
    ///   leaving out the Ore Prep Chamber's corner of it.
    /// - Vapor Chasm (5): likewise its underground-only circle missed its label; it becomes the
    ///   lava chasm under its roof (terra_arieki_cavern_rocky_roof, x 223 to 387, z 106 to 414)
    ///   below y 135.
    /// - Ore Prep Chamber (4): "Head into the Ore Prep Chamber, kill the Chief Operations
    ///   Technician, and obtain the access codes for the Communication Terminal": its 153 m
    ///   surface-only circle reached across Vapor Chasm into the ventilation; it becomes the ore
    ///   chamber (arch_bane_industrial_chunnel_ore_chamber at (172, 104, 468)), the machine hall
    ///   east of it, and the galleries over them with their command chairs, terminals and window
    ///   (x 171 to 334, z 395 to 533).
    /// - Ventilation (8): "Use the console in the Ore Prep Chamber to unlock the door to the
    ///   Ventilation area. Open all four vents ... using the Vent Controls": the chunnel past the
    ///   Ore Prep Chamber's tee lined with eight ventilation machines
    ///   (arch_bane_industrial_chunnel_machine_05) and four console pedestals (x 396 to 457,
    ///   z 367 to 515).
    ///
    /// Left without volumes: Operations, which nothing on the map or in the missions places.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is
    /// loaded and no box of that region is at that spot; a recut row changes only while it is
    /// the circle as seeded. Down takes out what went in and gives the circles back.
    /// </summary>
    public static class FluxiteMinesRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        /// <summary>A seeded circle as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, double X, double Y, double Z, double Radius, double MinY, double MaxY, byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2115;
        public const uint DeepPlanetScanRegion = 3, OrePrepRegion = 4, VaporChasmRegion = 5, VentilationRegion = 8, FlightControlRegion = 9,
            MainPassageRegion = 10, MaintenanceRegion = 12, MiningChamberRegion = 14;
        public const byte Circle = 1, Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "arieki_torden_mires_banefluxitemines - ";

        public static readonly Volume[] Volumes =
        {
            Of(MainPassageRegion, -38, 113, -26, 37, 140, 170, Map + "Main Passage (chunnel from the way in)"),
            Of(MainPassageRegion, 81, 119, 36, 74, 140, 172, Map + "Main Passage (into the mine)"),
            Of(MaintenanceRegion, -6, 86, -54, -11, 140, 160, Map + "Maintenance (south service rooms)"),
            Of(MaintenanceRegion, 84, 121, -29, 4, 133, 160, Map + "Maintenance (ramps up)"),
            Of(MaintenanceRegion, 109, 197, 4, 50, 133, 160, Map + "Maintenance (to the dropship yard)"),
            Of(MaintenanceRegion, 3, 81, 35, 128, 140, 160, Map + "Maintenance (north service rooms)"),
            Of(FlightControlRegion, 107, 133, -80, -40, 140, 172, Map + "Flight Control (window room)"),
            Of(MiningChamberRegion, 50, 150, 86, 200, 100, 175, Map + "Mining Chamber (south)"),
            Of(MiningChamberRegion, 150, 222, 51, 395, 100, 175, Map + "Mining Chamber (east)"),
            Of(MiningChamberRegion, 150, 262, 533, 610, 100, 175, Map + "Mining Chamber (north-east)"),
            Of(VentilationRegion, 396, 458, 366, 516, 102, 130, Map + "Ventilation (vent chunnel)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20168, -12.0, 143.8, 123.8, 88.0, 139.3, 172.0, SurfaceOnly, Map + "Deep Planet Scan",
                Of(DeepPlanetScanRegion, -46, 21, 83, 165, 139, 172, Map + "Deep Planet Scan (control room)")),
            new(20169, 294.8, 89.2, 254.2, 80.0, -2000, 2000, UndergroundOnly, Map + "Vapor Chasm",
                Of(VaporChasmRegion, 224, 388, 106, 395, 75, 135, Map + "Vapor Chasm (lava chasm)")),
            new(20170, 292.0, 113.0, 468.0, 153.3, -2000, 2000, SurfaceOnly, Map + "Ore Prep Chamber",
                Of(OrePrepRegion, 171, 334, 395, 533, 100, 131, Map + "Ore Prep Chamber (chamber, machine hall, galleries)")),
            new(20171, 98.0, 141.8, 261.2, 80.0, -2000, 2000, UndergroundOnly, Map + "Mining Chamber",
                Of(MiningChamberRegion, 0, 150, 200, 610, 100, 175, Map + "Mining Chamber (the cavern)")),
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
                    $"half_x = 0, half_z = 0, min_y = {N(r.MinY)}, max_y = {N(r.MaxY)}, underground = {r.Underground}, comment = {S(r.Comment)} " +
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
