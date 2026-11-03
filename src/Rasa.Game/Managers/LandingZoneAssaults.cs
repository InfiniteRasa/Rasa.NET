using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Rasa.Data;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Packets.MapChannel.Server;

namespace Rasa.Managers
{
    /// <summary>Public Wilderness Landing Zone assaults. All deadlines use the map worker's monotonic clock.</summary>
    internal static partial class LandingZoneAssaults
    {
        private sealed class Assault
        {
            internal long Due, Started;
            internal int Waves;
            internal bool WarningSent;
            internal readonly List<SpawnPool> Pools = new();
            internal readonly List<SpawnPool> Reinforcements = new();
            internal Creature Boss;
            /// <summary>Who is holding the point's use now, while a claim runs; null between claims.</summary>
            internal Creature Claimant;
            internal long? Claim;
            internal int Vitality;
            /// <summary>When a claim broken off may be begun again.</summary>
            internal long NextClaimAt;
        }

        /// <summary>How long a claimant waits after a claim is broken off before it begins another.</summary>
        internal const long ClaimRetryMs = 5000;

        /// <summary>How near the point's object, across the ground and at most, a claimant claims from.</summary>
        internal const float ClaimReach = 6f;

        /// <summary>A troop not yet fighting gets a target this many metres further off rather than one already taken by as many troops (<see cref="Pick"/>).</summary>
        internal const float LoadPenalty = 12f;
        private static readonly ConditionalWeakTable<MapChannel, Assault> States = new();
        internal static Func<long> Now = () => Environment.TickCount64;
        internal static bool ControlsCapture(MapChannel map, ControlPoints.Point point) =>
            map != null && !map.IsCopy && map.MapInfo?.MapContextId == LandingZoneAssaultContent.MapId &&
            point?.Id == LandingZoneAssaultContent.PointId && !point.IsBattleground &&
            ControlPoints.Instance.Config.LandingZoneAssaultEnabled;

        internal static void Worker(MapChannel map, ControlPoints.Point point, bool warn = true)
        {
            if (!ControlsCapture(map, point) || !point.HeldByAfs)
            {
                if (map != null && States.TryGetValue(map, out var old))
                    Clear(map, point, old);
                if (map != null) States.Remove(map);
                return;
            }
            var config = ControlPoints.Instance.Config;
            var now = Now();
            var state = States.GetValue(map, _ => new Assault { Due = now + Math.Max(1, config.LandingZoneAssaultPeriodSeconds) * 1000L });
            // Warn once for the pending assault, on the first worker tick in its final five minutes.
            // A skipped deadline or a GM-forced immediate start must not produce a late warning.
            if (warn && state.Waves == 0 && !state.WarningSent && now < state.Due && state.Due - now <= 300000)
            {
                state.WarningSent = true;
                var message = $"The Bane are mustering for an attack on {point.Name} Control Point. Reinforcements requested!";
                foreach (var client in map.ClientList.ToArray())
                    if (client?.State == ClientState.Ingame && MapInstanceScope.Contains(map, client.Player))
                        CommunicatorManager.Instance.SystemMessage(client, message);
            }
            if (state.Waves == 0 && now >= state.Due)
            {
                state.Started = now;
                state.WarningSent = false;
                state.Due = now + Math.Max(1, config.LandingZoneAssaultPeriodSeconds) * 1000L;
                Launch(map, point, state, 1);
                Reinforce(map, state);
            }
            if (state.Waves == 0) return;
            var gap = Math.Max(1, config.LandingZoneAssaultWaveSeconds) * 1000L;
            // Fixed offsets from the start; survivors never delay a later wave.
            while (state.Waves < 3 && now - state.Started >= state.Waves * gap)
                Launch(map, point, state, state.Waves + 1);
            if (now - state.Started >= Math.Max(config.LandingZoneAssaultLifetimeSeconds * 1000L, 2 * gap + 60000))
            {
                Clear(map, point, state);
                return;
            }
            if (state.Claimant != null && !CreatureManager.IsLivingOnMap(map, state.Claimant)) Cancel(point, state);
            var guards = EntityManager.Instance.Creatures.Values.Where(c => c.SpawnPool != null && point.AfsPools.Contains(c.SpawnPool.DbId) &&
                ControlPoints.Instance.ServicePointOf(map, c.SpawnPool) == null && CreatureManager.IsLivingOnMap(map, c)).Concat(Reinforcements(map, state)).ToArray();
            var troops = EntityManager.Instance.Creatures.Values.Where(c => state.Pools.Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(map, c)).ToArray();
            // How many troops are on each target already: a new one is spread over the defenders
            // rather than every troop of a wave going for the nearest one, all down one path in file.
            var load = new Dictionary<ulong, int>();
            foreach (var troop in troops)
                if (troop.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting && troop.Controller.ActionFighting.TargetEntityId != 0)
                    load[troop.Controller.ActionFighting.TargetEntityId] = load.GetValueOrDefault(troop.Controller.ActionFighting.TargetEntityId) + 1;
            var claimant = ClaimantOf(map, point, state, troops);
            // The point is claimed only when nothing of the AFS garrison is left to stop it: the
            // gate's field down and every defender of it dead, the relief with them. Until then
            // the claimant fights as the rest do, and a claim under way when one comes back is
            // broken off.
            var mayClaim = LandingZoneBreach.StandingField(map, point) == null && GarrisonDown(map, point, guards);
            if (!mayClaim) Cancel(point, state);
            foreach (var creature in troops)
            {
                creature.OnClaimDuty = creature == claimant && mayClaim;
                if (creature.OnClaimDuty && ClaimDuty(map, point, creature, state, now)) { if (!point.HeldByAfs) return; continue; }
                if (LandingZoneBreach.Order(map, point, creature, guards, load)) continue;
                if (creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting) continue;
                // Not fighting while any of the garrison lives: on to the next of them it can
                // take on, the nearest first, as the load spreads them.
                if (!Engage(creature, guards, load))
                    Approach(creature, Destination(map, point, creature));
            }
        }

