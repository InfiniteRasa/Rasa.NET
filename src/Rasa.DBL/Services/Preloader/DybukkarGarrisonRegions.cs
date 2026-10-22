using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Dybukkar Garrison's rooms (adv_arieki_torden_abyss_dybukkar, 2190; Dybukkar_garrison_regions).
    ///
    /// The garrison runs north from the way in from Torden Abyss (map link at (-65.75, 321.75,
    /// -568)): the Security room, Operations, the two ore chambers of Super Soldier
    /// Incubation, the Main Production Chamber, and out through the north tunnels to the Repair
    /// Yard and the Dropship Area under the cavern roof.
    ///
    /// The label circles MapRegionMarkerPreloader seeded, Region_label_rooms gave their rooms'
    /// heights, were wider than the rooms and in two places the wrong place:
    ///
    /// - Security Overlook (regions 19 and 13) had no label of its own and got Security's circle;
    ///   13, the higher priority, named the Security room "Security Overlook". It is the room at
    ///   the north end of the Main Production Chamber that looks out over the Repair Yard: the
    ///   overlook couplers from the chamber (x 16 to 48, z -64 to -16, y 328) to the garrison's one
    ///   exterior window (arch_bane_industrial_overlook_exterior_window_v01 at (32, 328, -16)),
    ///   with its command chairs and consoles. Region 19 is that window's sky
    ///   (sky_arieki_torden_abyss_dybukkar_exterior_window), 13 its ambience and name; both become
    ///   the room.
    /// - Security (6): its 82.5 m circle took in the way in; it becomes its room
    ///   (arch_bane_industrial_room_control_24m_v16 at (-104, 320, -512)).
    /// - Operations (8): its room (arch_bane_industrial_room_control_24m_v05 at (0, 320, -384), the
    ///   information extraction room in it and the command chairs over it).
    /// - Super Soldier Incubation (18): its 123.7 m circle reached Operations' label and named it;
    ///   it becomes the two ore chambers with their rows of sleep pods (x -33 to 161, z -332 to
    ///   -260).
    ///
    /// And regions that had no volume:
    ///
    /// - Harvesting Chamber (10): "In the Bane Harvesting chambers, find the seedling from the
    ///   young white guiche plant": the east wing off the Main Production Chamber, six rooms of
    ///   guiche plants with Bane medical stations and weapon dumps (x 161 to 223, z -185 to -120)
    ///   and the corridor between them.
    /// - North Concourse (14) and South Concourse (7), both REGION_BANE_TUNNEL_04: the garrison's
    ///   two sets of 32 m tunnels. The north ones run from the Security Overlook's side door past
    ///   the tunnel crosses and down the ramp to the Repair Yard (x 72 to 189, z -57 to 88); the
    ///   south ones are the way in, the tunnel cross with the medical team and the tee to
    ///   Security (x -72 to -24, z -585 to -507).
    ///
    /// - Maintenance Areas A, B and C (3, 4 and 5, REGION_BANE_TUNNEL_01): the garrison's narrow
    ///   service corridors (arch_bane_industrial_service_*), in three sets, lettered in order from
    ///   the way in to the back. A is the front set, round Security and Operations: north from
    ///   Security's back door, two service rooms, and the block with the 24 m room and the ramp
    ///   down at Operations' west side (x -109 to -44, z -489 to -372). B is the middle set, along
    ///   both sides of the Super Soldier Incubation ore chambers at y 328 (x 7 to 121, z -359 to
    ///   -249; the chambers between are Incubation's) and the branch past the machines and down
    ///   the ramp to the Harvesting Chamber (x 113 to 156, z -244 to -188). C is the back set,
    ///   west of the Main Production Chamber at y 292 to 307: the room, cross and ramp behind the
    ///   two service entrances that open on the Repair Yard (x -146 to -99, z -39 to 38). A
    ///   stays out of the Security room's corner, whose region is the lower priority.
    ///
    /// Left without volumes: the Drug Development Ward (the bio labs are
    /// inside the Production Chamber and Operations, whose regions outrank it), the Transport
    /// Annex, the second Repair Yard and Dropship Area regions, and the unnamed ones.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is
    /// loaded and no box of that region is at that spot; a recut row changes only while it is
    /// the circle as seeded. Down takes out what went in and gives the circles back as
    /// Region_label_rooms left them.
    /// </summary>
    public static class DybukkarGarrisonRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        /// <summary>A seeded label circle, as Region_label_rooms left it, and the box it becomes.</summary>
        public sealed record Recut(uint RowId, double X, double Y, double Z, double Radius, double MinY, double MaxY, string Comment, Volume Box);

        public const uint MapContextId = 2190;
        public const uint MaintenanceARegion = 3, MaintenanceBRegion = 4, MaintenanceCRegion = 5, SecurityRegion = 6, SouthConcourseRegion = 7, OperationsRegion = 8, HarvestingRegion = 10, OverlookRegion = 13,
            NorthConcourseRegion = 14, IncubationRegion = 18, OverlookWindowRegion = 19;
        public const byte Circle = 1, Box = 2, AnyLevel = 0, SurfaceOnly = 2;

        private const string Map = "arieki_torden_abyss_dybukkar - ";

        public static readonly Volume[] Volumes =
        {
            Of(HarvestingRegion, 141, 225, -186, -119, 318, 345, Map + "Harvesting Chamber (guiche wing)"),
            Of(NorthConcourseRegion, 66, 190, -58, 90, 295, 345, Map + "North Concourse (tunnels to the Repair Yard)"),
            Of(SouthConcourseRegion, -73, -23, -586, -506, 318, 340, Map + "South Concourse (the way in)"),
            Of(MaintenanceARegion, -72, -43, -490, -454, 318, 337, Map + "Maintenance Area A (service rooms behind Security)"),
            Of(MaintenanceARegion, -88, -72, -474, -454, 318, 337, Map + "Maintenance Area A (west service room)"),
            Of(MaintenanceARegion, -110, -43, -418, -371, 318, 337, Map + "Maintenance Area A (west of Operations)"),
            Of(MaintenanceBRegion, 6, 122, -360, -248, 318, 337, Map + "Maintenance Area B (either side of the ore chambers)"),
            Of(MaintenanceBRegion, 105, 157, -248, -187, 318, 340, Map + "Maintenance Area B (down to the Harvesting Chamber)"),
            Of(MaintenanceCRegion, -147, -98, -40, 39, 290, 316, Map + "Maintenance Area C (behind the Repair Yard doors)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20185, -104.0, 322.0, -512.0, 82.5, 316.3, 349.0, Map + "Security Overlook",
                Of(OverlookWindowRegion, 14, 50, -65, -8, 326, 341, Map + "Security Overlook (window room)")),
            new(20189, -104.0, 322.0, -512.0, 82.5, 316.3, 349.0, Map + "Security Overlook",
                Of(OverlookRegion, 14, 50, -65, -8, 326, 341, Map + "Security Overlook (window room)")),
            new(20186, -104.0, 322.0, -512.0, 82.5, 316.3, 349.0, Map + "Security",
                Of(SecurityRegion, -138, -70, -546, -478, 318, 349, Map + "Security (room)")),
            new(20187, 0.0, 319.5, -384.0, 66.9, 316.3, 349.0, Map + "Operations",
                Of(OperationsRegion, -34, 34, -425, -343, 318, 350, Map + "Operations (room)")),
            new(20188, 52.0, 331.0, -296.0, 123.7, 318.9, 344.9, Map + "Super Soldier Incubation",
                Of(IncubationRegion, -34, 162, -333, -259, 318, 345, Map + "Super Soldier Incubation (ore chambers)")),
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
                    $"half_x = 0, half_z = 0, min_y = {N(r.MinY)}, max_y = {N(r.MaxY)}, underground = {SurfaceOnly}, comment = {S(r.Comment)} " +
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
