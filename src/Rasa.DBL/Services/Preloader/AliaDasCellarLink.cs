using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The way from Alia Das into the CELLAR Arena (map link 17) and the room it is in, the
    /// hospital at the west end of the barracks (Alia_Das_cellar_link).
    ///
    /// Alia_Das_arena_link put the link under the Challenge Arena holosign at (748.10, 383.98),
    /// 1.5 m round. The sign hangs on the room's west wall (the floor ends at x 748.5), so half the
    /// circle was in the wall and only a sliver along it could be stood in: a player in front of
    /// the sign was outside it. It goes where a player stands in front of the sign, (749.64,
    /// 382.55), as reported in game.
    ///
    /// That is 0.5 m from where the hospital set the revived down (teleporter 103, 750.07, 382.29),
    /// which is why the link was moved off it: a player revived there and stepping about was taken
    /// to the arena. The revive point goes to the east end of the same room, (757, 382.5), 7.4 m
    /// off, facing the sign (yaw pi/2: Atan2(-dx, -dz) for a heading to -x). And the arrival from
    /// the arena (link 136's destination, 751.5, 382.5, faced west at 1.1053) put players 0.36 m
    /// from the new circle facing into it: it goes to (753.5, 382.5) facing east into the room.
    /// The floor is flat there at 294.21 (adv_foreas_concordia_wilderness).
    ///
    /// Guarded: each row changes only from the values it had. Plain SQL, valid on SQLite and MySQL
    /// alike.
    /// </summary>
    public static class AliaDasCellarLink
    {
        public const uint ArenaLinkId = 17;
        public const uint ReturnLinkId = 136;
        public const uint HospitalId = 103;

        public static readonly (double X, double Y, double Z, double Radius) LinkWas = (748.1018, 294.2103, 383.9836, 1.5);
        public static readonly (double X, double Y, double Z, double Radius) Link = (749.6406, 294.2109, 382.5547, 1.5);

        public static readonly (double X, double Y, double Z, double Rotation) ArrivalWas = (751.5, 294.2103, 382.5, 1.1053);
        public static readonly (double X, double Y, double Z, double Rotation) Arrival = (753.5, 294.2103, 382.5, 4.7124);

        public static readonly (double X, double Y, double Z, double Rotation) HospitalWas = (750.0742, 294.21094, 382.28906, 4.69);
        public static readonly (double X, double Y, double Z, double Rotation) Hospital = (757.0, 294.21094, 382.5, 1.5708);

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            yield return MoveLink(LinkWas, Link);
            yield return MoveArrival(ArrivalWas, Arrival);
            yield return MoveHospital(HospitalWas, Hospital);
        }

        public static IEnumerable<string> Down()
        {
            yield return MoveLink(Link, LinkWas);
            yield return MoveArrival(Arrival, ArrivalWas);
            yield return MoveHospital(Hospital, HospitalWas);
        }

        private static string MoveLink((double X, double Y, double Z, double Radius) from, (double X, double Y, double Z, double Radius) to) =>
            $"UPDATE map_link SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)}, radius = {N(to.Radius)} " +
            $"WHERE id = {ArenaLinkId} AND map_context_id = 1220 AND dest_map_context_id = 20000009 " +
            $"AND {Near("pos_x", from.X)} AND {Near("pos_y", from.Y)} AND {Near("pos_z", from.Z)} AND {Near("radius", from.Radius)};";

        private static string MoveArrival((double X, double Y, double Z, double Rotation) from, (double X, double Y, double Z, double Rotation) to) =>
            $"UPDATE map_link SET dest_pos_x = {N(to.X)}, dest_pos_y = {N(to.Y)}, dest_pos_z = {N(to.Z)}, dest_rotation = {N(to.Rotation)} " +
            $"WHERE id = {ReturnLinkId} AND map_context_id = 20000009 AND dest_map_context_id = 1220 " +
            $"AND {Near("dest_pos_x", from.X)} AND {Near("dest_pos_y", from.Y)} AND {Near("dest_pos_z", from.Z)} AND {Near("dest_rotation", from.Rotation)};";

        private static string MoveHospital((double X, double Y, double Z, double Rotation) from, (double X, double Y, double Z, double Rotation) to) =>
            $"UPDATE teleporter SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)}, rotation = {N(to.Rotation)} " +
            $"WHERE id = {HospitalId} AND map_context_id = 1220 " +
            $"AND {Near("pos_x", from.X)} AND {Near("pos_y", from.Y)} AND {Near("pos_z", from.Z)} AND {Near("rotation", from.Rotation)};";

        private static string Near(string column, double value) => $"ABS({column} - {N(value)}) < 0.001";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
