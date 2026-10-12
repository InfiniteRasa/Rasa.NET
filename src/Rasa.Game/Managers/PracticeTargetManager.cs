using System;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The Practice Dummy (entity class 29365, UsableStatelessHumPracticeDummyV01): what a
    /// player's weapon or ability may hit, and what a hit does.
    ///
    /// A hit knocks it back and it swings up again. The client has one animation for the
    /// dummy's mesh, arch_hum_practice_target_hit_v01.anm - 0.9 s: back 85 degrees in a tenth
    /// of a second, up again by 0.8 s with a small bounce - and plays it for one thing only:
    /// the dummy is an InertDestroyable, and usabledata.animation gives class 29365 in
    /// USE_STATE_DESTROYED (2) the animation "Usable Destroyed", overridden from "move to last
    /// frame" to a one-shot. So the dummy is "destroyed" by a hit and intact again when the
    /// animation is over: ForceState 2, and <see cref="SwingMs"/> later ForceState
    /// USE_IDES_STATE_INTACT (110), which stops the animation and stands it at rest.
    ///
    /// While it is in the destroyed state the client does not let it be targeted
    /// (InertDestroyable._UpdateTargetableSetting), so the dummy cannot be locked on to until
    /// it is up. The server asks nothing of the state: a hit that arrives during the swing
    /// counts as any other, and does not start the swing over.
    ///
    /// The object's own StateId is left as its scene made it. The swing is the clients'
    /// picture, kept by the map (MapChannel.PracticeSwings) for as long as it lasts; a client
    /// that comes into range partway is told the state that ends it and no more.
    /// </summary>
    internal static class PracticeTargetManager
    {
        internal const uint EntityClassId = 29365;
        internal const uint HitPoints = 100;

        /// <summary>The length of arch_hum_practice_target_hit_v01.anm: its last key is at 0.9 s.</summary>
        internal const int SwingMs = 900;

        /// <summary>Environment.TickCount64; a test's to replace.</summary>
        internal static Func<long> Now { get; set; } = () => Environment.TickCount64;

        internal static bool TryGetTarget(MapChannel map, ulong entityId, out DynamicObject target)
        {
            // A destructible prop of the .map (WorldDestructibles): its stand-in, while it stands.
            if (WorldDestructibles.TryGetTarget(map, entityId, out target))
                return true;

            // One the server put down (PlacedDestructibles), while it stands.
            if (PlacedDestructibles.TryGetTarget(map, entityId, out target))
                return true;

            target = null;
            if (map == null || !EntityManager.Instance.TryGetObject(entityId, out var candidate) ||
                candidate.DynamicObjectType != DynamicObjectType.PracticeDummy ||
                !candidate.IsInWorld || !candidate.IsEnabled ||
                candidate.MissionDestruction != null && candidate.CurrentHitPoints == 0 ||
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
                (target?.MissionDestruction == null ||
                    target.SceneOwnerCharacterId == player.Id && target.CurrentHitPoints > 0) &&
                TryGetTarget(map, target?.EntityId ?? 0, out var current) &&
                ReferenceEquals(current, target);
        }

        internal static void RecordHit(
            MapChannel map, Actor source, DynamicObject target, ActionId actionId,
            MissionApplication missions = null, int damage = 1)
        {
            if (!CanHit(map, source, target))
                return;

            var client = map.ClientList.FirstOrDefault(candidate => ReferenceEquals(candidate.Player, source));
            if (client?.State != ClientState.Ingame || client.PendingTransfer != null)
                return;

            // A destructible prop of the .map: its own hit points, damaged states and wreck.
            if (WorldDestructibles.IsOne(target))
            {
                WorldDestructibles.Hit(map, client, target, damage);
                return;
            }

            // One the server put down: the same, and a spawner's creatures.
            if (PlacedDestructibles.IsOne(target))
            {
                PlacedDestructibles.Hit(map, client.Player, target, damage);
                return;
            }

            if (target.MissionDestruction != null)
            {
                (missions ?? MissionApplication.Instance).Scenes.DamageObject(client, target, damage);
                return;
            }

            // Knocked back, for everyone who sees it - before the mission hears of the hit, so
            // that a mission that will not take it does not leave the dummy standing still.
            Swing(map, target);

            (missions ?? MissionApplication.Instance).RecordProgress(client,
                MissionProgressEvent.ObjectHit((uint)target.EntityClassId, (uint)actionId));
        }

        /// <summary>
        /// The dummy is knocked back by a hit: its hit animation, on every client in range. One
        /// already swinging swings on. Nothing for an object that is no Practice Dummy - the
        /// state means something else to another class - or that a mission has destroyed.
        /// </summary>
        internal static void Swing(MapChannel map, DynamicObject target)
        {
            if (map == null || target == null || (uint)target.EntityClassId != EntityClassId || target.MissionDestruction != null)
                return;

            lock (map.PracticeSwings)
            {
                if (map.PracticeSwings.ContainsKey(target.EntityId))
                    return;

                map.PracticeSwings[target.EntityId] = Now() + SwingMs;
            }

            CellManager.Instance.CellCallMethod(map, target, new ForceStatePacket(UseObjectState.StateDestroyed, (int)target.WindupTime));
        }

        /// <summary>Every tick: the dummies whose swing is over are intact again, to everyone in range.</summary>
        internal static void Worker(MapChannel map)
        {
            if (map == null || map.PracticeSwings.Count == 0)
                return;

            var now = Now();
            System.Collections.Generic.List<ulong> over;

            lock (map.PracticeSwings)
            {
                over = map.PracticeSwings.Where(swing => swing.Value <= now).Select(swing => swing.Key).ToList();

                foreach (var entityId in over)
                    map.PracticeSwings.Remove(entityId);
            }

            foreach (var entityId in over)
            {
                // Taken out of the world meanwhile: there is nothing to stand up.
                if (!EntityManager.Instance.TryGetObject(entityId, out var target) || !target.IsInWorld ||
                    !ReferenceEquals(target.RuntimeMapChannel, map) || !IsFinite(target.Position))
                    continue;

                CellManager.Instance.CellCallMethod(map, target, new ForceStatePacket(UseObjectState.IdesStateIntact, (int)target.WindupTime));
            }
        }

        private static bool IsFinite(Vector3 position) =>
            float.IsFinite(position.X) && float.IsFinite(position.Y) && float.IsFinite(position.Z);
    }
}
