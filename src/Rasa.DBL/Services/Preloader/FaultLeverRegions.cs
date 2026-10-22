using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Fault Lever's monitoring stations, power transmitters, security yards and Atta caves
    /// (adv_arieki_ligo_thunderhead_faultlever, 2103; Fault_lever_regions).
    ///
    /// The facility is built round the fault lever itself (arch_bane_fault_lever at (1, 322, 13),
    /// out under the sky) in two wings that mirror each other, the A side to the west and the
    /// B side to the east, with the way in between them (map link at (0.5, 325.75, 120)) and
    /// the north beyond. Most of its regions are a name and an ambience. These the map's pieces
    /// place:
    ///
    /// - Hammer Monitoring Station A (16) and B (11, and 22 with the Thunderhead sky): each wing
    ///   has one monitoring room, an overlook gallery of consoles, research bio tables and
    ///   command chairs ending at an exterior window over the fault (A: window at (-294, 318,
    ///   140), gallery to z 188; B: window at (260, 294, 32), gallery to z 80). Their label
    ///   circles were surface only and the rooms are underground on the navmesh, so they applied
    ///   only on the ground outside; they become the rooms, and B's region 22 with the outside
    ///   sky the strip at its window (as with the Conscription Facility's and Atta Laboratory's
    ///   window regions).
    /// - Power Transmitter A1, A2, B1 and B2 (17, 19, 12, 18, REGION_BANE_TUNNEL_04): the map's
    ///   four circuit tubes (arch_bane_industrial_circuit_*), two to each wing: on the A side
    ///   one at y 312 (x -227 to -201) and one at y 344 further west (x -387 to -361), on the B
    ///   side two at y 288 (x 167 to 193 and 327 to 353). Which of a wing's pair is 1, nothing
    ///   says; the one nearer the way in is taken as 1.
    /// - West and East Sector Security (35 and 34, names only): each wing's walled checkpoint
    ///   yard outside it, the only two Bane minor walls on the map, each with its gate
    ///   (arch_bane_outpost_wall_minor_entrance at (-289, 338, 64) and (263, 314, -46)).
    /// - Hidden Cave (33, REGION_ATTA_INTERNAL_LARGE_CAVE_ROOM): the Atta cave pockets cut into
    ///   the facility, "holding the Atta back in remote parts of the facility": the cavern pieces
    ///   with the Atta queen's growths and egg clusters, three in the west wing (the furthest
    ///   with four Eloh logos signs at (-209, 375, 444)) and one north of the way in.
    ///
    /// Left without volumes: the Sectors, Service Concourses, Maintenance Levels, Operations
    /// and the Transfer Station, which nothing on the map or in the missions tells apart.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is
    /// loaded and no box of that region is at that spot; a recut row changes only while it is
    /// the circle as seeded. Down takes out what went in and gives the circles back.
    /// </summary>
    public static class FaultLeverRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        /// <summary>A seeded label circle and the box it becomes.</summary>
        public sealed record Recut(uint RowId, double X, double Y, double Z, double Radius, string Comment, Volume Box);

        public const uint MapContextId = 2103;
        public const uint StationBRegion = 11, TransmitterB1Region = 12, StationARegion = 16, TransmitterA1Region = 17, TransmitterB2Region = 18,
            TransmitterA2Region = 19, StationBWindowRegion = 22, HiddenCaveRegion = 33, EastSecurityRegion = 34, WestSecurityRegion = 35;
        public const byte Circle = 1, Box = 2, AnyLevel = 0, SurfaceOnly = 2;

        private const string Map = "arieki_ligo_thunderhead_faultlever - ";

        public static readonly Volume[] Volumes =
        {
            Of(TransmitterA1Region, -231, -197, 153, 287, 306, 335, Map + "Power Transmitter A1 (circuit tube)"),
            Of(TransmitterA2Region, -391, -357, 169, 303, 338, 367, Map + "Power Transmitter A2 (circuit tube)"),
            Of(TransmitterB1Region, 163, 197, 61, 195, 282, 311, Map + "Power Transmitter B1 (circuit tube)"),
            Of(TransmitterB2Region, 323, 357, 61, 195, 282, 311, Map + "Power Transmitter B2 (circuit tube)"),
            Of(WestSecurityRegion, -313, -273, 58, 115, 330, 350, Map + "West Sector Security (walled yard)"),
            Of(EastSecurityRegion, 239, 278, -51, 6, 306, 326, Map + "East Sector Security (walled yard)"),
            Of(HiddenCaveRegion, -305, -189, 358, 498, 338, 390, Map + "Hidden Cave (west, with the logos signs)"),
            Of(HiddenCaveRegion, -321, -269, 270, 361, 298, 337, Map + "Hidden Cave (west, lower)"),
            Of(HiddenCaveRegion, -263, -229, 153, 218, 322, 360, Map + "Hidden Cave (west, by the transmitter)"),
            Of(HiddenCaveRegion, 27, 79, 404, 495, 332, 370, Map + "Hidden Cave (north)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20164, -294.0, 319.5, 139.5, 42.7, Map + "Hammer Monitoring Station A",
                Of(StationARegion, -314, -274, 131, 189, 315, 338, Map + "Hammer Monitoring Station A (gallery and window)")),
            new(20163, 260.0, 295.5, 31.2, 40.6, Map + "Hammer Monitoring Station B",
                Of(StationBRegion, 240, 280, 23, 81, 291, 314, Map + "Hammer Monitoring Station B (gallery)")),
            new(20165, 260.0, 295.5, 31.2, 40.6, Map + "Hammer Monitoring Station B",
                Of(StationBWindowRegion, 240, 280, 23, 42, 283, 314, Map + "Hammer Monitoring Station B (window)")),
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
                    $"half_x = 0, half_z = 0, min_y = -2000, max_y = 2000, underground = {SurfaceOnly}, comment = {S(r.Comment)} " +
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
