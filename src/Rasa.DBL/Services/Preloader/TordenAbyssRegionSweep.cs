using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on Torden Abyss (adv_arieki_torden_abyss, 2028;
    /// 20270724000000_Torden_abyss_region_sweep).
    ///
    /// The Pitchfork (34): the tavern arch_hum_fort_tavern_v02 at (-308, 538, -41), with tables,
    /// chairs, beer kegs, a sink and dozens of beer bottles inside. It is the bar the region's
    /// REGION_HUMAN_BAR ambience names, and it sits in the Tantalus Base priority group. It showed the
    /// map default or, at its west end, the edge of the base circle. One box, floor 538, roof 551.
    /// Definite.
    ///
    /// Tantalus Cave (6): it has Tantalus Base's sky, env map and ambience with field music, one
    /// priority below the base, so it is the cavern the base sits in. That cavern is the map's west
    /// lobe, x -495..-240, z -440..190, floor 525-548. The base circle covers only its middle, so the
    /// north and south ends showed the map default. One box, y 515-575. Penumbra HQ, the base,
    /// hospital, offices and tavern all outrank it. Probable.
    ///
    /// Tampeii Cavern (40): it has the same settings as The Ruins of Tampeii, one priority below it,
    /// with Tampeii Settlement above. That makes it the east lobe's cave south of the Dybukkar
    /// Underground complex: destroyed Brann domes, prison walls and turbines, and the Tampeii waypoint.
    /// One box x 180..650, z -235..142, y 495-620. Probable.
    ///
    /// Region 21 (unnamed Brann interior): the Brann dome at (565, 577, 71), whose entrance ramp leads
    /// to arch_brann_corridor_instance, the door to the Tampeii instance map. Its settings are exactly
    /// that map's 'Tampeii Gateway' room. One underground-only box over the ramp and door; the doorway
    /// mouth and dome roof stay outdoors. Probable.
    ///
    /// Tampeii Settlement (recut row 224 plus one box): the settlement's rooms are underground Brann
    /// quarters at floor 505.5 with the medical station, below the dome at (344, 529, -59). The old
    /// any-level box also put the Brann interior sky and settlement name on the open cave floor around
    /// and above them. Recut to underground-only y 500-530, plus a small surface box for the dome's
    /// entrance corridor.
    ///
    /// Dybukkar Underground (recut row 232): the seeded 180 m surface circle reached south to z 21,
    /// over the Ruins of Tampeii, which then showed the Bane interior sky (the region outranks the
    /// ruins). Cut back to the complex's footprint, which box row 225 already covers.
    ///
    /// Archeron Outpost (recut row 20130): the circle was underground-only, but the camp's floor is not
    /// flagged, so it never applied. Replaced by a box over the camp (medic tent, generic tent, crates,
    /// sandbags), x -165..-90, z 265..360, floor 552-558. Definite.
    ///
    /// Not placed: region 17 'Torden Abyss' (dark cave variant at priority 28, no place found). Regions
    /// 1 (Molder.sky) and 38 (marker-only) skipped.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class TordenAbyssRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2028;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "arieki_torden_abyss - ";

        public static readonly Volume[] Volumes =
        {
            Of(34, -326, -290, -56, -27, 535, 552, AnyLevel, Map + "The Pitchfork (Tantalus tavern)"),
            Of(6, -495, -240, -440, 190, 515, 575, AnyLevel, Map + "Tantalus Cave (the cavern round Tantalus Base)"),
            Of(40, 180, 650, -235, 142, 495, 620, AnyLevel, Map + "Tampeii Cavern (east cave, ruins and settlement)"),
            Of(21, 540, 598, 45, 100, 560, 590, UndergroundOnly, Map + "region 21 (Brann gateway dome to Tampeii)"),
            Of(42, 336, 352, -86, -50, 524, 540, AnyLevel, Map + "Tampeii Settlement (dome entrance corridor)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(224, 42, 2, 316, 543.5, -66, 0, 39.5, 50, -2000, 2000, 0, "arieki_torden_abyss - Tampeii Settlement",
                Of(42, 280, 353, -113, -17, 500, 530, UndergroundOnly, Map + "Tampeii Settlement (underground Brann rooms)")),
            new(232, 43, 1, 445.9, 505.6, 201.2, 180, 0, 0, -2000, 2000, 2, "arieki_torden_abyss - Dybukkar Underground",
                Of(43, 268, 642, 143, 409, 480, 620, SurfaceOnly, Map + "Dybukkar Underground (Bane complex, surface)")),
            new(20130, 37, 1, -121.2, 554.4, 292.5, 80, 0, 0, -2000, 2000, 1, "arieki_torden_abyss - Archeron Outpost",
                Of(37, -165, -90, 265, 360, 545, 575, AnyLevel, Map + "Archeron Outpost (AFS camp)")),
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
