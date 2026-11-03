using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Live Target Pens (adv_foreas_valverde_pools_livetargetpensv2,
    /// 1763; 20270723010000_Live_target_pens_region_sweep).
    ///
    /// Main Test Pit (region 20, recut row 149): the seeded label circle was radius 200 with priority
    /// 11, so it named nearly the whole facility 'Main Test Pit' with the arena sky and buried every
    /// lower-priority room region. It now covers only arch_bane_industrial_chunnel_room_arena_v02 (x
    /// -128..0, z -117..8, y 49..210), where its REGION_LABEL is.
    ///
    /// Test Pit (region 19, recut row 20085 plus four boxes): the old circle sat on the Main Test Pit
    /// label under Main Test Pit and never showed. The map's three POI_LABEL 'Creature Test Pit'
    /// markers are in the three corner pit rooms (chunnel_straight_01_32m_v04 with drill wrecks), each
    /// with an observation overlook (overlook_straight_coupler_16m x2, command chair, specimen racks)
    /// leading to a control room. SW pit is the recut row; NW and SE pits and the three overlooks are
    /// new boxes.
    ///
    /// Transport Hanger (region 14): the dome room (chunnel_room_turn_v05 + 3x room_turn_v02, x 8..160,
    /// z -8..120) labelled 'Dropship Hangar' by the map, arena-style sky like the Main Test Pit,
    /// mission text about a 'teleportation hanger'.
    ///
    /// Cell Block - East (region 15): the only one of the three prison chunnels on the east side (x
    /// 33..79, z -120..-88), corpses in the cells.
    ///
    /// Guard Room (region 11): room_control_24m_v16 (x -41..25, z -337..-271), the first room off the
    /// entrance tunnel, containing the 'Guardstation' hospital marker.
    ///
    /// No navmesh polygon on this map has the underground flag, so all boxes use 0. Floors are 152-162,
    /// the zone-in tunnel 185-199, the arena pit bottom 51.
    ///
    /// Not placed: Cell Block - West / Central (two mirror-image prison chunnels on the west arm, can't
    /// tell which is which), Armory, Maintenance, Operations (twin candidate rooms or several
    /// disconnected service networks), the three Concourses (two competing readings), region 2
    /// (Molder.sky). These rooms now show the map name 'Live Target Pens' and its interior sky instead
    /// of 'Main Test Pit'.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class LiveTargetPensRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 1763;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_valverde_pools_livetargetpensv2 - ";

        public static readonly Volume[] Volumes =
        {
            Of(19, -197, -133, 23, 89, 148, 180, AnyLevel, Map + "Test Pit (NW creature pit)"),
            Of(19, -133, -104, 40, 72, 148, 180, AnyLevel, Map + "Test Pit (NW pit observation overlook)"),
            Of(19, 4, 69, -201, -135, 148, 180, AnyLevel, Map + "Test Pit (SE creature pit)"),
            Of(19, -24, 4, -184, -152, 148, 180, AnyLevel, Map + "Test Pit (SE pit observation overlook)"),
            Of(19, -133, -104, -184, -152, 148, 180, AnyLevel, Map + "Test Pit (SW pit observation overlook)"),
            Of(14, 8, 160, -8, 120, 148, 196, AnyLevel, Map + "Transport Hanger (dropship hangar dome)"),
            Of(15, 33, 79, -120, -88, 148, 180, AnyLevel, Map + "Cell Block - East (east prison chunnel)"),
            Of(11, -41, 25, -337, -271, 148, 180, AnyLevel, Map + "Guard Room (guard station room by the entrance)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(149, 20, 1, -63.9, 152.1, -56.6, 200, 0, 0, 49.6, 209.3, 2, "foreas_valverde_pools_livetargetpensv2 - Main Test Pit",
                Of(20, -128, 0, -117, 8, 49, 210, AnyLevel, Map + "Main Test Pit (central arena)")),
            new(20085, 19, 1, -63.9, 152.1, -56.6, 79.3, 0, 0, 49.6, 209.3, 0, "foreas_valverde_pools_livetargetpensv2 - Test Pit",
                Of(19, -197, -133, -201, -135, 148, 180, AnyLevel, Map + "Test Pit (SW creature pit)")),
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
