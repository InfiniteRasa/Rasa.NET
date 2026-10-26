using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Region label circles given the floor and ceiling of the room their label stands in
    /// (Region_label_rooms).
    ///
    /// The label circles MapRegionPreloader and MapRegionMarkerPreloader seeded go from -2000 to
    /// 2000 high. On a map of one level that does not matter; in a building of several floors
    /// a room's region went through the floors above and below it: Omega Labs' Pharmacogenetics
    /// Unit, on the lowest of the labs' floors, was the region of the corridors two floors over
    /// it and of whatever is under it.
    ///
    /// Each circle below has its label standing inside a room piece of the map's architecture
    /// (an arch_* mesh named for a room, a hall, a chamber, a lab, a silo or the like, 12 to 200 m
    /// across; corridors, ramps, walls, floors, bulkheads, columns, platforms and doors do not
    /// count), and keeps its centre and radius but gets the height of the room pieces it stands
    /// in, in the .map: their lowest floor to their highest ceiling, a metre either side. Left
    /// as they are: Fort Defiance's circle (its label is on a bunker, the region is the open
    /// fort), the two wargame Staging Areas (disabled by Region_sky_circles), the three regions
    /// that share the Wetland Refinery's Processing Power Supply label in one 200 m circle, and
    /// Lou's Tavern in the arena (its label is on the top of its room).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike; each row changes only while it is the circle
    /// as seeded, and Down gives it its full height back.
    /// </summary>
    public static class RegionLabelRooms
    {
        public sealed record Room(uint MapContextId, uint RegionId, double X, double Z, double Radius, double MinY, double MaxY, string Place);

        public const double SeededMinY = -2000;
        public const double SeededMaxY = 2000;

        public static readonly Room[] Rooms =
        {
            // adv_foreas_concordia_divide_timoramines (1348)
            new(1348, 13, 472.5, -200.1, 94.0, 228.3, 261.0, "Timora Command Center"),
            // adv_foreas_valverde_marshes_wetlandrefinery (1429)
            new(1429, 19, -286.0, 61.3, 200.0, 60.4, 149.4, "Chemical Processing"),
            // adv_foreas_valverde_pools_test_weapons_center (1465)
            new(1465, 20, 184.0, -16.0, 109.0, 6.6, 51.7, "Testing Arena"),
            new(1465, 21, 249.0, -16.1, 40.0, 6.7, 51.4, "Operations Overlook"),
            // adv_foreas_valverde_marshes_logosresearchfacility (1700)
            new(1700, 16, 760.2, -136.0, 25.5, 316.3, 349.0, "Security Checkpoint"),
            new(1700, 23, 608.2, -351.8, 106.5, 246.6, 277.0, "Logos Analysis Lab"),
            new(1700, 27, 608.0, -128.0, 64.9, 245.5, 299.2, "Test Chamber"),
            new(1700, 29, 696.0, 56.0, 88.4, 246.6, 277.0, "Bio Experiment Lab B1"),
            new(1700, 30, 520.0, 40.2, 88.4, 246.6, 277.0, "Bio Experiment Lab B2"),
            // adv_foreas_valverde_pools_livetargetpensv2 (1763)
            new(1763, 19, -63.9, -56.6, 79.3, 49.6, 209.3, "Test Pit"),
            new(1763, 20, -63.9, -56.6, 200.0, 49.6, 209.3, "Main Test Pit"),
            // adv_foreas_concordia_divide_purgasstation2 (1806)
            new(1806, 7, 152.2, 128.5, 107.6, 60.3, 93.0, "Level 01: Control Room"),
            new(1806, 14, 152.2, 128.5, 107.6, 60.3, 93.0, "Level 02: Control Room"),
            new(1806, 19, 152.2, 128.5, 107.6, 60.3, 93.0, "Level 03: Control Room"),
            // adv_arieki_ligo_ashendesert_baneconscriptfacility (1988)
            new(1988, 30, -233.5, -145.2, 119.3, 94.6, 139.6, "Atta Laboratory"),
            new(1988, 31, 238.5, -144.8, 120.0, 78.6, 139.4, "Atta Observatory"),
            // adv_arieki_ligo_thunderhead_quassostation (2105)
            new(2105, 12, -215.0, 531.0, 98.2, -60.7, 51.8, "Quasso Central Command"),
            // adv_arieki_torden_mires_banefluxitemines (2115)
            new(2115, 3, -12.0, 123.8, 88.0, 139.3, 172.0, "Deep Planet Scan"),
            // adv_arieki_torden_abyss_dybukkar (2190)
            new(2190, 6, -104.0, -512.0, 82.5, 316.3, 349.0, "Security"),
            new(2190, 8, 0.0, -384.0, 66.9, 316.3, 349.0, "Operations"),
            new(2190, 13, -104.0, -512.0, 82.5, 316.3, 349.0, "Security Overlook"),
            new(2190, 18, 52.0, -296.0, 123.7, 318.9, 344.9, "Super Soldier Incubation"),
            new(2190, 19, -104.0, -512.0, 82.5, 316.3, 349.0, "Security Overlook"),
            // adv_arieki_torden_abyss_omegalabs (2203)
            new(2203, 4, -64.2, -299.9, 40.0, 254.5, 281.5, "Involuntary Commitment Ward"),
            new(2203, 5, -2.0, -195.3, 25.0, 238.5, 265.5, "Bioweapons Unit"),
            new(2203, 6, -0.1, -304.5, 40.0, 214.5, 241.5, "Pharmacogenetics Unit"),
            new(2203, 7, 103.7, -233.5, 40.0, 222.5, 249.5, "Reeducation Ward"),
            new(2203, 9, 151.4, -139.0, 46.9, 238.5, 265.5, "Network Operations Center")
        };

        public static IEnumerable<string> Up()
        {
            foreach (var r in Rooms)
                yield return $"UPDATE map_region SET min_y = {N(r.MinY)}, max_y = {N(r.MaxY)} WHERE {Seeded(r)} " +
                    $"AND min_y = {N(SeededMinY)} AND max_y = {N(SeededMaxY)};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Rooms)
                yield return $"UPDATE map_region SET min_y = {N(SeededMinY)}, max_y = {N(SeededMaxY)} WHERE {Seeded(r)} " +
                    $"AND ABS(min_y - {N(r.MinY)}) < 0.01 AND ABS(max_y - {N(r.MaxY)}) < 0.01;";
        }

        private static string Seeded(Room r) =>
            $"map_context_id = {r.MapContextId} AND region_id = {r.RegionId} AND shape = 1 " +
            $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
