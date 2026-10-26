using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Penumbra Headquarters and its five Sections, inside Tantalus Base on Torden Abyss (2028),
    /// given their volumes, and Eloh Space's taken off them (Penumbra_hq_regions).
    ///
    /// The .map's regions 18 Penumbra Headquarters (priority 12) and 22 to 26, Sections 1 to 5
    /// (priorities 13 to 17: Information Technology, Covert Surveillance, Espionage, Special
    /// Weapons Technology, Special Projects), are interiors - a human interior sky and
    /// environment map, MUSIC_SAFE_ZONE_AFS_BASE and the base command ambience; Section 4 a
    /// dark sky, Section 5 MUSIC_ABYSS_05 and a distant battle - and none had a volume.
    ///
    /// Where each is, from the .map: Penumbra's building is the only one with Penumbra's own
    /// pieces (arch_hum_penumbra_entrance_32m and the four penumbra walls, x -576 to -452,
    /// z -196 to -108), at the Penumbra Headquarters label (-542, 536, -152), west of the base
    /// yard. Its lobby holds one end of each Section's local teleporter
    /// ("Local Teleporter: Penumbra Section 1" to 5), and each other end stands in a corridor
    /// network of its own that touches no other: Section 1 round (-580, -236), Section 2 round
    /// (-579, -62), Section 3 round (-644, -202), Section 4 round (-669, -24), Section 5 round
    /// (-714, -152), whose corridors end at the Omega Labs door (-868, 544.5, -82), as Omega Labs
    /// "can only be accessed from Section 5". The Headquarters are the lobby's network west of
    /// the yard (x below -450): the building, the corridors behind it, Penumbra's crafting
    /// stations and the near end of Section 5's teleporter.
    ///
    /// What stands in each network fits what the Sections are (TaRapedia, "Penumbra"): banks of
    /// monitors and workstations in Information Technology and Covert Surveillance; cots,
    /// footlockers, bags, laptops and supplies in Espionage ("field agent bunks and supplies");
    /// weapon scanners and salvaged mech guns and missiles in Special Weapons Technology; and
    /// corridor debris and hazmat barrels in Special Projects, under "code Red".
    ///
    /// Each region's volume is its network's footprint: the .map bounds of every human
    /// architecture piece in it, laid on a 4 m grid and merged into boxes, from a metre under
    /// the lowest floor to a metre over the highest roof, for players under the ground or over
    /// it. No two regions' boxes overlap.
    ///
    /// Eloh Space (region 15, priority 33, the Eloh interior sky) had the rectangle its minimap
    /// covers, x -541 to -276 and z -482 to 252 at any height, which put the Eloh sky over the
    /// east of Tantalus Base and Penumbra's lobby: its priority is above all of theirs. Its
    /// Eloh pieces are two shrines well under the base: rooms, corridors, obelisks and a logos
    /// dispenser at y 183 to 205 round (-315, -400), and at y 500 to 525 round (-465, 224).
    /// The rectangle becomes a box round the first and a second row is put round the other.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each new row goes in where the map is loaded
    /// and no box of that region is at that spot, Eloh Space's row only while it is as seeded;
    /// Down puts it back and takes the new rows out.
    /// </summary>
    public static class PenumbraHqRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, string Comment);

        public const uint MapContextId = 2028;
        public const uint HeadquartersRegion = 18, ElohSpaceRegion = 15;
        public const byte Box = 2;

        /// <summary>Eloh Space's row as MapRegionPreloader seeded it: its minimap's rectangle.</summary>
        public const uint ElohSpaceRowId = 223;
        public const double ElohSeededX = -409.0, ElohSeededZ = -115.0, ElohSeededHalfX = 132.5, ElohSeededHalfZ = 367.5;
        public const string ElohSeededComment = "arieki_torden_abyss - Eloh Space";

        /// <summary>The two Eloh shrines under Tantalus Base.</summary>
        public static readonly Volume ElohSouth = Of(ElohSpaceRegion, -345, -285, -455, -325, 175, 215, "arieki_torden_abyss - Eloh Space: the shrine under the base, south");
        public static readonly Volume ElohNorth = Of(ElohSpaceRegion, -520, -410, 195, 255, 495, 528, "arieki_torden_abyss - Eloh Space: the shrine under the base, north");

        public static readonly Volume[] Volumes =
        {
            // 18, Penumbra Headquarters: 18 boxes, y 530 to 573
            Of(18, -616.0, -584.0, -208.0, -168.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (1/18)"),
            Of(18, -496.0, -448.0, -200.0, -104.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (2/18)"),
            Of(18, -572.0, -552.0, -196.0, -108.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (3/18)"),
            Of(18, -520.0, -500.0, -192.0, -116.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (4/18)"),
            Of(18, -580.0, -572.0, -188.0, -116.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (5/18)"),
            Of(18, -552.0, -520.0, -188.0, -116.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (6/18)"),
            Of(18, -500.0, -496.0, -188.0, -116.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (7/18)"),
            Of(18, -664.0, -648.0, -184.0, -136.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (8/18)"),
            Of(18, -584.0, -580.0, -180.0, -128.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (9/18)"),
            Of(18, -600.0, -584.0, -168.0, -96.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (10/18)"),
            Of(18, -668.0, -664.0, -164.0, -140.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (11/18)"),
            Of(18, -648.0, -644.0, -164.0, -140.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (12/18)"),
            Of(18, -604.0, -600.0, -164.0, -140.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (13/18)"),
            Of(18, -680.0, -668.0, -160.0, -144.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (14/18)"),
            Of(18, -644.0, -604.0, -160.0, -144.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (15/18)"),
            Of(18, -616.0, -600.0, -136.0, -96.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (16/18)"),
            Of(18, -516.0, -508.0, -116.0, -112.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (17/18)"),
            Of(18, -492.0, -484.0, -104.0, -92.0, 530.0, 573.0, "arieki_torden_abyss - Penumbra Headquarters (18/18)"),
            // 22, Section 1: Information Technology: 4 boxes, y 535 to 544
            Of(22, -624.0, -588.0, -268.0, -228.0, 535.0, 544.0, "arieki_torden_abyss - Section 1: Information Technology (1/4)"),
            Of(22, -632.0, -624.0, -260.0, -244.0, 535.0, 544.0, "arieki_torden_abyss - Section 1: Information Technology (2/4)"),
            Of(22, -588.0, -580.0, -260.0, -228.0, 535.0, 544.0, "arieki_torden_abyss - Section 1: Information Technology (3/4)"),
            Of(22, -580.0, -572.0, -244.0, -228.0, 535.0, 544.0, "arieki_torden_abyss - Section 1: Information Technology (4/4)"),
            // 23, Section 2: Covert Surveillance: 5 boxes, y 533 to 545
            Of(23, -620.0, -584.0, -80.0, -40.0, 533.0, 545.0, "arieki_torden_abyss - Section 2: Covert Surveillance (1/5)"),
            Of(23, -628.0, -620.0, -72.0, -56.0, 533.0, 545.0, "arieki_torden_abyss - Section 2: Covert Surveillance (2/5)"),
            Of(23, -584.0, -568.0, -72.0, -56.0, 533.0, 545.0, "arieki_torden_abyss - Section 2: Covert Surveillance (3/5)"),
            Of(23, -584.0, -576.0, -56.0, -40.0, 533.0, 545.0, "arieki_torden_abyss - Section 2: Covert Surveillance (4/5)"),
            Of(23, -612.0, -592.0, -40.0, -36.0, 533.0, 545.0, "arieki_torden_abyss - Section 2: Covert Surveillance (5/5)"),
            // 24, Section 3: Espionage: 3 boxes, y 535 to 544
            Of(24, -696.0, -656.0, -224.0, -192.0, 535.0, 544.0, "arieki_torden_abyss - Section 3: Espionage (1/3)"),
            Of(24, -656.0, -652.0, -212.0, -192.0, 535.0, 544.0, "arieki_torden_abyss - Section 3: Espionage (2/3)"),
            Of(24, -652.0, -640.0, -208.0, -192.0, 535.0, 544.0, "arieki_torden_abyss - Section 3: Espionage (3/3)"),
            // 25, Section 4: Special Weapons Technology: 10 boxes, y 538 to 564
            Of(25, -724.0, -676.0, -124.0, -88.0, 538.0, 564.0, "arieki_torden_abyss - Section 4: Special Weapons Technology (1/10)"),
            Of(25, -708.0, -692.0, -88.0, -32.0, 538.0, 564.0, "arieki_torden_abyss - Section 4: Special Weapons Technology (2/10)"),
            Of(25, -736.0, -708.0, -84.0, -32.0, 538.0, 564.0, "arieki_torden_abyss - Section 4: Special Weapons Technology (3/10)"),
            Of(25, -692.0, -664.0, -84.0, -32.0, 538.0, 564.0, "arieki_torden_abyss - Section 4: Special Weapons Technology (4/10)"),
            Of(25, -768.0, -744.0, -80.0, -32.0, 538.0, 564.0, "arieki_torden_abyss - Section 4: Special Weapons Technology (5/10)"),
            Of(25, -656.0, -632.0, -80.0, -32.0, 538.0, 564.0, "arieki_torden_abyss - Section 4: Special Weapons Technology (6/10)"),
            Of(25, -744.0, -736.0, -76.0, -32.0, 538.0, 564.0, "arieki_torden_abyss - Section 4: Special Weapons Technology (7/10)"),
            Of(25, -664.0, -656.0, -76.0, -32.0, 538.0, 564.0, "arieki_torden_abyss - Section 4: Special Weapons Technology (8/10)"),
            Of(25, -740.0, -724.0, -32.0, -16.0, 538.0, 564.0, "arieki_torden_abyss - Section 4: Special Weapons Technology (9/10)"),
            Of(25, -676.0, -660.0, -32.0, -16.0, 538.0, 564.0, "arieki_torden_abyss - Section 4: Special Weapons Technology (10/10)"),
            // 26, Section 5: Special Projects: 20 boxes, y 543 to 552
            Of(26, -836.0, -804.0, -256.0, -176.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (1/20)"),
            Of(26, -796.0, -764.0, -224.0, -176.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (2/20)"),
            Of(26, -840.0, -836.0, -212.0, -188.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (3/20)"),
            Of(26, -804.0, -796.0, -212.0, -188.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (4/20)"),
            Of(26, -764.0, -760.0, -212.0, -188.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (5/20)"),
            Of(26, -860.0, -840.0, -208.0, -192.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (6/20)"),
            Of(26, -760.0, -740.0, -208.0, -192.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (7/20)"),
            Of(26, -756.0, -740.0, -192.0, -96.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (8/20)"),
            Of(26, -760.0, -756.0, -164.0, -140.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (9/20)"),
            Of(26, -740.0, -736.0, -164.0, -140.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (10/20)"),
            Of(26, -788.0, -760.0, -160.0, -144.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (11/20)"),
            Of(26, -736.0, -708.0, -160.0, -144.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (12/20)"),
            Of(26, -836.0, -804.0, -128.0, -48.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (13/20)"),
            Of(26, -796.0, -764.0, -128.0, -80.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (14/20)"),
            Of(26, -840.0, -836.0, -116.0, -92.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (15/20)"),
            Of(26, -804.0, -796.0, -116.0, -92.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (16/20)"),
            Of(26, -764.0, -760.0, -116.0, -92.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (17/20)"),
            Of(26, -876.0, -840.0, -112.0, -96.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (18/20)"),
            Of(26, -760.0, -756.0, -112.0, -96.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (19/20)"),
            Of(26, -876.0, -860.0, -96.0, -72.0, 543.0, 552.0, "arieki_torden_abyss - Section 5: Special Projects (20/20)"),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, comment);

        public static IEnumerable<string> Up()
        {
            yield return $"UPDATE map_region SET pos_x = {N(ElohSouth.X)}, pos_y = {N(ElohSouth.Y)}, pos_z = {N(ElohSouth.Z)}, half_x = {N(ElohSouth.HalfX)}, half_z = {N(ElohSouth.HalfZ)}, " +
                $"min_y = {N(ElohSouth.MinY)}, max_y = {N(ElohSouth.MaxY)}, comment = {S(ElohSouth.Comment)} WHERE {ElohSeeded()};";

            foreach (var v in new[] { ElohNorth }.Concat(Volumes))
                yield return Insert(v);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var v in Volumes.Reverse().Append(ElohNorth))
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)};";

            yield return $"UPDATE map_region SET pos_x = {N(ElohSeededX)}, pos_y = 624.1, pos_z = {N(ElohSeededZ)}, half_x = {N(ElohSeededHalfX)}, half_z = {N(ElohSeededHalfZ)}, " +
                $"min_y = -2000, max_y = 2000, comment = {S(ElohSeededComment)} " +
                $"WHERE id = {ElohSpaceRowId} AND map_context_id = {MapContextId} AND region_id = {ElohSpaceRegion} AND shape = {Box} " +
                $"AND ABS(pos_x - {N(ElohSouth.X)}) < 0.01 AND ABS(pos_z - {N(ElohSouth.Z)}) < 0.01 AND ABS(half_x - {N(ElohSouth.HalfX)}) < 0.01 AND ABS(half_z - {N(ElohSouth.HalfZ)}) < 0.01;";
        }

        private static string ElohSeeded() =>
            $"id = {ElohSpaceRowId} AND map_context_id = {MapContextId} AND region_id = {ElohSpaceRegion} AND shape = {Box} " +
            $"AND ABS(pos_x - {N(ElohSeededX)}) < 0.01 AND ABS(pos_z - {N(ElohSeededZ)}) < 0.01 AND ABS(half_x - {N(ElohSeededHalfX)}) < 0.01 AND ABS(half_z - {N(ElohSeededHalfZ)}) < 0.01";

        private static string Insert(Volume v) =>
            "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
            $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, 0, 1, {S(v.Comment)} " +
            $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
