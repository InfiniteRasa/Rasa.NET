using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Torcastra Prison's prison (adv_foreas_concordia_divide_torcastraprison, 1349;
    /// Torcastra_prison_region).
    ///
    /// The map has three regions besides its own. Region 2 (DefaultSky.sky, lowest priority,
    /// nothing else) is the editor's base region and region 3 sets nothing; neither is a place.
    /// The map's own region, "Torcastra Prison", has the Divide battlefield sky and yellow-sky
    /// environment map: it is the canyon from the way in (map link 33 at (-304, 13.8, 286.8))
    /// up past the Torcastra Gateway, the Bane garrisons and the Predator pad to the prison's
    /// walled gate (arch_bane_outpost_wall_major_entrance at (-110, 98, -258)), the "entrance of
    /// Torcastra Prison" where Lt. Cisco waits and the missions send you to "bring down the
    /// gate". Region 1, also "Torcastra Prison" (priority 2: sky_bane_interior_default,
    /// env_bane_interior_tunnel_no_light, REGION_BANE_TUNNEL_02), had no volume. It is the prison
    /// itself: the Bane complex in the cliff behind the gate yard, entered by the chunnel door at
    /// (-80, 100, -404). Its prison chunnels, the Security Block's control rooms (label at
    /// (-16, 101.5, -516)), the Prisoner Examination room with its information extraction room
    /// (label at (-144, 115.5, -548)), the two control rooms at the far end and the tunnels and
    /// service rooms between, floors 100 to 111, lie in x -249 to 73, z -693 to -373. Its box is
    /// x -252 to 80, z -700 to -381 (the door's inside), y 95 to 135; the gate yard and the field
    /// medic outside the door keep the map's own outdoor sky.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. The row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes it out.
    /// </summary>
    public static class TorcastraPrisonRegion
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 1349;
        public const uint PrisonRegion = 1;
        public const byte Box = 2, AnyLevel = 0;

        public static readonly Volume[] Volumes =
        {
            Of(PrisonRegion, -252, 80, -700, -381, 95, 135, "foreas_concordia_divide_torcastraprison - Torcastra Prison (the prison complex)"),
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
