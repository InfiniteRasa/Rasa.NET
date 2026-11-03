using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Eloh Space's two boxes on Torden Abyss widened to the whole of its two shrines
    /// (adv_arieki_torden_abyss, 2028; Eloh_space_shrines).
    ///
    /// Eloh Space (region 15, priority 33: the Eloh interior sky and environment map,
    /// MUSIC_ELOH_TEMPLES, REGION_ELOH_RUINS_01) is the map's only Eloh architecture: two shrines
    /// deep under Tantalus Base, the two ends of its minimap rectangle (x -541 to -276,
    /// z -482 to 252). Penumbra_hq_regions cut that rectangle down to a box round each, but both
    /// boxes were short of the rooms:
    ///
    /// - The south shrine (arch_eloh_corridor_room_v02 at (-315, 183, -403), x -353 to -277,
    ///   z -482 to -403, two obelisks and a logos dispenser, with a ramp and a corridor north to
    ///   z -339) is walkable at x -355 to -275 and z -480 to -340, floor 183 to 199. Its box,
    ///   x -345 to -285 and z -455 to -325, left out the room's south end and both sides. It is
    ///   now x -357 to -273, z -484 to -336, y 175 to 215.
    /// - The north shrine (arch_eloh_corridor_room_v01 at (-484, 500, 224), x -533 to -484, with
    ///   two obelisks, the temple obstacles and a logos dispenser; a 48 m ramp up east to 516 and
    ///   the corridor and coupler to x -392, where the rocky cave from the Magma Caverns door
    ///   comes in) is walkable at x -535 to -390, z 195 to 250. Its box, x -520 to -410, left out
    ///   the room's west end and the coupler. It is now x -537 to -392, z 193 to 253, y 495 to 530,
    ///   still under the cave's floor at 538 and up.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row changes only while it is as
    /// Penumbra_hq_regions left it; Down puts it back.
    /// </summary>
    public static class ElohSpaceShrines
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        /// <summary>A box as Penumbra_hq_regions left it and the box it becomes.</summary>
        public sealed record Resize(Volume Was, Volume Box);

        public const uint MapContextId = 2028;
        public const uint ElohSpaceRegion = 15;
        public const byte Box = 2;

        private const string South = "arieki_torden_abyss - Eloh Space: the shrine under the base, south";
        private const string North = "arieki_torden_abyss - Eloh Space: the shrine under the base, north";

        public static readonly Resize[] Resizes =
        {
            new(Of(-345, -285, -455, -325, 175, 215, South), Of(-357, -273, -484, -336, 175, 215, South)),
            new(Of(-520, -410, 195, 255, 495, 528, North), Of(-537, -392, 193, 253, 495, 530, North)),
        };

        private static Volume Of(double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(ElohSpaceRegion, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up() => Resizes.Select(r => Move(r.Was, r.Box));

        public static IEnumerable<string> Down() => Resizes.Reverse().Select(r => Move(r.Box, r.Was));

        private static string Move(Volume from, Volume to) =>
            $"UPDATE map_region SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)}, half_x = {N(to.HalfX)}, half_z = {N(to.HalfZ)}, " +
            $"min_y = {N(to.MinY)}, max_y = {N(to.MaxY)} " +
            $"WHERE map_context_id = {MapContextId} AND region_id = {from.RegionId} AND shape = {Box} " +
            $"AND ABS(pos_x - {N(from.X)}) < 0.01 AND ABS(pos_z - {N(from.Z)}) < 0.01 AND ABS(half_x - {N(from.HalfX)}) < 0.01 AND ABS(half_z - {N(from.HalfZ)}) < 0.01 " +
            $"AND ABS(min_y - {N(from.MinY)}) < 0.01 AND ABS(max_y - {N(from.MaxY)}) < 0.01 AND comment = {S(from.Comment)};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
