using System;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    internal static class PracticeTargetManager
    {
        internal const uint EntityClassId = 29365;
        internal const uint HitPoints = 100;
        internal static bool TryGetTarget(MapChannel map, ulong entityId, out DynamicObject target)
        {
            target = null;
            if (map == null || !EntityManager.Instance.TryGetObject(entityId, out var candidate) ||
                candidate.DynamicObjectType != DynamicObjectType.PracticeDummy ||
                !candidate.IsInWorld || !candidate.IsEnabled ||
                !ReferenceEquals(candidate.RuntimeMapChannel, map) ||
                !map.DynamicObjects.Contains(candidate) ||
                !IsFinite(candidate.Position))
                return false;
            target = candidate;
            return true;
        }

        internal static bool CanHit(MapChannel map, Actor source, DynamicObject target)
        {
            return source is Manifestation player &&
                ReferenceEquals(player.MapChannel, map) &&
                player.State != CharacterState.Dead &&
                player.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0 &&
                IsFinite(player.Position) &&
                EntityManager.Instance.Players.TryGetValue(player.EntityId, out var registered) &&
                ReferenceEquals(player, registered) &&
                (!map.IsPrivateInstance || map.OwnerCharacterId == player.Id) &&
                TryGetTarget(map, target?.EntityId ?? 0, out var current) &&
                ReferenceEquals(current, target);
        }

        internal static void RecordHit(
            MapChannel map, Actor source, DynamicObject target, ActionId actionId,
            MissionApplication missions = null)
        {
            if (!CanHit(map, source, target))
                return;

            var client = map.ClientList.FirstOrDefault(candidate => ReferenceEquals(candidate.Player, source));
            if (client?.State != ClientState.Ingame || client.PendingTransfer != null)
                return;

            (missions ?? MissionApplication.Instance).RecordProgress(client,
                MissionProgressEvent.ObjectHit((uint)target.EntityClassId, (uint)actionId));
        }

        private static bool IsFinite(Vector3 position) =>
            float.IsFinite(position.X) && float.IsFinite(position.Y) && float.IsFinite(position.Z);
    }
}
