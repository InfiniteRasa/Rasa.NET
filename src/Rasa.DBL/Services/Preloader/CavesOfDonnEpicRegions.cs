using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The places of the epic Caves of Donn (adv_foreas_concordia_wilderness_cavesofdonn_epic,
    /// 2368; Caves_of_donn_epic_regions).
    ///
    /// The map is the Caves of Donn's own (adv_foreas_concordia_wilderness_cavesofdonn02, 1506):
    /// the same architecture, piece for piece, and the same regions, but without the region
    /// labels that gave Caves of Donn its circles, so it had nothing but the three minimap
    /// rectangles. Its caverns are open to the sky (the navmesh has them above the ground) and
    /// take the map's own sky (sky_foreas_concordia_wilderness_donn); the tunnels between them
    /// are under it. From west to east, as CavesOfDonnContent walks it:
    ///
    /// - Mar Fend Village (region 6: the wilderness environment map and music) is the west
    ///   cavern: the burned Forean village (huts, stalls, platforms and ramps from x -365 to -279)
    ///   round the walled hill where Elder Nekala stands, Mar Fend (-322, 21.6, -228), and the Eloh
    ///   pyramid north of it (arch_forean_eloh_temple at (-294, 7, -167)). The 1506 label stands at
    ///   (-326.8, 33.3, -215). Box x -395 to -258, z -345 to -125.
    /// - Spirit Cave (region 7: the cave hall sky, the cave moans) is the room north of the
    ///   central cavern where the Lost Foreans wait (terra_tunnel_room_half and its turn, x -228
    ///   to -176, z -191 to -97; label (-191, 6, -145)). Box x -230 to -174, z -193 to -95.
    /// - Eloh Chantry (region 5) is the central cavern, with the Chantry's shrines, colossi and
    ///   braziers on its hill (-160, 36, -325) and Excavator Xang's pincer. Box x -236 to -122,
    ///   z -370 to -200.
    /// - Undone Fairway (region 3) is the cavern east of the Chantry tunnel, the mortar bowl
    ///   with the Heal test (7.6, 22.2, -232.3). Box x -56 to 90, z -318 to -192.
    /// - Vitiated Shrine (region 4: the cave big-room sky, the Eloh temple music) is the round
    ///   pit with the stepped Eloh gate at its middle (arch_forean_eloh_temple_stair_corner_out,
    ///   x 154 to 186; label (169.5, 5.3, -302)). Box x 113 to 226, z -355 to -249.
    /// - Region 8 (the Eloh interior sky and environment map, REGION_ELOH_RUINS_01, the temple
    ///   music; priority 13) is the hidden Forean-Eloh chamber behind the gate: the Eloh coupler
    ///   and room (arch_eloh_corridor_room_v01, x 254 to 303) with the reliquary, the projector,
    ///   the statues and the hologram teleporter. Box x 224 to 306, z -330 to -274.
    /// - Region 15 (the Eloh sky, environment map and music, no ambience; priority 4, just over
    ///   the entrance's region 9) is the Eloh corridor by the entrance where that teleporter puts
    ///   you (arch_eloh_corridor_straight_16m and its coupler, x -328 to -284, z -370 to -348), in
    ///   the corner of region 9's rectangle. Box x -330 to -284, z -372 to -346, y 18 to 40.
    /// - Region 2 (the short cave sky, blue fog, cave echo) is the tunnels: from the west cavern
    ///   round to Spirit Cave, from Spirit Cave down to the central cavern, and from the Chantry
    ///   to Undone Fairway (where Sergeant Napoli is lost). Region 16, the same with the Eloh
    ///   temple music, is the tunnel from Undone Fairway down to the Vitiated Shrine. Their boxes
    ///   are underground-only, so they take the tunnels and stop at the caverns' mouths.
    ///
    /// Not placed: region 10 (Spirit Cave's sky and environment map with the plain cave echo),
    /// region 14 (the short cave sky alone, the highest priority) and region 1 (Molder.sky), which
    /// nothing marks, and region 11, which sets nothing. The entrance tunnel keeps region 9's
    /// rectangle (cave environment map and echo, underground-only).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes them out.
    /// </summary>
    public static class CavesOfDonnEpicRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        public const uint MapContextId = 2368;
        public const uint TunnelRegion = 2, FairwayRegion = 3, ShrineRegion = 4, ChantryRegion = 5, MarFendRegion = 6, SpiritCaveRegion = 7,
            ChamberRegion = 8, CorridorRegion = 15, ShrineTunnelRegion = 16;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1;

        private const string Map = "foreas_concordia_wilderness_cavesofdonn_epic - ";

        public static readonly Volume[] Volumes =
        {
            Of(MarFendRegion, -395, -258, -345, -125, -10, 45, AnyLevel, Map + "Mar Fend Village (the west cavern)"),
            Of(SpiritCaveRegion, -230, -174, -193, -95, -10, 40, AnyLevel, Map + "Spirit Cave"),
            Of(ChantryRegion, -236, -122, -370, -200, -10, 45, AnyLevel, Map + "Eloh Chantry (the central cavern)"),
            Of(FairwayRegion, -56, 90, -318, -192, -10, 45, AnyLevel, Map + "Undone Fairway (the east cavern)"),
            Of(ShrineRegion, 113, 226, -355, -249, -10, 45, AnyLevel, Map + "Vitiated Shrine (the round pit)"),
            Of(ChamberRegion, 224, 306, -330, -274, -5, 30, AnyLevel, Map + "region 8 (the hidden Forean-Eloh chamber)"),
            Of(CorridorRegion, -330, -284, -372, -346, 18, 40, AnyLevel, Map + "region 15 (the Eloh corridor by the entrance)"),
            Of(TunnelRegion, -325, -228, -127, -90, -10, 45, UndergroundOnly, Map + "region 2 (tunnel, west cavern to Spirit Cave)"),
            Of(TunnelRegion, -212, -186, -216, -191, -10, 40, UndergroundOnly, Map + "region 2 (tunnel, Spirit Cave to the Chantry)"),
            Of(TunnelRegion, -122, -56, -314, -270, -10, 40, UndergroundOnly, Map + "region 2 (tunnel, Chantry to Undone Fairway)"),
            Of(ShrineTunnelRegion, 76, 116, -312, -228, -10, 40, UndergroundOnly, Map + "region 16 (tunnel, Undone Fairway to the shrine)"),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, byte underground, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, underground, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {v.Underground}, 1, {S(v.Comment)} " +
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
