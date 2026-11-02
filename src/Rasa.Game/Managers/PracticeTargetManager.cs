using System;
using System.Collections.Generic;
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

        /// <summary>
        /// The objects a player's weapon can damage within a cone from them (ConeWeapons): those
        /// <see cref="TryGetTarget"/> finds and <see cref="CanHit"/> lets them hit - a mission's
        /// destructibles (their own), the server's placed ones, and the .map's (WorldDestructibles,
        /// which are in no cell) - within range and halfAngleDegrees of the aim on the flat, as a
        /// creature is (AbilityManager.InCone). A shotgun cannot lock on to anything, so these
        /// are the only way it hits an object at all.
        /// </summary>
        internal static List<DynamicObject> TargetsInCone(MapChannel map, Manifestation player, Vector3 aim, float range, float halfAngleDegrees)
        {
            var found = new List<DynamicObject>();

            if (map == null || player?.Cells == null || !(range > 0))
                return found;

            bool Near(Vector3 position) => new Vector2(position.X - player.Position.X, position.Z - player.Position.Z).Length() <= range;

            var candidates = CellManager.CellsIn(map, player.Cells).SelectMany(cell => cell.DynamicObjectList)
                .Where(obj => obj != null && Near(obj.Position)).Select(obj => obj.EntityId)
                .Concat(WorldDestructibles.OnMap(map.MapInfo?.MapContextId ?? 0).Where(d => Near(d.Usable.Position)).Select(d => d.EntityId))
                .Distinct()
                .ToList();

            foreach (var entityId in candidates)
                if (TryGetTarget(map, entityId, out var target) && !found.Contains(target) && CanHit(map, player, target) &&
                    AbilityManager.InCone(player.Position, aim, target.Position, range, halfAngleDegrees))
                    found.Add(target);

            return found;
        }

        /// <summary>
        /// Whether the object is a plain practice target - the Bootcamp's Practice Dummy, or any
        /// other PracticeDummy-type object no mission has the player destroy - rather than one a
        /// weapon destroys: a mission's destructible, one the server put down, or the .map's. A
        /// plain one counts what was aimed at it (MissionProgressEvent.ObjectHit), so no blast or
        /// area reaches it (TargetsWithin) and only the abilities that were always aimed at it may be.
        /// </summary>
        internal static bool IsDummy(DynamicObject target) =>
            target != null && target.MissionDestruction == null &&
            !WorldDestructibles.IsOne(target) && !PlacedDestructibles.IsOne(target);

        /// <summary>
        /// The objects a player's blast or area can damage within radius metres of a point: those
        /// <see cref="TryGetTarget"/> finds and <see cref="CanHit"/> lets them hit, found as
        /// <see cref="TargetsInCone"/> finds them - in the cells around the player, and the .map's -
        /// and within radius of it as a creature is (AbilityManager.HostilesWithin). No plain
        /// practice target (<see cref="IsDummy"/>).
        /// </summary>
        internal static List<DynamicObject> TargetsWithin(MapChannel map, Manifestation player, Vector3 centre, float radius)
        {
            var found = new List<DynamicObject>();

            if (map == null || player?.Cells == null || !(radius > 0) || !IsFinite(centre))
                return found;

            bool Near(Vector3 position) => Vector3.Distance(centre, position) <= radius;

            var candidates = CellManager.CellsIn(map, player.Cells).SelectMany(cell => cell.DynamicObjectList)
                .Where(obj => obj != null && Near(obj.Position)).Select(obj => obj.EntityId)
                .Concat(WorldDestructibles.OnMap(map.MapInfo?.MapContextId ?? 0).Where(d => Near(d.Usable.Position)).Select(d => d.EntityId))
                .Distinct()
                .ToList();

            foreach (var entityId in candidates)
                if (TryGetTarget(map, entityId, out var target) && !found.Contains(target) && !IsDummy(target) &&
                    CanHit(map, player, target) && Near(target.Position))
                    found.Add(target);

            return found;
        }

        /// <summary>
        /// A blast or an area of a player's at a point: each object within radius of it they can
        /// damage (<see cref="TargetsWithin"/>) but <paramref name="except"/> - the one it was aimed
        /// at, which takes its own hit - takes <paramref name="damage"/>'s roll as a weapon's hit
        /// (<see cref="RecordHit"/>), unless a standing force field is between it and the blast
        /// (ForceFields.Blocks), as it would be between the blast and a creature. A rocket's splash,
        /// a grenade, an explosion, a burning pool all destroyed the creatures around a fuel
        /// container and left the container standing. What was hit, and for how much.
        /// </summary>
        internal static List<(DynamicObject Target, int Damage)> Blast(
            MapChannel map, Actor source, Vector3 centre, float radius, Func<int> damage,
            DynamicObject except = null, MissionApplication missions = null)
        {
            var hits = new List<(DynamicObject Target, int Damage)>();

            if (!(source is Manifestation player) || damage == null)
                return hits;

            var from = Cover.BlastFrom(centre);

            foreach (var target in TargetsWithin(map, player, centre, radius))
            {
                if (ReferenceEquals(target, except) ||
                    ForceFields.Blocks(map, player, from, target.Position + new Vector3(0f, Cover.BlastHeight, 0f)))
                    continue;

                var amount = damage();

                if (amount <= 0)
                    continue;

                // The action counts for a plain practice target alone, and a blast reaches none.
                RecordHit(map, player, target, default, missions, amount);
                hits.Add((target, amount));
            }

            return hits;
        }

        /// <summary>The entries a packet lists the hits on objects as: the amount and type, nothing an actor's.</summary>
        internal static IEnumerable<TickEntry> Entries(IEnumerable<(DynamicObject Target, int Damage)> hits, DamageType damageType) =>
            hits.Where(hit => hit.Damage > 0)
                .Select(hit => new TickEntry { EntityId = hit.Target.EntityId, Amount = hit.Damage, DamageType = damageType });

        /// <summary>ABILITY_DAMAGE_EFFECT: a plain BaseGameEffect on the client, nothing of its own on attach or detach.</summary>
        internal const int AnnouncerTypeId = 436;

        /// <summary>
        /// Floats the damage of hits on objects that the packet of what hit them cannot list: Vortex
        /// flails each target it lists (VortexAction.OnAbility, TransitionTo, an actor's), and a
        /// creature attack's rawInfo class is an actor's too. An ABILITY_DAMAGE_EFFECT is put on
        /// the source for the moment it takes, asked to announce the hits
        /// (BaseGameEffect.Recv_AnnounceDamage, Usable.AnnounceDamage, the number over each), and
        /// taken off again.
        /// </summary>
        internal static void Announce(MapChannel map, Actor source, IEnumerable<(DynamicObject Target, int Damage)> hits, DamageType damageType)
        {
            var entries = Entries(hits, damageType).ToList();

            if (map == null || source == null || entries.Count == 0)
                return;

            var effectId = GameEffectManager.Instance.NextEffectId(map);

            CellManager.Instance.CellCallMethod(map, source, new GameEffectAttachedPacket
            {
                EffectTypeId = AnnouncerTypeId,
                EffectId = effectId,
                EffectLevel = 1,
                SourceId = source.EntityId,
                Announced = false,
                Duration = null,
                DamageType = (int)damageType,
                AttrId = 1,
                IsActive = true,
                Extras = new Dictionary<string, object>(),
                Args = new List<object>()
            });

            var announce = new GameEffectAnnounceDamagePacket(effectId);
            announce.Hits.AddRange(entries);
            CellManager.Instance.CellCallMethod(map, source, announce);

            CellManager.Instance.CellCallMethod(map, source, new GameEffectDetachedPacket { EffectId = effectId });
        }

        /// <param name="damageType">What the hit was, where an object takes one kind harder than another (EnergyWeaponCenter.Struck); 0 where the caller has none.</param>
        internal static void RecordHit(
            MapChannel map, Actor source, DynamicObject target, ActionId actionId,
            MissionApplication missions = null, int damage = 1, DamageType damageType = 0)
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

            // One the server put down: the same, and a spawner's creatures. The Energy Weapon
            // Center's generator takes Lightning harder (EnergyWeaponCenter.Struck).
            if (PlacedDestructibles.IsOne(target))
            {
                PlacedDestructibles.Hit(map, client.Player, target, EnergyWeaponCenter.Struck(map, target, damage, damageType));
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
