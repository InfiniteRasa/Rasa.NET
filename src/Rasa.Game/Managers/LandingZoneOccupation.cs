using System.Collections.Generic;
using System.Linq;
using Rasa.Data;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.MapChannel.Client;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Managers
{
    /// <summary>The four ordinary Bane garrison pools belonging to Landing Zone.</summary>
    internal static class LandingZoneOccupation
    {
        internal static bool IsGroup(SpawnPool pool) => pool != null && pool.MapContextId == 1220 && LandingZoneOccupationContent.IsPool(pool.DbId);
        internal static bool TryDispatch(MapChannel map, SpawnPool pool, List<Creature> creatures)
        {
            if (!IsGroup(pool)) return false;
            if (map.IsCopy || pool.Suspended || !pool.IsGarrison) return true;
            pool.Delivered = arrived => Patrol(map, pool, arrived);
            pool.HasSpawned = true;
            SpawnPoolManager.Instance.EnqueueDropship(map, pool, creatures, new SpawnPoolArrivalEntry {
                Kind = SpawnPoolArrivalEntry.KindDropship, PosX = pool.Position.X, PosY = pool.Position.Y,
                PosZ = pool.Position.Z, Rotation = pool.Rotation });
            return true;
        }
        private static void Patrol(MapChannel map, SpawnPool pool, IReadOnlyList<Creature> creatures)
        {
            var index = (int)(pool.DbId - LandingZoneOccupationContent.Left);
            for (var i = 0; i < creatures.Count; i++)
            {
                var creature = creatures[i];
                var beat = pool.DbId == LandingZoneOccupationContent.Ramparts
                    ? LandingZoneGarrisonContent.Catwalks[i % 3] : LandingZoneOccupationContent.Beats[index];
                creature.Patrol = LandingZoneGarrison.Route(map, beat);
                creature.AggroRange = 35;
                creature.FightFromPatrol = true;
                // Native Bane arrival FX on the troops as they beam down from the ship.
                CellManager.Instance.CellCallMethod(map, creature, new PreTeleportPacket(TeleportType.BaneTeleporter));
                CellManager.Instance.CellCallMethod(map, creature, new TeleportPacket(creature.Position, creature.Rotation, TeleportType.BaneTeleporter, 0));
                BehaviorManager.Instance.WalkIn(creature, creature.Patrol[0].Position);
            }
        }
        internal static void CancelInbound(MapChannel map, IReadOnlyCollection<SpawnPool> pools)
        {
            foreach (var ship in DynamicObjectManager.Instance.Dropships.Values.Where(s =>
                ReferenceEquals(s.RuntimeMapChannel, map) && IsGroup(s.SpawnPool) && pools.Contains(s.SpawnPool)).ToArray())
            {
                CellManager.Instance.RemoveFromWorld(map, ship);
                DynamicObjectManager.Instance.Dropships.Remove(ship.EntityId);
                var pool = ship.SpawnPool;
                SpawnPoolManager.Instance.DecreaseQueuedCreatureCount(pool, pool.QueuedCreatures);
                SpawnPoolManager.Instance.DecreaseQueueCount(pool);
            }
        }
    }
}
