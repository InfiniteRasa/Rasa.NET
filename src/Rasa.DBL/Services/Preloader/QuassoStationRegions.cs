using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Quasso Station's Dropship Zone and Prison (Quasso_station_regions).
    ///
    /// Both are rooms of the instance adv_arieki_ligo_thunderhead_quassostation (2105), not
    /// buildings on Ligo Thunderhead: its .map has regions 3, "Quasso Dropship Zone" (priority 5:
    /// sky_arieki_ligo_default, env_heavy_brown_fog, REGION_ARIEKI_LAVASTORM_01, MUSIC_THUNDERHEAD_02),
    /// and 10, "Quasso Prison" (priority 10, the map's highest: REGION_BANE_PRISON,
    /// MUSIC_THUNDERHEAD_03), and neither had a volume.
    ///
    /// The Dropship Zone is the station's one open-air yard, under the Bane headquarters'
    /// exterior (arch_bane_exterior_Headquarters, y 74 to 214, south of it): three Bane landing
    /// pads at y 9 to 13 (x -53 to 79, z 0 to 71) inside minor outpost walls, sandbags and
    /// catwalks, ringed by rock spikes, the observation level's window looking down on it from
    /// the south (11, 52, -53). The tunnels into the station leave it south (the two chunnel
    /// entrances at z -40 to -16, the Access Floor's) and north (the one at z 79 to 103). Its
    /// box runs between them, x -110 to 170 and z -16 to 74, floor 0 to the top of the rocks
    /// at 100, clear of the Access Floor's, the Observation Level's, the Supply Warehouse's and
    /// the Subterranean Command's rectangles.
    ///
    /// The Prison is the ring of Bane chunnels at the bottom of the station, y -49: the three
    /// prison corridor pieces (arch_bane_industrial_chunnel_straight_02_prison, x -502 to -345,
    /// z 171 to 236) with their endcaps, turns and overlooks round the information extraction
    /// room in the middle (arch_bane_industrial_room_Information_extraction with its extraction
    /// sensors, in a 24 m control room, x -463 to -383), where the map's "Conscription Chamber"
    /// label stands. Its box is the ring, x -505 to -343 and z 116 to 252, from y -52 to -22,
    /// under the stasis rooms' floor (their sleep pods stand at y -18). Its priority is above
    /// the Barracks' and the Subterranean Command's, whose rectangles it lies in.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes out the rows as they went in.
    /// </summary>
    public static class QuassoStationRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 2105;
        public const uint DropshipZoneRegion = 3, PrisonRegion = 10;
        public const byte Box = 2;

        public static readonly Volume[] Volumes =
        {
            Of(DropshipZoneRegion, -110, 170, -16, 74, 0, 100, "arieki_ligo_thunderhead_quassostation - Quasso Dropship Zone (yard)"),
            Of(PrisonRegion, -505, -343, 116, 252, -52, -22, "arieki_ligo_thunderhead_quassostation - Quasso Prison (ring)"),
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
