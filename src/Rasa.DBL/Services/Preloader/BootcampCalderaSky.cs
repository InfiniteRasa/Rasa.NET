using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Proving Grounds' outdoors under its own sky (Bootcamp_caldera_sky).
    ///
    /// A map's sky is its region's: adv_bootcamp.map gives each region a sky, and the client draws
    /// the sky of the highest-priority region the server says the player is in, or region 0's
    /// when it says none (regionmanager.py OnUpdateRegions, DEFAULT_REGION_ID). Its regions:
    ///
    ///   region  priority  name               sky
    ///   0       0         Luna Cavern        sky_foreas_bootcamp_lunacave (dim red light, no sun)
    ///   2       2         Luna Cavern        sky_foreas_cave_bigroom
    ///   10      3         Denzil's Caldera   sky_foreas_bootcamp_d11 (sun, moon, clouds, daylight)
    ///   9       4         Denzil's Caldera   sky_foreas_bootcamp_d11
    ///   4       5         Denzil's Caldera   sky_foreas_bootcamp_d11
    ///   5       6         Denzil's Caldera   sky_foreas_general_human_interior (no sun)
    ///
    /// The volumes seeded for them (map_region) were 200 m circles round the two names' map
    /// labels, every region of a name in each. So inside the Caldera's circle the player was in
    /// region 5 as well, whose priority is the highest and whose sky is a building's interior;
    /// and outside the two circles - most of the outdoors, the crash site among it - in none, so
    /// under the cavern's. The outdoors was dark wherever one stood, at any time of day: the d11
    /// sky's light does not change with the time.
    ///
    /// The cavern is the east of the map (the start, the firing range, the front), behind the
    /// Eloh cavern entrance at x 208 and the cavern entrance at x 288; the Caldera, with the fort,
    /// the lakes and the crash site, is everything west of it. So:
    ///
    ///  - region 4's volume becomes a box over the whole map west of x 208, on the surface. Its
    ///    priority is above the cavern's, so where the cavern's circle reaches past the entrance
    ///    the outdoors still wins; under the ground (NavMeshFlags.Underground) nothing is sent and
    ///    the tunnels have the cavern's sky.
    ///  - region 5's volume is disabled: no interior of the Caldera is known for it. A game master
    ///    puts it where it belongs (.region).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike, each row changed only while it is as it was
    /// seeded; Down puts both back.
    /// </summary>
    public static class BootcampCalderaSky
    {
        public const uint BootcampMapContextId = 1985;
        public const uint CalderaRegion = 4, InteriorRegion = 5;

        /// <summary>The seeded circle round "Denzil's Caldera".</summary>
        public const double LabelX = 8.8, LabelZ = 245.8, LabelRadius = 200.0;

        /// <summary>The outdoors: west of the Eloh cavern entrance (x 208), the map's whole depth.</summary>
        public const double West = -512, East = 208, South = -512, North = 512;

        public const double BoxX = (West + East) / 2, BoxZ = (South + North) / 2, HalfX = (East - West) / 2, HalfZ = (North - South) / 2;

        public const string SeededComment = "bootcamp - Denzil's Caldera";
        public const string BoxComment = "bootcamp - Denzil's Caldera: the outdoors, west of the Eloh cavern entrance";

        public const byte Circle = 1, Box = 2;

        public static IEnumerable<string> Up()
        {
            yield return $"UPDATE map_region SET shape = {Box}, pos_x = {N(BoxX)}, pos_z = {N(BoxZ)}, radius = 0, half_x = {N(HalfX)}, half_z = {N(HalfZ)}, comment = {S(BoxComment)} " +
                $"WHERE {Seeded(CalderaRegion)};";

            yield return $"UPDATE map_region SET enabled = 0 WHERE {Seeded(InteriorRegion)} AND enabled = 1;";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"UPDATE map_region SET enabled = 1 WHERE {Seeded(InteriorRegion)} AND enabled = 0;";

            yield return $"UPDATE map_region SET shape = {Circle}, pos_x = {N(LabelX)}, pos_z = {N(LabelZ)}, radius = {N(LabelRadius)}, half_x = 0, half_z = 0, comment = {S(SeededComment)} " +
                $"WHERE map_context_id = {BootcampMapContextId} AND region_id = {CalderaRegion} AND shape = {Box} " +
                $"AND ABS(pos_x - {N(BoxX)}) < 0.01 AND ABS(pos_z - {N(BoxZ)}) < 0.01 AND ABS(half_x - {N(HalfX)}) < 0.01 AND ABS(half_z - {N(HalfZ)}) < 0.01;";
        }

        /// <summary>The region's volume as seeded: the circle round the Caldera's label.</summary>
        private static string Seeded(uint region) =>
            $"map_context_id = {BootcampMapContextId} AND region_id = {region} AND shape = {Circle} " +
            $"AND ABS(pos_x - {N(LabelX)}) < 0.01 AND ABS(pos_z - {N(LabelZ)}) < 0.01 AND ABS(radius - {N(LabelRadius)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
