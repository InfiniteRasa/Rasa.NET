using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Treeback Camp's places (adv_foreas_concordia_palisades_treebackcamp, 1397;
    /// Treeback_camp_regions).
    ///
    /// The camp is "a Bane Scientific Camp, where Bane research seems to be trying to turn the
    /// normally peaceful Treebacks into violent siege weapons". Its map has no labels, so no
    /// region of it had a volume. From the .map's pieces, the minimap and the mission text:
    ///
    /// - Cave to Palisades (4, REGION_FOREAS_CAVE_WITH_ECHO): the way in from Palisades, the Bane
    ///   chunnel from the instance door (map link at (-337.5, 103.7, 398.7)) down two turns to
    ///   its entrance (arch_bane_industrial_chunnel_entrance at (-334, 102, 349)), with a dead
    ///   Hominas Machina in it: x -352 to -309, z 324 to 410, y 102 to 130.
    /// - AFS Staging Area (5): the AFS camp below the tunnel mouth, the only human things on the
    ///   map: an overrun camp of cots, crates, blankets, fans and catwalk lights with its dead and
    ///   a smashed holo sign (x -405 to -325, z 262 to 292, y 106), the field medic's cots and
    ///   medical crate where the Treeback Medic stands (-281, 100, 287; "Deliver the Treeback
    ///   medicine to Field Medic Wohlgemuth at the AFS staging area") and the sandbags down the
    ///   gully south of it to z 182.
    /// - Treeback Pen (6): the green field in the north the Bane hold the Treebacks in, inside the
    ///   ring of five large sonic towers ("Destroy the powerpacks at the base of the Sonic Towers
    ///   that are keeping the treebacks in check"; arch_bane_large_sonic_tower at (49, 157, 277),
    ///   (234, 187, 324), (-34, 173, 424), (170, 165, 444) and (58, 160, 484)), the Bane wall
    ///   along its south side at z 288.
    /// - Howler Kennels (7): the walled yard in the north-east with its gate on the north side
    ///   (arch_bane_outpost_wall_minor_entrance at (321, 150, 272)) and the minimap's four kennel
    ///   houses inside it: x 283 to 345, z 206 to 277, y 151.
    /// - Bane Construction Site (8): the basin in the south-east, behind a Bane wall at z -69 to
    ///   -44: a Bane treecutter, most of the map's felled trees and stumps (55 of them) round the
    ///   four dirty ponds, the walled post with its mining control terminal, command chair and
    ///   pillbox (x 211 to 255, z -215 to -151), and the articulated drill at the retaining walls
    ///   at its south end (248, 145, -267).
    ///
    /// Left out: region 9, the editor's default region (Molder.sky). The Bane base in the middle
    /// (barracks, dropship launch, towers) has no region of its own; it is the map's.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes out the rows as they went in.
    /// </summary>
    public static class TreebackCampRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 1397;
        public const uint CaveRegion = 4, StagingAreaRegion = 5, PenRegion = 6, KennelsRegion = 7, ConstructionSiteRegion = 8;
        public const byte Box = 2, AnyLevel = 0;

        private const string Map = "foreas_concordia_palisades_treebackcamp - ";

        public static readonly Volume[] Volumes =
        {
            Of(CaveRegion, -353, -308, 323, 411, 98, 132, Map + "Cave to Palisades (chunnel)"),
            Of(StagingAreaRegion, -412, -260, 172, 300, 92, 135, Map + "AFS Staging Area (camp and medic)"),
            Of(PenRegion, -50, 250, 270, 490, 100, 250, Map + "Treeback Pen (inside the sonic towers)"),
            Of(KennelsRegion, 283, 348, 200, 278, 140, 175, Map + "Howler Kennels (walled yard)"),
            Of(ConstructionSiteRegion, 5, 285, -310, -40, 100, 170, Map + "Bane Construction Site (south-east basin)"),
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
