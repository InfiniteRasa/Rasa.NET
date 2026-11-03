using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The control tower pads (ControlTowerTeleporters) moved from 1100-1156 to 800-856
    /// (Control_tower_teleporter_ids).
    ///
    /// The travel window keeps waypoints and wormholes in one list by id, and a wormhole's id is
    /// its entity id, which begins at 1000 (EntityManager): every teleporter on a map has to stay
    /// under it, so that no row of the world's is ever the id of a wormhole the window lists, nor
    /// a pick of one is taken for a pad (DynamicObjectManager.SelectWaypoint looks a pick up by id
    /// in the world's teleporters first). The control tower pads went in at 1100-1156, the only
    /// mapped rows past 999. 800-856 were free: nothing is placed between 624 and 899.
    ///
    /// The pads are type 1, which nobody gains, so no character has them; only their rows and
    /// their links (local_teleporter_link, both ends) move. Each id moves by -300.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A row moves only as Control_tower_teleporters
    /// put it - its class, type, map and spot - and a link only once the row it names has moved;
    /// Down moves them back the same way.
    /// </summary>
    public static class ControlTowerTeleporterIds
    {
        public const uint Offset = 300;
        public const uint FirstTeleporter = ControlTowerTeleporters.FirstTeleporter - Offset, LastTeleporter = ControlTowerTeleporters.LastTeleporter - Offset;

        /// <summary>The pads that move: (old id, new id, map, x, z).</summary>
        public static IEnumerable<(uint From, uint To, uint MapContextId, double X, double Z)> Moves() =>
            ControlTowerTeleporters.NewPads()
                .Where(p => p.Id >= ControlTowerTeleporters.FirstTeleporter && p.Id <= ControlTowerTeleporters.LastTeleporter)
                .Select(p => (p.Id, p.Id - Offset, p.MapContextId, p.X, p.Z));

        public static IEnumerable<string> Up() => Move(Moves().Select(m => (m.From, m.To, m.MapContextId, m.X, m.Z)));

        public static IEnumerable<string> Down() => Move(Moves().Select(m => (m.To, m.From, m.MapContextId, m.X, m.Z)));

        private static IEnumerable<string> Move(IEnumerable<(uint From, uint To, uint MapContextId, double X, double Z)> moves)
        {
            var list = moves.ToList();

            foreach (var (from, to, map, x, z) in list)
                yield return $"UPDATE teleporter SET id = {to} WHERE id = {from} AND class_id = {ControlTowerTeleporters.TeleporterClass} " +
                    $"AND type = {ControlTowerTeleporters.LocalTeleporterType} AND map_context_id = {map} " +
                    $"AND ABS(pos_x - {N(x)}) < 0.01 AND ABS(pos_z - {N(z)}) < 0.01;";

            foreach (var (from, to, _, _, _) in list)
            {
                var moved = $"EXISTS (SELECT 1 FROM teleporter t WHERE t.id = {to} AND t.class_id = {ControlTowerTeleporters.TeleporterClass} " +
                    $"AND t.type = {ControlTowerTeleporters.LocalTeleporterType}) AND NOT EXISTS (SELECT 1 FROM teleporter o WHERE o.id = {from})";
                yield return $"UPDATE {LocalTeleporterLinkEntry.TableName} SET from_teleporter_id = {to} WHERE from_teleporter_id = {from} AND {moved};";
                yield return $"UPDATE {LocalTeleporterLinkEntry.TableName} SET to_teleporter_id = {to} WHERE to_teleporter_id = {from} AND {moved};";
            }
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
