using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The world's own destructible props: the .map's Brann dissection tables, Bane stasis
    /// chambers, articulated drills and tesla coils, which players can shoot down and which come
    /// back after <see cref="RespawnMinMs"/> to <see cref="RespawnMaxMs"/>.
    ///
    /// The client has their destruction and nothing else: each class's destroyed state, with its
    /// explosion on the way into it and its wreck in it (DestroyableStates), and no hit points
    /// (usabledata.lookup), so it takes them for undamageable. DamageInfo gives them some
    /// (Usable.Recv_DamageInfo), and with them a target: a usable can be locked on to when it can
    /// be damaged and is destroyable and not destroyed (IsDirectTargetable) - an InertDestroyable
    /// and a TeslaCoil are destroyable - and its class is targetable. Those four classes are
    /// (entityclass target flag 1). The Bane outpost and Brann generators, the foundries and the
    /// sonic towers have destroyed states too, but their classes are not targetable, and are left
    /// out.
    ///
    /// The server holds no object for a .map's entity, and its weapons hit DynamicObjects
    /// (PracticeTargetManager). Each of these has one on each map channel, made when it is first
    /// asked for: not in the map's objects or its cells or the entity registry, so it is never
    /// sent to a client - the client has the entity already, under the .map's id, which the
    /// object carries. PracticeTargetManager.TryGetTarget finds it, MissileManager takes it for
    /// an object (its id is in no registry), and a hit on it comes here (RecordHit) - only a
    /// player's, as the shooter has to be a player there (CanHit).
    ///
    /// Hit points (ours): the tables and chambers <see cref="SmallHitPoints"/>, the drills and
    /// coils <see cref="LargeHitPoints"/>. A hit leaves it standing until they run out; the
    /// tables, chambers and drills step down their damaged states on the way
    /// (DestroyableStates). Destroyed, it plays its explosion, stands as a wreck, cannot be
    /// targeted, and some 45 to 60 seconds later is back: Use from destroyed to its working
    /// state, which the classes have (DESTROYED to INTACT, DESTROYED to POWER_DOWN), at full hit
    /// points. A map channel's are its own; a copy of the map has its own.
    ///
    /// Their state changes go to everyone on the map, near or not, as everyone has the entity
    /// (MapUsables); their hit points, which move the bar over them, to those in range. Whoever
    /// arrives is sent the state (MapUsables.SetState) and the hit points (PlayerEnteredMap).
    /// </summary>
    public static class WorldDestructibles
    {
        public const uint SmallHitPoints = 300;
        public const uint LargeHitPoints = 600;

        public const long RespawnMinMs = 45_000;
        public const long RespawnMaxMs = 60_000;

        public sealed class Destructible
        {
            public Destructible(MapUsables.Usable usable, uint hitPoints)
            {
                Usable = usable;
                HitPoints = hitPoints;
            }

            public MapUsables.Usable Usable { get; }
            public uint HitPoints { get; }
            public ulong EntityId => Usable.EntityId;

            /// <summary>The state it works in, and comes back to.</summary>
            public UseObjectState UpState => Usable.State;

            public override string ToString() => $"{Usable} ({HitPoints} hit points)";
        }

        private static readonly Dictionary<uint, uint> HitPointsByClass = new()
        {
            [MapUsables.BrannDissectionTable] = SmallHitPoints,
            [MapUsables.BaneStasisChamber] = SmallHitPoints,
            [MapUsables.BaneArticulatedDrill] = LargeHitPoints,
            [MapUsables.BaneTeslaCoil] = LargeHitPoints
        };

        public static readonly IReadOnlyList<Destructible> All = MapUsables.All
            .Where(usable => HitPointsByClass.ContainsKey(usable.ClassId))
            .Select(usable => new Destructible(usable, HitPointsByClass[usable.ClassId]))
            .ToList();

        private static readonly Dictionary<ulong, Destructible> ById = All.ToDictionary(destructible => destructible.EntityId);

        /// <summary>The clock; a test's to replace.</summary>
        internal static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>The wait before one comes back, from the least to the most inclusive; a test's to replace.</summary>
        internal static Func<long, long, long> Roll { get; set; } = (min, max) => Random.Shared.NextInt64(min, max + 1);

        internal static void Reset()
        {
            Now = () => Environment.TickCount64;
            Roll = (min, max) => Random.Shared.NextInt64(min, max + 1);
        }

        /// <summary>One on one map channel: its stand-in object and when it comes back, if it is down.</summary>
        private sealed class Live
        {
            public Live(Destructible destructible, DynamicObject proxy)
            {
                Destructible = destructible;
                Proxy = proxy;
            }

            public Destructible Destructible { get; }
            public DynamicObject Proxy { get; }
            public long RespawnAt { get; set; }
            public bool IsDown => Proxy.CurrentHitPoints == 0;
        }

        private static readonly ConditionalWeakTable<MapChannel, Dictionary<ulong, Live>> Channels = new();

        /// <summary>The destructible with this .map id, on any map, or null.</summary>
        public static Destructible Find(ulong entityId) => ById.TryGetValue(entityId, out var destructible) ? destructible : null;

        /// <summary>The map's destructibles.</summary>
        public static IEnumerable<Destructible> OnMap(uint mapContextId) => All.Where(d => d.Usable.MapContextId == mapContextId);

        private static Live LiveOn(MapChannel map, ulong entityId)
        {
            var destructible = Find(entityId);

            if (map?.MapInfo == null || destructible == null || destructible.Usable.MapContextId != map.MapInfo.MapContextId)
                return null;

            var lives = Channels.GetOrCreateValue(map);

            lock (lives)
            {
                if (lives.TryGetValue(entityId, out var live))
                    return live;

                // The constructor takes an id from the pool; the object carries the .map's, so it goes straight back.
                var proxy = new DynamicObject();
                EntityManager.Instance.FreeEntity(proxy.EntityId);
                proxy.EntityId = entityId;
                proxy.EntityClassId = (EntityClasses)destructible.Usable.ClassId;
                proxy.DynamicObjectType = DynamicObjectType.Scenery;
                proxy.Position = destructible.Usable.Position;
                proxy.MapContextId = destructible.Usable.MapContextId;
                proxy.RuntimeMapChannel = map;
                proxy.TargetCategory = TargetCategory.Object;
                proxy.StateId = MapUsables.StateOf(map, destructible.Usable);
                proxy.CurrentHitPoints = proxy.StateId == UseObjectState.StateDestroyed ? 0 : destructible.HitPoints;
                proxy.IsEnabled = true;
                proxy.IsInWorld = true;
                proxy.Comment = destructible.Usable.Name;

                live = new Live(destructible, proxy);
                proxy.ObjectData = live;
                lives[entityId] = live;
                return live;
            }
        }

        /// <summary>The stand-in object of a destructible on this map channel that can be hit now: there, and standing.</summary>
        public static bool TryGetTarget(MapChannel map, ulong entityId, out DynamicObject target)
        {
            var live = LiveOn(map, entityId);
            target = live != null && !live.IsDown ? live.Proxy : null;
            return target != null;
        }

        /// <summary>Whether an object is one of these stand-ins.</summary>
        public static bool IsOne(DynamicObject obj) => obj?.ObjectData is Live;

        /// <summary>Whether a destructible is standing on a map channel (a tesla coil zaps only then).</summary>
        public static bool IsUp(MapChannel map, ulong entityId)
        {
            var live = LiveOn(map, entityId);
            return live != null && !live.IsDown;
        }

        /// <summary>
        /// A player's hit: the hit points down, the bar to those in range, the damaged states on the
        /// way, and its destruction when they run out. Returns the hit points left.
        /// </summary>
        public static uint Hit(MapChannel map, Client client, DynamicObject target, int damage)
        {
            if (map == null || client?.Player == null || damage <= 0 || target?.ObjectData is not Live live ||
                !ReferenceEquals(target.RuntimeMapChannel, map) || live.IsDown)
                return target?.CurrentHitPoints ?? 0;

            var destructible = live.Destructible;
            var proxy = live.Proxy;
            var shown = proxy.StateId;

            proxy.CurrentHitPoints = (uint)damage >= proxy.CurrentHitPoints ? 0 : proxy.CurrentHitPoints - (uint)damage;
            CellManager.Instance.CellCallMethod(map, proxy.Position, proxy.EntityId, new UpdateHitPointsPacket((int)proxy.CurrentHitPoints));

            if (proxy.CurrentHitPoints > 0)
            {
                // Down to the damaged state its hit points are at, where its class has them.
                if (DestroyableStates.HasHealthStates(proxy))
                    foreach (var step in DestroyableStates.Down(shown, DestroyableStates.HealthStateFor(proxy.CurrentHitPoints, destructible.HitPoints)))
                        Show(map, live, step, client.Player.EntityId);

                return proxy.CurrentHitPoints;
            }

            // Destroyed: no longer damageable (and so no longer a target), and through to its wreck.
            ToEveryone(map, proxy.EntityId, new DamageInfoPacket(false, false, destructible.HitPoints, 0));

            IEnumerable<UseObjectState> steps = DestroyableStates.HasHealthStates(proxy)
                ? DestroyableStates.Down(shown, UseObjectState.IdesState25pHealth).Append(UseObjectState.StateDestroyed)
                : new[] { UseObjectState.StateDestroyed };

            foreach (var step in steps)
                Show(map, live, step, client.Player.EntityId);

            live.RespawnAt = Now() + Math.Clamp(Roll(RespawnMinMs, RespawnMaxMs), RespawnMinMs, RespawnMaxMs);
            return 0;
        }

        /// <summary>Brings back whatever is due on the map channel: its working state, at full hit points.</summary>
        public static void Worker(MapChannel map)
        {
            if (map == null || !Channels.TryGetValue(map, out var lives))
                return;

            var now = Now();
            List<Live> due;

            lock (lives)
                due = lives.Values.Where(live => live.IsDown && live.RespawnAt <= now).ToList();

            foreach (var live in due)
                Respawn(map, live);
        }

        private static void Respawn(MapChannel map, Live live)
        {
            var destructible = live.Destructible;
            var proxy = live.Proxy;

            proxy.CurrentHitPoints = destructible.HitPoints;
            live.RespawnAt = 0;

            Show(map, live, destructible.UpState, 0);
            ToEveryone(map, proxy.EntityId, new DamageInfoPacket(true, false, destructible.HitPoints, destructible.HitPoints));
            CellManager.Instance.CellCallMethod(map, proxy.Position, proxy.EntityId, new UpdateHitPointsPacket((int)destructible.HitPoints));
        }

        /// <summary>
        /// Whoever arrives on the map is told each one's hit points, and that it can be damaged if
        /// it is standing - which is what makes it a target. Its state goes with MapUsables.
        /// Returns how many were sent.
        /// </summary>
        public static int PlayerEnteredMap(Client client)
        {
            var map = client?.Player?.MapChannel;

            if (map?.MapInfo == null)
                return 0;

            var sent = 0;

            foreach (var destructible in OnMap(map.MapInfo.MapContextId))
            {
                var live = LiveOn(map, destructible.EntityId);

                client.CallMethod(destructible.EntityId, new TargetCategoryPacket(TargetCategory.Object));
                client.CallMethod(destructible.EntityId, new DamageInfoPacket(!live.IsDown, false, destructible.HitPoints, live.Proxy.CurrentHitPoints));
                sent++;
            }

            return sent;
        }

        /// <summary>The hit points a destructible has on a map channel.</summary>
        public static uint HitPointsOf(MapChannel map, ulong entityId) => LiveOn(map, entityId)?.Proxy.CurrentHitPoints ?? 0;

        /// <summary>When a destructible that is down on a map channel comes back; 0 if it is standing.</summary>
        public static long RespawnAtOf(MapChannel map, ulong entityId) => LiveOn(map, entityId)?.RespawnAt ?? 0;

        /// <summary>Forgets a map channel's (tests, and a channel being torn down).</summary>
        public static void Forget(MapChannel map)
        {
            if (map != null)
                Channels.Remove(map);
        }

        private static void Show(MapChannel map, Live live, UseObjectState state, ulong actorId)
        {
            live.Proxy.StateId = state;
            MapUsables.SetState(map, live.Proxy.EntityId, state);
            ToEveryone(map, live.Proxy.EntityId, new UsePacket(actorId, state, 0));
        }

        /// <summary>To every client on the map channel that has its .map loaded.</summary>
        private static void ToEveryone(MapChannel map, ulong entityId, PythonPacket packet)
        {
            foreach (var client in map.ClientList.ToList())
                if (client?.Player != null && !client.AwaitingMapLoaded)
                    client.CallMethod(entityId, packet);
        }
    }
}
