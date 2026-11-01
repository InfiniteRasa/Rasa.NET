using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Logos Research Facility
    /// (adv_foreas_valverde_marshes_logosresearchfacility, 1700;
    /// 20270719020000_Logos_research_facility_region_sweep).
    ///
    /// Logos Research Facility is a single interior map with no underground-flagged navmesh, three
    /// floors: top (y 320..367: entrance ramp from the east, field medic, Security Checkpoint), middle
    /// (y 288..316: three research control rooms, the prison chunnel, Specimen Control overlook) and
    /// bottom (y 248..271: the Test Chamber voro room with four wings, Bio Labs B1/B2, Logos Analysis
    /// Lab).
    ///
    /// Advance Materials Research North/South/West/East (42/39/41/40): the four wings around the Test
    /// Chamber on the bottom floor, paired by compass (+z north, +x east). North: goo pool and service
    /// rooms x 545..672, z -40..25. South: goo pool hall x 520..712, z -282..-216. West: chunnel run
    /// with machines x 476..544. East: service rooms and tunnels x 696..776, z -174..-82.
    ///
    /// Level 3 (38): the bottom floor as a whole, so its corridors stop showing the map default; all
    /// bottom-floor rooms sit just above it in priority and override it. Medium confidence.
    ///
    /// Holding Cells (2): the prison chunnel pieces on the middle floor (x 705..751, z -287..-216),
    /// under the Specimen Control overlook.
    ///
    /// Specimen Control (15) recut: its circle ran through all floors and at priority 24 overrode the
    /// bottom-floor east wing and part of the Security room. It is now the same footprint on the middle
    /// floor only.
    ///
    /// Security Checkpoint (16) recut: the r 25.5 circle only covered the room centre; the box follows
    /// the control room v14 so its outer ring stops showing the default.
    ///
    /// Bio Experiment Lab B1/B2 (29/30) and Logos Analysis Lab (23) recut: their big label circles
    /// covered the north and south wings and corridors on the bottom floor. Boxes follow each
    /// room_control_24m_v13; these are shrink-only, so their tests show areas released to the wings and
    /// Level 3 plus keep-tests inside the rooms.
    ///
    /// Not placed: Level 1, Level 2 (floor split unclear against the priority order), Specimen Research
    /// 1-3 (three candidate rooms identified, numbering unknown), Facility Operations, Records, region
    /// 1 (Molder.sky).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class LogosResearchFacilityRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 1700;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_valverde_marshes_logosresearchfacility - ";

        public static readonly Volume[] Volumes =
        {
            Of(42, 545, 672, -40, 25, 244, 284, AnyLevel, Map + "Advance Materials Research - North (north wing)"),
            Of(39, 520, 712, -282, -216, 244, 284, AnyLevel, Map + "Advance Materials Research - South (south wing)"),
            Of(41, 476, 544, -160, -96, 244, 284, AnyLevel, Map + "Advance Materials Research - West (west wing)"),
            Of(40, 696, 776, -174, -82, 244, 284, AnyLevel, Map + "Advance Materials Research - East (east wing)"),
            Of(38, 470, 860, -400, 100, 240, 284, AnyLevel, Map + "Level 3 (bottom floor)"),
            Of(2, 704, 753, -288, -215, 284, 314, AnyLevel, Map + "Holding Cells (prison chunnel)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20057, 15, 1, 728.5, 297.5, -176, 25.5, 0, 0, -2000, 2000, 0, "foreas_valverde_marshes_logosresearchfacility - Specimen Control",
                Of(15, 703, 754, -202, -150, 286, 314, AnyLevel, Map + "Specimen Control (overlook above the cells)")),
            new(20052, 16, 1, 760.2, 320.2, -136, 25.5, 0, 0, 316.3, 349, 0, "foreas_valverde_marshes_logosresearchfacility - Security Checkpoint",
                Of(16, 727, 793, -169, -103, 316, 349, AnyLevel, Map + "Security Checkpoint (top-floor control room)")),
            new(20054, 29, 1, 696, 260.5, 56, 88.4, 0, 0, 246.6, 277, 0, "foreas_valverde_marshes_logosresearchfacility - Bio Experiment Lab B1",
                Of(29, 655, 730, 28, 84, 244, 280, AnyLevel, Map + "Bio Experiment Lab B1 (lab room)")),
            new(20055, 30, 1, 520, 260.2, 40.2, 88.4, 0, 0, 246.6, 277, 0, "foreas_valverde_marshes_logosresearchfacility - Bio Experiment Lab B2",
                Of(30, 486, 561, 12, 68, 244, 280, AnyLevel, Map + "Bio Experiment Lab B2 (lab room)")),
            new(20056, 23, 1, 608.2, 262.8, -351.8, 106.5, 0, 0, 246.6, 277, 0, "foreas_valverde_marshes_logosresearchfacility - Logos Analysis Lab",
                Of(23, 580, 636, -386, -311, 244, 280, AnyLevel, Map + "Logos Analysis Lab (lab room)")),
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
