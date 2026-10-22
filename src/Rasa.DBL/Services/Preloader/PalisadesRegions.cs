using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Palisades' temple passages, caves, Eloh and Bane interiors, ruins, fields and land bridge
    /// (adv_foreas_concordia_palisades, 1244; Palisades_regions).
    ///
    /// - The four Eloh temples (regions 21, 22, 23 and 25): each temple's front on the Walk of
    ///   Giants (arch_forean_eloh_temple, or for the Proud Patriarch an arch_forean_eloh_cavern_entrance)
    ///   opens on a short underground passage to its map link into the Eloh Temples instance, and
    ///   where each link comes out there names the temple (Eloh_temples_regions: the hospitals
    ///   "Temple of the ... Patriarch Entrance"): the link at (-769, 190, 48) is the Patriarch
    ///   Protector's, (-753, 187, -91) the Bowed Patriarch's, (-676, 175, -160) the Proud
    ///   Patriarch's and (-440, 173, 131) the Raging Patriarch's, whose passage is a cave
    ///   (terra_foreas_cavern ramp; its region has the cave echo). The wardens wait "outside the
    ///   temple"; each region is its passage, from the front to the link, underground.
    /// - Region 34 (an Eloh temple sky and environment map, no name): the map's two Eloh
    ///   interiors, rooms of arch_eloh_corridor with logos signs and dispensers: the one at the
    ///   back of the north cave (x -300 to -236, z 964 to 1004) and the room off the bottom of the
    ///   Waterfall Cave (x -209 to -132, z -145 to -95, floor 64).
    /// - Regions 12 and 13 (a cave tunnel sky and a cave hall sky, the cave music and echo, no
    ///   name): both have the Waterfall Cave's lighting (env_bane_interior_tunnel_little_lights),
    ///   which no other cave region has, so they are taken for its two parts, under its name. The
    ///   cave behind the falls is in two layers: the lower tunnels (floor 63 to 68, x -147 to -82,
    ///   z -127 to -26), which get region 12's tunnel sky, and the upper level, its middle floor
    ///   (about 80) and top floor (about 95) up to the mouth behind the waterfall (x -203 to -55,
    ///   z -73 to 2), which gets region 13's hall sky. The Eloh room off the
    ///   lower tunnels keeps region 34, one priority higher.
    /// - Region 28 (the Bane interior sky, no name): the map's only Bane interiors, at Skive Base:
    ///   the chunnel and service tunnels from the Treeback Camp link north to their entrances
    ///   (x 594 to 722, z -935 to -822, floor 145), and the control room
    ///   (arch_bane_industrial_room_control_24m, x 539 to 605, z -793 to -727, floor 132).
    /// - Forean Ruins (27): the destroyed Forean village on the islands of Lake Elinor (eleven
    ///   arch_forean_hut_*_destroyed, debris and wrecked shop stalls), one box to each island.
    /// - Cumbria Fields (35): the crop beds of Cumbria, the corn, carrots and teeks by the
    ///   hydroponics inside the town (x -838 to -758) and the burnt cornfield east of it
    ///   (x -684 to -646), the only crops on the map.
    /// - Eye of the Needle (20): the natural stone arch over the gorge north-east of Horsetail Falls
    ///   (terra_foreas_landbridge at (-372, 168, -362)), the map's one land bridge: its top
    ///   (y 212 to 215) and the gap under it (y 171 to 195). Nothing names it; the arch is the
    ///   needle's eye by its shape. Where the box reaches Horsetail Falls' circle, the falls,
    ///   priority 23 to its 13, keep their name.
    /// - Cumbria Research Facility (11): its label stands on the wrecked research building east of
    ///   the town (arch_corman_researchhq destroyed, (-528, 139, 671)), and its 200 m circle missed
    ///   the town itself, where the vendors, the auction house, the hospital and the dropship all
    ///   bear the facility's name. "Cumbria ... was twice the size it is now": the Bane "wiped out
    ///   the Corman structures out beyond the wall". The circle becomes a box over both, from the
    ///   dropship pad (x -870) to the wrecked windmills and auction house (x -460).
    ///
    /// Not placed: Forbidden Temples (the temples are all on the Walk of Giants and nothing marks
    /// a district apart from it), region 14 (Bane territory ambience, no name), and regions 1 and 36.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is
    /// loaded and no box of that region is at that spot; the recut row changes only while it is
    /// the circle as seeded. Down takes out what went in and gives the circle back.
    /// </summary>
    public static class PalisadesRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        /// <summary>A seeded circle and the box it becomes.</summary>
        public sealed record Recut(uint RowId, double X, double Y, double Z, double Radius, string Comment, Volume Box);

        public const uint MapContextId = 1244;
        public const uint CumbriaResearchRegion = 11, ProtectorRegion = 21, ProudRegion = 22, BowedRegion = 23, RagingRegion = 25,
            ForeanRuinsRegion = 27, EyeOfTheNeedleRegion = 20, WaterfallTunnelsRegion = 12, WaterfallUpperRegion = 13, BaneInteriorRegion = 28, ElohInteriorRegion = 34, CumbriaFieldsRegion = 35;
        public const byte Circle = 1, Box = 2;

        private const string Map = "foreas_concordia_palisades - ";

        public static readonly Volume[] Volumes =
        {
            Of(ProtectorRegion, -780, -735, 37, 61, 184, 206, Map + "Temple of the Patriarch Protector (passage)"),
            Of(BowedRegion, -764, -720, -101, -80, 182, 208, Map + "Temple of the Bowed Patriarch (passage)"),
            Of(ProudRegion, -689, -632, -188, -145, 170, 205, Map + "Temple of the Proud Patriarch (passage)"),
            Of(RagingRegion, -456, -423, 55, 142, 168, 205, Map + "Temple of the Raging Patriarch (cave passage)"),
            Of(ElohInteriorRegion, -302, -236, 962, 1006, 155, 175, Map + "Eloh interior (north cave)"),
            Of(ElohInteriorRegion, -211, -132, -147, -93, 60, 84, Map + "Eloh interior (under the Waterfall Cave)"),
            Of(WaterfallTunnelsRegion, -150, -80, -130, -26, 58, 78, Map + "Waterfall Cave lower tunnels (region 12)"),
            Of(WaterfallUpperRegion, -205, -55, -73, 2, 78, 122, Map + "Waterfall Cave upper level (region 13)"),
            Of(BaneInteriorRegion, 594, 722, -935, -822, 140, 165, Map + "Bane interior (Skive Base tunnels)"),
            Of(BaneInteriorRegion, 538, 608, -797, -724, 128, 162, Map + "Bane interior (Skive Base control room)"),
            Of(ForeanRuinsRegion, 400, 495, 355, 435, 100, 130, Map + "Forean Ruins (south-west island)"),
            Of(ForeanRuinsRegion, 530, 600, 445, 485, 100, 130, Map + "Forean Ruins (east island)"),
            Of(ForeanRuinsRegion, 450, 545, 495, 580, 100, 130, Map + "Forean Ruins (middle islands)"),
            Of(ForeanRuinsRegion, 455, 555, 585, 645, 100, 130, Map + "Forean Ruins (north islands)"),
            Of(ForeanRuinsRegion, 505, 535, 695, 725, 100, 130, Map + "Forean Ruins (north shore)"),
            Of(CumbriaFieldsRegion, -838, -758, 660, 790, 130, 155, Map + "Cumbria Fields (town crop beds)"),
            Of(EyeOfTheNeedleRegion, -410, -334, -402, -322, 160, 222, Map + "Eye of the Needle (land bridge)"),
            Of(CumbriaFieldsRegion, -684, -646, 674, 736, 130, 155, Map + "Cumbria Fields (burnt cornfield)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(39, -517.1, 141.1, 705.0, 200.0, Map + "Cumbria Research Facility",
                Of(CumbriaResearchRegion, -880, -460, 540, 800, 120, 220, Map + "Cumbria Research Facility (town and ruins)")),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, 0, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            foreach (var r in Recuts)
                yield return $"UPDATE map_region SET shape = {Box}, pos_x = {N(r.Box.X)}, pos_y = {N(r.Box.Y)}, pos_z = {N(r.Box.Z)}, radius = 0, " +
                    $"half_x = {N(r.Box.HalfX)}, half_z = {N(r.Box.HalfZ)}, min_y = {N(r.Box.MinY)}, max_y = {N(r.Box.MaxY)}, underground = 0, comment = {S(r.Box.Comment)} " +
                    $"WHERE id = {r.RowId} AND map_context_id = {MapContextId} AND region_id = {r.Box.RegionId} AND shape = {Circle} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01;";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {Circle}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = 0, half_z = 0, min_y = -2000, max_y = 2000, underground = 2, comment = {S(r.Comment)} " +
                    $"WHERE id = {r.RowId} AND {At(r.Box, "map_region")} AND ABS(half_x - {N(r.Box.HalfX)}) < 0.01 AND ABS(half_z - {N(r.Box.HalfZ)}) < 0.01;";

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
