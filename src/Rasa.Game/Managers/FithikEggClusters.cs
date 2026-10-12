using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Services.Preloader.Missions.Wilderness;
    using Structures;

    /// <summary>
    /// A Fithik egg cluster (UsableCrSpawnerDestFithikEggClusterV01, 10180) hatches when a player
    /// walks onto it: its hatching animation, three hostile Fithik climbing out, its hatched
    /// animation, and it grows back.
    ///
    /// The client has the whole of it, as a creature spawner's states (usabledata.animation for
    /// the class) and its models' animations:
    ///  - USE_CS_STATE_IDLE (187), "Usable Active": the cluster pulsing
    ///    (arch_forean_fithiceggcluster_idle_v01, 1 s, looping);
    ///  - USE_CS_STATE_SPAWN (189), "Usable Do Spawn": it hatches (_hatch_v01, 2 s);
    ///  - USE_CS_STATE_END (190), "Usable End Spawn": the hatched cluster (_hatched_v01, 3 s);
    ///  - END to IDLE, "Usable state End to Idle": it grows again (_grow_v01, 4 s), a transition,
    ///    which the client plays on Use (Recv_Use) and not on ForceState.
    /// The lengths are the last keys of the animations' tracks.
    ///
    /// Ours, since the client says nothing of when a cluster hatches or how many come out:
    ///  - a living player within <see cref="TriggerRadius"/> of the cluster on the ground and
    ///    <see cref="TriggerHeight"/> of it upright - standing on it - sets it off;
    ///  - ForceState SPAWN; at the end of the hatching <see cref="Hatchlings"/> of
    ///    RanjaEggClusterHatching's Fithik climb out beside it, each its CREATURE_BIRTH
    ///    (155 at its class, 7.2 s) and held where it is for as long, then after whoever set the
    ///    cluster off; ForceState END; at the end of that, Use to IDLE, so it grows back;
    ///  - a cluster hatches again only once everything that came out of it is dead and
    ///    <see cref="RearmMs"/> have passed since it last did;
    ///  - a hatchling still alive <see cref="LifetimeMs"/> after it came out, and fighting nothing,
    ///    is taken away;
    ///  - a cluster that is destroyed or taken off the map stops where it is; what has come out of
    ///    it stays.
    /// The cluster's own StateId follows, so a client that comes into range is shown it as it is.
    /// </summary>
    public static class FithikEggClusters
    {
        public const EntityClasses EggClusterClass = (EntityClasses)10180;

        /// <summary>How close on the ground a player has to come: standing on the cluster.</summary>
        public const float TriggerRadius = 1.5f;

        /// <summary>How far above or below the cluster's origin a player may stand and still be on it.</summary>
        public const float TriggerHeight = 2.5f;

        public const int HatchMs = 2000;
        public const int HatchedMs = 3000;
        public const int GrowMs = 4000;
        public const int Hatchlings = 3;
        public const long RearmMs = 60_000;
        public const long LifetimeMs = 300_000;

        /// <summary>CREATURE_BIRTH, at the born creature's class.</summary>
        public const ActionId BirthAction = (ActionId)155;
        public const int BirthTypeId = 10;

        private enum Stage { Hatching, Hatched, Growing, Resting }

        private sealed class Clutch
        {
            public MapChannel Map;
            public DynamicObject Egg;
            public Stage Stage;
            public long NextAt;
            public long HatchedAt;
            public ulong SetOffBy;
            public readonly List<Creature> Brood = new List<Creature>();
        }

        private sealed class Hatchling
        {
            public MapChannel Map;
            public Creature Creature;
            public long BornUntil;
            public long RemoveAt;
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<ulong, Clutch> Clutches = new Dictionary<ulong, Clutch>();
        private static readonly List<Hatchling> Broods = new List<Hatchling>();
        private static readonly Random Random = new Random();

        /// <summary>The clock; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>Whether the creature is still climbing out of its cluster: it does nothing else.</summary>
        public static bool IsHatching(Creature creature)
        {
            lock (Sync)
            {
                if (Broods.Count == 0)
                    return false;

                var now = Now();

                foreach (var hatchling in Broods)
                    if (hatchling.Creature == creature)
                        return now < hatchling.BornUntil;

                return false;
            }
        }

        /// <summary>The clusters set off, the ones on their way through it, and the hatchlings whose time is up. From the map channel worker.</summary>
        public static void Worker(MapChannel map)
        {
            if (map?.DynamicObjects == null)
                return;

            var now = Now();

            foreach (var egg in map.DynamicObjects.Where(obj => obj.EntityClassId == EggClusterClass).ToList())
            {
                Clutch clutch;

                lock (Sync)
                    Clutches.TryGetValue(egg.EntityId, out clutch);

                if (!Standing(egg))
                {
                    if (clutch != null)
                        lock (Sync)
                            Clutches.Remove(egg.EntityId);

                    continue;
                }

                if (clutch == null)
                {
                    if (egg.StateId != UseObjectState.CsStateIdle)
                        continue;

                    var player = OnIt(map, egg);

                    if (player != null)
                        SetOff(map, egg, player, now);

                    continue;
                }

                if (now < clutch.NextAt)
                    continue;

                switch (clutch.Stage)
                {
                    case Stage.Hatching:
                        Hatch(clutch, now);
                        break;

                    case Stage.Hatched:
                        clutch.Stage = Stage.Growing;
                        clutch.NextAt = now + GrowMs;
                        egg.StateId = UseObjectState.CsStateIdle;
                        CellManager.Instance.CellCallMethod(map, egg, new UsePacket(clutch.SetOffBy, UseObjectState.CsStateIdle, 0));
                        break;

                    case Stage.Growing:
                        clutch.Stage = Stage.Resting;
                        clutch.NextAt = clutch.HatchedAt + RearmMs;
                        break;

                    case Stage.Resting:
                        if (clutch.Brood.Any(Alive))
                            break;

                        lock (Sync)
                            Clutches.Remove(egg.EntityId);
                        break;
                }
            }

            Expire(map, now);
        }

        /// <summary>Takes away every cluster and hatchling the server is keeping; for tests.</summary>
        public static void Reset()
        {
            lock (Sync)
            {
                Clutches.Clear();
                Broods.Clear();
            }
        }

        private static void SetOff(MapChannel map, DynamicObject egg, Manifestation player, long now)
        {
            var clutch = new Clutch
            {
                Map = map,
                Egg = egg,
                Stage = Stage.Hatching,
                NextAt = now + HatchMs,
                SetOffBy = player.EntityId
            };

            lock (Sync)
                Clutches[egg.EntityId] = clutch;

            egg.StateId = UseObjectState.CsStateSpawn;
            CellManager.Instance.CellCallMethod(map, egg, new ForceStatePacket(UseObjectState.CsStateSpawn, 0));

            Logger.WriteLog(LogType.Debug, $"Fithik egg cluster {egg.EntityId} set off by {player.FamilyName}.");
        }

        private static void Hatch(Clutch clutch, long now)
        {
            var map = clutch.Map;
            var egg = clutch.Egg;
            var target = EntityManager.Instance.Players.TryGetValue(clutch.SetOffBy, out var player) ? player : null;

            for (var i = 0; i < Hatchlings; i++)
            {
                var hatchling = Birth(map, egg, RanjaEggClusterHatching.HatchlingCreatureId, target, now);

                if (hatchling != null)
                    clutch.Brood.Add(hatchling);
            }

            clutch.Stage = Stage.Hatched;
            clutch.HatchedAt = now;
            clutch.NextAt = now + HatchedMs;
            egg.StateId = UseObjectState.CsStateEnd;
            CellManager.Instance.CellCallMethod(map, egg, new ForceStatePacket(UseObjectState.CsStateEnd, 0));
        }

        /// <summary>
        /// One creature out of another spawner, as a hatchling comes out of a cluster: beside it, its
        /// birth, held for as long, then after <paramref name="target"/>; taken away
        /// <see cref="LifetimeMs"/> later if it is fighting nothing (PlacedDestructibles). Null if the
        /// creature row cannot be made.
        /// </summary>
        public static Creature Hatch(MapChannel map, DynamicObject from, uint creatureDbId, Manifestation target) =>
            map == null || from == null ? null : Birth(map, from, creatureDbId, target, Now());

        private static Creature Birth(MapChannel map, DynamicObject egg, uint creatureDbId, Manifestation target, long now)
        {
            var hatchling = CreatureManager.Instance.CreateCreature(creatureDbId, null);

            if (hatchling == null)
                return null;

            // Out of the cluster, on the ground beside it, facing whoever set it off.
            // The navmesh's random point is from a polygon the circle touches, which can be a long
            // way off across a big one: one further than that is not beside the cluster.
            var angle = Random.NextDouble() * Math.PI * 2;
            var spot = egg.Position + new Vector3((float)Math.Cos(angle) * 1.5f, 0, (float)Math.Sin(angle) * 1.5f);

            if (NavMeshManager.RandomPointAround(map, egg.Position, 1.5f) is Vector3 walkable
                && Vector2.Distance(new Vector2(walkable.X, walkable.Z), new Vector2(egg.Position.X, egg.Position.Z)) <= 2.5f)
                spot = walkable;
            var facing = target != null && Vector3.DistanceSquared(target.Position, spot) > 0.01f
                ? Math.Atan2(spot.X - target.Position.X, spot.Z - target.Position.Z)
                : angle;

            CreatureManager.Instance.SetLocation(hatchling, NavMeshManager.SnapToGround(map, spot), facing, egg.MapContextId);
            hatchling.LastYaw = (float)facing;

            CellManager.Instance.AddToWorld(map, hatchling);

            var birthMs = AbilityManager.Instance != null && AbilityManager.Instance.TryGetLevel(BirthAction, (uint)hatchling.EntityClass, out var birth) && birth.RecoveryMs > 0
                ? birth.RecoveryMs
                : 7200;

            GameEffectManager.Instance.Attach(map, hatchling, new GameEffect
            {
                TypeId = BirthTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(map),
                EffectLevel = 1,
                SourceId = hatchling.EntityId,
                Source = hatchling,
                SourceLevel = (int)hatchling.Level,
                IsBuff = true,
                AllowDetach = false,
                AnnounceOnAttach = true,
                ExpiresTick = Environment.TickCount64 + birthMs
            });

            CreatureSummons.PlayBirth(map, hatchling, (uint)hatchling.EntityClass);

            lock (Sync)
                Broods.Add(new Hatchling { Map = map, Creature = hatchling, BornUntil = now + birthMs, RemoveAt = now + LifetimeMs });

            // Out for whoever woke it: it goes for them once it is out.
            if (target != null && target.State != CharacterState.Dead)
                hatchling.Hate.Ensure(target.EntityId, Threat.NoticedThreat);

            return hatchling;
        }

        /// <summary>Forgets the dead and takes away the ones whose time is up and that are fighting nothing.</summary>
        private static void Expire(MapChannel map, long now)
        {
            List<Hatchling> here;

            lock (Sync)
                here = Broods.Where(h => h.Map == map).ToList();

            foreach (var hatchling in here)
            {
                var creature = hatchling.Creature;
                var inWorld = EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) && registered == creature;

                if (!inWorld || !Alive(creature))
                {
                    lock (Sync)
                        Broods.Remove(hatchling);

                    continue;
                }

                if (now < hatchling.RemoveAt || creature.Hate.Ranked().Any())
                    continue;

                lock (Sync)
                    Broods.Remove(hatchling);

                CellManager.Instance.RemoveCreatureFromWorld(map, creature);
            }
        }

        /// <summary>A cluster that is there to hatch: on the map, and not destroyed.</summary>
        private static bool Standing(DynamicObject egg) =>
            egg.IsInWorld && egg.StateId != UseObjectState.StateDestroyed
            && (egg.MissionDestruction == null || egg.CurrentHitPoints > 0);

        /// <summary>A living player standing on the cluster, if there is one.</summary>
        private static Manifestation OnIt(MapChannel map, DynamicObject egg)
        {
            if (map.ClientList == null)
                return null;

            foreach (var client in map.ClientList.ToList())
            {
                var player = client?.Player;

                if (player == null || player.State == CharacterState.Dead || player.MapChannel != map)
                    continue;

                if (player.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current <= 0)
                    continue;

                var ground = Vector2.Distance(new Vector2(player.Position.X, player.Position.Z), new Vector2(egg.Position.X, egg.Position.Z));

                if (ground <= TriggerRadius && Math.Abs(player.Position.Y - egg.Position.Y) <= TriggerHeight)
                    return player;
            }

            return null;
        }

        private static bool Alive(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying;
    }
}
