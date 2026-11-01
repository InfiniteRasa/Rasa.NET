using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on the Proving Grounds (adv_wargame_provinggroundsv002, 2361;
    /// 20270726030000_Proving_grounds_region_sweep).
    ///
    /// Red Base (3) and Blue Base (4) - human interior sky/env with REGION_HUMAN_BASE_COMMAND - are the
    /// two team HQs: small arch_hum_corridor_* complexes built into the arena wall, one cross of
    /// corridors on two floors (y 420.5 / 428.5) joined by a stairwell, holding each team's hospital
    /// marker, workstations and weapon racks, with the REGION_LABEL '&lt;team&gt; Team Base', the POI
    /// '&lt;team&gt; Team: Head Quarters' and the region 7/8 minimap rects on them. Each gets two boxes
    /// following the cross (east-west arm with the stairwell, north-south arm with the endcaps), y
    /// 420..436; outside ground is 418.6 and the roof navmesh 439+. Definite.
    ///
    /// Staging Area (16) - human interior sky/env with REGION_HUMAN_GUILD_HALL - is the separate
    /// staging hall about 60 m below the arena (arch_hum_corr_room_* hall with an upper office ring),
    /// holding the Staging Area REGION_LABEL, footlockers, class trainer and vendors; its seeded circle
    /// is disabled. One any-level box over the hall's navmesh, y 350..392. Definite.
    ///
    /// Disputed Territory (1) - battlefield ambient and the match music, name id WARGAME_CP1_MAIN - is
    /// the arena itself. With no volume, the whole battlefield shows no name. Box = the map's region 0
    /// minimap extent above y 395 (the staging hall stays out); the HQs and team-tunnel names override
    /// inside it. Probable.
    ///
    /// Recuts: the six 'Red/Blue Team: To ...' name rows were all seeded on the Blue team's markers, in
    /// Red/Blue pairs sharing one circle. The map also has the Red markers (Red Team: North Base, South
    /// Base, Head Quarters). At Blue's north and south tunnels the Red row has the higher priority, so
    /// Blue's own tunnels show the Red team's name. The three Red rows move to the Red markers as area-
    /// equal boxes with the same surface-only flag, mirroring the Blue rows. Probable.
    ///
    /// Not placed: region 2 (Molder.sky).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class ProvingGroundsRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2361;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "wargame_provinggroundsv002 - ";

        public static readonly Volume[] Volumes =
        {
            Of(4, -432, -388, 158, 170, 420, 436, AnyLevel, Map + "Blue Base (HQ east-west corridor and stairwell)"),
            Of(4, -410, -394, 150, 178, 420, 436, AnyLevel, Map + "Blue Base (HQ north-south corridor)"),
            Of(3, 268, 312, 166, 178, 420, 436, AnyLevel, Map + "Red Base (HQ east-west corridor and stairwell)"),
            Of(3, 274, 290, 158, 186, 420, 436, AnyLevel, Map + "Red Base (HQ north-south corridor)"),
            Of(16, 0, 84, -76, 20, 350, 392, AnyLevel, Map + "Staging Area (underground staging hall)"),
            Of(1, -494, 346, -80, 384, 395, 2000, AnyLevel, Map + "Disputed Territory (the arena and its approaches)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20212, 10, 1, -397.2, 428.5, 299.5, 88.6, 0, 0, -2000, 2000, 2, "wargame_provinggroundsv002 - Red Team: To North Base",
                Of(10, 138, 294, 272, 428, -2000, 2000, SurfaceOnly, Map + "Red Team: To North Base (red north tunnel)")),
            new(20214, 11, 1, -336, 428.5, -14, 89.7, 0, 0, -2000, 2000, 2, "wargame_provinggroundsv002 - Red Team:  To South Base",
                Of(11, 198, 356, -43, 115, -2000, 2000, SurfaceOnly, Map + "Red Team:  To South Base (red south tunnel)")),
            new(20209, 12, 1, -402, 428.5, 164, 40, 0, 0, -2000, 2000, 2, "wargame_provinggroundsv002 - Red Team: To Head Quarters ",
                Of(12, 247, 317, 136, 206, -2000, 2000, SurfaceOnly, Map + "Red Team: To Head Quarters (red HQ)")),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, byte underground, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, underground, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {v.Underground}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            foreach (var r in Recuts)
                yield return $"UPDATE map_region SET shape = {Box}, pos_x = {N(r.Box.X)}, pos_y = {N(r.Box.Y)}, pos_z = {N(r.Box.Z)}, radius = 0, " +
                    $"half_x = {N(r.Box.HalfX)}, half_z = {N(r.Box.HalfZ)}, min_y = {N(r.Box.MinY)}, max_y = {N(r.Box.MaxY)}, underground = {r.Box.Underground}, " +
                    $"comment = {S(r.Box.Comment)} " +
                    $"WHERE id = {r.RowId} AND map_context_id = {MapContextId} AND region_id = {r.RegionId} AND shape = {r.Shape} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01 " +
                    $"AND ABS(half_x - {N(r.HalfX)}) < 0.01 AND ABS(half_z - {N(r.HalfZ)}) < 0.01 " +
                    $"AND ABS(min_y - {N(r.MinY)}) < 0.01 AND ABS(max_y - {N(r.MaxY)}) < 0.01 AND underground = {r.Underground};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {r.Shape}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = {N(r.HalfX)}, half_z = {N(r.HalfZ)}, min_y = {N(r.MinY)}, max_y = {N(r.MaxY)}, underground = {r.Underground}, comment = {S(r.Comment)} " +
                    $"WHERE id = {r.RowId} AND {Is(r.Box)};";

            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {Is(v)};";
        }

        private static string Is(Volume v) =>
            $"{At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 " +
            $"AND ABS(min_y - {N(v.MinY)}) < 0.01 AND ABS(max_y - {N(v.MaxY)}) < 0.01 AND underground = {v.Underground} AND comment = {S(v.Comment)}";

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
