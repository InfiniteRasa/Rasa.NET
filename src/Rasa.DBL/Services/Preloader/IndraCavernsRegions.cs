using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Indra Caverns' Brann storage facility and the Bane plant beyond it
    /// (adv_arieki_ligo_ashendesert_indracaverns, 2055; Indra_caverns_regions).
    ///
    /// The Brann facility (east) and the Gotha Processing Plant (west) are under the terrain
    /// (underground on the navmesh); the caverns round them and the mountain top over the plant
    /// are not.
    ///
    /// The Brann facility, Indra Storage Facility (regions 8, 11 and 13, alike in all but their
    /// priority; 8, the lowest, is placed):
    ///
    /// - Indra Storage Facility (8): its corridors, as the map's own minimap rectangles for it
    ///   (regions 33 and 35, which set nothing) have them, underground only so the cavern floor
    ///   round it stays the caverns', and cut round the Security Check Point, whose region is
    ///   the lower priority.
    /// - Security Check Point (7): the entrance from the cavern (arch_brann_corridor_entrance_01
    ///   at (64, 16, -328)) and the guard cross behind it, with its computer, chair and tables at
    ///   the map's label, to the door at x 80 to 84.
    /// - Science Lab (15): the four quarter rooms at y -8 round its label at (52, -396)
    ///   (x 21 to 83, z -426 to -364), one room.
    /// - Maintenance Lab (16): likewise the four quarter rooms round its label at (-24, -216)
    ///   (x -58 to 4, z -247 to -185); Region_room_volumes gave it the one quarter its label
    ///   is nearest, and that box becomes the room.
    ///
    /// The Bane plant, from the mission text (Desistance, Subversion):
    ///
    /// - Gotha Processing Plant (regions 19, 20 and 25 to 30): MapRegionPreloader put a 200 m
    ///   circle round its label for each, surface only, so the plant's Bane interior sky was over
    ///   the ridge at its north door and the landing pad on top of the mountain, and nowhere in
    ///   the plant. Region 19's circle becomes the plant (x -460 to -60, z -470 to -30, y 110 to
    ///   200: about the map's minimap rectangle 37 for it), underground only; the other seven are
    ///   disabled, as Region_sky_circles did elsewhere: which parts of the plant they are, nothing
    ///   says.
    /// - Juggernaut Bay (24): the great round hall at its label (arch_bane_industrial_chunnel_room
    ///   turns round (-340, 128, -264), x -428 to -268, z -336 to -200), its Juggernauts on the
    ///   floor at y 128, under the landing pad (y 170).
    /// - Juggernaut Operations (32): "the Juggernaut Operations room ... the doors leading to the
    ///   Juggernaut Bay below", with "Juggernaut maintenance consoles": the console gallery up the
    ///   bay's west wall at y 137 (the enclosed overlooks, x -411 to -386, z -307 to -220), over
    ///   the Juggernauts.
    /// - Barracks (23), where the Thrax Life Support Systems are: the two halls of Bane sleep pods
    ///   on catwalks off the guard octagon south-east (x -117 to -67, z -389 to -331, and x -201
    ///   to -143, z -433 to -383, y 129 to 152).
    /// - Bane Central Administration (22): "a sort of central administration for the Gotha gas
    ///   refinery" seen beside the landing pad on top of the mountain: the room under the Bane
    ///   tower there (arch_bane_exterior_large_tower_v03 over arch_bane_industrial_room_control_
    ///   24m_v10 at (-364, 168, -432)), with Overlord Kretas' command console and medical bays.
    ///
    /// The caverns, Indra Caverns (regions 1, 17 and 18, alike in name and cave sky; which cavern
    /// each is, nothing says). Region 17, the lowest, is given the two the facility's other regions
    /// leave out:
    ///
    /// - the cavern behind the facility's rear exit ("the cave area outside the rear exit of the
    ///   storage facility"), under its roof (terra_arieki_ligo_cave_roof02 at (77, 41, -111),
    ///   x -71 to 228, z -241 to 22), floor y 16 to 65: north of the facility's rooms at any
    ///   level, beside them on the cavern floor only;
    /// - the tunnel west from it to the ridge at the plant's north door, from the cave entrance
    ///   at (-56, 60, -112) through the cavern pieces to (-248, 68, -56), y 53 to 75, under the
    ///   ground (x -272 to -70, z -170 to -50), below the plant.
    ///
    /// The cavern at the way in keeps the Indra Watch Post's volumes; regions 1 and 18 stay
    /// without.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is
    /// loaded and no box of that region is at that spot; a changed row changes only while it is
    /// as seeded. Down takes out what went in and gives the changed rows back as they were.
    /// </summary>
    public static class IndraCavernsRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>A seeded row as it was (shape, radius or half sizes, height range, level) and the box it becomes.</summary>
        public sealed record Recut(uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2055;
        public const uint CheckPointRegion = 7, StorageFacilityRegion = 8, ScienceLabRegion = 15, MaintenanceLabRegion = 16, CavernsRegion = 17, GothaRegion = 19,
            CentralAdministrationRegion = 22, BarracksRegion = 23, JuggernautBayRegion = 24, JuggernautOperationsRegion = 32;
        public const byte Circle = 1, Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "arieki_ligo_ashendesert_indracaverns - ";

        public static readonly Volume[] Volumes =
        {
            Of(StorageFacilityRegion, -59, 187, -314, -184.5, -12, 40, UndergroundOnly, Map + "Indra Storage Facility (north)"),
            Of(StorageFacilityRegion, -59, 52, -343.5, -314, -12, 40, UndergroundOnly, Map + "Indra Storage Facility (west of the check point)"),
            Of(StorageFacilityRegion, 84, 187, -343.5, -314, -12, 40, UndergroundOnly, Map + "Indra Storage Facility (east of the check point)"),
            Of(StorageFacilityRegion, 52, 84, -343.5, -314, -12, 14, UndergroundOnly, Map + "Indra Storage Facility (under the check point)"),
            Of(StorageFacilityRegion, 20.5, 121.5, -429, -343.5, -12, 40, UndergroundOnly, Map + "Indra Storage Facility (south)"),
            Of(CheckPointRegion, 52, 84, -342, -314, 14, 28, AnyLevel, Map + "Security Check Point (entrance)"),
            Of(ScienceLabRegion, 20, 84, -427, -363, -10, 8, AnyLevel, Map + "Science Lab (room)"),
            Of(JuggernautBayRegion, -428, -270, -336, -200, 124, 165, AnyLevel, Map + "Juggernaut Bay (hall)"),
            Of(JuggernautOperationsRegion, -412, -386, -309, -218, 134, 152, AnyLevel, Map + "Juggernaut Operations (console gallery)"),
            Of(BarracksRegion, -118, -66, -390, -330, 126, 153, AnyLevel, Map + "Barracks (east pod hall)"),
            Of(BarracksRegion, -202, -142, -434, -383, 126, 153, AnyLevel, Map + "Barracks (south pod hall)"),
            Of(CentralAdministrationRegion, -398, -330, -466, -398, 165, 200, AnyLevel, Map + "Bane Central Administration (tower room)"),
            Of(CavernsRegion, -70, 210, -183, -30, 0, 100, AnyLevel, Map + "Indra Caverns (behind the facility's rear exit)"),
            Of(CavernsRegion, -70, 210, -230, -183, 0, 100, SurfaceOnly, Map + "Indra Caverns (behind the facility, beside its rooms)"),
            Of(CavernsRegion, -272, -70, -170, -50, 45, 90, AnyLevel, Map + "Indra Caverns (tunnel west to the plant)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(GothaRegion, Circle, -308.2, 137.6, -110.7, 200, 0, 0, -2000, 2000, SurfaceOnly, Map + "Gotha Processing Plant",
                Of(GothaRegion, -460, -60, -470, -30, 110, 200, UndergroundOnly, Map + "Gotha Processing Plant (the plant)")),
            new(MaintenanceLabRegion, Box, -12.0, 0.5, -200.7, 0, 16.0, 15.5, -1.0, 14.5, AnyLevel, Map + "Maintenance Lab (room)",
                Of(MaintenanceLabRegion, -59, 5, -248, -184, -1, 15, AnyLevel, Map + "Maintenance Lab (four quarters)")),
        };

        /// <summary>The Gotha Processing Plant circles turned off: region, as MapRegionPreloader seeded them.</summary>
        public static readonly uint[] DisabledGothaRegions = { 20, 25, 26, 27, 28, 29, 30 };
        public const double GothaX = -308.2, GothaZ = -110.7, GothaRadius = 200;

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, byte underground, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, underground, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {v.Underground}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            foreach (var r in Recuts)
                yield return $"UPDATE map_region SET {Set(r.Box)} WHERE {Was(r)};";

            foreach (var region in DisabledGothaRegions)
                yield return $"UPDATE map_region SET enabled = 0 WHERE {Gotha(region)} AND enabled = 1;";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var region in DisabledGothaRegions.Reverse())
                yield return $"UPDATE map_region SET enabled = 1 WHERE {Gotha(region)} AND enabled = 0;";

            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {r.Shape}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = {N(r.HalfX)}, half_z = {N(r.HalfZ)}, min_y = {N(r.MinY)}, max_y = {N(r.MaxY)}, underground = {r.Underground}, comment = {S(r.Comment)} " +
                    $"WHERE {At(r.Box, "map_region")} AND ABS(half_x - {N(r.Box.HalfX)}) < 0.01 AND ABS(half_z - {N(r.Box.HalfZ)}) < 0.01 AND comment = {S(r.Box.Comment)};";

            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)};";
        }

        private static string Set(Volume v) =>
            $"shape = {Box}, pos_x = {N(v.X)}, pos_y = {N(v.Y)}, pos_z = {N(v.Z)}, radius = 0, half_x = {N(v.HalfX)}, half_z = {N(v.HalfZ)}, " +
            $"min_y = {N(v.MinY)}, max_y = {N(v.MaxY)}, underground = {v.Underground}, comment = {S(v.Comment)}";

        private static string Was(Recut r) =>
            $"map_context_id = {MapContextId} AND region_id = {r.RegionId} AND shape = {r.Shape} " +
            $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01 " +
            $"AND ABS(half_x - {N(r.HalfX)}) < 0.01 AND ABS(half_z - {N(r.HalfZ)}) < 0.01 AND comment = {S(r.Comment)}";

        private static string Gotha(uint region) =>
            $"map_context_id = {MapContextId} AND region_id = {region} AND shape = {Circle} " +
            $"AND ABS(pos_x - {N(GothaX)}) < 0.01 AND ABS(pos_z - {N(GothaZ)}) < 0.01 AND ABS(radius - {N(GothaRadius)}) < 0.01";

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
