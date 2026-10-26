using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Torden Mires' interiors (adv_arieki_torden_mires, 1759; Torden_mires_regions).
    ///
    /// Five of the map's regions set an interior sky and had no volume: the label circles for Fort
    /// Haroun and the Bane Occupied Mires were turned off (Region_sky_circles) because they put
    /// that sky over open ground, and the other three never had one.
    ///
    /// - Bailey's Blunder (region 8: sky_foreas_interior_ustor_yard, env_human_interior,
    ///   REGION_BANE_TUNNEL_03) is the AFS tunnel. "You know that's what we call the tunnel, right?
    ///   Bailey's Blunder?"; "the tunnel we started digging to get into Bane territory without
    ///   going overland". The one human tunnel on the map runs from a sandbagged pit at
    ///   (-545, 211, -95) north for 560 m to an exit at (-688, 252, 516), underground all the way
    ///   (arch_hum_corridor_*: an entrance, corridors, a room at z 48 to 80, a crossing with a side
    ///   entrance at z 176 to 224, ramps, a hall at z 392 to 442). Its waypoint (-591, 217, 192) is
    ///   in the crossing, and Tharvox, "overseeing their side of the tunnel", is posted just past
    ///   the north exit at (-715, 256, 532). Eleven boxes follow it, from the south entrance's
    ///   mouth to the north one's.
    /// - Bane Occupied Mires (region 18: sky_bane_interior_default, env_bane_interior_default,
    ///   REGION_BANE_TUNNEL_02) is the Bane interior. The only enclosed Bane structure is the
    ///   chunnel works under the "Bane Mines" label: a chunnel from x -369 to -7 at z 863 to 927
    ///   (two entrances, mining machines) and the room it turns into, x -142 to 22, z 898 to 1040,
    ///   with an overlook window east; underground, floor 262 to 274. Three boxes. The region of
    ///   the same name without a sky (16) keeps its outdoor circle.
    /// - Fort Haroun (region 4: sky_arieki_general_human_interior, REGION_HUMAN_BASE_LARGE_CLOSED)
    ///   is the fort's closed base: a hall (arch_hum_corr_room_*, x 556 to 628, z 300 to 378) with
    ///   the waypoint and the requisition vendors in it, and corridors east through the Medical Bay
    ///   (688, 224, 375) to a second gate, x 548 to 788, between two gates in the hill
    ///   (arch_hum_fort_wall_entrance_32m at x 537 and 760); underground, floor 220 to 226. Its
    ///   label (612, 224, 404) is in the west corridor. Three boxes; the landing pads outside stay
    ///   out.
    /// - Region 21 (sky_arieki_general_human_interior, REGION_HUMAN_HOSPITAL, no name) is Baylor
    ///   Base's hospital: the map's only arch_hum_fort_hospital, at (-781, 227, -412), with beds,
    ///   stretchers, monitors and the Baylor Base Medical Center in it. Its box is the building,
    ///   floor 228, under 250.
    /// - Region 22 (sky_arieki_general_human_interior, REGION_HUMAN_BASE_COMMAND, no name) is
    ///   Baylor Base's headquarters: the map's only arch_hum_fort_headqtrs, at (-752, 253, -385),
    ///   with its consoles on the deck at 255. Its box is the building above 251, so it sits
    ///   over the hospital without overlapping it.
    ///
    /// Every box is for any level. Plain SQL, valid on SQLite and MySQL alike. Each row goes in
    /// only where the map is loaded and no box of that region is at that spot; Down takes them out.
    /// </summary>
    public static class TordenMiresRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 1759;
        public const uint FortHarounRegion = 4, BaileysBlunderRegion = 8, BaneInteriorRegion = 18, HospitalRegion = 21, CommandRegion = 22;
        public const byte Box = 2, AnyLevel = 0;

        private const string Map = "arieki_torden_mires - ";

        public static readonly Volume[] Volumes =
        {
            Of(BaileysBlunderRegion, -576, -528, -48, 48, 205, 232, Map + "Bailey's Blunder south entrance and corridor"),
            Of(BaileysBlunderRegion, -620, -532, 48, 80, 200, 232, Map + "Bailey's Blunder room"),
            Of(BaileysBlunderRegion, -592, -560, 80, 176, 204, 218, Map + "Bailey's Blunder twin corridors"),
            Of(BaileysBlunderRegion, -632, -556, 176, 224, 204, 232, Map + "Bailey's Blunder crossing"),
            Of(BaileysBlunderRegion, -608, -592, 224, 264, 205, 225, Map + "Bailey's Blunder ramp"),
            Of(BaileysBlunderRegion, -624, -592, 264, 360, 212, 226, Map + "Bailey's Blunder north corridors"),
            Of(BaileysBlunderRegion, -626, -608, 360, 422, 212, 242, Map + "Bailey's Blunder ramps up"),
            Of(BaileysBlunderRegion, -650, -608, 408, 424, 228, 248, Map + "Bailey's Blunder turn west"),
            Of(BaileysBlunderRegion, -746, -646, 392, 442, 236, 250, Map + "Bailey's Blunder hall"),
            Of(BaileysBlunderRegion, -712, -680, 440, 498, 230, 264, Map + "Bailey's Blunder north ramps"),
            Of(BaileysBlunderRegion, -704, -672, 496, 514, 245, 270, Map + "Bailey's Blunder north entrance"),
            Of(BaneInteriorRegion, -372, -5, 862, 930, 255, 292, Map + "Bane Occupied Mires chunnel"),
            Of(BaneInteriorRegion, -145, -8, 895, 1045, 255, 308, Map + "Bane Occupied Mires chunnel room"),
            Of(BaneInteriorRegion, -8, 22, 940, 985, 255, 300, Map + "Bane Occupied Mires overlook"),
            Of(FortHarounRegion, 556, 628, 300, 378, 216, 244, Map + "Fort Haroun hall"),
            Of(FortHarounRegion, 548, 730, 370, 414, 218, 236, Map + "Fort Haroun corridors"),
            Of(FortHarounRegion, 710, 788, 342, 394, 218, 236, Map + "Fort Haroun east corridors"),
            Of(HospitalRegion, -815, -750, -444, -379, 225, 250, Map + "Baylor Base hospital"),
            Of(CommandRegion, -782, -724, -421, -349, 251, 292, Map + "Baylor Base headquarters"),
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
