using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Eloh Vale's caves, gate, pyramid, waterfalls and canyon (adv_foreas_concordia_elohcommtower2,
    /// 2084; Eloh_vale_regions).
    ///
    /// The vale runs south to north: the way in (the "Eloh Vale Entrance" hospital at
    /// (-46, 171, -455)), a cave, the plaza with the temples, the long bridge over the lake, the
    /// gate, the Eloh Cave, and past its waterfalls the path to the wormhole tower. The missions
    /// only name the vale; the map places the regions:
    ///
    /// - Entry Cave (16, a giant-room cave sky): the cave between the way in and the plaza
    ///   (terra_foreas_cavern pieces with crystals, stalagmites and an aqueduct, x -64 to -3,
    ///   z -373 to -298), the first thing past the entrance.
    /// - Eloh Shield Gate (21, the Eloh temple interior sky): the covered part of the gate at the
    ///   north end of the bridge (arch_eloh_elohvale_front_gate, with its shield generator,
    ///   arch_eloh_elohvale_shield_generator at (-62, 166, 108)), under the "Eloh Shield Gate"
    ///   label: the causeway under its roof, z 78 to 120.
    /// - Pyramid Temple (10): the walled temple terrace on the plaza's east side at the "Forean
    ///   Pyramid" label (66, 193, -233), where the AFS has landed a dropship and set up its
    ///   hospital (ELOH_VALE_HOSPITAL_PYRAMID): x 27 to 82, z -259 to -176, floor 185.
    /// - Cave of Ancients (13, a short cave sky): the one other cave, by Elder Falls (x 242 to
    ///   329, z -245 to -160), down from the temples to the pool below the falls, where the
    ///   gravestones are.
    /// - Waterfall Outflow (15): the terraced pools north of the Eloh Cave (x -123 to 19,
    ///   z 224 to 340), where the cave's water flows out down the steps and over the two great
    ///   falls into the canyon (vfx_waterfall_32m_64m at (6, 48, 333) and (-100, 48, 340)).
    ///
    /// - Region 19 (a falling sky, sky_dark_fall_test, no name): the canyon under the tower path
    ///   (terra_foreas_elohvale_waterfall_canyon, x -265 to 102, z 220 to 487), where the two
    ///   great falls go down. The path crosses it at 145 and the terraces and ledges round it stand
    ///   above 125; the box is everything below 120, the drop and the canyon floor.
    /// - Region 20 (waterfall ambience, REGION_FOREAS_PINHOLE_FALLS_01, no name): the first
    ///   waterfall past the way in: the pool by the entrance (water_foreas_pool_32m at
    ///   (-59, 158, -430), held by river dams) and the fall out of it (vfx_waterfall_16m_64m at
    ///   (-71, 102, -427)).
    ///
    /// Not placed: region 1, the editor's base region.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes them out.
    /// </summary>
    public static class ElohValeRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 2084;
        public const uint PyramidTempleRegion = 10, CaveOfAncientsRegion = 13, WaterfallOutflowRegion = 15, EntryCaveRegion = 16,
            CanyonFallRegion = 19, EntranceFallsRegion = 20, ShieldGateRegion = 21;
        public const byte Box = 2, AnyLevel = 0;

        private const string Map = "foreas_concordia_elohcommtower2 - ";

        public static readonly Volume[] Volumes =
        {
            Of(EntryCaveRegion, -66, 2, -376, -298, 166, 206, Map + "Entry Cave"),
            Of(ShieldGateRegion, -86, -44, 76, 124, 160, 190, Map + "Eloh Shield Gate (under the gate)"),
            Of(PyramidTempleRegion, 25, 84, -260, -176, 168, 200, Map + "Pyramid Temple (temple terrace)"),
            Of(CaveOfAncientsRegion, 240, 332, -245, -158, 124, 172, Map + "Cave of Ancients"),
            Of(WaterfallOutflowRegion, -125, 22, 238, 345, 138, 200, Map + "Waterfall Outflow (terraced pools)"),
            Of(CanyonFallRegion, -265, 102, 220, 487, -50, 120, Map + "region 19 (the canyon under the tower path)"),
            Of(EntranceFallsRegion, -80, -40, -450, -410, 60, 180, Map + "region 20 (the waterfall by the way in)"),
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