        /// <summary>
        /// Whether the AFS garrison is all dead: none of its defenders alive (<paramref name="guards"/>,
        /// the relief that has landed among them), and none of its pools standing
        /// (ControlPoints.GarrisonOf) - none with a dropship or teleport of defenders due, nor
        /// one that has yet to set anyone down. A pool whose respawn has yet to come round is dead.
        /// </summary>
        private static bool GarrisonDown(MapChannel map, ControlPoints.Point point, IReadOnlyCollection<Creature> guards) =>
            guards.Count == 0 && ControlPoints.Instance.GarrisonOf(map, point, ControlPoints.Afs) != ControlPoints.Garrison.Standing;

        /// <summary>
        /// The troop set on one of <paramref name="targets"/>: the first it can take on, in the
        /// order <see cref="Pick"/> ranks them. False when none will do.
        /// </summary>
        internal static bool Engage(Creature troop, IEnumerable<Creature> targets, Dictionary<ulong, int> load)
        {
            foreach (var target in Ranked(troop, targets, load))
                if (BehaviorManager.Instance.TrySetActionFighting(troop, target.EntityId))
                {
                    load[target.EntityId] = load.GetValueOrDefault(target.EntityId) + 1;
                    return true;
                }
            return false;
        }

        private static IEnumerable<Creature> Ranked(Creature troop, IEnumerable<Creature> targets, IReadOnlyDictionary<ulong, int> load) =>
            targets.OrderBy(c => Vector3.Distance(troop.Position, c.Position) + LoadPenalty * load.GetValueOrDefault(c.EntityId));

        /// <summary>
        /// The target a troop takes from <paramref name="targets"/>: the nearest, counting each
        /// troop already on one as <see cref="LoadPenalty"/> metres more, so a wave spreads over
        /// the defenders it finds.
        /// </summary>
        internal static Creature Pick(Creature troop, IEnumerable<Creature> targets, IReadOnlyDictionary<ulong, int> load) =>
            Ranked(troop, targets, load).FirstOrDefault();

        /// <summary>Walks the troop to <paramref name="destination"/>, unless it is already on its way there.</summary>
        private static void Approach(Creature troop, Vector3 destination)
        {
            var wander = troop.Controller.ActionWander;
            if (troop.Controller.CurrentAction == BehaviorManager.BehaviorActionWander && wander.Arriving &&
                Vector3.DistanceSquared(wander.WanderDestination, destination) < 1f)
                return;
            BehaviorManager.Instance.WalkIn(troop, destination);
        }

        /// <summary>
        /// Who of the assault tries for the point: the boss of the third wave while it lives, and
        /// otherwise the highest in rank of the Thrax infantry still standing - a sergeant, a
        /// private, a trainee - the nearest of them to the point. The one claiming keeps at it.
        /// </summary>
        private static Creature ClaimantOf(MapChannel map, ControlPoints.Point point, Assault state, IReadOnlyCollection<Creature> troops)
        {
            if (state.Claimant != null && troops.Contains(state.Claimant)) return state.Claimant;
            if (state.Boss != null && troops.Contains(state.Boss)) return state.Boss;
            foreach (var rank in new[] { LandingZoneAssaultContent.Sergeant, LandingZoneAssaultContent.Private, LandingZoneAssaultContent.Trainee })
            {
                var best = troops.Where(c => c.DbId == rank).OrderBy(c => Vector3.DistanceSquared(c.Position, point.Position)).FirstOrDefault();
                if (best != null) return best;
            }
            return null;
        }

