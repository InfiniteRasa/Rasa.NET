using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Colonel "Snake" Washington to his sourced place at the Snake Pit (the user's choice of 2026-10-25).
    /// PlateauSnakePitV1 put him at (-968, 919.22, 363), an ESTIMATE that is Scout Ryan's sourced place (TaRapedia
    /// -972, 919, 362; IGN TR Vault -966.4, 919.1, 364.7).
    ///
    /// Sourced: TaRapedia Colonel 'Snake' Washington /loc (-981, 915, 413); IGN TR Vault (-979.2, 927.5, 402.6). The IGN
    /// point is on a 101 m2 navmesh island 12 m above the ground (a roof or platform), so TaRapedia's place is used, on
    /// the navmesh's main region (0.6 m above it: height 915.6). South Of The Border's marker at Snake
    /// (PlateauSouthOfTheBorderV1, objective Deliver) moves with him. Down puts both back.
    /// </summary>
    public static class PoolsWashingtonPlaceV1
    {
        public const uint Snake = Plateau.PlateauSnakePitV1.Snake;
        public static readonly (double X, double Y, double Z) At = (-981, 915.6, 413);
        public static readonly (double X, double Y, double Z) Before = Plateau.PlateauSnakePitV1.SnakeAt;

        public static IEnumerable<string> Up()
        {
            yield return Move(At);
            yield return MoveMarker(Before, At);
        }

        public static IEnumerable<string> Down()
        {
            yield return Move(Before);
            yield return MoveMarker(At, Before);
        }

        /// <summary>South Of The Border's marker at Snake, only while it is still where he stood.</summary>
        private static string MoveMarker((double X, double Y, double Z) from, (double X, double Y, double Z) to) =>
            string.Format(CultureInfo.InvariantCulture,
                "UPDATE mission_indicator SET pos_x = {0}, pos_y = {1}, pos_z = {2} WHERE mission_id = {3} AND content_revision = '{4}' " +
                "AND objective_id = {5} AND pos_x = {6} AND pos_z = {7};",
                to.X, to.Y, to.Z, Plateau.PlateauSouthOfTheBorderV1.MissionId, Plateau.PlateauMissionDataV1.Revision,
                Plateau.PlateauSouthOfTheBorderV1.Deliver, from.X, from.Z);

        private static string Move((double X, double Y, double Z) at) =>
            string.Format(CultureInfo.InvariantCulture, "UPDATE spawnpool SET pos_x = {0}, pos_y = {1}, pos_z = {2} WHERE id = {3};",
                at.X, at.Y, at.Z, Snake);
    }
}
