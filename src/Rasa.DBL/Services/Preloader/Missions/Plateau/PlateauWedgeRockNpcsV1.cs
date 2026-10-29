using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Rasa.Structures;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Wedge Rock Outpost's four NPCs come down from the rock's top into the outpost. MissionNpcSpawnpoolPreloader
    /// put Colonel Thibodeau, Comm Officer Devinchy, Comm Officer McKinley and Private Remick together on the rock
    /// (about 480, 426, 427), 46 m over the outpost's tents (378) and with no path to them, as it had Science
    /// Officer Clark (PlateauPerduNpcsV1). Playtest: the rock's top is no place for the outpost's officers.
    ///  - Devinchy (510178): TaRapedia /loc 363, 369, 360, in the outpost's west part (its floor plates at
    ///    369.41). To (363, 369.41, 360).
    ///  - McKinley (510179): TaRapedia /loc 466, 378, 359, beside the research tent. To (466, 378.16, 359).
    ///  - Remick (510188): TaRapedia /loc 369, 369, 353, beside Devinchy. To (369, 369.37, 353).
    ///  - Thibodeau (510177): no source gives his place (TaRapedia has no page for him). ESTIMATE: the HQ tent
    ///    with Outpost Cmdr. Russ, at (451, 378.16, 362).
    /// Cryptic Messages' (557) markers on McKinley (objective 1) and Devinchy (3) move with them. Down puts all
    /// back. Plain SQL, updates of the rows that are there.
    /// </summary>
    public static class PlateauWedgeRockNpcsV1
    {
        public const uint Thibodeau = 510177, Devinchy = 510178, McKinley = 510179, Remick = 510188;

        /// <summary>(pool, where it was, where it goes).</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) At)[] Moves =
        {
            (Thibodeau, (480.9, 426.07, 425.5), (451, 378.16, 362)),
            (Devinchy, (482.3, 426.42, 430), (363, 369.41, 360)),
            (McKinley, (477.1, 426.08, 428.3), (466, 378.16, 359)),
            (Remick, (477.8, 424.54, 423.5), (369, 369.37, 353)),
        };

        /// <summary>Cryptic Messages' markers: (objective, indicator, where it was, where it goes).</summary>
        public static readonly (uint Objective, uint Indicator, (double X, double Y, double Z) Before, (double X, double Y, double Z) At)[] Markers =
        {
            (1, 1, (477.4, 426, 428.4), (466, 378.16, 359)),    // McKinley
            (3, 3, (482, 426, 430), (363, 369.41, 360)),        // Devinchy
        };

        public static IEnumerable<string> Up() =>
            Moves.Select(move => PlateauFortDefianceNpcsV1.MoveSql(move.Pool, move.At))
                .Concat(Markers.Select(marker => MarkerSql(marker.Objective, marker.Indicator, marker.At)));

        public static IEnumerable<string> Down() =>
            Moves.Select(move => PlateauFortDefianceNpcsV1.MoveSql(move.Pool, move.Before))
                .Concat(Markers.Select(marker => MarkerSql(marker.Objective, marker.Indicator, marker.Before)));

        private static string MarkerSql(uint objective, uint indicator, (double X, double Y, double Z) at) =>
            $"UPDATE mission_indicator SET pos_x = {F(at.X)}, pos_y = {F(at.Y)}, pos_z = {F(at.Z)} " +
            $"WHERE mission_id = {PlateauCrypticMessagesV1.MissionId} AND content_revision = '{PlateauMissionDataV1.Revision}' " +
            $"AND objective_id = {objective} AND indicator_id = {MissionIndicator.UnnamedFrom + indicator};";

        private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
