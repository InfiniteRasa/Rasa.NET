using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Lightning strikes across the Lightning Fields on Torden Plains (map 1764), the open ground
    /// round control point 23: one world_usable row (PlacedUsables, a RANDOMEXPLOSION) at each of
    /// 50 points, rows 10401-10450. The zone's briefing has the ground's metallic minerals drawing
    /// "air to ground lightning storms".
    ///
    /// What strikes is RandomExplosionAriekiLightning (8002), which no .map places: its effect is
    /// the bolt coming down (vfx_arieki_lightning_strikes_v01, after a wind-up), and its blast is
    /// OBJ_DAMAGE 3, Electrical (PlacedUsables.ArgOf: a class named for lightning).
    /// The blast is the client's: 250-750 within 5 m (actiondata OBJ_DAMAGE 3).
    ///
    /// The points fill the Lightning Fields' label circle (centre -51.3, 530.5, 175 m) at least
    /// 28 m apart, on the navmesh's ground (heights from it), and keep off what players stand
    /// at or walk through:
    ///  - the control point's compound (x -182 to -75, z 456 to 571, which holds its waypoint
    ///    and hospital) and the road west of it toward Mt. Hellas Outpost (x below -178);
    ///  - 10 m round every structure and prop the .map places;
    ///  - the hillside to the north and the rocks, which the zone map draws dark.
    ///
    /// How often each strikes is ours: a shortest wait of 20-45 s and a longest 15-40 s more,
    /// drawn once per point (seeded by its row id, so the rows are the same each time they are
    /// made), every wait between two strikes a fresh roll between the two. Over the 50, about
    /// one strike a second somewhere on the field.
    /// </summary>
    public sealed class LightningFieldsStrikes : WorldUsablePreloader
    {
        public const uint FirstId = 10401;
        public const uint LastId = 10450;

        public const uint MapContextId = 1764;  // Torden Plains
        public const uint LightningClass = 8002; // RandomExplosionAriekiLightning

        public const uint DamageMin = 250, DamageMax = 750;
        public const double Radius = 5;

        /// <summary>The Lightning Fields' label circle (map_region regions 2 and 29, the label and the control point's): x, z, radius.</summary>
        public const double FieldX = -51.3, FieldZ = 530.5, FieldRadius = 175;

        /// <summary>Row id, the point (on the navmesh), the shortest and longest wait in seconds.</summary>
        public static readonly (uint Id, double X, double Y, double Z, uint MinSeconds, uint MaxSeconds)[] Strikes =
        {
            (10401, -141.21, 419.80, 634.30, 33, 54),
            (10402, 53.96, 430.80, 634.24, 27, 49),
            (10403, 12.32, 431.95, 624.54, 33, 72),
            (10404, -113.78, 422.88, 622.94, 32, 70),
            (10405, -35.60, 431.68, 612.68, 23, 52),
            (10406, 74.05, 431.06, 607.45, 21, 49),
            (10407, -71.56, 431.93, 605.23, 20, 46),
            (10408, 30.33, 431.88, 598.46, 44, 72),
            (10409, -11.02, 431.37, 592.58, 24, 42),
            (10410, -160.95, 426.55, 588.14, 33, 61),
            (10411, -94.87, 432.81, 586.31, 21, 52),
            (10412, -123.59, 431.86, 582.41, 41, 77),
            (10413, 109.68, 431.45, 582.25, 31, 63),
            (10414, -60.40, 431.92, 575.32, 24, 61),
            (10415, 13.44, 431.51, 573.47, 21, 57),
            (10416, 56.45, 431.67, 564.14, 30, 61),
            (10417, 96.79, 432.31, 556.76, 37, 58),
            (10418, -16.87, 431.16, 550.97, 41, 72),
            (10419, 16.56, 432.05, 544.17, 42, 58),
            (10420, -53.30, 431.18, 524.62, 36, 74),
            (10421, 54.72, 431.58, 520.74, 42, 62),
            (10422, -25.93, 430.97, 512.75, 36, 65),
            (10423, 86.29, 431.13, 510.96, 38, 69),
            (10424, 0.00, 430.99, 500.00, 42, 75),
            (10425, -41.49, 432.40, 489.08, 21, 36),
            (10426, 51.89, 431.30, 486.68, 39, 79),
            (10427, 83.78, 431.47, 483.01, 41, 81),
            (10428, 21.44, 431.38, 476.73, 33, 71),
            (10429, -67.80, 432.21, 476.35, 32, 59),
            (10430, 108.64, 433.18, 470.05, 37, 67),
            (10431, -5.23, 431.60, 458.19, 32, 69),
            (10432, -90.31, 432.11, 451.85, 23, 44),
            (10433, 36.01, 432.50, 449.41, 24, 54),
            (10434, -149.43, 432.34, 446.86, 29, 62),
            (10435, 66.93, 432.01, 446.07, 44, 62),
            (10436, -119.10, 431.33, 445.03, 29, 55),
            (10437, 9.55, 433.13, 428.07, 27, 50),
            (10438, -75.83, 432.85, 426.99, 33, 55),
            (10439, -175.22, 435.98, 423.35, 38, 60),
            (10440, -109.78, 433.03, 417.54, 26, 51),
            (10441, 47.98, 434.30, 414.53, 20, 43),
            (10442, -3.73, 439.49, 402.73, 38, 55),
            (10443, -150.07, 435.84, 400.86, 39, 68),
            (10444, -36.08, 441.56, 399.05, 31, 62),
            (10445, -78.67, 444.80, 396.36, 32, 66),
            (10446, 29.07, 440.06, 387.35, 35, 51),
            (10447, -8.42, 441.69, 370.03, 38, 57),
            (10448, -41.36, 439.00, 368.93, 40, 68),
            (10449, -69.83, 439.04, 368.51, 45, 82),
            (10450, -100.06, 444.13, 367.60, 32, 53),
        };

        protected override IEnumerable<object[]> GetRows()
        {
            foreach (var strike in Strikes)
                yield return Explosion(strike.Id, MapContextId, LightningClass, strike.X, strike.Y, strike.Z,
                    "Lightning strike, Lightning Fields",
                    minSeconds: strike.MinSeconds, maxSeconds: strike.MaxSeconds, damageMin: DamageMin, damageMax: DamageMax, radius: Radius);
        }
    }
}
