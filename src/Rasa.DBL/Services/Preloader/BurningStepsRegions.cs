using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Staal's buildings, the Eloh places and the Chaukus Robotics Facility on Burning Steps
    /// (adv_arieki_ligo_burningsteps, 1993; Burning_steps_regions).
    ///
    /// Staal is a Brann prison city under the crater floor, inside a ring of lava walls
    /// (arch_brann_staal_detention_lava_wall), its gatehouse (arch_brann_prison at (-76, 212,
    /// 320)) opening east onto a walled yard where the map's "Staal" label stands. Every room
    /// in it is under the terrain (underground on the navmesh), so the label circles seeded for
    /// it (surface only) applied in none of it. Its regions:
    ///
    /// - 3, "Staal Detention Center" (priority 6: the Brann interior sky,
    ///   REGION_GENERIC_ROOM_TONE_MEDIUM, MUSIC_CRUCIBLE_04), the lowest of Staal's interiors:
    ///   the whole city, x -385 to -10, z 180 to 500, underground only, so the quarters'
    ///   regions take their rooms over it and the ground over it stays outdoors.
    /// - 2, "Staal Centrum" (priority 9, REGION_HUMAN_BASE_LARGE_OPEN): the panopticon
    ///   (arch_brann_corridor_room_01_panopticon at (-216, 108, 320)), its ring of cells and
    ///   floor at y 181 to 189 over a lava pit, with the Centrum's crafting stations and vendors
    ///   and the shops at its east door; up to y 230, under Kappa Gruppa's office at the top of
    ///   its tower (y 245).
    /// - 24, "Staal: Incurables Ward Entrance" (priority 7): the operations room north of the
    ///   panopticon (x -228 to -204, z 388 to 420, y 181) with the Incurables Ward's door (map
    ///   link 97), under the Viddia Quarter's ore chamber.
    /// - 20, "Staal: Larai Arena" (priority 16): the Brann hall at the label "The Larai Arena"
    ///   (arch_brann_corridor_room_01_v03 at (-348, 164, 328), x -380 to -316, z 288 to 368,
    ///   y 172 to 195), under the junkyard bridge.
    /// - 28, "Staal Nightclub" (priority 17): the dorm room off the Larai Quarter's corridor
    ///   (x -308 to -284, z 356 to 388, y 197 to 211) with its bar, beer kegs, speakers and the
    ///   platform between them, and the weapon racks at its door.
    /// - 27, "Staal Detention Center" (priority 5, the red dam sky and REGION_ARIEKI_LAVA_FIELDS:
    ///   outdoors), whose 40 m circle stood at the waypoint inside the panopticon: the gate yard
    ///   (x -30 to 60, z 266 to 374, between the prison walls, from its doors), the one part
    ///   of the detention center under the sky.
    ///
    /// Region 33, "Eloh Space" (priority 30, the Eloh interior sky), is the map's three Eloh
    /// corridors, each with a logos dispenser: one beside the Top Khana Quarter (x -288 to -252,
    /// z 208 to 248), one behind an Eloh door with its projectors north of the Staal Overlook
    /// (x 12 to 52, z -12 to 68), one by Khulago Base (x 589 to 611, z -240 to -192).
    ///
    /// Region 21, "Eloh Crash Site" (priority 4, MUSIC_ELOH_BATTLEFIELD_01), is the crater east
    /// of the Devil's Pitchfork with the Eloh reliquary escape pod at (575, 157, 232) in it,
    /// ringed with lava: x 520 to 640, z 170 to 295. The Devil's Pitchfork's label circle
    /// (149 m) reached over the whole crater; it is cut to 75 m, the lava pitchfork it is named
    /// for.
    ///
    /// Region 29, "Chaukus Robotics Facility" (priority 11, outdoors), is the walled Brann
    /// compound at the robotics facility's door (map link 98 at (-528.6, 249.4, 581.6)): its
    /// walls, turbines and spotlights from x -621 to -435 and z 591 north (the box from z 570,
    /// taking in the door outside the south gate), round the facility's
    /// own entrance structure (arch_wardenbot_corridor_entrance_01 at (-528, 232, 784), to
    /// z 835 and y 396).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is
    /// loaded and no box of that region is at that spot; a changed row changes only while it is
    /// as seeded. Down takes out what went in and gives the changed rows back as they were.
    /// </summary>
    public static class BurningStepsRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        public const uint MapContextId = 1993;
        public const uint CentrumRegion = 2, DetentionCenterRegion = 3, LaraiArenaRegion = 20, IncurablesWardEntranceRegion = 24,
            DetentionCenterOutdoorsRegion = 27, NightclubRegion = 28, ElohSpaceRegion = 33, ElohCrashSiteRegion = 21,
            RoboticsFacilityRegion = 29, DevilsPitchforkRegion = 17;
        public const byte Circle = 1, Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "arieki_ligo_burningsteps - ";

        public static readonly Volume[] Volumes =
        {
            Of(DetentionCenterRegion, -385, -10, 180, 500, 165, 262, UndergroundOnly, Map + "Staal Detention Center (the city under the crater floor)"),
            Of(CentrumRegion, -290, -134, 250, 388, 175, 230, AnyLevel, Map + "Staal Centrum (panopticon)"),
            Of(IncurablesWardEntranceRegion, -229, -203, 386, 426, 178, 189, AnyLevel, Map + "Staal: Incurables Ward Entrance (operations room)"),
            Of(LaraiArenaRegion, -381, -315, 287, 369, 168, 196, AnyLevel, Map + "Staal: Larai Arena (hall)"),
            Of(NightclubRegion, -309, -283, 344, 389, 194, 212, AnyLevel, Map + "Staal Nightclub (dorm room)"),
            Of(ElohSpaceRegion, -290, -250, 206, 250, 178, 192, AnyLevel, Map + "Eloh Space (corridor by the Top Khana Quarter)"),
            Of(ElohSpaceRegion, 10, 54, -14, 70, 229, 242, AnyLevel, Map + "Eloh Space (corridor north of the Staal Overlook)"),
            Of(ElohSpaceRegion, 587, 613, -242, -190, 177, 192, AnyLevel, Map + "Eloh Space (corridor by Khulago Base)"),
            Of(ElohCrashSiteRegion, 520, 640, 170, 295, 120, 240, AnyLevel, Map + "Eloh Crash Site (crater)"),
            Of(RoboticsFacilityRegion, -622, -434, 570, 840, 225, 400, AnyLevel, Map + "Chaukus Robotics Facility (compound)"),
        };

        /// <summary>Region 27's seeded circle at the Detention Center waypoint and the gate yard box it becomes.</summary>
        public const uint YardRowId = 20118;
        public const double YardSeededX = -232.0, YardSeededY = 181.0, YardSeededZ = 320.0, YardSeededRadius = 40.0;
        public const string YardSeededComment = Map + "Staal Detention Center";
        public static readonly Volume Yard = Of(DetentionCenterOutdoorsRegion, -30, 60, 266, 374, 180, 230, AnyLevel, Map + "Staal Detention Center (gate yard)");

        /// <summary>The Devil's Pitchfork's seeded circle and the radius it is cut to.</summary>
        public const uint PitchforkRowId = 20119;
        public const double PitchforkX = 444.7, PitchforkZ = 253.8, PitchforkSeededRadius = 149.4, PitchforkRadius = 75;

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, byte underground, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, underground, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {v.Underground}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            yield return $"UPDATE map_region SET shape = {Box}, pos_x = {N(Yard.X)}, pos_y = {N(Yard.Y)}, pos_z = {N(Yard.Z)}, radius = 0, " +
                $"half_x = {N(Yard.HalfX)}, half_z = {N(Yard.HalfZ)}, min_y = {N(Yard.MinY)}, max_y = {N(Yard.MaxY)}, underground = {AnyLevel}, comment = {S(Yard.Comment)} " +
                $"WHERE id = {YardRowId} AND map_context_id = {MapContextId} AND region_id = {DetentionCenterOutdoorsRegion} AND shape = {Circle} " +
                $"AND ABS(pos_x - {N(YardSeededX)}) < 0.01 AND ABS(pos_z - {N(YardSeededZ)}) < 0.01 AND ABS(radius - {N(YardSeededRadius)}) < 0.01;";

            yield return $"UPDATE map_region SET radius = {N(PitchforkRadius)} WHERE {Pitchfork(PitchforkSeededRadius)};";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"UPDATE map_region SET radius = {N(PitchforkSeededRadius)} WHERE {Pitchfork(PitchforkRadius)};";

            yield return $"UPDATE map_region SET shape = {Circle}, pos_x = {N(YardSeededX)}, pos_y = {N(YardSeededY)}, pos_z = {N(YardSeededZ)}, radius = {N(YardSeededRadius)}, " +
                $"half_x = 0, half_z = 0, min_y = -2000, max_y = 2000, underground = {SurfaceOnly}, comment = {S(YardSeededComment)} " +
                $"WHERE id = {YardRowId} AND {At(Yard, "map_region")} AND ABS(half_x - {N(Yard.HalfX)}) < 0.01 AND ABS(half_z - {N(Yard.HalfZ)}) < 0.01;";

            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)};";
        }

        private static string Pitchfork(double radius) =>
            $"id = {PitchforkRowId} AND map_context_id = {MapContextId} AND region_id = {DevilsPitchforkRegion} AND shape = {Circle} " +
            $"AND ABS(pos_x - {N(PitchforkX)}) < 0.01 AND ABS(pos_z - {N(PitchforkZ)}) < 0.01 AND ABS(radius - {N(radius)}) < 0.01";

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
