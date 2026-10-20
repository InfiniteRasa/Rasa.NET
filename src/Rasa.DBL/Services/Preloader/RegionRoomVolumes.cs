using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Interior regions given the room their map label stands in (Region_room_volumes).
    ///
    /// A region is a .map row of sky, environment map, ambience and music the client applies when
    /// the server says the player is in it (regionmanager.py OnUpdateRegions); the volumes that
    /// said where each one is did not ship (the maps' GBB_RegionTrigger entities, classes 4318 to
    /// 4320, are in no .map). map_region was seeded from the region labels and cavern minimaps
    /// (MapRegionPreloader) and the markers named like a region (MapRegionMarkerPreloader), which
    /// left out interiors: a circle cannot tell a room from the ground round it.
    ///
    /// Each row here is an interior region - a Bane, Brann, human or Eloh interior sky, ambience or
    /// environment map - whose name is a POI label, region label or safe zone on its map that
    /// stands inside exactly one room piece of the map's architecture (an arch_* mesh named for a
    /// room, a hospital or a tomb, 12 to 200 m across; corridors, ramps, walls, floors and
    /// entrances do not count). The volume is that piece's bounds in the .map: a box, its floor
    /// to its ceiling a metre either side, for players under the ground or over it. Where two
    /// regions' labels stand in one room, or a label stands in more than one, the region is
    /// left out. A region bigger than its room (the Intelligence Wing) has the room for now.
    ///
    /// Every other region still without a volume is in Rasa\world\region_volume_backlog.csv,
    /// with what it sets and why it is not placed.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where its map is loaded
    /// and no volume of that region is already at that spot; Down takes out the rows as they went
    /// in.
    /// </summary>
    public static class RegionRoomVolumes
    {
        public sealed record Room(uint MapContextId, uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const byte Box = 2;
        public const byte UndergroundAny = 0;

        public static readonly Room[] Rooms =
        {
            // adv_arieki_ligo_ashendesert_baneconscriptfacility (1988)
            new(1988, 3, 226.5, 81.2, -145.2, 76.2, 64.2, 78.8, 139.4, "arieki_ligo_ashendesert_baneconscriptfacility - Atta Observatory (room)"),
            new(1988, 10, 6.5, 71.5, 26.5, 32.7, 36.4, 70.5, 101.0, "arieki_ligo_ashendesert_baneconscriptfacility - Enslaver Fortress (room)"),
            new(1988, 23, 6.5, 104.8, -469.0, 40.2, 36.4, 94.5, 125.1, "arieki_ligo_ashendesert_baneconscriptfacility - Sentient Conditioning (room)"),
            // adv_arieki_ligo_ashendesert_indracaverns (2055)
            new(2055, 9, 167.7, 9.6, -296.0, 15.8, 11.7, 7.5, 23.8, "arieki_ligo_ashendesert_indracaverns - Brann Sanctuary (room)"),
            new(2055, 10, 103.7, 0.5, -352.0, 15.8, 11.7, -0.5, 15.8, "arieki_ligo_ashendesert_indracaverns - Science Dorm (room)"),
            new(2055, 12, 28.0, 16.0, -224.0, 20.2, 24.2, 14.6, 33.9, "arieki_ligo_ashendesert_indracaverns - Indra Security Station (room)"),
            new(2055, 14, 16.3, 8.5, -280.0, 15.8, 11.7, 7.5, 23.8, "arieki_ligo_ashendesert_indracaverns - Maintenance Dorm (room)"),
            new(2055, 16, -12.0, 0.5, -200.7, 16.0, 15.5, -1.0, 14.5, "arieki_ligo_ashendesert_indracaverns - Maintenance Lab (room)"),
            // adv_arieki_ligo_burningsteps (1993)
            new(1993, 8, -300.0, 180.5, 320.0, 16.1, 11.7, 179.5, 191.0, "arieki_ligo_burningsteps - Staal: Larai Quarter (room)"),
            new(1993, 9, -216.0, 196.2, 472.0, 24.0, 24.0, 194.4, 216.4, "arieki_ligo_burningsteps - Staal: Viddia Quarter (room)"),
            new(1993, 11, -215.2, 196.5, 204.7, 15.2, 15.5, 195.5, 210.5, "arieki_ligo_burningsteps - Staal: Top Khana Quarter (room)"),
            // adv_arieki_torden_abyss (2028)
            new(2028, 33, -421.8, 536.6, -17.6, 21.9, 26.7, 535.0, 558.4, "arieki_torden_abyss - Tantalus Base Hospital (room)"),
            // adv_arieki_torden_abyss_dybukkar (2190)
            new(2190, 12, 20.0, 319.8, -141.0, 76.3, 77.3, 318.8, 363.6, "arieki_torden_abyss_dybukkar - Main Production Chamber (room)"),
            // adv_arieki_torden_incline_wardenbotfactory (2111)
            new(2111, 2, -424.0, 71.8, -12.0, 71.7, 72.3, 70.6, 108.9, "arieki_torden_incline_wardenbotfactory - Raw Material Handling (room)"),
            // adv_foreas_concordia_wilderness_guardianprom (1416)
            new(1416, 3, 352.5, 333.1, 256.0, 24.6, 25.4, 330.7, 350.5, "foreas_concordia_wilderness_guardianprom - Chamber of Eternity (room)"),
            // adv_foreas_howlingmaw_cuthahbase (2162)
            new(2162, 20, 149.0, 139.6, -331.8, 40.2, 36.4, 138.5, 169.1, "foreas_howlingmaw_cuthahbase - Level 2: Intelligence Wing (room)"),
            // adv_foreas_valverde_plateau (1497)
            new(1497, 32, -651.3, 466.6, 540.0, 37.8, 30.0, 460.2, 548.7, "foreas_valverde_plateau - Vogren's Tomb (room)"),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var r in Rooms)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {r.MapContextId}, {r.RegionId}, {Box}, {N(r.X)}, {N(r.Y)}, {N(r.Z)}, 0, {N(r.HalfX)}, {N(r.HalfZ)}, {N(r.MinY)}, {N(r.MaxY)}, {UndergroundAny}, 1, {S(r.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {r.MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(r, "x")});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Rooms)
                yield return $"DELETE FROM map_region WHERE {At(r, "map_region")} AND ABS(half_x - {N(r.HalfX)}) < 0.01 AND ABS(half_z - {N(r.HalfZ)}) < 0.01 AND comment = {S(r.Comment)};";
        }

        private static string At(Room r, string t) =>
            $"{t}.map_context_id = {r.MapContextId} AND {t}.region_id = {r.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(r.X)}) < 0.01 AND ABS({t}.pos_z - {N(r.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
