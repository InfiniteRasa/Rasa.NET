using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The way back up from the Few logos (Few_shaft_teleporter).
    ///
    /// The Few logos (Howling Maw, -482, 123, -501) is in an Eloh room at the foot of a mine
    /// shaft in the middle of the Dead Zone Power Station: seven rings of outpost ditch stacked
    /// 10.6 m apart from the base's floor at 201 m down to the cave mouth at 125 m, with the
    /// power station's pipes down the middle. The way in is down the rings, ring to ring. Nothing
    /// leads back up: no ring can be climbed to from the one below, the corridor behind the room
    /// is closed by a cave-in (seven rocks across it, z -528 to -540), and its alcove is behind
    /// that - so a player who took the logos had /stuck or dying and nothing else.
    ///
    /// A human teleporter pad (UsableTwoStateHumTeleporter, as the CELLAR Arena's pads are) behind
    /// the logos, at the back of the room, off the way in from the shaft. It takes whoever steps
    /// onto it up to the base floor on the north side of the shaft, 8 m from its rim, facing
    /// north into the base. That end is a teleporter row of class 0, which the server never shows
    /// a client and which has no link: the pad goes up and nothing comes down, so the rings stay
    /// the way in.
    ///
    /// Ids 988 and 989: under 1000, as every placed teleporter's is (CellarTeleporterPads).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A row goes in only where its id is free, the
    /// link only where the pad has none and both its ends are there; Down takes out the link and
    /// the rows only as Up put them.
    /// </summary>
    public static class FewShaftTeleporter
    {
        public const uint MapContextId = 2051;          // adv_foreas_howlingmaw1
        public const uint HumanTeleporter = 7895;       // UsableTwoStateHumTeleporter
        public const uint LocalTeleporterType = 1;
        public const uint Pad = 988, Landing = 989;

        public sealed record Row(uint Id, uint ClassId, double X, double Y, double Z, double Rotation, string Description);

        /// <summary>The pad on the Few room's floor, 13 m behind the logos; the landing on the base floor, facing +Z (rotation pi).</summary>
        public static readonly Row[] Rows =
        {
            new(Pad, HumanTeleporter, -482.0, 123.3, -514.0, 0.0, "Local Teleporter: Few logos chamber, up to Dead Zone Power Station"),
            new(Landing, 0, -488.0, 200.6, -398.0, 3.14159265358979, "Local Teleporter: Dead Zone Power Station, from the Few logos chamber"),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var r in Rows)
                yield return "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
                    $"SELECT {r.Id}, {r.ClassId}, {LocalTeleporterType}, {S(r.Description)}, {N(r.X)}, {N(r.Y)}, {N(r.Z)}, {N(r.Rotation)}, {MapContextId} " +
                    (r.ClassId != 0 ? $"FROM entityclass e WHERE e.id = {r.ClassId} AND " : "FROM map_info m WHERE m.map_context_id = " + MapContextId + " AND ") +
                    $"NOT EXISTS (SELECT 1 FROM teleporter x WHERE x.id = {r.Id});";

            yield return $"INSERT INTO {LocalTeleporterLinkEntry.TableName} (from_teleporter_id, to_teleporter_id) " +
                $"SELECT {Pad}, {Landing} FROM teleporter a, teleporter b " +
                $"WHERE a.id = {Pad} AND a.type = {LocalTeleporterType} AND a.map_context_id = {MapContextId} " +
                $"AND b.id = {Landing} AND b.type = {LocalTeleporterType} AND b.map_context_id = {MapContextId} " +
                $"AND NOT EXISTS (SELECT 1 FROM {LocalTeleporterLinkEntry.TableName} x WHERE x.from_teleporter_id = {Pad});";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM {LocalTeleporterLinkEntry.TableName} WHERE from_teleporter_id = {Pad} AND to_teleporter_id = {Landing};";

            foreach (var r in Rows)
                yield return $"DELETE FROM teleporter WHERE id = {r.Id} AND class_id = {r.ClassId} AND type = {LocalTeleporterType} " +
                    $"AND map_context_id = {MapContextId} AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01;";
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
