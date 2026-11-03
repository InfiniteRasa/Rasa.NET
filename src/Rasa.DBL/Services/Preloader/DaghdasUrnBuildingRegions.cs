using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The three AFS buildings of Daghda's Urn Village in the Wilderness (1220; Daghdas_urn_building_regions)
    /// under the .map's human interior regions, not its cave.
    ///
    /// Wilderness_urn_enigma_regions gave the hospital, the housing and the research HQ each a box
    /// of region 38, taking it for the village's interior because it bears the village's name. It
    /// is a cave: sky_foreas_cave_short, env_foreas_cave_interior and REGION_FOREAS_CAVE_WITH_ECHO,
    /// so walking into the hospital darkened it to cave light and played the cave's echo.
    ///
    /// The .map has the regions for them. 26, 27 and 30 are its three human interiors with a sky
    /// of their own (sky_foreas_general_human_interior, env_human_interior), and the highest
    /// priorities on the map that set one (44 to 46), over the village's (36 to 38) and Daghda's
    /// Urn's (40) - the only ones that could win inside buildings standing in the village. 26 is
    /// the hospital's: its ambient is REGION_HUMAN_HOSPITAL. 27 and 30 are alike
    /// (REGION_HUMAN_OUTPOST); 27 is the housing, 30 the research HQ. None of the three had a
    /// volume anywhere. Each box keeps its place and size and takes its region.
    ///
    /// Region 38 is left with no volume, as its circle was turned off (Region_sky_circles): its
    /// cave is not the village's buildings, and where it is has not been found.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A box changes only while it is as
    /// Wilderness_urn_enigma_regions put it; Down gives it back to region 38.
    /// </summary>
    public static class DaghdasUrnBuildingRegions
    {
        public const uint MapContextId = WildernessUrnEnigmaRegions.MapContextId;
        public const uint CaveRegion = WildernessUrnEnigmaRegions.VillageInteriorRegion;   // 38
        public const uint HospitalRegion = 26, HousingRegion = 27, ResearchHqRegion = 30;

        /// <summary>Each building's box, as Wilderness_urn_enigma_regions placed it, and the region it takes.</summary>
        public static readonly (WildernessUrnEnigmaRegions.Volume Box, uint RegionId)[] Buildings =
        {
            (Box("(hospital)"), HospitalRegion),
            (Box("(housing)"), HousingRegion),
            (Box("(research HQ)"), ResearchHqRegion),
        };

        public static IEnumerable<string> Up() =>
            Buildings.Select(b => $"UPDATE map_region SET region_id = {b.RegionId} WHERE {At(b.Box, CaveRegion)};");

        public static IEnumerable<string> Down() =>
            Buildings.Select(b => $"UPDATE map_region SET region_id = {CaveRegion} WHERE {At(b.Box, b.RegionId)};");

        private static WildernessUrnEnigmaRegions.Volume Box(string building) =>
            WildernessUrnEnigmaRegions.Volumes.Single(v => v.RegionId == CaveRegion && v.Comment.EndsWith(building));

        private static string At(WildernessUrnEnigmaRegions.Volume v, uint regionId) =>
            $"map_context_id = {MapContextId} AND region_id = {regionId} AND shape = {WildernessUrnEnigmaRegions.Box} " +
            $"AND ABS(pos_x - {N(v.X)}) < 0.01 AND ABS(pos_z - {N(v.Z)}) < 0.01 " +
            $"AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)}";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
