using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// /killmap. The development server's killed a map instance outright; here each map is one
    /// persistent instance, so killing it resets its creatures instead: every creature its
    /// automatic spawn pools brought, alive or dead - corpse and loot with it - and everything
    /// those creatures brought in their turn (summons, the Amoeboid's Spawn: whatever has one of
    /// them as its master), is taken off the map, and the pools start over, spawning afresh on the
    /// pool worker's next pass. Players stay where they are, and what is theirs (minions, traps,
    /// the risen) stays with them. Creatures of control point and scripted pools are left alone,
    /// for nothing would bring them back until their own trigger does; so is anything a GM put
    /// down with .creature, which has no pool.
    ///
    /// Asked for from a client's packet thread, done on the map's own tick (<see cref="Worker"/>)
    /// between the creatures' thinking, as creatures are always taken off.
    /// </summary>
    public static class MapReset
    {
        private static readonly Dictionary<MapChannel, Client> Pending = new Dictionary<MapChannel, Client>();
        private static readonly object PendingLock = new object();

        /// <summary>Reset the map on its next tick, and tell the GM when it is done.</summary>
        public static void Request(MapChannel mapChannel, Client requestedBy)
        {
            if (mapChannel == null)
                return;

            lock (PendingLock)
                Pending[mapChannel] = requestedBy;
        }

        public static bool IsPending(MapChannel mapChannel)
        {
            lock (PendingLock)
                return Pending.ContainsKey(mapChannel);
        }

        /// <summary>Resets the map if one was asked for. Run on every map's tick, players or none.</summary>
        public static void Worker(MapChannel mapChannel)
        {
            Client requestedBy;

            lock (PendingLock)
            {
                if (!Pending.TryGetValue(mapChannel, out requestedBy))
                    return;

                Pending.Remove(mapChannel);
            }

            var (removed, pools) = Reset(mapChannel);

            Logger.WriteLog(LogType.Command, $"MapReset: map {mapChannel.MapInfo.MapContextId} {mapChannel.MapInfo.MapName}: {removed} creature(s) removed, {pools} spawn pool(s) restarted");

            if (requestedBy?.Player != null)
                CommunicatorManager.Instance.SystemMessage(requestedBy,
                    $"{mapChannel.MapInfo.MapName} ({mapChannel.MapInfo.MapContextId}) reset: {removed} creature(s) removed, {pools} spawn pool(s) spawning afresh.");
        }

        /// <summary>
        /// Which of the creatures go: those of an automatic pool on this map, then, until there are
        /// no more, those whose master is one that goes.
        /// </summary>
        public static List<Creature> CreaturesToRemove(uint mapContextId, IEnumerable<Creature> creatures)
        {
            var all = creatures.Where(c => c != null).Distinct().ToList();
            var going = all.Where(c => c.SpawnPool != null && c.SpawnPool.MapContextId == mapContextId
                                       && c.SpawnPool.Mode == SpawnPoolManager.ModeAutomatic).ToList();
            var ids = new HashSet<ulong>(going.Select(c => c.EntityId));
            var added = true;

            while (added)
            {
                added = false;

                foreach (var creature in all)
                {
                    if (creature.MasterEntityId == 0 || ids.Contains(creature.EntityId) || !ids.Contains(creature.MasterEntityId))
                        continue;

                    going.Add(creature);
                    ids.Add(creature.EntityId);
                    added = true;
                }
            }

            return going;
        }

        /// <summary>The creatures taken off and the pools set to spawn again.</summary>
        private static (int Removed, int Pools) Reset(MapChannel mapChannel)
        {
            var mapContextId = mapChannel.MapInfo.MapContextId;
            var creatures = mapChannel.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).ToList();
            var going = CreaturesToRemove(mapContextId, creatures);

            foreach (var creature in going)
            {
                // Dead to everything still holding it: a windup, a bomb or a summons waiting on it
                // lets a dead creature be.
                creature.State = CharacterState.Dead;
                creature.Hate.Clear();
                GameEffectManager.Instance.ClearEffects(mapChannel, creature);

                if (creature.LootDispenserObjectEntityId != 0)
                {
                    if (EntityManager.Instance.TryGetObject(creature.LootDispenserObjectEntityId, out var lootObject))
                        DynamicObjectManager.Instance.DynamicObjectDestroy(mapChannel, lootObject);

                    creature.LootDispenserObjectEntityId = 0;
                }

                CellManager.Instance.RemoveCreatureFromWorld(mapChannel, creature);
            }

            var pools = 0;

            // The map's own pools (each map channel runs clones of the loaded templates); the
            // templates only for a map that has none.
            var mapPools = mapChannel.SpawnPools.Count > 0
                ? mapChannel.SpawnPools
                : SpawnPoolManager.Instance.LoadedSpawnPools.Values.ToList();

            foreach (var pool in mapPools)
            {
                if (pool.MapContextId != mapContextId || pool.Mode != SpawnPoolManager.ModeAutomatic)
                    continue;

                // Every creature of the pool is gone, so its counts start again from nothing - and
                // any drift in them goes too. What a dropship or a teleporter is still bringing in
                // is left counted; the pool waits for that as it always does.
                pool.AliveCreatures = 0;
                pool.DeadCreatures = 0;

                if (pool.QueuedCreatures > 0 || pool.DropshipQueue > 0)
                    continue;

                // Its respawn time already served: it spawns on the worker's next pass.
                pool.UpdateTimer = pool.RespawnTime;
                pools++;
            }

            return (going.Count, pools);
        }
    }
}
