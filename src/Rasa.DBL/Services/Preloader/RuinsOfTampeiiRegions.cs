using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Ruins of Tampeii's three places (adv_arieki_torden_abyss_tampei, 2155;
    /// Ruins_of_tampeii_regions).
    ///
    /// The map had no volumes at all: it has no region labels, so nothing set its skies.
    /// Its three regions each match one of its three parts:
    ///
    /// - Tampeii Gateway (region 2) sets the Brann interior sky, environment map and corridor
    ///   ambience (sky_arieki_brann_interior_default, REGION_BRANN_INTERNAL_COORIDORS). The one
    ///   Brann interior is the corridor complex on the east side, where the instance puts you
    ///   (map arrival at (140.5, 56, 143.3), arch_brann_corridor_instance_v01). It holds the
    ///   map's three "Security Area" labels, and its west corridor comes out through a cave
    ///   entrance (arch_brann_corridor_entrance_01_v01 at (16, 32, -24)) onto the lava cavern.
    ///   It is also the map's own name, as region 0 has it. Its boxes follow the complex: the
    ///   west corridor (x 6 to 80, floor 33), the hub rooms (x 80 to 204, z -48 to 8, floor 40),
    ///   the south rooms and their corridor (x 82 to 145, z -127 to -48), the greenhouse
    ///   (x 164 to 216, z -112 to -32), the north rooms (x 64 to 128, z 0 to 80), the east wing
    ///   and its dormitories (x 160 to 216, z 8 to 157), and the corridor from the arrival
    ///   (x 132 to 165, z 90 to 148).
    /// - Ruins of Tampeii (region 4) sets a cave sky, cave environment map and cave echo
    ///   (sky_arieki_cave_abyss_default, REGION_FOREAS_CAVE_WITH_ECHO). It is the lava cavern
    ///   west of the complex, full of wrecked Brann buildings (domes, barracks, towers) and the
    ///   broken prison wall: the missions' "Lava Fields" and "Tampeii perimeter wall". Its box
    ///   is the cavern, x -245 to 6, z -250 to 175, plus its north-west lobe to z 195 west of
    ///   x -128. It stops at x 6, where the complex's entrance begins, and at z 175, the security
    ///   door of the wardenbot gate.
    /// - Patch's Lair (region 3) sets the wardenbot room sky (sky_arieki_ligo_crucible_WBRoom).
    ///   Patch's computer room is the round wardenbot room at the north end
    ///   (arch_wardenbot_corridor_room_01_patch_v01, x -136 to 8, z 256 to 400). The mission
    ///   sends you "through the Lava Fields" to "the computer room that Patch is using as a base
    ///   of operations", and Danaher (Open Says Me) puts that room "behind the security door in
    ///   the Lava Fields". The one door off the cavern that isn't the complex's is in the great
    ///   wardenbot gate on its north wall (arch_wardenbot_corridor_doorjamb_01_8m at z 175 to
    ///   185); it leads into the wardenbot corridors and on to the room, so the lair also takes
    ///   those corridors (floor 1, roof 12): the spine x -86 to -58 and its west stub x -118 to
    ///   -86 (z 231 to 256).
    ///
    /// Region 1 (Molder.sky, lowest priority, nothing else) is the editor's base region, not a
    /// place.
    ///
    /// The navmesh has no roof over any of it, so every box is for any level. Plain SQL, valid on
    /// SQLite and MySQL alike. Each row goes in only where the map is loaded and no box of that
    /// region is at that spot; Down takes them out.
    /// </summary>
    public static class RuinsOfTampeiiRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 2155;
        public const uint GatewayRegion = 2, LairRegion = 3, RuinsRegion = 4;
        public const byte Box = 2, AnyLevel = 0;

        private const string Map = "arieki_torden_abyss_tampei";

        public static readonly Volume[] Volumes =
        {
            Of(GatewayRegion, 6, 80, -40, -8, 30, 46, $"{Map} - Tampeii Gateway west corridor"),
            Of(GatewayRegion, 80, 204, -48, 8, 36, 60, $"{Map} - Tampeii Gateway hub rooms"),
            Of(GatewayRegion, 82, 145, -127, -48, 46, 70, $"{Map} - Tampeii Gateway south rooms"),
            Of(GatewayRegion, 164, 216, -112, -32, 36, 60, $"{Map} - Tampeii Gateway greenhouse"),
            Of(GatewayRegion, 64, 128, 0, 80, 46, 64, $"{Map} - Tampeii Gateway north rooms"),
            Of(GatewayRegion, 160, 216, 8, 157, 38, 65, $"{Map} - Tampeii Gateway east wing"),
            Of(GatewayRegion, 132, 165, 90, 148, 46, 63, $"{Map} - Tampeii Gateway arrival corridor"),
            Of(RuinsRegion, -245, 6, -250, 175, -40, 180, $"{Map} - Ruins of Tampeii lava cavern"),
            Of(RuinsRegion, -245, -128, 175, 195, -40, 180, $"{Map} - Ruins of Tampeii north-west lobe"),
            Of(LairRegion, -86, -58, 175, 256, -2, 14, $"{Map} - Patch's Lair corridor"),
            Of(LairRegion, -118, -86, 231, 256, -2, 14, $"{Map} - Patch's Lair west stub"),
            Of(LairRegion, -136, 8, 256, 400, -2, 30, $"{Map} - Patch's Lair computer room"),
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
