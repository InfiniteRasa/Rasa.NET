using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Creatures of a map's own spawn pools that some players are not shown (MapChannel.HiddenPools), held by a
    /// mission scene's actor (HideSpawnPoolsIntent): the public Elder Hathen is not shown to the player whose escort
    /// copy of him stands in for him, while his squad mates see him and can talk to him.
    ///
    /// One check where a creature is given to a client (CreatureManager.CreateCreatureOnClient: the cells, an
    /// arrival, a mission contact from afar) and where what it does is told (CellManager.CellMoveObject and
    /// CellCallMethod), so a hidden creature is neither made nor moved on that client. Hiding takes it off the
    /// client at once; the last holder gone gives it back to a client in its cells.
    /// </summary>
    internal static class HiddenCreatures
    {
        public static bool IsHiddenFrom(Creature creature, Client viewer)
        {
            var map = creature?.RuntimeMapChannel;
            var pool = creature?.SpawnPool;
            return map != null && map.HiddenPools.Count != 0 && pool != null && pool.ScenarioKey == null &&
                   viewer?.Player != null && map.HiddenPools.ContainsKey((pool.DbId, viewer.Player.Id));
        }

        /// <summary>The pools' creatures not shown to the character, held by <paramref name="holder"/>.</summary>
        public static void Hide(MapChannel map, IEnumerable<uint> pools, uint characterId, string holder)
        {
            foreach (var pool in pools)
            {
                var key = (pool, characterId);
                if (!map.HiddenPools.TryGetValue(key, out var holders))
                    map.HiddenPools[key] = holders = new HashSet<string>();
                holders.Add(holder);
            }
            var viewers = map.ClientList.Where(client => client?.Player?.Id == characterId).ToArray();
            foreach (var creature in Standing(map, pools))
                foreach (var viewer in viewers)
                {
                    // Held from afar (MissionContacts) it is taken off too, and given from afar again once shown.
                    viewer.FarContacts.Remove(creature.EntityId);
                    viewer.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(creature.EntityId));
                }
        }

        /// <summary>Whatever <paramref name="holder"/> kept hidden: shown again where no other holder keeps it.</summary>
        public static void Release(MapChannel map, string holder)
        {
            var freed = new List<(uint Pool, uint Character)>();
            foreach (var entry in map.HiddenPools.ToArray())
                if (entry.Value.Remove(holder) && entry.Value.Count == 0)
                {
                    map.HiddenPools.Remove(entry.Key);
                    freed.Add(entry.Key);
                }
            foreach (var (pool, character) in freed)
                foreach (var creature in Standing(map, new[] { pool }))
                    foreach (var viewer in CellManager.Instance.GetClientsInCells(map, creature.Cells)
                                 .Where(client => client?.Player?.Id == character))
                        CreatureManager.Instance.CreateCreatureOnClient(viewer, creature);
        }

        /// <summary>Every holder whose key starts with <paramref name="prefix"/> (a run's actors): released.</summary>
        public static void ReleaseAll(MapChannel map, string prefix)
        {
            foreach (var holder in map.HiddenPools.Values.SelectMany(holders => holders)
                         .Where(holder => holder.StartsWith(prefix, System.StringComparison.Ordinal)).Distinct().ToArray())
                Release(map, holder);
        }

        private static IEnumerable<Creature> Standing(MapChannel map, IEnumerable<uint> pools)
        {
            var wanted = pools.ToHashSet();
            return map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct()
                .Where(creature => creature.SpawnPool != null && creature.SpawnPool.ScenarioKey == null &&
                                   wanted.Contains(creature.SpawnPool.DbId) && ReferenceEquals(creature.RuntimeMapChannel, map))
                .ToArray();
        }
    }
}
