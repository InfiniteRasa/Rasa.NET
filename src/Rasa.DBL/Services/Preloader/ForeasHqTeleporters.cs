using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The way down from Foreas Base's headquarters on Concordia Divide to the corridors under it,
    /// and on to the Awake logos (Foreas_hq_teleporters).
    ///
    /// Under the base, 64 m below the yard, is a complex of corridors and rooms (floors at 48 and
    /// 52.5 m) round a main hall, and to the north-west of it, apart, a room of its own with the
    /// Awake logos (-134, 48, 534). Neither has a way in: no navmesh joins the complex to the
    /// surface or the logos room to the complex. The headquarters (ArchHumanBaseHeadquartersForeas
    /// at -42, 116, 447) has an alcove at its back, below its floor and under the mezzanine
    /// (-42, 116.3, 435), with a light hung over it and nothing in it: where the pad down was.
    ///
    /// Two pairs of human teleporter pads (UsableTwoStateHumTeleporter, as the CELLAR Arena's and
    /// the Few shaft's are), each pair linked both ways:
    /// - the alcove and the south-west corner of the corridor ring at the complex's west end
    ///   (-112, 52.5, 464), off every way through the complex;
    /// - the ring's north-west corner (-112, 52.5, 496), and the logos room's south-east corner
    ///   (-113.86, 48, 534.25), across the room from the logos.
    /// A traveller faces: into the headquarters; along the ring to its other pad; back along the
    /// ring to the pad up; at the logos.
    ///
    /// Ids 948 to 951: under 1000, as every placed teleporter's is (CellarTeleporterPads).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A row goes in only where its id is free, a
    /// link only where its pad has none and both its ends are there; Down takes out the links and
    /// the rows only as Up put them.
    /// </summary>
    public static class ForeasHqTeleporters
    {
        public const uint MapContextId = 1148;          // adv_foreas_concordia_divide
        public const uint HumanTeleporter = 7895;       // UsableTwoStateHumTeleporter
        public const uint LocalTeleporterType = 1;
        public const uint Alcove = 948, RingSouth = 949, RingNorth = 950, LogosRoom = 951;
        public const uint FirstTeleporter = Alcove, LastTeleporter = LogosRoom;

        public sealed record Row(uint Id, double X, double Y, double Z, double Rotation, string Description);

        /// <summary>On the floor; rotation r faces (-sin r, -cos r) in x and z.</summary>
        public static readonly Row[] Rows =
        {
            new(Alcove, -42.0, 116.31, 435.0, 3.14159265358979, "Local Teleporter: Foreas Base headquarters, down to the lower levels"),
            new(RingSouth, -112.0, 52.5, 464.0, 3.14159265358979, "Local Teleporter: Foreas Base lower levels, up to the headquarters"),
            new(RingNorth, -112.0, 52.5, 496.0, 0.0, "Local Teleporter: Foreas Base lower levels, to the logos room"),
            new(LogosRoom, -113.86, 48.0, 534.25, 1.5707963267949, "Local Teleporter: Foreas Base logos room, back to the lower levels"),
        };

        public static readonly (uint From, uint To)[] Links =
        {
            (Alcove, RingSouth), (RingSouth, Alcove),
            (RingNorth, LogosRoom), (LogosRoom, RingNorth),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var r in Rows)
                yield return "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
                    $"SELECT {r.Id}, {HumanTeleporter}, {LocalTeleporterType}, {S(r.Description)}, {N(r.X)}, {N(r.Y)}, {N(r.Z)}, {N(r.Rotation)}, {MapContextId} " +
                    $"FROM entityclass e WHERE e.id = {HumanTeleporter} AND NOT EXISTS (SELECT 1 FROM teleporter x WHERE x.id = {r.Id});";

            foreach (var (from, to) in Links)
                yield return $"INSERT INTO {LocalTeleporterLinkEntry.TableName} (from_teleporter_id, to_teleporter_id) " +
                    $"SELECT {from}, {to} FROM teleporter a, teleporter b " +
                    $"WHERE a.id = {from} AND a.type = {LocalTeleporterType} AND a.map_context_id = {MapContextId} " +
                    $"AND b.id = {to} AND b.type = {LocalTeleporterType} AND b.map_context_id = {MapContextId} " +
                    $"AND NOT EXISTS (SELECT 1 FROM {LocalTeleporterLinkEntry.TableName} x WHERE x.from_teleporter_id = {from});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (from, to) in Links)
                yield return $"DELETE FROM {LocalTeleporterLinkEntry.TableName} WHERE from_teleporter_id = {from} AND to_teleporter_id = {to};";

            foreach (var r in Rows)
                yield return $"DELETE FROM teleporter WHERE id = {r.Id} AND class_id = {HumanTeleporter} AND type = {LocalTeleporterType} " +
                    $"AND map_context_id = {MapContextId} AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01;";
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