        /// <summary>
        /// The claimant's part, once the gate's field is down and the whole AFS garrison is dead
        /// (<see cref="GarrisonDown"/>; Creature.OnClaimDuty): to the point, fighting nobody on
        /// the way, and a claim begun there. A player's harm, or a stun, breaks it off.
        /// </summary>
        private static bool ClaimDuty(MapChannel map, ControlPoints.Point point, Creature claimant, Assault state, long now)
        {
            if (claimant.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting)
                BehaviorManager.Instance.GiveUp(claimant);
            var destination = Destination(map, point);
            if (Flat(claimant.Position, destination) > ClaimReach)
            {
                Cancel(point, state, retry: now);
                Approach(claimant, destination);
                return true;
            }
            if (now < state.NextClaimAt)
            {
                if (claimant.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting)
                    BehaviorManager.Instance.SetActionAnchor(claimant, claimant.Position);
                return true;
            }
            Claim(map, point, claimant, state, now);
            return true;
        }

        private static float Flat(Vector3 a, Vector3 b) => new Vector2(a.X - b.X, a.Z - b.Z).Length();

        /// <summary>
        /// Whether a force field taken down is held down: an assault is on at Landing Zone and the
        /// field is one of the point's (PlacedDestructibles waits on it before it puts it back).
        /// </summary>
        internal static bool HoldsDown(MapChannel map, ForceFields.Field field)
        {
            if (map == null || field == null || !States.TryGetValue(map, out var state) || state.Waves == 0) return false;
            var point = ControlPoints.Instance.ById(LandingZoneAssaultContent.PointId);
            return point != null && Vector3.DistanceSquared(field.Position, point.Position) <= 10000;
        }

        internal static bool TryStart(MapChannel map, ControlPoints.Point point)
        {
            if (!ControlsCapture(map, point) || !point.HeldByAfs) return false;
            Worker(map, point, warn: false);
            var state = States.GetValue(map, _ => new Assault());
            if (state.Waves != 0) return false;
            state.Due = Now();
            Worker(map, point, warn: false);
            return true;
        }
        private static Vector3 Destination(MapChannel map, ControlPoints.Point point) =>
            NavMeshManager.NearestWalkable(map, point.Position) ?? point.Position;

        /// <summary>
        /// Where a troop with nobody to fight makes for: a place of its own about the point, 4 to
        /// 13 m off on a bearing its entity id picks, where it can walk to - so a wave spreads
        /// over the courtyard rather than every troop of it filing to the one spot.
        /// </summary>
        private static Vector3 Destination(MapChannel map, ControlPoints.Point point, Creature troop)
        {
            var centre = Destination(map, point);
            var angle = (troop.EntityId * 2.399963) % (2 * Math.PI);
            var radius = 4 + (float)(troop.EntityId % 4) * 3;
            var raw = centre + new Vector3((float)Math.Cos(angle) * radius, 0, (float)Math.Sin(angle) * radius);
            return map.NavMesh?.WalkTo(centre, raw) ?? centre;
        }

        private static void Launch(MapChannel map, ControlPoints.Point point, Assault state, int wave)
        {
            var bossShip = Random.Shared.Next(6);
            for (var i = 0; i < LandingZoneAssaultContent.Drops.Length; i++)
            {
                var drop = LandingZoneAssaultContent.Drops[i];
                var slots = new List<SpawnPoolSlot> {
                    new(wave == 1 ? LandingZoneAssaultContent.Trainee : LandingZoneAssaultContent.Private, 3, 5),
                    new(wave == 1 ? LandingZoneAssaultContent.Private : LandingZoneAssaultContent.Sergeant, 1, 1) };
                if (wave == 1 || Random.Shared.Next(2) == 0) slots.Add(new(LandingZoneAssaultContent.Hunter, 1, 1));
                else slots.Add(new(LandingZoneAssaultContent.Linker, 2, 2));
                if (wave == 3 && i == bossShip) slots.Add(new(LandingZoneAssaultContent.Boss, 1, 1));
                var pool = new SpawnPool { MapContextId = LandingZoneAssaultContent.MapId, RuntimeMapChannel = map,
                    Position = drop.Position, Rotation = drop.Rotation, SpawnSlot = slots, AnimType = 1,
                    Mode = SpawnPoolManager.ModeScripted, SpawnPolicy = MissionSpawnGroupPolicy.ScenarioControlled,
                    HasSpawned = true, IsControlPointAssault = true };
                pool.Delivered = creatures => {
                    foreach (var creature in creatures)
                    {
                        if (creature.DbId == LandingZoneAssaultContent.Boss) state.Boss = creature;
                        BehaviorManager.Instance.WalkIn(creature, Destination(map, point, creature));
                    }
                };
                state.Pools.Add(pool);
                map.SpawnPools.Add(pool);
                SpawnPoolManager.Instance.EnqueueDropship(map, pool, SpawnPoolManager.Instance.CreateListOfCreatures(pool),
                    new SpawnPoolArrivalEntry { Kind = SpawnPoolArrivalEntry.KindDropship,
                        PosX = drop.Position.X, PosY = drop.Position.Y, PosZ = drop.Position.Z, Rotation = drop.Rotation });
            }
            state.Waves = wave;
            foreach (var client in map.ClientList)
                CommunicatorManager.Instance.SystemMessage(client, $"Landing Zone: Bane assault wave {wave}/3 incoming!");
        }
        private static int Vitality(Creature boss) =>
            (int)boss.Attributes[Attributes.Health].Current + (int)boss.Attributes[Attributes.Armor].Current;

