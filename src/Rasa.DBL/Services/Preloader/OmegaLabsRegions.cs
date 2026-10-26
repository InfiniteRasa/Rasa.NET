using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Omega Labs' missile silo and Doctor Sandorf's Quarters (Omega_labs_regions).
    ///
    /// adv_arieki_torden_abyss_omegalabs (2203), "Section 5: Omega Labs", is the level-50
    /// instance off Section 5 of Penumbra HQ (Torden Abyss). Its .map has regions 3, "Omega
    /// Missile Silo" (priority 16: sky_arieki_cave_abyss_default, env_red_cave, MUSIC_ABYSS_04,
    /// REGION_ARIEKI_LAVA_FIELDS), and 8, "Doctor Sandorf's Quarters" (priority 14:
    /// MUSIC_ABYSS_03), and neither had a volume.
    ///
    /// The silo is the cavern at the foot of the Sublevel, past the Omega Project Control office
    /// (arch_hum_corr_room_office_v10, x -25 to 25, z -53 to -27): the missile silo piece
    /// (terra_arieki_cavern_abyss_missilesilo_v01 at 0, 180, 60), the lava, the catwalks and
    /// the steam vents round it, the rotating warning lights at x ±42 either side of z 60,
    /// back to the cavern wall at about z 200, the far side of the Sublevel's own minimap
    /// (region 15's box reaches z 217). Floor 168, roof 260.
    ///
    /// The Quarters are the one room built for the instance, arch_hum_omegalab_room_control_v01
    /// at (-44.5, 181, 46), on the silo floor's west side: its upper storey has a desk with a
    /// workstation, chairs, a lamp, a fan and the only footlocker below the entrance barracks.
    /// The silo's priority is above the Quarters', so its volume is three boxes that leave the
    /// room out: east of it, and west of it to the south and to the north.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes out the rows as they went in.
    /// </summary>
    public static class OmegaLabsRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 2203;
        public const uint SiloRegion = 3, QuartersRegion = 8;
        public const byte Box = 2;

        /// <summary>The Quarters: the room's bounds in the .map.</summary>
        public const double RoomWest = -54, RoomEast = -37, RoomSouth = 36, RoomNorth = 53, RoomFloor = 178, RoomRoof = 204;

        /// <summary>The silo cavern.</summary>
        public const double SiloWest = -100, SiloEast = 100, SiloSouth = -27, SiloNorth = 205, SiloFloor = 168, SiloRoof = 260;

        public static readonly Volume[] Volumes =
        {
            Of(SiloRegion, RoomEast, SiloEast, SiloSouth, SiloNorth, SiloFloor, SiloRoof, "arieki_torden_abyss_omegalabs - Omega Missile Silo (cavern)"),
            Of(SiloRegion, SiloWest, RoomEast, SiloSouth, RoomSouth, SiloFloor, SiloRoof, "arieki_torden_abyss_omegalabs - Omega Missile Silo (west, south)"),
            Of(SiloRegion, SiloWest, RoomEast, RoomNorth, SiloNorth, SiloFloor, SiloRoof, "arieki_torden_abyss_omegalabs - Omega Missile Silo (west, north)"),
            Of(QuartersRegion, RoomWest, RoomEast, RoomSouth, RoomNorth, RoomFloor, RoomRoof, "arieki_torden_abyss_omegalabs - Doctor Sandorf's Quarters (room)"),
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
