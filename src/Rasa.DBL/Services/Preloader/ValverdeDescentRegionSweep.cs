using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Valverde Descent (adv_foreas_valverde_descent, 2047;
    /// 20270721000000_Valverde_descent_region_sweep).
    ///
    /// AFS Fort Virgil (12): the seeded label circle (r 90, surface-only) never reached the fort's
    /// underground cargo bay, which therefore showed 'Valverde Descent', and it missed the west half of
    /// the HQ (Class Trainer). Recut to a box over the fort plateau (walls/fences x -432..-196, z
    /// -712..-556, y 700-780) plus a second box over the wormhole platform south of the bay ('Wormhole:
    /// Fort Virgil').
    ///
    /// Region 51 (hospital ambient) and 52 (base-command ambient), both with the fort's human-interior
    /// sky and AFS base music: boxes over arch_hum_fort_hospital_v02 (Fort Virgil Hospital safe zone,
    /// medical vendor) and arch_hum_fort_headqtrs_v02 (Class Trainer).
    ///
    /// Region 24 (Camp Cato - Valor Pass cave tunnel): its minimap rect covered only the middle of the
    /// tunnel; extended to both cave mouths, y 665-712, still underground-only.
    ///
    /// Antaeus Hollow (21): box extended north to z 708 to cover the rest of the flagged cave floor
    /// (cavern_floor x 106..154, z 609..705), y limited to 280-440. Antaeus Hollow Medic Station (22):
    /// the surface-only seeded circle sat over the underground tunnel room holding the hospital, medic
    /// tent and medical vendor, so it never applied; recut to an underground box over the tunnel room
    /// (x 88..163, z 564..610, y 370-410).
    ///
    /// Core Drill Monitoring Station (30 interior, 35 tier-5-sky twin): the underground Bane complex
    /// next to the drill - control room with two command chairs and overlook windows facing the drill,
    /// plus tunnels - which minimap rects 32/34 (created right after 30) outline. 30 is an underground-
    /// only box over the complex; 35 covers the control room at any level, so the two unflagged window
    /// bays get the pit sky.
    ///
    /// Merrick's Quarters (44 interior, 39 tier-5-sky twin): the underground room with four sleep pods
    /// and an overlook window at (411, 378, 308); its minimap rect is region 45, created right after
    /// 44. Same interior/window split as the drill station.
    ///
    /// Block AA-23 (38 interior, 43 tier-4-sky twin): the minimap-rect box had no height or underground
    /// limit and put the interior sky over the Mal Dys ground and barracks above; recut to underground-
    /// only, y 400-440. 43 covers the room at its level so its unflagged overlook-window bay gets the
    /// outdoor sky.
    ///
    /// Mal Dys interiors (26, 28): both minimap-rect boxes had no height or underground limit and put
    /// the Bane interior sky over open ground; recut to underground-only with the interiors' heights.
    /// Region 27 (Mal Dys outdoor variant) is added over the ground above each so that ground keeps the
    /// Mal Dys name.
    ///
    /// Not placed: the five Descent tiers (3, 5, 6, 7, 8) - tiers follow the spiral road rather than
    /// flat heights and nothing fixes their edges; Sanctus Caverns' outdoor variant (11); region 20
    /// (human 'large open' interior); unnamed caves 9 and 15; Bane interior 18; swamp ambient 46; test
    /// sky 19. Molder.sky (2) and marker-only rows (4, 53) skipped.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class ValverdeDescentRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2047;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_valverde_descent - ";

        public static readonly Volume[] Volumes =
        {
            Of(12, -368, -332, -756, -712, 700, 730, AnyLevel, Map + "AFS Fort Virgil (wormhole platform)"),
            Of(51, -266, -211, -637, -591, 735, 760, AnyLevel, Map + "region 51 (Fort Virgil hospital building)"),
            Of(52, -428, -372, -674, -604, 735, 776, AnyLevel, Map + "region 52 (Fort Virgil headquarters building)"),
            Of(30, 150, 290, -22, 166, 236, 312, UndergroundOnly, Map + "Core Drill Monitoring Station (rooms and tunnels)"),
            Of(35, 170, 260, 84, 166, 268, 312, AnyLevel, Map + "Core Drill Monitoring Station (window bays)"),
            Of(44, 372, 446, 268, 343, 372, 420, UndergroundOnly, Map + "Merrick's Quarters (sleep-pod room)"),
            Of(39, 372, 446, 268, 343, 372, 420, AnyLevel, Map + "Merrick's Quarters (window bay and entry)"),
            Of(43, 508, 578, 155, 256, 402, 440, AnyLevel, Map + "Block AA-23 (window bay)"),
            Of(27, 329, 563, -103, 61, 255, 380, AnyLevel, Map + "Mal Dys (lower city surface over the tunnels)"),
            Of(27, 609, 817, 200, 352, 440, 500, AnyLevel, Map + "Mal Dys (east city surface over the tunnels)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20133, 12, 1, -300.5, 737.2, -664, 89.9, 0, 0, -2000, 2000, 2, "foreas_valverde_descent - AFS Fort Virgil",
                Of(12, -432, -196, -712, -556, 700, 780, AnyLevel, Map + "AFS Fort Virgil (fort plateau and cargo bay)")),
            new(246, 24, 2, -296, 709, -399, 0, 70, 35, -2000, 2000, 1, "foreas_valverde_descent - region 24 minimap",
                Of(24, -366, -226, -470, -336, 665, 712, UndergroundOnly, Map + "region 24 (Camp Cato-Valor Pass cave tunnel)")),
            new(244, 21, 2, 187, 355.9, 560, 0, 112.5, 87.5, -2000, 2000, 1, "foreas_valverde_descent - Antaeus Hollow",
                Of(21, 74, 300, 472, 708, 280, 440, UndergroundOnly, Map + "Antaeus Hollow (cave and AFS camp)")),
            new(20135, 22, 1, 126.2, 382.5, 589.5, 74.2, 0, 0, -2000, 2000, 2, "foreas_valverde_descent - Antaeus Hollow Medic Station",
                Of(22, 88, 163, 564, 610, 370, 410, UndergroundOnly, Map + "Antaeus Hollow Medic Station (tunnel room)")),
            new(252, 38, 2, 543, 0, 221, 0, 52.5, 52.5, -2000, 2000, 0, "foreas_valverde_descent - Block AA-23",
                Of(38, 490, 596, 168, 274, 400, 440, UndergroundOnly, Map + "Block AA-23 (underground cell block room)")),
            new(248, 26, 2, 713, 0, 276, 0, 103.5, 75.5, -2000, 2000, 0, "foreas_valverde_descent - Mal Dys",
                Of(26, 609, 817, 200, 352, 415, 495, UndergroundOnly, Map + "Mal Dys (east underground rooms and tunnels)")),
            new(249, 28, 2, 446, 0, -21, 0, 116.5, 81.5, -2000, 2000, 0, "foreas_valverde_descent - Mal Dys",
                Of(28, 329, 563, -103, 61, 250, 360, UndergroundOnly, Map + "Mal Dys (lower underground rooms and tunnels)")),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, byte underground, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, underground, comment);

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
                    $"WHERE id = {r.RowId} AND map_context_id = {MapContextId} AND region_id = {r.RegionId} AND shape = {r.Shape} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01 " +
                    $"AND ABS(half_x - {N(r.HalfX)}) < 0.01 AND ABS(half_z - {N(r.HalfZ)}) < 0.01 " +
                    $"AND ABS(min_y - {N(r.MinY)}) < 0.01 AND ABS(max_y - {N(r.MaxY)}) < 0.01 AND underground = {r.Underground};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {r.Shape}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = {N(r.HalfX)}, half_z = {N(r.HalfZ)}, min_y = {N(r.MinY)}, max_y = {N(r.MaxY)}, underground = {r.Underground}, comment = {S(r.Comment)} " +
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