        /// <summary>
        /// A claim by the claimant standing at the point: begun - the point's use locked to it,
        /// "Bane claiming" across the screen - and held for the capture time, the point the Bane's
        /// at the end of it. Broken off by any harm done to it, a stun, or its moving off, and
        /// begun again <see cref="ClaimRetryMs"/> later.
        /// </summary>
        private static void Claim(MapChannel map, ControlPoints.Point point, Creature claimant, Assault state, long now)
        {
            var ready = CreatureManager.IsLivingOnMap(map, claimant) &&
                Flat(claimant.Position, Destination(map, point)) <= ClaimReach &&
                !claimant.ActiveEffects.Values.Any(e => e.IsStun && !e.IsExpired);
            if (!ready || (state.Claim.HasValue && (state.Claimant != claimant || Vitality(claimant) < state.Vitality)))
            {
                Cancel(point, state, retry: now);
                return;
            }
            if (point.Object == null || (point.Object.UsedBy != null && point.Object.UsedBy != claimant)) return;
            if (!state.Claim.HasValue)
            {
                if (claimant.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting)
                    BehaviorManager.Instance.GiveUp(claimant);
                state.Claim = now;
                state.Claimant = claimant;
                point.Object.UsedBy = claimant;
                CellManager.Instance.CellCallMethod(point.Object, new LockToActorPacket(claimant.EntityId));
                CellManager.Instance.CellCallMethod(point.Object, new UseInterruptiblePacket(claimant.EntityId));
                BehaviorManager.Instance.SetActionAnchor(claimant, claimant.Position);
                ControlPoints.Instance.BaneClaiming(map, point);
            }
            state.Vitality = Vitality(claimant);
            if (now - state.Claim.Value >= Math.Max(1, ControlPoints.Instance.Config.LandingZoneAssaultCaptureSeconds) * 1000L)
            {
                Cancel(point, state);
                ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null);
                Clear(map, point, state);
            }
        }
        internal static bool IsCapturing(MapChannel map, Actor actor, DynamicObject obj) =>
            map != null && States.TryGetValue(map, out var state) && state.Claim.HasValue && state.Claimant == actor && obj.UsedBy == actor;
        /// <summary>The claim broken off, if one runs; with <paramref name="retry"/>, the next not before ClaimRetryMs on.</summary>
        private static void Cancel(ControlPoints.Point point, Assault state, long? retry = null)
        {
            if (!state.Claim.HasValue)
                return;
            if (point?.Object != null && point.Object.UsedBy == state.Claimant && state.Claimant != null)
            {
                CellManager.Instance.CellCallMethod(point.Object, new UseInterruptedPacket(state.Claimant.EntityId));
                CellManager.Instance.CellCallMethod(point.Object, new LockToActorPacket(0));
                point.Object.UsedBy = null;
            }
            state.Claim = null;
            state.Claimant = null;
            if (retry.HasValue)
                state.NextClaimAt = retry.Value + ClaimRetryMs;
        }
        private static void Clear(MapChannel map, ControlPoints.Point point, Assault state)
        {
            Cancel(point, state);
            LandingZoneGarrison.CancelReinforcements(map, state.Reinforcements);
            var pools = state.Pools.Concat(state.Reinforcements).ToList();
            foreach (var pool in pools) pool.Suspended = true;
            foreach (var ship in DynamicObjectManager.Instance.Dropships.Values.Where(s => pools.Contains(s.SpawnPool)).ToArray())
            {
                CellManager.Instance.RemoveFromWorld(map, ship);
                DynamicObjectManager.Instance.Dropships.Remove(ship.EntityId);
                ship.SpawnPool.QueuedCreatures = 0;
                ship.SpawnPool.DropshipQueue = 0;
                ship.SpawnPool.QueuedCreatureList = null;
            }
            ControlPoints.TakeOffLiving(map, pools);
            foreach (var pool in pools) { pool.Delivered = null; map.SpawnPools.Remove(pool); }
            state.Pools.Clear();
            state.Reinforcements.Clear();
            state.Boss = null;
            state.Claimant = null;
            state.NextClaimAt = 0;
            state.Waves = 0;
        }
    }
}
