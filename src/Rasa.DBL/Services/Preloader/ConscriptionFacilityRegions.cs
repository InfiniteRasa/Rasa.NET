using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Bane Conscription Facility's way in, cell blocks, central rooms and bridge
    /// (adv_arieki_ligo_ashendesert_baneconscriptfacility, 1988; Conscription_facility_regions).
    ///
    /// The facility runs north from the way in at its south end (map link at (7.25, 153.5,
    /// -670.25)): the prison gate, a yard, Sentient Conditioning, the research HQ, a bridge
    /// across the lava lake, the Enslaver Fortress room and the round hall behind it, with the
    /// Atta Laboratory and Atta Observatory to west and east (Atta_hive_laboratory_regions). Of
    /// its regions these had no volume:
    ///
    /// - Entry (28, the Bane interior sky and REGION_BANE_TUNNEL_03): the chunnel through the
    ///   prison gate from the map link to its entrance (arch_bane_industrial_chunnel_entrance at
    ///   (7, 152, -636)), with the field medic: x -11 to 26, z -660 to -615.
    /// - Hidden Cave (35, sky_ashen_desert_cave, REGION_FOREAS_CAVE_DRY): the cave west of the
    ///   gate, its mouth (terra_arieki_ligo_mesa_cave_entrance at (-125, 145, -567)) on the yard
    ///   and its cavern pieces winding down to an Eloh logos dispenser at (-113, 114, -602):
    ///   x -141 to -98, z -661 to -549.
    /// - Central Operations (7, the Bane interior; 29, the Ashen Desert sky): the control room of
    ///   the research HQ (arch_bane_industrial_room_control_24m_v18 at (7, 104, -321)) with its
    ///   consoles, under the HQ's landing pad. Like the Atta Laboratory's and Observatory's
    ///   pairs, region 29 with the outside sky is the room's exterior window
    ///   (arch_bane_industrial_overlook_exterior_window at (6, 112, -281)) over the lava; 7 the
    ///   rest of the room.
    /// - Central Service Passage (22, REGION_BANE_TUNNEL_01): the service corridors in the
    ///   middle of the HQ, the tee, ramps and turns climbing from the tunnels at y 72 to the
    ///   control room level at y 104 (x -22 to 47, z -398 to -341).
    /// - Immolation Bridge (17, REGION_ARIEKI_VOLCANO): the bridge from the HQ's north door
    ///   (arch_bane_industrial_tunnel_entrance at (7, 72, -281)) across the lava lake (y 17) to
    ///   the landing in front of the Enslaver Fortress's door, floor y 69: x -50 to 65, z -256
    ///   to -93 (all one set piece, arch_bane_industrial_tunnel_entrance_conscription).
    /// - Cell Block - West and Cell Block - East (26 and 27, REGION_BANE_TUNNEL_03): the
    ///   facility has five cell blocks ("Open the cells for all 5 Bane cell blocks") built of
    ///   prison chunnels (arch_bane_industrial_chunnel_straight_02_prison_32m); the two built
    ///   alike, in mirror image about x = 7, are the four-chunnel blocks at y 96 west and east of
    ///   the HQ (x -280 to -193 and 206 to 293, z -370 to -305).
    /// - Information Extraction (9, sky_bane_interior_arena): "Go to Information Extraction to
    ///   Find the Atta Queen": the round hall behind the Enslaver Fortress room
    ///   (arch_bane_industrial_chunnel_room_turn pieces round (7, 72, 151)) with its four
    ///   extraction chairs and sensors, research bio tables, the Bane enslaver cells and the
    ///   molten pit in its middle: x -82 to 95, z 62 to 217.
    ///
    /// The facility's other three cell blocks have no region of their own.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes out the rows as they went in.
    /// </summary>
    public static class ConscriptionFacilityRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 1988;
        public const uint CentralOperationsRegion = 7, InformationExtractionRegion = 9, ImmolationBridgeRegion = 17, CentralServicePassageRegion = 22,
            CellBlockWestRegion = 26, CellBlockEastRegion = 27, EntryRegion = 28, CentralOperationsWindowRegion = 29, HiddenCaveRegion = 35;
        public const byte Box = 2, AnyLevel = 0;

        private const string Map = "arieki_ligo_ashendesert_baneconscriptfacility - ";

        public static readonly Volume[] Volumes =
        {
            Of(EntryRegion, -12, 27, -662, -614, 148, 182, Map + "Entry (chunnel through the gate)"),
            Of(HiddenCaveRegion, -142, -97, -662, -548, 105, 170, Map + "Hidden Cave (cave west of the gate)"),
            Of(CentralOperationsRegion, -27, 40, -355, -281, 102, 133, Map + "Central Operations (control room)"),
            Of(CentralOperationsWindowRegion, -13, 26, -296, -272, 101, 131, Map + "Central Operations (exterior window)"),
            Of(CentralServicePassageRegion, -23, 48, -399, -340, 70, 112, Map + "Central Service Passage (HQ service corridors)"),
            Of(ImmolationBridgeRegion, -50, 65, -256, -93, 55, 100, Map + "Immolation Bridge (across the lava lake)"),
            Of(CellBlockWestRegion, -281, -192, -371, -304, 94, 122, Map + "Cell Block - West (prison chunnels)"),
            Of(CellBlockEastRegion, 205, 294, -371, -304, 94, 122, Map + "Cell Block - East (prison chunnels)"),
            Of(InformationExtractionRegion, -83, 96, 62, 218, 60, 116, Map + "Information Extraction (round hall)"),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {AnyLevel}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";
        }

        public static IEnumerable<string> Down()
        {
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
