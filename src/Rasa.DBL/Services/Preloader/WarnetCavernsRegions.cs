using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Warnet Caverns' places (adv_foreas_concordia_palisades_warnetcaverns, 1384;
    /// Warnet_caverns_regions).
    ///
    /// The map has no labels, so none of its named regions had a volume. The caverns run from
    /// the way in from Palisades in the west (terra_foreas_cavern_instance at (-138, 103, 22),
    /// the first aid station at (-120, 97, 45)) east and down: a tunnel at y 75 to 100, two
    /// tunnel-room chambers at y 60 to 75, a passage over to a two-level tee at (214, 102),
    /// ramps down from it to the dig site (y 34) and the root-grown rooms east of it (y 33 to
    /// 50), and a passage north from those down into the Nest (y 0). South of the first chamber,
    /// through a cavern mouth at (38, 95, 14), is the big cavern with the research camp.
    ///
    /// - Research Base Camp (3: sky_foreas_cave_bigroom, REGION_HUMAN_OUTPOST,
    ///   MUSIC_CONCORDIA_PALISADES_01): the big cavern (under terra_foreas_cavern_roof_500m_x_280m)
    ///   with the camp in it: fenced catwalk platforms at y 78 and 98, cots, sleeping bags, a
    ///   monitor, loudspeaker and turrets, the second first aid station and medical vendor
    ///   (x 13 to 90, z -97 to -24), and its pools: x -20 to 140, z -145 to 20.
    /// - The Dig Site (4): "Report to Albert Corman inside the main dig site": the two tunnel rooms
    ///   at y 34 with Corman's drills, jackhammers, folding tables, camera and work lights and the
    ///   Corman tunnels in their walls (x 134 to 228, z 18 to 70).
    /// - The Nest (2: sky_foreas_cave_giantroom, MUSIC_CONCORDIA_PALISADES_03), the Warnet Queen's:
    ///   the giant cavern in the north (terra_foreas_cavern_roof_160m, x 140 to 393, z 191 to 442)
    ///   round its six pools at y 0.
    /// - Bug City (5): the rooms east of the dig site (x 242 to 329, z 11 to 166, floor y 33 to
    ///   50), the wildest of the caverns: the most crystal growths (54), roots (30) and plants (37)
    ///   anywhere on the map and almost nothing human, under dripping water.
    /// - Upper Chambers (6): the two tunnel-room chambers on the upper level (terra_tunnel_room
    ///   pieces at (-2, 79, 46) and (78, 79, 78), and at (94, 71, 94) and (174, 71, 126)), with
    ///   their ponds, crystals and dead explorers: x -16 to 78, z 46 to 101 and x 78 to 174, z 71
    ///   to 149.
    /// - Upper Passages (9): the upper level's passages: the tunnel from the way in to the first
    ///   chamber (x -145 to -16, z 22 to 61), and the passage from the second chamber over to the
    ///   tee (x 151 to 224, z 110 to 168, y 58 to 76).
    /// - The Lower Passage (7): the lower level's: the tee's lower level and the ramps from it down
    ///   to the dig site and Bug City (x 198 to 254, z 70 to 110), and the passage north from Bug
    ///   City to the Nest (x 252 to 272, z 166 to 198, y 34 to 47).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes out the rows as they went in.
    /// </summary>
    public static class WarnetCavernsRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 1384;
        public const uint NestRegion = 2, BaseCampRegion = 3, DigSiteRegion = 4, BugCityRegion = 5, UpperChambersRegion = 6, LowerPassageRegion = 7,
            UpperPassagesRegion = 9;
        public const byte Box = 2, AnyLevel = 0;

        private const string Map = "foreas_concordia_palisades_warnetcaverns - ";

        public static readonly Volume[] Volumes =
        {
            Of(BaseCampRegion, -20, 140, -145, 20, 70, 140, Map + "Research Base Camp (big cavern)"),
            Of(DigSiteRegion, 133, 230, 12, 71, 22, 60, Map + "The Dig Site (Corman's rooms)"),
            Of(NestRegion, 140, 393, 191, 442, -40, 100, Map + "The Nest (giant cavern)"),
            Of(BugCityRegion, 241, 330, 10, 167, 22, 75, Map + "Bug City (rooms east of the dig site)"),
            Of(UpperChambersRegion, -17, 79, 45, 102, 60, 100, Map + "Upper Chambers (first chamber)"),
            Of(UpperChambersRegion, 78, 175, 70, 150, 52, 92, Map + "Upper Chambers (second chamber)"),
            Of(UpperPassagesRegion, -147, -16, 21, 62, 70, 115, Map + "Upper Passages (tunnel from the way in)"),
            Of(UpperPassagesRegion, 151, 225, 110, 169, 48, 80, Map + "Upper Passages (over to the tee)"),
            Of(LowerPassageRegion, 198, 256, 69, 112, 30, 70, Map + "The Lower Passage (tee and ramps down)"),
            Of(LowerPassageRegion, 248, 276, 164, 200, 26, 60, Map + "The Lower Passage (north to the Nest)"),
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
