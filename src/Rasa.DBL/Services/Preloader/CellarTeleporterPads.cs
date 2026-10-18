using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// A pad on each of the CELLAR Arena's teleports to the battlefields (Cellar_teleporter_pads).
    ///
    /// The arena's links out (map_link, kind 0, 4 m) are circles on the floor of the hub, round the
    /// Alia Das one: Concordia Divide, Valverde Plateau, Torden Plains, Burning Steps and the
    /// Abyss. adv_afs_arena.map places nothing on them - the pads were the server's - so they work
    /// and cannot be seen. Each gets the human teleporter the Alia Das link has (991,
    /// AliaDasTouchups): UsableTwoStateHumTeleporter, a teleporter row of type 1 with no
    /// local_teleporter_link, which the server puts in the world running and which takes nobody
    /// anywhere itself; the link under it does. On the link's own point, at the floor's height
    /// (36.5), as 991 is.
    ///
    /// Ids 992 to 996: under 1000, as every placed teleporter's is (the travel window lists
    /// waypoints and wormholes together by id, and a wormhole's is its entity id, from 1000).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike; a pad goes in only where its id is free, and
    /// Down takes out only the pads as Up put them.
    /// </summary>
    public static class CellarTeleporterPads
    {
        public const uint ArenaMapContextId = 20000009;
        public const uint HumanTeleporter = 7895, LocalTeleporterType = 1;

        public sealed record Pad(uint Id, uint LinkId, double X, double Y, double Z, string Description);

        public static readonly Pad[] Pads =
        {
            new(992, 135, 27.5014, 36.5, -4.7462, "CELLAR Arena link: to Concordia Divide"),
            new(993, 137, -18.8436, 36.5, -4.7228, "CELLAR Arena link: to Valverde Plateau"),
            new(994, 138, 12.1857, 36.5, -28.7285, "CELLAR Arena link: to Torden Plains"),
            new(995, 139, -18.5192, 36.5, -28.9129, "CELLAR Arena link: to Burning Steps"),
            new(996, 140, 27.5964, 36.5, -28.6574, "CELLAR Arena link: to the Abyss"),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var p in Pads)
                yield return "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
                    $"SELECT {p.Id}, {HumanTeleporter}, {LocalTeleporterType}, {S(p.Description)}, {N(p.X)}, {N(p.Y)}, {N(p.Z)}, 0, {ArenaMapContextId} " +
                    $"FROM entityclass e WHERE e.id = {HumanTeleporter} AND NOT EXISTS (SELECT 1 FROM teleporter x WHERE x.id = {p.Id});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var p in Pads)
                yield return $"DELETE FROM teleporter WHERE id = {p.Id} AND class_id = {HumanTeleporter} AND type = {LocalTeleporterType} " +
                    $"AND map_context_id = {ArenaMapContextId} AND ABS(pos_x - {N(p.X)}) < 0.01 AND ABS(pos_z - {N(p.Z)}) < 0.01;";
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
