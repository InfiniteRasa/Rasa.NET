using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Ligo Thunderhead's Eloh chambers, its fault chasm, the passage to Brimstone Cavern and the
    /// two Brann camps (adv_arieki_ligo_thunderhead, 1911; Thunderhead_regions).
    ///
    /// - Region 37 (sky_eloh_interior_default, env_eloh_interior_default, MUSIC_ELOH_03; priority
    ///   24, no name) had no volume. The map has two Eloh chambers (arch_eloh_corridor_*: a door
    ///   or a sealed coupler, a crossing and a room with an obelisk). One is "the Eloh chamber
    ///   inside Vashtar Pass" that Tough Translating sends you into, past the door Field Sgt.
    ///   Eiserloh's squad could not get through, "just to the east" of them: its obelisk carries
    ///   the logos Eiserloh reads out ("Everyone Plant Love Seeds Or Disperse"). It stands off the
    ///   pass's caves, under Vashtar Pass's cave sky (region 38, priority 23) until now: x 966 to
    ///   1074, z 225 to 277. The other is behind a door in the north-west of the map, with
    ///   nothing over it: x -933 to -881, z 352 to 439.
    /// - Region 13 (sky_dark_fall_test alone; priority 29, the map's highest) had no volume. It is
    ///   the dark sky the maps give the depths a player falls into. The Ligo Fault Line is the
    ///   chasm across the middle of the map with the Bane Fault Lever in it; its floor is at 160
    ///   to 190 and its western arm at 210 to 250, while the plains round it, the Fault Lever
    ///   control point, the Fault Hammers and the Bane overlooks on its walls are all at 290 and
    ///   above. Nothing else on the map is below 290. The box is the chasm below 280.
    /// - Region 39, "Thunderhead Base" (sky_arieki_torden_abyss_cave, env_arieki_cave_interior,
    ///   REGION_FOREAS_CAVE_WITH_ECHO; priority 4, just over the base's region 25) had no volume
    ///   since its label circle was turned off (Region_sky_circles). It is the cave passage from
    ///   the base's south side, past the hospital, down to Brimstone Cavern
    ///   (terra_arieki_cavern_rocky_*): Cleaning Crew's "entrance to the Brimstone Cavern where we
    ///   get all of our fresh water" and "the passage to the Water Treatment Facility in Brimstone
    ///   Cavern". Its box is the passage, underground-only, so it stops at the cavern's mouth.
    /// - The Brann camps have the map's "Brann Viddia Camp" and "Brann Larai Camp" labels but
    ///   regions named "Viddia Syndicate Camp" and "Larai Syndicate Camp", so no circle was
    ///   seeded. Viddia Outpost is Scientist Irial's, Sigal Swona's and Zupa Yrun's, with the Viddia
    ///   Camp Hospital and its medical vendor (-2, 337, -431); Larai Outpost is Zupar Dyro's,
    ///   Enforcer Bonree's and Sigal Cisra's, with its medical vendor and local teleporter. Each is
    ///   a fenced square of Brann field tents, solar towers and a waypoint. Viddia's region 30
    ///   (MUSIC_SAFE_ZONE_BRANN and the name) gets its camp, x -60 to 52, z -485 to -385; Larai's
    ///   region 31 (the same) and region 12 (the name and REGION_HUMAN_BASE_DISTANT_BATTLE_01) get
    ///   its, x -592 to -488, z -297 to -186. Region 11 (Viddia's name alone, under 30) would
    ///   change nothing and is left out.
    ///
    /// Not placed: regions 6 and 7 (human and Brann interiors with no music), 19 and 20 (battle
    /// ambience and MUSIC_THUNDERHEAD_04), which nothing ties to one place.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes them out.
    /// </summary>
    public static class ThunderheadRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        public const uint MapContextId = 1911;
        public const uint BaseCaveRegion = 39, LaraiRegion = 12, DarkFallRegion = 13, ViddiaRegion = 30, LaraiCampRegion = 31, ElohRegion = 37;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1;

        private const string Map = "arieki_ligo_thunderhead - ";

        public static readonly Volume[] Volumes =
        {
            Of(ElohRegion, 966, 1074, 225, 277, 440, 466, AnyLevel, Map + "region 37 (the Eloh chamber in Vashtar Pass)"),
            Of(ElohRegion, -933, -881, 352, 439, 390, 416, AnyLevel, Map + "region 37 (the Eloh chamber in the north-west)"),
            Of(DarkFallRegion, -460, 660, -160, 270, 0, 280, AnyLevel, Map + "region 13 (the Ligo Fault Line chasm)"),
            Of(BaseCaveRegion, 593, 644, -759, -648, 360, 402, UndergroundOnly, Map + "Thunderhead Base (the passage to Brimstone Cavern)"),
            Of(ViddiaRegion, -60, 52, -485, -385, 330, 366, AnyLevel, Map + "Viddia Syndicate Camp (Viddia Outpost)"),
            Of(LaraiCampRegion, -592, -488, -297, -186, 358, 396, AnyLevel, Map + "Larai Syndicate Camp (Larai Outpost)"),
            Of(LaraiRegion, -592, -488, -297, -186, 358, 396, AnyLevel, Map + "Larai Syndicate Camp (Larai Outpost, ambience)"),
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
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 " +
                    $"AND ABS(min_y - {N(v.MinY)}) < 0.01 AND ABS(max_y - {N(v.MaxY)}) < 0.01 AND underground = {v.Underground} AND comment = {S(v.Comment)};";
        }

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
