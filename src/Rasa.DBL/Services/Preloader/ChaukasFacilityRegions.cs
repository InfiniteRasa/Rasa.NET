using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Chaukas Robotics Facility's three rooms (adv_arieki_ligo_crucible_wbfacility, 2146;
    /// Chaukas_facility_regions).
    ///
    /// The facility is a grid of Wardenbot corridors at y 17 from the way in at its south end
    /// (map link at (-31.9, 17.2, -282.1), the "Chaukas Entrance" hospital beside it) to three
    /// large rooms, the only rooms on the map. Its three named regions (each the Wardenbot room
    /// sky, sky_arieki_ligo_crucible_WBRoom, and a name) had no volumes, the map having no
    /// labels:
    ///
    /// - Chaukus Brain (2): "the Chaukas Brain is located in the large circular room due North of
    ///   this location", says Cipher Specialist Brian at the entrance: the round room due north
    ///   of it (arch_wardenbot_corridor_room_01_patch at (-16, 16, -8), x -88 to 56, z -8 to
    ///   136), with its two columns, its ring of spotlights and the Brain's computer and
    ///   equipment.
    /// - Robotics Assembly (3): the assembly room to the north-east
    ///   (arch_wardenbot_corridor_room_01_assembly at (88, 16, -48), x 16 to 160, z -192 to -48).
    /// - Ore Processing (4): the third, to the north-west (arch_wardenbot_corridor_room_01 at
    ///   (-152, 16, -152), x -225 to -82, z -152 to -8), its floor sunk to y 9 in the middle,
    ///   with three computers and the equipment by its north wall; the one room left once the
    ///   Brain's and the assembly room are known.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes out the rows as they went in.
    /// </summary>
    public static class ChaukasFacilityRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 2146;
        public const uint BrainRegion = 2, AssemblyRegion = 3, OreProcessingRegion = 4;
        public const byte Box = 2, AnyLevel = 0;

        private const string Map = "arieki_ligo_crucible_wbfacility - ";

        public static readonly Volume[] Volumes =
        {
            Of(BrainRegion, -89, 57, -9, 137, 10, 47, Map + "Chaukus Brain (round room)"),
            Of(AssemblyRegion, 15, 161, -193, -47, 6, 47, Map + "Robotics Assembly (assembly room)"),
            Of(OreProcessingRegion, -226, -81, -153, -7, 7, 47, Map + "Ore Processing (west room)"),
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
