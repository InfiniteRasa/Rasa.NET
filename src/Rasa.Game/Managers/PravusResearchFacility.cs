using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Missions;
    using Packets.MapChannel.Server;
    using Models;
    using Structures;
    using Structures.World;

    using P = Services.Preloader.PravusResearchContent;

    /// <summary>
    /// What Pravus Research Facility (1430) does that its rows do not: each copy of it - a squad's
    /// instance, or the map's own channel - runs its story for whoever is in it
    /// (PravusResearchContent has the people, the Bane, the Machina and the missions).
    ///
    ///  - The frontline (Assemble With Lieutenant Perkins): while a member on it is within
    ///    <see cref="FrontlineRadius"/> of the Frontlines Entrance, Machina come on at the AFS
    ///    line from the Bane side, one every <see cref="FrontlineEveryMs"/>, at most
    ///    <see cref="FrontlineCap"/> of them alive, after the nearest member.
    ///  - The dishes (Pirate Radio): three Living Infestations about each of the three
    ///    communication dishes. Each destroyed is an event of its dish's scenario for everyone on
    ///    that dish's objective, and stays down in the copy.
    ///  - The Bane gate: the force field in the wall's major entrance stops players and takes
    ///    nothing from their shots (ForceFields.Field.Sealed). The keypass scanner to its right,
    ///    used by a player holding the Level 1 Keypass, takes it down for everyone in the copy,
    ///    for good; without one the use is refused as the client puts it (PmMissingReqItem). The
    ///    keypass is not taken.
    ///  - The Machina chamber (The Means of Production): a member on Find the source within
    ///    <see cref="SourceRadius"/> of the fueling capsule, down in the control room, has found it.
    ///    While the capsule stands and someone is near it the chamber turns out Machina without
    ///    end, one every <see cref="ProductionEveryMs"/>, at most <see cref="ProductionCap"/>
    ///    alive. Destroyed, it stays down: production stops, it is the objective for everyone on
    ///    Destroy Fueling Capsule, six Bane come at the chamber from both its doors - each one
    ///    dead an event of the ambush for everyone on Ambush! - and Ranger Nylla is in the
    ///    overlook hall from then on to take the mission and give A Higher Calling.
    /// Members who come later are given what the copy has done: the infestations down, the
    /// capsule down and the ambush dead count for them when they reach those objectives. A copy
    /// that is closed and opened again (a restart, or the squad instance unloaded) starts over.
    /// </summary>
    public static class PravusResearchFacility
    {
        public const float FrontlineRadius = 60f;
        public const int FrontlineCap = 6;
        public const long FrontlineEveryMs = 5_000;

        public const float SourceRadius = 25f;
        public const float ChamberRadius = 60f;

        /// <summary>The facility is under the hills: what is above this is not in it.</summary>
        public const float FacilityCeiling = 30f;

        public const int ProductionCap = 4;
        public const long ProductionEveryMs = 12_000;

        public const long NyllaBackMs = 30_000;
        public const int UseWindupMs = 2_000;

        /// <summary>One copy of the facility, as it stands.</summary>
        internal sealed class State
        {
            public bool Set;
            public PlacedDestructibles.Live Field;
            public DynamicObject Scanner;
            public bool GateOpen;
            public PlacedDestructibles.Live Capsule;

            /// <summary>Each dish's infestations, in Pirate Radio's order of dishes.</summary>
            public readonly List<PlacedDestructibles.Live>[] Infestations =
                { new List<PlacedDestructibles.Live>(), new List<PlacedDestructibles.Live>(), new List<PlacedDestructibles.Live>() };

            public readonly List<Creature> Frontline = new();
            public long FrontlineNextAt;

            public readonly List<Creature> Production = new();
            public long ProductionNextAt;

            public bool CapsuleDown;
            public readonly List<Creature> Ambush = new();

            public Creature Nylla;
            public long NyllaAt;

            /// <summary>The events already recorded: the assignment, the scenario, the event.</summary>
            public readonly HashSet<(string Assignment, uint Scenario, uint Event)> Told = new();
        }

        private static readonly ConditionalWeakTable<MapChannel, State> States = new();

        /// <summary>The clock; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>Whether a map channel is a copy of the facility.</summary>
        public static bool IsFacility(MapChannel map) => map?.MapInfo?.MapContextId == P.Pravus;

        internal static State StateOf(MapChannel map) => IsFacility(map) ? States.GetValue(map, _ => new State()) : null;

        /// <summary>Whether the copy's gate is open, for a test or a GM.</summary>
        public static bool GateOpen(MapChannel map) => StateOf(map)?.GateOpen ?? false;

        /// <summary>From the map channel worker.</summary>
        public static void Worker(MapChannel map)
        {
            if (!IsFacility(map))
                return;

            var state = StateOf(map);
            var now = Now();

            if (!state.Set)
                SetUp(map, state);

            KeepDown(state);

            var members = Members(map);

            if (members.Count == 0)
                return;

            Frontline(map, state, members, now);
            Dishes(state, members);
            Chamber(map, state, members, now);
        }

        #region Set up

        /// <summary>The copy's gate, scanner, capsule and infestations, put down once.</summary>
        private static void SetUp(MapChannel map, State state)
        {
            state.Set = true;

            state.Field = PlacedDestructibles.PutDown(map, (EntityClasses)P.ForceFieldClass, P.Gate, P.GateYaw,
                side: WorldDestructibleEntry.SideBane);

            if (state.Field?.Field != null)
                state.Field.Field.Sealed = true;

            state.Scanner = Usable(map, "pravus:keypass-scanner", P.KeypassScannerClass, Ground(map, P.Scanner), P.ScannerYaw, UseObjectState.TsState0);
            state.Capsule = PlacedDestructibles.PutDown(map, (EntityClasses)P.CapsuleClass, Ground(map, P.Capsule), 0, P.CapsuleHitPoints);

            for (var dish = 0; dish < P.Dishes.Length; dish++)
                foreach (var spot in InfestationSpots(P.Dishes[dish].Spot))
                    if (PlacedDestructibles.PutDown(map, (EntityClasses)P.InfestationClass, Ground(map, spot.Position), spot.Facing) is { } live)
                        state.Infestations[dish].Add(live);

            Logger.WriteLog(LogType.Debug, $"Pravus Research {map.InstanceId}: the gate, its scanner, the capsule and " +
                $"{state.Infestations.Sum(dish => dish.Count)} infestations put down.");
        }

        /// <summary>Where a dish's infestations stand: about its middle, a third of a turn apart, facing out.</summary>
        public static IEnumerable<(Vector3 Position, float Facing)> InfestationSpots(Vector3 dish)
        {
            for (var i = 0; i < P.InfestationsPerDish; i++)
            {
                var facing = (float)(i * 2 * Math.PI / P.InfestationsPerDish);
                yield return (dish + new Vector3(MathF.Sin(facing), 0, MathF.Cos(facing)) * P.InfestationRadius, facing);
            }
        }

        private static DynamicObject Usable(MapChannel map, string key, uint classId, Vector3 position, double rotation, UseObjectState initial)
        {
            var existing = map.DynamicObjects.FirstOrDefault(obj => obj.ScenarioKey == key);

            if (existing != null)
                return existing;

            var obj = DynamicObjectManager.Instance.CreateScenarioDynamicObject(map, (EntityClasses)classId, position, rotation, key, true, UseWindupMs);
            obj.StateId = initial;
            map.DynamicObjects.Add(obj);
            CellManager.Instance.AddToWorld(map, obj);
            obj.IsInWorld = true;
            return obj;
        }

        /// <summary>What is destroyed in the facility stays so: it is never due back.</summary>
        private static void KeepDown(State state)
        {
            foreach (var live in state.Infestations.SelectMany(dish => dish).Append(state.Capsule).Append(state.Field))
                if (live != null && live.IsDown && live.RespawnAt != long.MaxValue)
                    live.RespawnAt = long.MaxValue;
        }

        #endregion

        #region The gate

        /// <summary>
        /// A use asked for (DynamicObjectManager.RequestUseObjectPacket): why the copy's keypass
        /// scanner will not take it - no keypass, or the gate open already - or null.
        /// </summary>
        public static PlayerMessage? Refusal(Client client, DynamicObject obj)
        {
            var state = StateOf(client?.Player?.MapChannel);

            if (state == null || obj == null || !ReferenceEquals(obj, state.Scanner))
                return null;

            if (state.GateOpen)
                return PlayerMessage.PmUseObjectNotUsable;

            return InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)P.KeypassClass) < 1
                ? PlayerMessage.PmMissingReqItem
                : null;
        }

        /// <summary>
        /// A finished use of an object (DynamicObjectManager.FootlockerRecovery): true if it is the
        /// copy's keypass scanner, which this takes - the gate comes down, if the keypass is still held.
        /// </summary>
        public static bool Used(MapChannel map, Client client, DynamicObject obj)
        {
            var state = StateOf(map);

            if (state == null || obj == null || !ReferenceEquals(obj, state.Scanner) || client?.Player == null)
                return false;

            if (state.GateOpen || Refusal(client, obj) != null)
                return true;

            Open(map, state, client.Player.EntityId);
            Logger.WriteLog(LogType.Debug, $"Pravus Research {map.InstanceId}: {client.Player.FamilyName} opens the gate with the keypass.");
            return true;
        }

        private static void Open(MapChannel map, State state, ulong by)
        {
            state.GateOpen = true;

            if (state.Field?.Field is { } field && !field.IsDestroyed)
                ForceFields.Damage(field, field.Health, by);

            KeepDown(state);

            state.Scanner.StateId = UseObjectState.TsState1;
            CellManager.Instance.CellCallMethod(map, state.Scanner, new UsePacket(by, UseObjectState.TsState1, 0));
        }

        #endregion

        #region The frontline

        private static void Frontline(MapChannel map, State state, List<Client> members, long now)
        {
            state.Frontline.RemoveAll(creature => !Alive(creature));

            var fighting = members.Where(client => Wants(client.Player, P.AssembleWithPerkins, P.DefeatTheMachina) &&
                Vector3.Distance(client.Player.Position, P.Frontline) <= FrontlineRadius).ToList();

            if (fighting.Count == 0 || state.Frontline.Count >= FrontlineCap || now < state.FrontlineNextAt)
                return;

            state.FrontlineNextAt = now + FrontlineEveryMs;

            var from = P.FrontlineSpawns[state.Frontline.Count % P.FrontlineSpawns.Length];

            if (Spawn(map, P.Machina, from, 3f, P.Frontline) is not { } machina)
                return;

            machina.Hate.Ensure(Nearest(fighting, machina.Position).Player.EntityId, Threat.NoticedThreat);
            state.Frontline.Add(machina);
        }

        #endregion

        #region The dishes

        private static void Dishes(State state, List<Client> members)
        {
            for (var dish = 0; dish < P.Dishes.Length; dish++)
            {
                var (objective, _, _, _, scenario, _, _) = P.Dishes[dish];
                var down = state.Infestations[dish].Select((live, i) => (live, Event: (uint)i + 1)).Where(entry => entry.live.IsDown).ToList();

                if (down.Count == 0)
                    continue;

                foreach (var client in members.Where(client => Wants(client.Player, P.PirateRadio, objective)))
                    foreach (var (_, eventId) in down)
                        Record(state, client, P.PirateRadio, scenario, eventId);
            }
        }

        #endregion

        #region The Machina chamber

        private static void Chamber(MapChannel map, State state, List<Client> members, long now)
        {
            var inside = members.Where(client => client.Player.Position.Y < FacilityCeiling &&
                Vector3.Distance(client.Player.Position, P.Capsule) <= ChamberRadius).ToList();

            foreach (var client in inside.Where(client => Wants(client.Player, P.MeansOfProduction, P.FindTheSource) &&
                         Vector3.Distance(client.Player.Position, P.Capsule) <= SourceRadius))
                Record(state, client, P.MeansOfProduction, P.ChamberScenario, P.SourceFound);

            if (!state.CapsuleDown && state.Capsule != null && state.Capsule.IsDown)
                CapsuleDestroyed(map, state, members);

            if (!state.CapsuleDown)
                Produce(map, state, inside, now);

            if (!state.CapsuleDown)
                return;

            foreach (var client in members.Where(client => Wants(client.Player, P.MeansOfProduction, P.DestroyTheCapsule)))
                Record(state, client, P.MeansOfProduction, P.ChamberScenario, P.CapsuleDestroyed);

            var dead = state.Ambush.Select((creature, i) => (creature, Event: (uint)i + 1)).Where(entry => !Alive(entry.creature)).ToList();

            foreach (var client in members.Where(client => Wants(client.Player, P.MeansOfProduction, P.Ambush)))
                foreach (var (_, eventId) in dead)
                    Record(state, client, P.MeansOfProduction, P.AmbushScenario, eventId);

            // Nylla is in the overlook hall from now on.
            if (!Alive(state.Nylla) && now >= state.NyllaAt)
            {
                state.Nylla = Spawn(map, P.NyllaControl, P.ControlCentre, 0f, P.Chamber);
                state.NyllaAt = now + NyllaBackMs;
            }
        }

        /// <summary>The chamber turns out Machina while the capsule stands and someone is there to meet them.</summary>
        private static void Produce(MapChannel map, State state, List<Client> inside, long now)
        {
            state.Production.RemoveAll(creature => !Alive(creature));

            if (inside.Count == 0 || state.Production.Count >= ProductionCap || now < state.ProductionNextAt)
                return;

            state.ProductionNextAt = now + ProductionEveryMs;

            var from = P.ProductionSpawns[state.Production.Count % P.ProductionSpawns.Length];

            if (Spawn(map, P.ProductionMachina, from, 1f, P.Chamber) is not { } machina)
                return;

            machina.Hate.Ensure(Nearest(inside, machina.Position).Player.EntityId, Threat.NoticedThreat);
            state.Production.Add(machina);
        }

        /// <summary>The capsule down: production stops, and the ambush comes at whoever is in the chamber.</summary>
        private static void CapsuleDestroyed(MapChannel map, State state, List<Client> members)
        {
            state.CapsuleDown = true;

            var near = members.Where(client => client.Player.Position.Y < FacilityCeiling &&
                Vector3.Distance(client.Player.Position, P.Capsule) <= ChamberRadius).ToList();

            foreach (var spot in P.AmbushSpawns.Take((int)P.AmbushCount))
            {
                var bane = Spawn(map, P.AmbushThrax, spot, 0f, P.Chamber);

                // A row that would not make one is one the ambush is short of: counted dead.
                state.Ambush.Add(bane);

                if (bane != null && near.Count > 0)
                    bane.Hate.Ensure(Nearest(near, bane.Position).Player.EntityId, Threat.NoticedThreat);
            }

            Logger.WriteLog(LogType.Debug, $"Pravus Research {map.InstanceId}: the fueling capsule is down; " +
                $"{state.Ambush.Count(Alive)} Bane come at the chamber.");
        }

        #endregion

        #region Helpers

        /// <summary>The living players in the copy.</summary>
        private static List<Client> Members(MapChannel map) =>
            map.ClientList?.Where(client => client?.Player != null && client.State == ClientState.Ingame &&
                    client.Player.MapChannel == map && client.Player.State != CharacterState.Dead).ToList()
            ?? new List<Client>();

        private static Client Nearest(List<Client> clients, Vector3 to) =>
            clients.OrderBy(client => Vector3.Distance(client.Player.Position, to)).First();

        /// <summary>Whether the player has the mission with the objective open.</summary>
        public static bool Wants(Manifestation player, uint missionId, uint objectiveId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Incomplete;

        /// <summary>An event of a scenario, once for each assignment of the mission.</summary>
        private static void Record(State state, Client client, uint missionId, uint scenarioId, uint eventId)
        {
            if (!client.Player.Missions.TryGetValue(missionId, out var log))
                return;

            var key = (log.AssignmentId ?? string.Empty, scenarioId, eventId);

            if (state.Told.Contains(key))
                return;

            if (MissionApplication.Instance.TryEmitScenarioProgressEvent(client, missionId, scenarioId, eventId))
                state.Told.Add(key);
        }

        /// <summary>The navmesh under a spot, if it is near enough; the spot as it is if not.</summary>
        private static Vector3 Ground(MapChannel map, Vector3 spot) => NavMeshManager.SnapToGround(map, spot);

        /// <summary>A creature of the row, put on the ground near the spot, facing the other.</summary>
        private static Creature Spawn(MapChannel map, uint dbId, Vector3 spot, float spread, Vector3 facingTo)
        {
            var creature = CreatureManager.Instance.CreateCreature(dbId, null);

            if (creature == null)
                return null;

            var at = spot;

            if (spread > 0 && NavMeshManager.RandomPointAround(map, spot, spread) is { } walkable &&
                Vector2.Distance(new Vector2(walkable.X, walkable.Z), new Vector2(spot.X, spot.Z)) <= spread + 1f)
                at = walkable;

            at = Ground(map, at);

            var facing = Math.Atan2(facingTo.X - at.X, facingTo.Z - at.Z);

            CreatureManager.Instance.SetLocation(creature, at, facing, map.MapInfo.MapContextId);
            creature.LastYaw = (float)facing;
            CellManager.Instance.AddToWorld(map, creature);
            return creature;
        }

        /// <summary>Alive and still in the world as itself.</summary>
        private static bool Alive(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying &&
            EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) && ReferenceEquals(registered, creature);

        /// <summary>Forgets every copy; for tests.</summary>
        public static void Reset() => States.Clear();

        #endregion
    }
}
