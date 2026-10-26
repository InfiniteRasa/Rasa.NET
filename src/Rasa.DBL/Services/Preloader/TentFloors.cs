namespace Rasa.Services.Preloader
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Seven NPCs that stood on the roof of the tent they belong in, put on its floor: Colonel
    /// Bosley and Supply Sergeant Lipschitz at Camp Resistance were reported standing on top of
    /// their tents, and the five others are the same.
    ///
    /// Each pool's point is in its tent's footprint at the height of the ridge, entered from a
    /// map marker over the tent. StandingHeights takes the floor under a point to be the surface
    /// facing up with nothing over it for 1.7 m, and that is the roof for a point that is on it,
    /// so it left Lipschitz and the Wedge Rock and Ortho vendors as they were and took Bosley up
    /// 0.22 m and Field Medic DuBois down 0.20 m, to the roof's skin.
    ///
    /// Measured as StandingHeights was, against the collision mesh of each tent at its place in
    /// the .map (Rasa.ClientData): the tents' floor is 4.7 to 5.6 m under the ridge and clear to
    /// the inside of the roof, 3.5 m up at the least. Nothing else stands on the floor within
    /// 0.5 m of the point, except a desk monitor half a metre from DuBois. The points stay
    /// where they are across the ground; only their height changes.
    ///
    /// Every statement tests the height it replaces, so a row changed since is left alone and a
    /// second run does nothing; Down puts each back.
    /// </summary>
    public static class TentFloors
    {
        /// <summary>A pool, the roof height it had, and its tent's floor.</summary>
        private static readonly (uint Pool, double Was, double Floor)[] Heights =
        {
            // adv_arieki_torden_incline (1761)
            (500079, 264.9f, 260.18),     // Hospital: Ortho (Control Point), medic tent: 4.72 m down
            // adv_foreas_howlingmaw1 (2051)
            (500204, 195.5f, 190.39),     // Dia Uyona Hospital, command tent: 5.11 m down
            // adv_foreas_valverde_marshes (1454)
            (510161, 224.05, 219.08),     // Field Medic DuBois, Falcon Hold, medic tent: 4.97 m down
            // adv_foreas_valverde_plateau (1497)
            (500276, 383.7f, 378.16),     // Armor Vendor: Wedge Rock Outpost, tent: 5.54 m down
            (500283, 448.0f, 442.43),     // Supply Sergeant Lipschitz, Camp Resistance, medic tent: 5.57 m down
            (500285, 383.6f, 378.16),     // Weapons Vendor: Wedge Rock Outpost, tent: 5.44 m down
            (510176, 447.95, 442.43),     // Colonel Bosley, Camp Resistance, communications tent: 5.52 m down
        };

        public static IReadOnlyList<uint> Pools { get; } = Heights.Select(row => row.Pool).ToArray();

        public static readonly string[] WorldUp = Statements(false);

        public static readonly string[] WorldDown = Statements(true);

        /// <summary>Each pool's height after Up.</summary>
        public static IEnumerable<(uint Pool, double Y)> Corrected() => Heights.Select(row => (row.Pool, row.Floor));

        /// <summary>Each pool's height before Up.</summary>
        public static IEnumerable<(uint Pool, double Y)> Original() => Heights.Select(row => (row.Pool, row.Was));

        private static string[] Statements(bool back)
        {
            var statements = new List<string>();

            foreach (var (pool, was, floor) in Heights)
            {
                var (from, to) = back ? (floor, was) : (was, floor);

                statements.Add($"update spawnpool set pos_y = {N(to)} where id = {pool} and abs(pos_y - {N(from)}) < 0.005;");
            }

            return statements.ToArray();
        }

        private static string N(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
