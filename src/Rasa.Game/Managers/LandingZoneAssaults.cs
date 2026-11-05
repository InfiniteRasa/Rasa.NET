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
    /// <summary>
    /// Timed Bane assaults on public control points the AFS holds: Wilderness Landing Zone, and
    /// each point with a plan in <see cref="Plans"/>. All deadlines use the map worker's monotonic clock.
    /// </summary>
    internal static partial class LandingZoneAssaults
    {
        private sealed class Assault
        {
            internal long Due, Started, Logged;
            internal int Waves;
            /// <summary>Assaults the AFS have held off since the point was last taken: the next is that much bigger.</summary>
            internal int Repelled;
            internal bool WarningSent, Relieved;
            internal readonly List<SpawnPool> Pools = new();
            internal readonly List<SpawnPool> Reinforcements = new();
            internal Creature Boss;
            /// <summary>Who is holding the point's use now, while a claim runs; null between claims.</summary>
            internal Creature Claimant;
            internal long? Claim;
            internal int Vitality;
            /// <summary>When a claim broken off may be begun again.</summary>
            internal long NextClaimAt;
            /// <summary>The AFS garrison pools held back so far in this assault (HoldsGarrison), each logged once.</summary>
            internal readonly HashSet<uint> HeldBack = new();
            /// <summary>Whether the garrison was down at the last pass, and the claimant last named (for the Debug log).</summary>
            internal bool GarrisonDown;
            internal Creature Named;
            /// <summary>The garrison that stood when the first wave was launched (a planned point's relief rule).</summary>
            internal int GarrisonAtStart;
            internal readonly FightWatch Watch = new();
        }

        /// <summary>How long, past its time, an assault on a planned point whose garrison is down goes on while a claimant stands (ESTIMATE).</summary>
        internal const long ClaimOvertimeMs = 300000;

        /// <summary>How near the point, across the ground, a troop of a planned point's assault whose garrison is down fights what it finds (ESTIMATE).</summary>
        internal const float RallyReach = 20f;

        /// <summary>How long a claimant waits after a claim is broken off before it begins another.</summary>
        internal const long ClaimRetryMs = 5000;

        /// <summary>How near the point's object, across the ground and at most, a claimant claims from.</summary>
        internal const float ClaimReach = 6f;

        /// <summary>A troop not yet fighting gets a target this many metres further off rather than one already taken by as many troops (<see cref="Pick"/>).</summary>
        internal const float LoadPenalty = 12f;
        private static readonly ConditionalWeakTable<MapChannel, Dictionary<uint, Assault>> States = new();
        internal static Func<long> Now = () => Environment.TickCount64;
        internal static bool ControlsCapture(MapChannel map, ControlPoints.Point point) =>
            map != null && !map.IsCopy && point != null && !point.IsBattleground &&
            map.MapInfo?.MapContextId == point.MapContextId && PlanFor(point) != null;

        private static Dictionary<uint, Assault> StatesOf(MapChannel map) => States.GetValue(map, _ => new Dictionary<uint, Assault>());

        /// <summary>Each point of the map with an assault plan, from the control points' worker.</summary>
        internal static void Worker(MapChannel map, IEnumerable<ControlPoints.Point> points)
        {
            States.TryGetValue(map, out var states);
            foreach (var point in points)
            {
                // A point whose plan is gone (ControlPointAssaultEnabled turned off, or the plan taken
                // out) while its assault runs: Worker clears it and takes its Bane off.
                if (PlanFor(point) != null || (point != null && states?.ContainsKey(point.Id) == true))
                    Worker(map, point);
                AfsSorties.Worker(map, point);
            }
        }

        internal static void Worker(MapChannel map, ControlPoints.Point point, bool warn = true)
        {
            if (point == null) return;
            if (!ControlsCapture(map, point) || !point.HeldByAfs)
            {
                if (map != null && States.TryGetValue(map, out var states) && states.Remove(point.Id, out var old))
                    Clear(map, point, old);
                return;
            }
            var plan = PlanFor(point);
            var now = Now();
            var state = StatesOf(map).TryGetValue(point.Id, out var known) ? known
                : StatesOf(map)[point.Id] = new Assault { Due = now + plan.FirstDelayMs };
            // Overdue by a whole period or more: no worker ran (the map worker runs only while a
            // player is on the map). The point is attacked as after a server start, not the moment
            // a player comes back.
            if (plan.HoldsGarrison && state.Waves == 0 && now - state.Due >= plan.PeriodMs)
            {
                state.Due = now + plan.FirstDelayMs;
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): assault overdue (no worker ran); the next in {plan.FirstDelayMs / 1000} s.");
            }
            // Warn once for the pending assault, on the first worker tick in its final five minutes.
            // A skipped deadline or a GM-forced immediate start must not produce a late warning.
            if (warn && state.Waves == 0 && !state.WarningSent && now < state.Due && state.Due - now <= 300000)
            {
                state.WarningSent = true;
                var message = $"The Bane are mustering for an attack on {plan.Name ?? point.Name} Control Point. Reinforcements requested!";
                foreach (var client in map.ClientList.ToArray())
                    if (client?.State == ClientState.Ingame && MapInstanceScope.Contains(map, client.Player))
                        CommunicatorManager.Instance.SystemMessage(client, message);
            }
            if (state.Waves == 0 && now >= state.Due)
            {
                state.GarrisonAtStart = Guards(map, point, state).Length;
                state.Started = now;
                state.WarningSent = false;
                state.Due = now + plan.PeriodMs;
                Launch(map, point, plan, state, 1);
            }
            if (state.Waves == 0) return;
            var gap = plan.WaveGapMs;
            // Fixed offsets from the start; survivors never delay a later wave.
            while (state.Waves < plan.Waves && now - state.Started >= state.Waves * gap)
                Launch(map, point, plan, state, state.Waves + 1);
            if (!state.Relieved && now - state.Started >= plan.ReliefDelayMs)
            {
                state.Relieved = true;
                // A planned point's relief is the one help its garrison gets in an assault, and it
                // comes only to a garrison that still holds more than half of what it was when the
                // assault began (ESTIMATE, balance: live runs 2026-11-05, every natural assault was
                // held off with the relief's help); one that has fallen is the Bane's to claim.
                var standing = Guards(map, point, state).Length;
                if (plan.HoldsGarrison && (GarrisonDown(map, point, Guards(map, point, state)) || state.GarrisonAtStart > 0 && standing * 2 <= state.GarrisonAtStart))
                    Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): no AFS relief at +{(now - state.Started) / 1000} s: " +
                        $"{standing} of the garrison's {state.GarrisonAtStart} stand.");
                else
                    plan.Reinforce(map, point, state);
            }
            if (state.Claimant != null && !CreatureManager.IsLivingOnMap(map, state.Claimant)) Cancel(point, state);
            var guards = Guards(map, point, state);
            var troops = EntityManager.Instance.Creatures.Values.Where(c => state.Pools.Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(map, c)).ToArray();
            var claimant = ClaimantOf(map, point, state, troops);
            // The point is claimed only when nothing of the AFS garrison is left to stop it: the
            // gate's field down and every defender of it dead, the relief with them. Until then
            // the claimant fights as the rest do, and a claim under way when one comes back is
            // broken off.
            var mayClaim = LandingZoneBreach.StandingField(map, point) == null && GarrisonDown(map, point, guards);
            if (mayClaim != state.GarrisonDown)
            {
                state.GarrisonDown = mayClaim;
                Logger.WriteLog(LogType.Debug, mayClaim
                    ? $"Control point {point.Id} ({point.Name}): AFS garrison down at +{(now - state.Started) / 1000} s, {troops.Length} Bane stand; the claim may begin."
                    : $"Control point {point.Id} ({point.Name}): AFS garrison standing again at +{(now - state.Started) / 1000} s ({guards.Length} defenders); a claim is broken off.");
            }
            if (mayClaim && claimant != null && claimant != state.Named)
            {
                state.Named = claimant;
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): the claimant is the {claimant.Name ?? "troop"} {claimant.DbId}, " +
                    $"{Flat(claimant.Position, point.Position):0} m from the point.");
            }
            // A planned point's garrison stays down for the rest of the assault (HoldsGarrison): while
            // it is down and a claimant stands, the assault goes on past its time, up to ClaimOvertimeMs,
            // for the claimant to reach the point and claim it.
            var end = Math.Max(plan.LifetimeMs, (plan.Waves - 1) * gap + 60000);
            var overtime = plan.HoldsGarrison && mayClaim && claimant != null && now - state.Started < end + ClaimOvertimeMs;
            // A claim under way finishes first (live run 2026-10-30: the timeout ended one 8 s in, with
            // no defender left, and counted the assault as held off).
            if (now - state.Started >= end && !state.Claim.HasValue && !overtime)
            {
                // Over with the AFS still holding: held off (a capture leaves through Claim, and a
                // point that changes hands drops its state, so the count starts again).
                if (plan.Escalates) state.Repelled++;
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): assault held off ({state.Repelled} held off since it was last taken).");
                Clear(map, point, state);
                return;
            }
            if (now - state.Logged >= 60000)
            {
                state.Logged = now;
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): assault +{(now - state.Started) / 1000} s, {Defenders(map, point, state)} AFS defenders " +
                    $"and {EntityManager.Instance.Creatures.Values.Count(c => state.Pools.Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(map, c))} Bane stand: " +
                    string.Join("; ", EntityManager.Instance.Creatures.Values.Where(c => state.Pools.Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(map, c))
                        .Select(c => $"{c.DbId} {Vector3.Distance(c.Position, point.Position):0} m {(c.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting ? "fighting" : "idle")} " +
                            $"hp {c.Attributes[Attributes.Health].Current:0}")) + ".");
            }
            // How many troops are on each target already: a new one is spread over the defenders
            // rather than every troop of a wave going for the nearest one, all down one path in file.
            var load = new Dictionary<ulong, int>();
            foreach (var troop in troops)
                if (troop.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting && troop.Controller.ActionFighting.TargetEntityId != 0)
                    load[troop.Controller.ActionFighting.TargetEntityId] = load.GetValueOrDefault(troop.Controller.ActionFighting.TargetEntityId) + 1;
            if (!mayClaim) Cancel(point, state);
            foreach (var creature in troops)
            {
                creature.OnClaimDuty = creature == claimant && mayClaim;
                // The garrison of a planned point down: the rest make for the point with the claimant,
                // giving up any fight on the way but a player's (live run 2026-11-05: they stood 50-73 m
                // out, fighting what they had found there). Near the point they fight what they find.
                creature.OnRally = !creature.OnClaimDuty && mayClaim && plan.HoldsGarrison && Flat(creature.Position, point.Position) > RallyReach;
                if (creature.OnClaimDuty && ClaimDuty(map, point, creature, state, now)) { if (!point.HeldByAfs) return; continue; }
                // A planned point's troop gives up a fight with anything but the garrison, a player
                // or one fighting it (FightsOffPlan), and one that has got it nowhere for NoHarmMs
                // (FightWatch) - before the breach order, which holds a troop to the relief once it
                // has landed. The targets it is given leave out the ones it has given up as out of reach.
                if (plan.HoldsGarrison && FightsTheDown(map, creature))
                    BehaviorManager.Instance.GiveUp(creature);
                if (plan.HoldsGarrison && creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting)
                {
                    var offPlan = FightsOffPlan(creature, guards);
                    if (offPlan || state.Watch.Stalled(creature, now))
                    {
                        Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): the {creature.Name ?? "troop"} {creature.DbId} gives up its fight with " +
                            $"{creature.Controller.ActionFighting.TargetEntityId} {(offPlan ? "(not the garrison)" : $"(no harm done in {NoHarmMs / 1000} s)")}, {Flat(creature.Position, point.Position):0} m from the point.");
                        BehaviorManager.Instance.GiveUp(creature);
                    }
                }
                var targets = plan.HoldsGarrison ? guards.Where(g => !state.Watch.Shuns(creature, g, now)).ToArray() : guards;
                if (LandingZoneBreach.Order(map, point, creature, targets, load)) continue;
                if (creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting)
                {
                    if (!creature.OnRally) continue;
                    BehaviorManager.Instance.GiveUp(creature);
                }
                // Not fighting while any of the garrison lives: on to the next of them it can
                // take on, the nearest first, as the load spreads them.
                if (!Engage(creature, targets, load))
                    Approach(creature, Destination(map, point, creature));
            }
        }

        /// <summary>The point's AFS garrison that stands, its service pools (vendors) left out, and the relief that has landed.</summary>
        private static Creature[] Guards(MapChannel map, ControlPoints.Point point, Assault state) =>
            EntityManager.Instance.Creatures.Values.Where(c => c.SpawnPool != null && point.AfsPools.Contains(c.SpawnPool.DbId) &&
                ControlPoints.Instance.ServicePointOf(map, c.SpawnPool) == null && CreatureManager.IsLivingOnMap(map, c)).Concat(Reinforcements(map, state)).ToArray();

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

        /// <summary>
        /// A turret of the garrison (ControlPointTurrets: in the AFS garrison, link kind 2) is taken
        /// on after every other defender: its mount is off the navmesh, and troops set on the
        /// nearest one stalled there, leashed and came back, and never reached the point.
        /// </summary>
        private static IEnumerable<Creature> Ranked(Creature troop, IEnumerable<Creature> targets, IReadOnlyDictionary<ulong, int> load) =>
            targets.OrderBy(c => Emplacements.Is(c))
                .ThenBy(c => Vector3.Distance(troop.Position, c.Position) + LoadPenalty * load.GetValueOrDefault(c.EntityId));

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
        /// Who of the assault tries for the point: the boss of the last wave while it lives, and
        /// otherwise the highest in rank of the plan's troops still standing (Plan.ClaimRanks; at
        /// Landing Zone a sergeant, a private, a trainee) - the nearest of them to the point. The
        /// one claiming keeps at it.
        /// </summary>
        private static Creature ClaimantOf(MapChannel map, ControlPoints.Point point, Assault state, IReadOnlyCollection<Creature> troops)
        {
            if (state.Claimant != null && troops.Contains(state.Claimant)) return state.Claimant;
            if (state.Boss != null && troops.Contains(state.Boss)) return state.Boss;
            foreach (var rank in PlanFor(point)?.ClaimRanks ?? Array.Empty<uint>())
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
        /// Whether a force field taken down is held down: an assault is on at a point and the
        /// field is one of that point's (PlacedDestructibles waits on it before it puts it back).
        /// </summary>
        internal static bool HoldsDown(MapChannel map, ForceFields.Field field)
        {
            if (map == null || field == null || !States.TryGetValue(map, out var states)) return false;
            foreach (var (id, state) in states)
            {
                if (state.Waves == 0) continue;
                var point = ControlPoints.Instance.ById(id);
                if (point != null && Vector3.DistanceSquared(field.Position, point.Position) <= 10000) return true;
            }
            return false;
        }

        /// <summary>
        /// Whether a pool of the AFS garrison of a planned point (Plan.HoldsGarrison) waits while a
        /// Bane assault on the point runs (SpawnPoolManager.SpawnPoolWorker): it is not set down
        /// again, nor are its turrets repaired, and its respawn time does not run. Live run
        /// 2026-11-05 (Research Point): the GM-killed riflemen and machine gunners came back by
        /// dropship at the third wave, a turret was repaired to full health 19 s later, and the
        /// mechs' ships ended a "garrison down" after 14 s, before any claim could begin. The
        /// plan's relief is the only help an assault gets; the garrison comes back after it on
        /// what is left of its respawn time. A service pool (a vendor's) is no garrison.
        /// </summary>
        internal static bool HoldsGarrison(MapChannel map, SpawnPool pool)
        {
            if (map == null || pool == null || !pool.IsGarrison || pool.IsControlPointReinforcement || !States.TryGetValue(map, out var states))
                return false;
            var point = ControlPoints.Instance.PointOf(pool);
            if (point == null || !point.HeldByAfs || !point.AfsPools.Contains(pool.DbId) || ControlPoints.Instance.ServicePointOf(map, pool) != null)
                return false;
            if (!states.TryGetValue(point.Id, out var state) || state.Waves == 0 || PlanFor(point)?.HoldsGarrison != true)
                return false;
            if (state.HeldBack.Add(pool.DbId))
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): AFS garrison pool {pool.DbId} ({What(pool)}) is down; " +
                    $"no re-garrison{(pool.Wrecks.Count > 0 ? " and no turret repair" : "")} while the assault runs.");
            return true;
        }

        /// <summary>
        /// A garrison pool of a planned point back (SpawnPoolManager.SpawnPoolWorker): set down again
        /// by its dropship, or its turrets repaired where they stand (AlternateMesh.ReviveWrecks). For
        /// the Debug log; its first set-down is not logged.
        /// </summary>
        internal static void GarrisonBack(MapChannel map, SpawnPool pool, bool repaired)
        {
            var point = ControlPoints.Instance.PointOf(pool);
            if (map == null || point == null || !pool.HasSpawned || !ControlsCapture(map, point) || !point.PoolsOf(point.Owner).Contains(pool.DbId))
                return;
            var running = States.TryGetValue(map, out var states) && states.TryGetValue(point.Id, out var state) && state.Waves != 0;
            Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): {(point.HeldByAfs ? "AFS" : "Bane")} garrison pool {pool.DbId} ({What(pool)}) " +
                $"{(repaired ? "repaired where it stands, at full health" : "re-garrisoned")}{(running ? ", while an assault runs" : "")}.");
        }

        private static string What(SpawnPool pool) =>
            string.Join(", ", (pool.SpawnSlot ?? new List<SpawnPoolSlot>()).Where(s => s != null && s.CreatureId != 0 && s.CountMax > 0).Select(s => $"{s.CountMax}x {s.CreatureId}"));

        /// <summary>Whether an assault runs on the point now (its first wave launched).</summary>
        internal static bool IsRunning(MapChannel map, ControlPoints.Point point) =>
            map != null && point != null && States.TryGetValue(map, out var states) && states.TryGetValue(point.Id, out var state) && state.Waves != 0;

        /// <summary>The answer to a GM's ".cp &lt;id&gt; assault".</summary>
        internal static string GmStart(MapChannel map, ControlPoints.Point point)
        {
            if (TryStart(map, point)) return $"{point.Name} assault started.";
            return IsRunning(map, point) ? $"A Bane assault on {point.Name} is already under way."
                : "Stand on the point's public map with the point held by AFS and its assaults enabled.";
        }

        internal static bool TryStart(MapChannel map, ControlPoints.Point point)
        {
            if (!ControlsCapture(map, point) || !point.HeldByAfs) return false;
            var running = StatesOf(map).TryGetValue(point.Id, out var before) && before.Waves != 0;
            Worker(map, point, warn: false);
            var state = StatesOf(map).TryGetValue(point.Id, out var known) ? known : StatesOf(map)[point.Id] = new Assault();
            // One that was due and that the worker has just started is the one asked for.
            if (state.Waves != 0) return !running;
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

        private static void Launch(MapChannel map, ControlPoints.Point point, Plan plan, Assault state, int wave)
        {
            var bossShip = Random.Shared.Next(plan.Drops.Length);
            for (var i = 0; i < plan.Drops.Length; i++)
            {
                var drop = plan.Drops[i];
                var slots = plan.Ship(wave, i, bossShip, state.Repelled);
                if (slots.Count == 0) continue;
                var pool = new SpawnPool { MapContextId = point.MapContextId, RuntimeMapChannel = map,
                    Position = drop.Position, Rotation = drop.Rotation, SpawnSlot = slots, AnimType = 1,
                    Mode = SpawnPoolManager.ModeScripted, SpawnPolicy = MissionSpawnGroupPolicy.ScenarioControlled,
                    HasSpawned = true, IsControlPointAssault = true, AssaultPointId = point.Id };
                pool.Delivered = creatures => {
                    foreach (var creature in creatures)
                    {
                        if (creature.DbId == plan.Boss) state.Boss = creature;
                        // A planned point's troops seek out its garrison only (Creature.SeeksOnly).
                        if (plan.HoldsGarrison) creature.SeeksOnly = target => IsDefender(map, point, state, target);
                        BehaviorManager.Instance.WalkIn(creature, Destination(map, point, creature));
                        // Home is the point, not the drop (as LandingZoneBreach.Order sets it): a
                        // troop that leashes runs back to the point.
                        creature.HomePos.Position = point.Position;
                    }
                };
                state.Pools.Add(pool);
                map.SpawnPools.Add(pool);
                var brings = SpawnPoolManager.Instance.CreateListOfCreatures(pool);
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): assault wave {wave}/{plan.Waves}, ship {i + 1}: " +
                    $"{brings.Count} Bane ({string.Join(", ", brings.GroupBy(c => c.DbId).Select(g => $"{g.Count()}x {g.Key}"))}), {state.Repelled} held off before.");
                SpawnPoolManager.Instance.EnqueueDropship(map, pool, brings,
                    new SpawnPoolArrivalEntry { Kind = SpawnPoolArrivalEntry.KindDropship,
                        PosX = drop.Position.X, PosY = drop.Position.Y, PosZ = drop.Position.Z, Rotation = drop.Rotation });
            }
            state.Waves = wave;
            Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): before wave {wave}, {Defenders(map, point, state)} AFS defenders " +
                $"and {EntityManager.Instance.Creatures.Values.Count(c => state.Pools.Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(map, c))} Bane of the assault stand.");
            foreach (var client in map.ClientList)
                CommunicatorManager.Instance.SystemMessage(client, $"{plan.Name ?? point.Name}: Bane assault wave {wave}/{plan.Waves} incoming!");
        }
        /// <summary>
        /// Sets each pool's living count to the creatures of it that stand: TakeOffLiving takes
        /// nothing when every count is 0, and a count that has drifted below its living (live run
        /// 2026-10-29: a Commander and a Technician of a held-off assault stayed at the point until
        /// it fell) left them standing after their assault was over.
        /// </summary>
        internal static void CountLiving(MapChannel map, List<SpawnPool> pools)
        {
            foreach (var pool in pools)
            {
                var living = EntityManager.Instance.Creatures.Values.Count(c => c.SpawnPool == pool && CreatureManager.IsLivingOnMap(map, c));
                if (living > pool.AliveCreatures) pool.AliveCreatures = living;
            }
        }

        /// <summary>Assaults on the point the AFS have held off since it was last taken (.cp list).</summary>
        internal static int Repelled(MapChannel map, ControlPoints.Point point) =>
            map != null && point != null && States.TryGetValue(map, out var states) && states.TryGetValue(point.Id, out var state) ? state.Repelled : 0;

        /// <summary>The point's AFS garrison and relief that stand (for the Debug log).</summary>
        private static int Defenders(MapChannel map, ControlPoints.Point point, Assault state) =>
            EntityManager.Instance.Creatures.Values.Count(c => c.SpawnPool != null && CreatureManager.IsLivingOnMap(map, c) &&
                (point.AfsPools.Contains(c.SpawnPool.DbId) && ControlPoints.Instance.ServicePointOf(map, c.SpawnPool) == null || state.Reinforcements.Contains(c.SpawnPool)));

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
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): the {claimant.Name ?? "claimant"} claims it; AFS garrison " +
                    $"{ControlPoints.Instance.GarrisonOf(map, point, ControlPoints.Afs)}, {Defenders(map, point, state)} AFS defenders stand.");
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
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): claim done by the {claimant.Name ?? "claimant"} {claimant.DbId} " +
                    $"at +{(now - state.Started) / 1000} s; the point is the Bane's.");
                Cancel(point, state);
                ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null);
                Clear(map, point, state);
            }
        }
        internal static bool IsCapturing(MapChannel map, Actor actor, DynamicObject obj) =>
            map != null && States.TryGetValue(map, out var states) &&
            states.Values.Any(state => state.Claim.HasValue && state.Claimant == actor && obj.UsedBy == actor);
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
            CountLiving(map, pools);
            ControlPoints.TakeOffLiving(map, pools);
            var left = TakeOffLeftovers(map, pools);
            if (left > 0)
                Logger.WriteLog(LogType.Debug, $"Control point {point.Id} ({point.Name}): {left} of the assault and its relief still stood after it; taken off.");
            foreach (var pool in pools) { pool.Delivered = null; map.SpawnPools.Remove(pool); }
            state.Pools.Clear();
            state.Reinforcements.Clear();
            state.Boss = null;
            state.Claimant = null;
            state.NextClaimAt = 0;
            state.HeldBack.Clear();
            state.GarrisonDown = false;
            state.Named = null;
            state.Waves = 0;
            state.Relieved = false;
            state.GarrisonAtStart = 0;
            state.Watch.Clear();
        }

        /// <summary>Whether the creature is of the point's AFS garrison (its service pools left out) or the assault's relief.</summary>
        private static bool IsDefender(MapChannel map, ControlPoints.Point point, Assault state, Creature creature) =>
            creature?.SpawnPool != null && (point.AfsPools.Contains(creature.SpawnPool.DbId) && ControlPoints.Instance.ServicePointOf(map, creature.SpawnPool) == null
                || state.Reinforcements.Contains(creature.SpawnPool));
    }
}
