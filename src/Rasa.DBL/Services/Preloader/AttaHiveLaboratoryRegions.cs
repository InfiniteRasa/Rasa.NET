using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Avernus Outpost's two Atta Hive caverns and the Bane Conscription Facility's Atta
    /// Laboratory, Maintenance and Processing areas (Atta_hive_laboratory_regions).
    ///
    /// Avernus Outpost (adv_arieki_ligo_ashendesert_avernusoutpost, 2110) is the AFS fort on the
    /// mesa and, under it, the Atta caverns: five domed caverns (terra_arieki_atta_cavern_roof),
    /// joined by tunnels. Three have labels and their 176 to 183 m label circles: the Queen's
    /// Chamber (281, -588), Food Storage (556, -296) and the AFS Graveyard (920, -244). The
    /// other two are the hive's own: the cavern between Food Storage and the Queen's Chamber
    /// (276, -316; floor y 214 to 235 among historical obelisks, Atta columns and the queen's
    /// growths, its tunnel winding down to the Queen's Chamber) and the small one at the Eloh
    /// door (646, -553). The map's two "Atta Hive" regions are those: region 2 (priority 6,
    /// sky_arieki_atta_interior_cave_blueV02, MUSIC_DESCENT_01 like the Queen's Chamber's
    /// MUSIC_DESCENT_02, one priority under it) the cavern over the Queen's Chamber, and
    /// region 13 (priority 10, REGION_ATTA_INTERNAL_LARGE_CAVE_ROOM, MUSIC_ASHEN_DESERT_01, one
    /// priority under the Eloh Sanctuary) the cavern at the Eloh door.
    ///
    /// The caverns are not under the map's terrain, so the label circles (surface only) apply
    /// in them, and three reached over the hives: the Queen's Chamber's over the south of the
    /// upper hive, Food Storage's over its east, and the Eloh Sanctuary's, whose label is in
    /// the Eloh temple past the door, over the whole of the small one. Those three become boxes
    /// of their own caverns (the temple: its corridor and room, arch_eloh_corridor_room_v01,
    /// x 603 to 653, z -686 to -597); Down gives them their circles back.
    ///
    /// The Bane Conscription Facility (adv_arieki_ligo_ashendesert_baneconscriptfacility, 1988)
    /// is built in mirror image about x = 7: the Atta Laboratory in the west round room
    /// (chunnel room pieces at (-234, 96, -145), x -298 to -145, z -210 to -79, y 96 to 139),
    /// the Atta Observatory in the east one, each with a raised overlook (consoles and command
    /// chairs at y 106) behind an exterior window over the lava field (x -153 to -138, and
    /// 143 to 158). Each has two regions: the room (2 and 3: sky_bane_interior_arena) and one
    /// with the outside sky (30 and 31: sky_arieki_ligo_ashen_desert, one priority higher).
    /// Those two had their labels' 120 m circles (given the rooms' heights by
    /// Region_label_rooms), which would have put the desert sky over the whole of each room;
    /// being surface only, under the facility's terrain they applied nowhere. Region 2 gets the
    /// laboratory room; 30 and 31 become boxes of the overlooks (x -200 to -138 and 138 to 200,
    /// z -182 to -108, y 103 to 125). Region 3 already has its room (Region_room_volumes).
    ///
    /// Under them, on each side, a processing level of raw and coupler chunnels at y 64 with
    /// conveyors, junkyard heaps and pits to y 32: Processing - West (region 25) x -295 to -109,
    /// z -360 to -234, and its corridor south to z -402; Processing - East (region 24) x 106 to
    /// 252, z -417 to -292. Between the levels run the maintenance service tunnels
    /// (arch_bane_industrial_service_*, 9 m wide, y 64 to 111), one network to each side of the
    /// facility, each with a service room: Maintenance - West (13) from the processing catwalk
    /// up past its room (-162, 88, -241) to the laboratory, with a branch at y 96 to 111 west and
    /// south; Maintenance - East (14), its mirror; Maintenance - North (19) north of the
    /// observatory (room (239, 80, -97)), down to (199, 88, -25); Maintenance - South (18) the
    /// rooms and tee at y 64 west of Sentient Conditioning (x -106 to -1, z -504 to -456).
    /// Their boxes follow the tunnels; the maintenance regions have the higher priorities.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is
    /// loaded and no box of that region is at that spot; a recut row changes only while it is
    /// as seeded. Down takes out what went in and gives the recut rows back as they were.
    /// </summary>
    public static class AttaHiveLaboratoryRegions
    {
        public sealed record Volume(uint MapContextId, uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        /// <summary>A seeded label circle and the box it becomes.</summary>
        public sealed record Recut(uint RowId, double X, double Y, double Z, double Radius, double MinY, double MaxY, string Comment, Volume Box);

        public const uint AvernusMapContextId = 2110, ConscriptionMapContextId = 1988;
        public const uint UpperHiveRegion = 2, EastHiveRegion = 13, QueensChamberRegion = 10, FoodStorageRegion = 12, ElohSanctuaryRegion = 5;
        public const uint LaboratoryRegion = 2, LaboratoryOverlookRegion = 30, ObservatoryOverlookRegion = 31;
        public const uint MaintenanceWestRegion = 13, MaintenanceEastRegion = 14, MaintenanceSouthRegion = 18, MaintenanceNorthRegion = 19;
        public const uint ProcessingEastRegion = 24, ProcessingWestRegion = 25;
        public const byte Circle = 1, Box = 2;

        private const string Avernus = "arieki_ligo_ashendesert_avernusoutpost - ";
        private const string Conscription = "arieki_ligo_ashendesert_baneconscriptfacility - ";

        public static readonly Volume[] Volumes =
        {
            Of(AvernusMapContextId, UpperHiveRegion, 160, 390, -425, -210, 195, 300, Avernus + "Atta Hive (cavern over the Queen's Chamber)"),
            Of(AvernusMapContextId, EastHiveRegion, 568, 724, -597, -490, 250, 300, Avernus + "Atta Hive (cavern at the Eloh door)"),

            Of(ConscriptionMapContextId, LaboratoryRegion, -298, -138, -210, -79, 94, 140, Conscription + "Atta Laboratory (room)"),

            Of(ConscriptionMapContextId, ProcessingWestRegion, -296, -108, -361, -233, 30, 90, Conscription + "Processing - West (hall)"),
            Of(ConscriptionMapContextId, ProcessingWestRegion, -145, -108, -402, -361, 60, 90, Conscription + "Processing - West (south corridor)"),
            Of(ConscriptionMapContextId, ProcessingEastRegion, 106, 252, -417, -292, 30, 90, Conscription + "Processing - East (hall)"),

            Of(ConscriptionMapContextId, MaintenanceWestRegion, -168, -155, -291, -209, 79, 112, Conscription + "Maintenance - West (room and ramps)"),
            Of(ConscriptionMapContextId, MaintenanceWestRegion, -194, -161, -338, -261, 95, 112, Conscription + "Maintenance - West (upper branch)"),
            Of(ConscriptionMapContextId, MaintenanceWestRegion, -190, -181, -386, -338, 103, 112, Conscription + "Maintenance - West (south end)"),
            Of(ConscriptionMapContextId, MaintenanceEastRegion, 146, 175, -290, -209, 79, 112, Conscription + "Maintenance - East (room and ramps)"),
            Of(ConscriptionMapContextId, MaintenanceEastRegion, 168, 207, -338, -261, 87, 112, Conscription + "Maintenance - East (upper branch)"),
            Of(ConscriptionMapContextId, MaintenanceNorthRegion, 230, 256, -98, -57, 79, 88, Conscription + "Maintenance - North (room)"),
            Of(ConscriptionMapContextId, MaintenanceNorthRegion, 194, 256, -57, -25, 79, 104, Conscription + "Maintenance - North (tee and ramps)"),
            Of(ConscriptionMapContextId, MaintenanceNorthRegion, 162, 204, -82, -57, 87, 112, Conscription + "Maintenance - North (west turn)"),
            Of(ConscriptionMapContextId, MaintenanceSouthRegion, -107, 0, -505, -455, 62, 73, Conscription + "Maintenance - South (rooms)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(322, 289.4, 178.7, -554.9, 176, -2000, 2000, Avernus + "Queen's Chamber",
                Of(AvernusMapContextId, QueensChamberRegion, 125, 430, -730, -450, 140, 260, Avernus + "Queen's Chamber (cavern)")),
            new(324, 545.5, 368.4, -277.2, 183, -2000, 2000, Avernus + "Food Storage",
                Of(AvernusMapContextId, FoodStorageRegion, 395, 715, -450, -155, 340, 485, Avernus + "Food Storage (cavern)")),
            new(325, 626.3, 275.9, -654.4, 176, -2000, 2000, Avernus + "Eloh Sanctuary",
                Of(AvernusMapContextId, ElohSanctuaryRegion, 603, 653, -686, -597, 265, 300, Avernus + "Eloh Sanctuary (temple)")),
            new(20115, -233.5, 101.5, -145.2, 119.3, 94.6, 139.6, Conscription + "Atta Laboratory",
                Of(ConscriptionMapContextId, LaboratoryOverlookRegion, -200, -138, -182, -108, 103, 125, Conscription + "Atta Laboratory (overlook)")),
            new(20116, 238.5, 81.2, -144.8, 120.0, 78.6, 139.4, Conscription + "Atta Observatory",
                Of(ConscriptionMapContextId, ObservatoryOverlookRegion, 138, 200, -182, -108, 103, 125, Conscription + "Atta Observatory (overlook)")),
        };

        private static Volume Of(uint map, uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(map, region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {v.MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, 0, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {v.MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            foreach (var r in Recuts)
                yield return $"UPDATE map_region SET shape = {Box}, pos_x = {N(r.Box.X)}, pos_y = {N(r.Box.Y)}, pos_z = {N(r.Box.Z)}, radius = 0, " +
                    $"half_x = {N(r.Box.HalfX)}, half_z = {N(r.Box.HalfZ)}, min_y = {N(r.Box.MinY)}, max_y = {N(r.Box.MaxY)}, underground = 0, comment = {S(r.Box.Comment)} " +
                    $"WHERE id = {r.RowId} AND map_context_id = {r.Box.MapContextId} AND region_id = {r.Box.RegionId} AND shape = {Circle} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01 " +
                    $"AND ABS(min_y - {N(r.MinY)}) < 0.01 AND ABS(max_y - {N(r.MaxY)}) < 0.01;";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {Circle}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = 0, half_z = 0, min_y = {N(r.MinY)}, max_y = {N(r.MaxY)}, underground = 2, comment = {S(r.Comment)} " +
                    $"WHERE id = {r.RowId} AND {At(r.Box, "map_region")} AND ABS(half_x - {N(r.Box.HalfX)}) < 0.01 AND ABS(half_z - {N(r.Box.HalfZ)}) < 0.01;";

            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)};";
        }

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {v.MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
