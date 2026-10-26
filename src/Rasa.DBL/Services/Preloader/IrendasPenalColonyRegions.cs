using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Irendas Penal Colony's buildings on Torden Plains (Irendas_penal_colony_regions).
    ///
    /// The colony is the walled Brann compound round its label at (208, -116): prison walls
    /// (arch_brann_prisonwall, x 3 to 405, z -277 to 5) round the prison block
    /// (arch_brann_prison with its two barracks wings, x 166 to 250, z -260 to -189), the
    /// entrance dome north of it (208, -163), the HQ tower in the north-east (arch_brann_hq_01 at
    /// (320, 433, -34)), tents, solar towers and the vendors. Region 5, its outdoor half, has its
    /// 200 m circle. Its rooms are what had no volume:
    ///
    /// - Region 6, "Irendas Penal Colony" (priority 20: sky_arieki_brann_interior_default,
    ///   env_brann_interior, REGION_BRANN_INTERNAL_COORIDORS), whose seeded circle
    ///   Region_sky_circles turned off, is the Brann corridors under the prison block. Three
    ///   corridor entrances lead down from the surface (y 433) at (160, -163), (208, -147) under
    ///   the dome and (256, -163), by ramps to a level at y 425 (z -207 to -171), a level at
    ///   y 417 and one at y 401 to 409 (z -287 to -231), with the greenhouse at y 393 under the
    ///   dome (x 184 to 232, z -195 to -147): the map's minimap rectangles 22 and 23 (sets
    ///   nothing) are its two levels. A corridor runs west at y 401 to two Brann rooms with
    ///   machinery and a control room (x 48 to 160, z -307 to -251), at whose end is the door to
    ///   the Penal Research instance (map link 75 at (54.5, 401.5, -279)). Two boxes: the
    ///   corridors x 146 to 270, z -287 to -135, y 390 to 431; the west rooms x 44 to 146,
    ///   z -307 to -251, y 398 to 431. Both stay under the compound's ground.
    /// - Region 9, "Irendas Colony Hospital" (priority 21, all five settings and
    ///   MUSIC_SAFE_ZONE_BRANN), is the ward at the foot of the dome entrance's ramp
    ///   (arch_brann_corridor_room_01_dorm_v01 at (208, 425, -207)): six Brann beds, medical
    ///   crates and the hospital revive point (teleporter 145, "Hospital: Plains Post Medical
    ///   Tent", at (208, 425.5, -218)). Its box is the room, x 196 to 220, z -239 to -207,
    ///   y 424 to 432.
    /// - Regions 10 and 11, "Dormitory A" and "Dormitory B" (priorities 25 and 26, the Brann
    ///   interior sky and their names), are the two other dorm rooms, at y 417 off the
    ///   corridor west and east of the hospital (x 148 to 172 and 244 to 268, z -279 to -247),
    ///   where the class trainers stand. Nothing in the client says which is A; the west room is
    ///   taken as A.
    /// - Region 25, "Irendas Command Center" (priority 19: env_brann_interior,
    ///   REGION_HUMAN_BASE_COMMAND, no sky of its own), is the room at the top of the HQ tower:
    ///   workstations, monitors, Brann computers and midscreens at y 499 in its core (x 296 to
    ///   344, z -64 to -16), under its dome (to y 523). Its box is that core from y 495 to 524,
    ///   clear of the tower's legs and of the vendors at its foot.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes out the rows as they went in.
    /// </summary>
    public static class IrendasPenalColonyRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 1764;
        public const uint PenalColonyInteriorRegion = 6, HospitalRegion = 9, DormitoryARegion = 10, DormitoryBRegion = 11, CommandCenterRegion = 25;
        public const byte Box = 2;

        public static readonly Volume[] Volumes =
        {
            Of(PenalColonyInteriorRegion, 146, 270, -287, -135, 390, 431, "arieki_torden_plains - Irendas Penal Colony (corridors under the prison)"),
            Of(PenalColonyInteriorRegion, 44, 146, -307, -251, 398, 431, "arieki_torden_plains - Irendas Penal Colony (west rooms)"),
            Of(HospitalRegion, 196, 220, -239, -207, 424, 432, "arieki_torden_plains - Irendas Colony Hospital (ward)"),
            Of(DormitoryARegion, 148, 172, -279, -247, 416, 431, "arieki_torden_plains - Dormitory A (west)"),
            Of(DormitoryBRegion, 244, 268, -279, -247, 416, 431, "arieki_torden_plains - Dormitory B (east)"),
            Of(CommandCenterRegion, 296, 344, -64, -16, 495, 524, "arieki_torden_plains - Irendas Command Center (HQ tower top)"),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, 0, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var v in Volumes)
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)};";
        }

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
