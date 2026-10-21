using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;
    using Structures.World;

    /// <summary>
    /// Bane arriving at the arrival points the maps build for them (spawnpool_arrival).
    ///
    /// A pool with arrival points restocks through one of them, picked at random each time:
    /// - a landing pad or dropship bay (kind 1): the Bane dropship
    ///   (UsableCrSpawnerBaneDropshipV01) comes down on it - DynamicObjectManager.DropshipsWorker
    ///   runs the same begin / spawn / end states as for any spawner dropship - and the creatures
    ///   step off onto the deck;
    /// - a teleporter (kind 2): the map's own UsableTwoStateBaneTeleporterV01, which the client
    ///   builds from the .map, is switched to USE_TS_STATE_1 (its arch_bane_teleporter_v01.pkg,
    ///   the teleporter working) by a ForceState to its entity id; the creatures come through it
    ///   TeleportLeadMs later, and it goes back to USE_TS_STATE_0 at TeleportHoldMs.
    /// Either way they come out within StepOutRadius of the point and walk to a spot on their
    /// pool's ground, which becomes their home (BehaviorManager.WalkIn).
    ///
    /// The first spawn after the server starts is a plain one: the world starts stocked, not
    /// with every camp's dropship coming down at once.
    /// </summary>
    public static class BaneArrivals
    {
        /// <summary>How long a teleporter is on before the Bane come through it. Ours; nothing in the client times it.</summary>
        public const int TeleportLeadMs = 1500;

        /// <summary>How long a teleporter stays on in all.</summary>
        public const int TeleportHoldMs = 5000;

        /// <summary>How far from the point they step out.</summary>
        public const float StepOutRadius = 3f;

        private sealed class Teleport
        {
            public MapChannel MapChannel;
            public SpawnPool Pool;
            public SpawnPoolArrivalEntry Arrival;
            public int Queued;
            public long ElapsedMs;
            public bool Spawned;

            /// <summary>What comes through, when it is not the pool's own list: an alarm's wave (BaneAlarms).</summary>
            public List<Creature> Creatures;

            /// <summary>Told the creatures once they are through.</summary>
            public Action<List<Creature>> Arrived;
        }

        private static readonly List<Teleport> Teleports = new List<Teleport>();
        private static readonly Random Random = new Random();

        /// <summary>One of the pool's arrival points, or null when it has none or has not spawned yet.</summary>
        public static SpawnPoolArrivalEntry Pick(SpawnPool pool)
        {
            if (pool.Arrivals.Count == 0 || !pool.HasSpawned)
                return null;

            lock (Random)
                return pool.Arrivals[Random.Next(pool.Arrivals.Count)];
        }

        /// <summary>The teleporter comes on; the creatures follow in TeleportLeadMs.</summary>
        public static void BeginTeleport(MapChannel mapChannel, SpawnPool pool, SpawnPoolArrivalEntry arrival, int creatureCount)
        {
            SpawnPoolManager.Instance.IncreaseQueueCount(pool);
            SpawnPoolManager.Instance.IncreaseQueuedCreatureCount(pool, creatureCount);

            CellManager.Instance.CellCallMethod(mapChannel, arrival.Position, arrival.EntityId, new ForceStatePacket(UseObjectState.TsState1, 0));

            Teleports.Add(new Teleport { MapChannel = mapChannel, Pool = pool, Arrival = arrival, Queued = creatureCount });
        }

        /// <summary>
        /// As above, with the creatures given rather than drawn from the pool - an alarm's wave
        /// (BaneAlarms) - and <paramref name="arrived"/> told them once they are through.
        /// </summary>
        public static void BeginTeleport(MapChannel mapChannel, SpawnPool pool, SpawnPoolArrivalEntry arrival, List<Creature> creatures, Action<List<Creature>> arrived = null)
        {
            SpawnPoolManager.Instance.IncreaseQueueCount(pool);
            SpawnPoolManager.Instance.IncreaseQueuedCreatureCount(pool, creatures.Count);

            CellManager.Instance.CellCallMethod(mapChannel, arrival.Position, arrival.EntityId, new ForceStatePacket(UseObjectState.TsState1, 0));

            Teleports.Add(new Teleport { MapChannel = mapChannel, Pool = pool, Arrival = arrival, Queued = creatures.Count, Creatures = creatures, Arrived = arrived });
        }

        /// <summary>How many teleports are on their way on a map channel.</summary>
        public static int Pending(MapChannel mapChannel) => Teleports.Count(t => t.MapChannel == mapChannel);

        /// <summary>Runs this map's teleporters: the creatures through, then the teleporter off.</summary>
        public static void TeleportWorker(MapChannel mapChannel, long timePassed)
        {
            for (var i = Teleports.Count - 1; i >= 0; i--)
            {
                var teleport = Teleports[i];

                if (teleport.MapChannel != mapChannel)
                    continue;

                teleport.ElapsedMs += timePassed;

                if (!teleport.Spawned && teleport.ElapsedMs >= TeleportLeadMs)
                {
                    teleport.Spawned = true;

                    var creatures = teleport.Creatures ?? SpawnPoolManager.Instance.CreateListOfCreatures(teleport.Pool);

                    var spawned = SpawnPoolManager.Instance.SpawnCreatures(teleport.Pool, creatures, teleport.Arrival);
                    SpawnPoolManager.Instance.DecreaseQueuedCreatureCount(teleport.Pool, teleport.Queued);
                    teleport.Arrived?.Invoke(spawned);
                }

                if (teleport.ElapsedMs < TeleportHoldMs)
                    continue;

                CellManager.Instance.CellCallMethod(mapChannel, teleport.Arrival.Position, teleport.Arrival.EntityId, new ForceStatePacket(UseObjectState.TsState0, 0));
                SpawnPoolManager.Instance.DecreaseQueueCount(teleport.Pool);
                Teleports.RemoveAt(i);
            }
        }

        /// <summary>
        /// A creature come through a teleporter: Teleport(where it stands, its yaw,
        /// BANE_TELEPORTER) on it to everyone who can see it, which plays
        /// teleport_thrax_arrival.pkg on its body (actor.py Recv_Teleport, _PlayTeleportArrivalFX);
        /// the teleporter's own effect is its USE_TS_STATE_1. PreTeleport goes first so the
        /// Teleport's departure effect is the type's - none - and not the default teleporter's
        /// (Recv_PostTeleport). Nothing for one that steps off a dropship.
        /// </summary>
        public static void ShowArrival(MapChannel mapChannel, Creature creature, SpawnPoolArrivalEntry arrival)
        {
            if (mapChannel == null || creature == null || arrival == null || arrival.Kind != SpawnPoolArrivalEntry.KindTeleporter)
                return;

            CellManager.Instance.CellCallMethod(mapChannel, creature, new PreTeleportPacket(TeleportType.BaneTeleporter));
            CellManager.Instance.CellCallMethod(mapChannel, creature, new TeleportPacket(creature.Position, creature.Rotation, TeleportType.BaneTeleporter, 0));
        }

        /// <summary>Where a creature comes out: a walkable spot within StepOutRadius of the point, or the point.</summary>
        public static Vector3 StepOut(MapChannel mapChannel, SpawnPoolArrivalEntry arrival)
        {
            return NavMeshManager.RandomPointAround(mapChannel, arrival.Position, StepOutRadius)
                   ?? arrival.Position + SpawnPoolManager.InDisc(StepOutRadius);
        }
    }
}
