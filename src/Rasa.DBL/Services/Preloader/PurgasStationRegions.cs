using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Purgas Station's three levels (adv_foreas_concordia_divide_purgasstation2, 1806;
    /// Purgas_station_regions).
    ///
    /// The station is three floors stacked round one reactor shaft at (0, 0), on a 24 to 32 m
    /// rhythm, and the map names its rooms per level: Level 01's Entrance Hall (the map's own
    /// name), Juggernaut Bay, Service Corridor, Main Corridor, Power Chamber, Reactor Core and
    /// Control Room (regions 6 to 11), Level 02's Service Corridor, Main Corridor, Power Chamber,
    /// Reactor Core and Control Room (12 to 16) and Level 03's two Power Chambers, Main Corridor,
    /// Reactor Core and Control Room (17 to 21). Each level's regions have higher priorities
    /// than the one above it, so wherever two levels' volumes stood over each other, Level 03's
    /// won.
    ///
    /// Level 01 is the top floor. The instance puts you there, in the Entrance Hall (map link 83
    /// at (87.8, 65.8, 221.6), and the "Level 01: Entrance Hall" hospital beside it); the
    /// "Level 01: Control Room" hospital and the "Level 1: Control Room" waypoint stand on that
    /// floor's control room overlook (152.2, 73.6, 128.5). "The energy they are harnessing
    /// below"; "the reactor can be found on the bottom floor"; the fuel basins "dug into the
    /// cavern ground" are Level 03's (the missions that send you to them want the Level 3 Access
    /// Keypass; a teleporter row is named "Level 3: Fuel Basin"). So the levels by height:
    ///
    /// - Level 01, floors 64 to 87 (61 and up): the curved Entrance Hall and its corridor, the
    ///   round Juggernaut Bay in the north-east (four arch_bane_industrial_chunnel_room_turn, with
    ///   the prop_bane_juggernaut at (337, 65, 110) and the AFS Insertion Point), the control room
    ///   (arch_bane_industrial_room_control_24m_v07, x 119 to 185, z 55 to 128) with its
    ///   overlook, the grilled circuit corridor east to west through the shaft and its north and
    ///   south arms, the service tubes round the control room and the stubs off the corridor,
    ///   and the reactor's top tier (arch_bane_industrial_room_reactor_level03_wall, x and z -80
    ///   to 80, its ring at 81 to 83).
    /// - Level 02, floors 27 to 58 (24 to 61; 10 to 61 west of x -145, where the fuel pools of
    ///   its west rooms sink to 18): the control room north of the shaft (room_control_24m_v06,
    ///   x -33 to 32, z 48 to 128), the room with a fuel pool and a catwalk cross in the
    ///   south-west (room_control_24m_v10, x -273 to -207, z -177 to -111), the two grilled
    ///   circuit corridors (west at z -48, east at z 48) and the south-east arm, the reactor's
    ///   middle tier (its walkways at 27 to 31 and 52 to 58), and the west maze of service tubes
    ///   and fuel pool rooms.
    /// - Level 03, floors -3 to 23 (below 24): the reactor floor, the control room south of it
    ///   (room_control_24m_v07, x 8 to 81, z -185 to -119, its gallery at 18), the two fuel basin
    ///   rooms on the south corridor (x -200 to -120 and -64 to -16, z -229 to -203), and the
    ///   chunnel corridors joining them.
    ///
    /// The volumes, each in its level's height:
    ///
    /// - The six label circles (MapRegionMarkerPreloader) become boxes: the three Reactor Cores'
    ///   25 m circles (at any height, so Level 03's named the shaft on every floor) the
    ///   reactor's tier on each floor (x and z -80 to 80, -76 to 76 and -66 to 66), and the
    ///   three Control Rooms' 107.6 m circles (Region_label_rooms put all three on Level 01's
    ///   control room, so it was "Level 03: Control Room") each level's control room.
    /// - Level 01: the Juggernaut Bay, the Main Corridor and its two arms, and six Service Corridor
    ///   boxes. Everything else on the floor keeps the map's own name, the Entrance Hall.
    /// - Level 02: its Service Corridor over the whole floor (the level's own name for what has
    ///   no other), the Main Corridor over the circuit corridors, and the Power Chamber over the
    ///   fuel pool room.
    /// - Level 03: the two Power Chambers over the fuel basin rooms (which basin is which is a
    ///   guess: region 20, with the other Power Chambers' REGION_BANE_TUNNEL_03, takes the west
    ///   room with two basins), and the Main Corridor over the corridors, clear of region 17,
    ///   which is under it in priority.
    ///
    /// Not placed: Level 01: Power Chamber (region 8): nothing on that floor marks it.
    /// Region 22's minimap rectangle over the shaft stays as it is.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. New rows go in only where the map is loaded
    /// and no box of that region is at that spot; recut rows change only while they are as
    /// seeded. Down puts every seeded row back and takes the new ones out.
    /// </summary>
    public static class PurgasStationRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        /// <summary>A seeded circle as it is and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, double X, double Y, double Z, double Radius, double MinY, double MaxY, string Comment, Volume Box);

        public const uint MapContextId = 1806;
        public const uint JuggernautBayRegion = 6, LevelOneControlRoomRegion = 7, LevelOneServiceRegion = 9, LevelOneMainRegion = 10, LevelOneReactorRegion = 11,
            LevelTwoServiceRegion = 12, LevelTwoMainRegion = 13, LevelTwoControlRoomRegion = 14, LevelTwoPowerRegion = 15, LevelTwoReactorRegion = 16,
            LevelThreeEastPowerRegion = 17, LevelThreeMainRegion = 18, LevelThreeControlRoomRegion = 19, LevelThreeWestPowerRegion = 20, LevelThreeReactorRegion = 21;
        public const byte Circle = 1, Box = 2, AnyLevel = 0;

        private const string Map = "foreas_concordia_divide_purgasstation2 - ";

        public static readonly Volume[] Volumes =
        {
            Of(JuggernautBayRegion, 236, 386, 62, 242, 61, 110, Map + "Level 01: Juggernaut Bay (the round bay)"),
            Of(LevelOneMainRegion, -196, 196, -14, 14, 61, 95, Map + "Level 01: Main Corridor (east to west)"),
            Of(LevelOneMainRegion, -14, 14, 14, 100, 61, 95, Map + "Level 01: Main Corridor (north arm)"),
            Of(LevelOneMainRegion, -14, 14, -100, -14, 61, 95, Map + "Level 01: Main Corridor (south arm)"),
            Of(LevelOneServiceRegion, 170, 240, 80, 128, 61, 95, Map + "Level 01: Service Corridor (east of the control room)"),
            Of(LevelOneServiceRegion, 160, 242, 160, 222, 61, 95, Map + "Level 01: Service Corridor (by the entrance hall)"),
            Of(LevelOneServiceRegion, 120, 175, 14, 58, 61, 80, Map + "Level 01: Service Corridor (south of the control room)"),
            Of(LevelOneServiceRegion, 88, 120, -42, -14, 61, 80, Map + "Level 01: Service Corridor (east stub)"),
            Of(LevelOneServiceRegion, -112, -96, 14, 35, 61, 80, Map + "Level 01: Service Corridor (west console room)"),
            Of(LevelOneServiceRegion, -160, -144, -43, -14, 61, 80, Map + "Level 01: Service Corridor (west stub)"),

            Of(LevelTwoServiceRegion, -312, -145, -186, 126, 10, 61, Map + "Level 02: Service Corridor (the level, west)"),
            Of(LevelTwoServiceRegion, -145, 200, -186, 126, 24, 61, Map + "Level 02: Service Corridor (the level, east)"),
            Of(LevelTwoMainRegion, -196, -78, -64, -33, 24, 61, Map + "Level 02: Main Corridor (west)"),
            Of(LevelTwoMainRegion, 78, 196, 32, 64, 24, 61, Map + "Level 02: Main Corridor (east)"),
            Of(LevelTwoMainRegion, 33, 63, -100, -60, 24, 61, Map + "Level 02: Main Corridor (south-east arm)"),
            Of(LevelTwoPowerRegion, -276, -204, -180, -108, 24, 61, Map + "Level 02: Power Chamber (the fuel pool room)"),

            Of(LevelThreeWestPowerRegion, -208, -118, -236, -198, -30, 24, Map + "Level 03: Power Chamber (the west fuel basins)"),
            Of(LevelThreeEastPowerRegion, -66, -14, -236, -200, -30, 24, Map + "Level 03: Power Chamber (the east fuel basin)"),
            Of(LevelThreeMainRegion, -145, -66, -232, -166, -30, 24, Map + "Level 03: Main Corridor (between the basins)"),
            Of(LevelThreeMainRegion, -66, 10, -200, -118, -10, 24, Map + "Level 03: Main Corridor (to the control room)"),
            Of(LevelThreeMainRegion, -46, 6, -130, -64, -10, 24, Map + "Level 03: Main Corridor (south tube)"),
            Of(LevelThreeMainRegion, 38, 115, -206, 34, -10, 24, Map + "Level 03: Main Corridor (east)"),
            Of(LevelThreeMainRegion, 100, 160, -62, 46, -10, 24, Map + "Level 03: Main Corridor (east tubes)"),
            Of(LevelThreeMainRegion, 100, 130, -137, -118, -10, 24, Map + "Level 03: Main Corridor (east side room)"),
            Of(LevelThreeMainRegion, -6, 62, 64, 131, -10, 24, Map + "Level 03: Main Corridor (north tube)"),
        };

        private const double CoreX = -0.4, CoreY = 56.2, CoreZ = 10.9, CoreRadius = 25;
        private const double RoomX = 152.2, RoomY = 73.6, RoomZ = 128.5, RoomRadius = 107.6, RoomMinY = 60.3, RoomMaxY = 93;

        public static readonly Recut[] Recuts =
        {
            new(20104, LevelOneReactorRegion, CoreX, CoreY, CoreZ, CoreRadius, -2000, 2000, Map + "Level 01: Reactor Core",
                Of(LevelOneReactorRegion, -80, 80, -80, 80, 61, 125, Map + "Level 01: Reactor Core (the reactor's top tier)")),
            new(20105, LevelOneControlRoomRegion, RoomX, RoomY, RoomZ, RoomRadius, RoomMinY, RoomMaxY, Map + "Level 01: Control Room",
                Of(LevelOneControlRoomRegion, 117, 186, 52, 140, 61, 93, Map + "Level 01: Control Room (and its overlook)")),
            new(20106, LevelTwoReactorRegion, CoreX, CoreY, CoreZ, CoreRadius, -2000, 2000, Map + "Level 02: Reactor Core",
                Of(LevelTwoReactorRegion, -76, 76, -76, 76, 24, 61, Map + "Level 02: Reactor Core (the reactor's middle tier)")),
            new(20107, LevelTwoControlRoomRegion, RoomX, RoomY, RoomZ, RoomRadius, RoomMinY, RoomMaxY, Map + "Level 02: Control Room",
                Of(LevelTwoControlRoomRegion, -35, 34, 46, 130, 24, 61, Map + "Level 02: Control Room (north of the reactor)")),
            new(20108, LevelThreeReactorRegion, CoreX, CoreY, CoreZ, CoreRadius, -2000, 2000, Map + "Level 03: Reactor Core",
                Of(LevelThreeReactorRegion, -66, 66, -66, 66, -10, 24, Map + "Level 03: Reactor Core (the reactor floor)")),
            new(20109, LevelThreeControlRoomRegion, RoomX, RoomY, RoomZ, RoomRadius, RoomMinY, RoomMaxY, Map + "Level 03: Control Room",
                Of(LevelThreeControlRoomRegion, 0, 83, -187, -117, -10, 24, Map + "Level 03: Control Room (south of the reactor)")),
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
                    $"half_x = {N(r.Box.HalfX)}, half_z = {N(r.Box.HalfZ)}, min_y = {N(r.Box.MinY)}, max_y = {N(r.Box.MaxY)}, comment = {S(r.Box.Comment)} " +
                    $"WHERE id = {r.RowId} AND map_context_id = {MapContextId} AND region_id = {r.RegionId} AND shape = {Circle} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01 " +
                    $"AND ABS(min_y - {N(r.MinY)}) < 0.01 AND ABS(max_y - {N(r.MaxY)}) < 0.01 AND underground = {AnyLevel};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {Circle}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = 0, half_z = 0, min_y = {N(r.MinY)}, max_y = {N(r.MaxY)}, comment = {S(r.Comment)} " +
                    $"WHERE id = {r.RowId} AND {At(r.Box, "map_region")} AND ABS(half_x - {N(r.Box.HalfX)}) < 0.01 AND ABS(half_z - {N(r.Box.HalfZ)}) < 0.01 " +
                    $"AND ABS(min_y - {N(r.Box.MinY)}) < 0.01 AND ABS(max_y - {N(r.Box.MaxY)}) < 0.01;";

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
