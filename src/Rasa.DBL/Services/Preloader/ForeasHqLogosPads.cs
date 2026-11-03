using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The pads to the Awake logos under Foreas Base, where they were (Foreas_hq_logos_pads).
    ///
    /// Foreas_hq_teleporters put the pad to the logos room in the north-west corner of the
    /// corridor ring at the lower levels' west end (950, -112, 52.5, 496) and its partner in the
    /// logos room's south-east corner (951, -113.86, 48, 534.25). A player who knew the base places
    /// the pad on the ring's east side, facing the way in from the hall (-98, 53, 478.5), and has
    /// it set them down in the logos room's hall (-118, 48.2, 560), the Awake logos 30 m off
    /// (-134.2, 48.7, 534.3). Both move there, on the floor (52.5 and 48):
    /// - 950 faces out of the ring, towards the hall (east);
    /// - 951 faces the logos;
    /// - 949, the pad up to the headquarters in the ring's south-west corner, stays, and faces
    ///   950 where it now is rather than straight up the ring.
    /// The links stay as they are.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A row moves only while it is where
    /// Foreas_hq_teleporters put it, and Down moves it back only while it is where Up put it.
    /// </summary>
    public static class ForeasHqLogosPads
    {
        public sealed record Place(double X, double Y, double Z, double Rotation);

        public sealed record Move(uint Id, Place From, Place To);

        public static readonly Move[] Moves =
        {
            new(ForeasHqTeleporters.RingSouth, new(-112.0, 52.5, 464.0, 3.14159265358979), new(-112.0, 52.5, 464.0, -2.37)),
            new(ForeasHqTeleporters.RingNorth, new(-112.0, 52.5, 496.0, 0.0), new(-98.0, 52.5, 478.5, -1.5707963267949)),
            new(ForeasHqTeleporters.LogosRoom, new(-113.86, 48.0, 534.25, 1.5707963267949), new(-118.0, 48.0, 560.0, 0.56)),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var move in Moves)
                yield return Sql(move.Id, move.From, move.To);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var move in Moves)
                yield return Sql(move.Id, move.To, move.From);
        }

        private static string Sql(uint id, Place from, Place to) =>
            $"UPDATE teleporter SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)}, rotation = {N(to.Rotation)} " +
            $"WHERE id = {id} AND class_id = {ForeasHqTeleporters.HumanTeleporter} AND type = {ForeasHqTeleporters.LocalTeleporterType} " +
            $"AND map_context_id = {ForeasHqTeleporters.MapContextId} AND ABS(pos_x - {N(from.X)}) < 0.01 AND ABS(pos_y - {N(from.Y)}) < 0.01 " +
            $"AND ABS(pos_z - {N(from.Z)}) < 0.01 AND ABS(rotation - {N(from.Rotation)}) < 0.01;";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
