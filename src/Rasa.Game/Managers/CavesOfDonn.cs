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
    using Rasa.Missions.Scenes;
    using Structures;

    using D = Services.Preloader.CavesOfDonnContent;

    /// <summary>
    /// What the Caves of Donn (1506) do that their rows do not: each copy of the instance - a
    /// squad's, or the map's own channel - runs its story for whoever is in it
    /// (CavesOfDonnContent has the people, the Bane and the missions). The world is the copy's,
    /// one for everyone in it, and an event is everyone's in the copy who has its objective open.
    ///
    ///  - To the Caves: being in the instance.
    ///  - Too Quiet: the bottom of the hill; a member on Defeat Bane Ambush within
    ///    <see cref="FirstAmbushRadius"/> of Mar Fend brings the Bane down on it, all of them
    ///    dead is the objective; the second ambush likewise in the burned village
    ///    (<see cref="SecondAmbushRadius"/>); the Distress Signal, used; the Pyramid, reached.
    ///    Stone goes where the copy has got to - the entrance, Mar Fend once the first ambush is
    ///    beaten, the village once the second is, the Pyramid when someone is to follow him
    ///    there - walking where the navmesh lets him, and never in a fight.
    ///  - The bombs: the Fallen Soldier gives one to a member on What Evil Lurks who has none,
    ///    Nekala two with The Reason Why. A cave-in is used with a bomb: one goes, the cave-in
    ///    comes down and is gone <see cref="CaveInClearMs"/> later. Spirit Cave's is What Evil
    ///    Lurks' first objective.
    ///  - The Lost Foreans in Spirit Cave: found within <see cref="LostRadius"/>; when a member is
    ///    to escort them they follow the nearest such member to the extraction point, all of them
    ///    there is the objective, and one dying fails it - the rest go, and the party is back in
    ///    the cave <see cref="SurvivorsBackMs"/> later. Xang dead, then or since, secures it.
    ///  - The three tests: coming to a site is its ruin; its four braziers lit in its order wake
    ///    the site's Logos shrine, which stands asleep (out of service) until then; one out of turn
    ///    puts them all out.
    ///  - The gate in the Vitiated Shrine, locked by the three Logos: opened, it plays the
    ///    camera's pass over the shrine (script 1), the chamber's Eloh door opens and Scavenger
    ///    D'rak comes out of the Bane dig with two soldiers, at whoever opened it. In the chamber,
    ///    found within <see cref="ChamberRadius"/> of its middle: three Forean shamans each with a
    ///    Mirrored Eloh Statue, the Eloh Reliquary, the projector's conversation and the hologram
    ///    platform, which takes the user to the corridor by the entrance. Nekala waits by the
    ///    entrance once the gate is open and nobody in the copy still needs her at Mar Fend.
    ///  - Bugged to Death: an egg cluster destroyed (PlacedDestructibles) counts once for the
    ///    copy, for everyone on it, now or later.
    /// </summary>
    public static class CavesOfDonn
    {
        public const float FirstAmbushRadius = 60f;
        public const float SecondAmbushRadius = 40f;
        public const float HillRadius = 15f;
        public const float PyramidRadius = 20f;
        public const float RuinRadius = 20f;
        public const float LostRadius = 20f;
        public const float ExtractionRadius = 12f;
        public const float EscortPickupRadius = 40f;
        public const float ChamberRadius = 22f;
        public const float GateCameraRadius = 80f;
        public const uint GateCameraScript = 1;
        public const long CaveInClearMs = 3_000;
        public const long SurvivorsBackMs = 60_000;
        public const long RetryMs = 10_000;
        public const int UseWindupMs = 3_000;
        public const int BrazierWindupMs = 1_000;
        public const uint NekalaBombs = 2;

        /// <summary>Facing +X, toward the entrance, as Player.Rotation (0 faces -Z).</summary>
        public const float CorridorFacing = -MathF.PI / 2;

        public enum Kind { Brazier, CaveIn, FallenSoldier, Signal, Gate, Statue, Reliquary, Teleporter }

        /// <summary>Where Stone is: by the copy's furthest progress.</summary>
        public enum StoneStage { Entrance, MarFend, Village, Pyramid }

        /// <summary>One of the copy's usables: what it is, and which (a site and a place in its order, a cave-in, a statue).</summary>
        internal sealed class Usable
        {
            public Kind Kind;
            public int Index;
            public int Site;
            public DynamicObject Object;
        }

        /// <summary>One copy of the instance, as it stands.</summary>
        internal sealed class State
        {
            public bool Set;
            public readonly List<Usable> Usables = new();
            public readonly List<Logos> Shrines = new();

            public readonly int[] Lit = new int[3];
            public readonly bool[] Solved = new bool[3];

            public readonly bool[] Blown = new bool[3];
            public readonly long[] ClearAt = new long[3];

            public readonly List<Creature> FirstAmbush = new();
            public readonly List<Creature> SecondAmbush = new();
            public bool FirstBeaten, SecondBeaten, SignalFound;
            public StoneStage Stone = StoneStage.Entrance;
            public bool NekalaAtEntrance;

            public readonly List<Creature> Survivors = new();
            public bool Escorting, Extracted;
            public ulong EscortLead;
            public long SurvivorsBackAt;

            public bool GateOpen;
            public DynamicObject Gate, ChamberDoor, Projector;
            public bool DrakSpawned, DrakDead;
            public Creature Drak;

            public readonly HashSet<int> StatuesFound = new();
            public bool ReliquaryOpened;

            public readonly HashSet<uint> EggsDestroyed = new();

            /// <summary>Events already given, by player, assignment and event: given once; one that changed nothing is tried again later.</summary>
            public readonly Dictionary<string, long> Given = new();
        }

        private static readonly ConditionalWeakTable<MapChannel, State> States = new();

        /// <summary>The clock; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>Whether a map channel is a copy of the Caves of Donn.</summary>
        public static bool IsDonn(MapChannel map) => map?.MapInfo?.MapContextId == D.Donn;

        internal static State StateOf(MapChannel map) => IsDonn(map) ? States.GetValue(map, _ => new State()) : null;

        /// <summary>From the map channel worker.</summary>
        public static void Worker(MapChannel map)
        {
            if (!IsDonn(map))
                return;

            var state = StateOf(map);
            var now = Now();

            if (!state.Set)
                SetUp(map, state);

            ClearCaveIns(map, state, now);

            var members = Members(map);

            if (members.Count == 0)
                return;

            foreach (var client in members.Where(client => Wants(client.Player, D.ToTheCaves, 1)))
                Record(state, client, D.ToTheCaves, D.Arrived, now);

            TooQuiet(map, state, members, now);
            MoveStone(map, state);
            MoveNekala(map, state, members);
            WhatEvilLurks(map, state, members, now);
            TheReasonWhy(map, state, members, now);

            foreach (var eventId in state.EggsDestroyed)
                foreach (var client in members.Where(client => Wants(client.Player, D.BuggedToDeath, 1)))
                    Record(state, client, D.BuggedToDeath, eventId, now);
        }

        #region Set up

        /// <summary>The copy's usables, put down once; ones it already has (a copy made of a copy) are its own.</summary>
        private static void SetUp(MapChannel map, State state)
        {
            state.Set = true;

            for (var site = 0; site < D.Tests.Length; site++)
                for (var i = 0; i < D.Tests[site].Braziers.Length; i++)
                    Add(state, Kind.Brazier, i, site, Place(map, $"donn:brazier-{site}-{i}", D.BrazierClass,
                        D.Tests[site].Braziers[i], 0, UseObjectState.TsState0, BrazierWindupMs));

            for (var i = 0; i < D.CaveIns.Length; i++)
                Add(state, Kind.CaveIn, i, 0, Place(map, D.CaveIns[i].Key, D.CaveInClass, D.CaveIns[i].Position, D.CaveIns[i].Yaw,
                    UseObjectState.DoorStateClosed, UseWindupMs));

            Add(state, Kind.FallenSoldier, 0, 0, Place(map, "donn:fallen-soldier", D.FallenSoldierClass, D.FallenSoldier, 1.2,
                UseObjectState.TdStateClosed, UseWindupMs, obj => obj.NameOverrideId = D.FallenSoldierName));

            Add(state, Kind.Signal, 0, 0, Place(map, "donn:distress-signal", D.SignalClass, D.DistressSignal, 0.6,
                UseObjectState.TsState0, UseWindupMs, obj => obj.NameOverrideId = D.SignalName));

            state.Gate = Place(map, "donn:gate", D.GateClass, D.Gate, -Math.PI / 2, UseObjectState.DoorStateClosed, UseWindupMs, obj =>
            {
                obj.Lock = new UsableLock
                {
                    LockStateId = UseObjectState.DoorStateClosed,
                    UnlockedStateId = UseObjectState.DoorStateOpen
                };
                obj.Lock.LogosIds.AddRange(new[] { D.TrapLogos, D.FeelingLogos, D.HealLogos });
            });
            Add(state, Kind.Gate, 0, 0, state.Gate);

            for (var i = 0; i < D.StatueSpots.Length; i++)
                Add(state, Kind.Statue, i, 0, Place(map, $"donn:statue-{i}", D.ShamanCorpseClass, D.StatueSpots[i], 0.8 + 2.1 * i,
                    UseObjectState.TdStateClosed, UseWindupMs));

            Add(state, Kind.Reliquary, 0, 0, Place(map, "donn:reliquary", D.ReliquaryClass, D.Reliquary, 0, UseObjectState.TdStateClosed, UseWindupMs));

            Add(state, Kind.Teleporter, 0, 0, Place(map, "donn:teleporter", D.TeleporterClass, D.Teleporter, 0,
                UseObjectState.TsState1, UseWindupMs, obj => obj.NameOverrideId = D.TeleporterName));

            state.Projector = Place(map, "donn:projector", D.ProjectorClass, D.Projector, -2.302, UseObjectState.TsState0, UseWindupMs,
                obj => obj.MissionConversation = new SceneObjectConversation(D.TheReasonWhy, 7, D.ProjectorPackage, 7));

            state.ChamberDoor = Scenery(map, "donn:chamber-door", D.ChamberDoorClass, D.ChamberDoor, MathF.PI / 2, UseObjectState.DoorStateClosed);

            // The tests' Logos sleep until their braziers are lit.
            foreach (var shrine in map.DynamicObjects.OfType<Logos>())
            {
                var site = Array.FindIndex(D.Tests, test => test.Logos == shrine.Id);

                if (site < 0)
                    continue;

                state.Shrines.Add(shrine);
                SetEnabled(map, shrine, state.Solved[site]);
            }

            Logger.WriteLog(LogType.Debug, $"Caves of Donn {map.InstanceId}: {state.Usables.Count} usables put down, {state.Shrines.Count} Logos asleep.");
        }

        private static void Add(State state, Kind kind, int index, int site, DynamicObject obj) =>
            state.Usables.Add(new Usable { Kind = kind, Index = index, Site = site, Object = obj });

        /// <summary>A usable of the copy, made as <paramref name="init"/> has it before anyone is shown it.</summary>
        private static DynamicObject Place(MapChannel map, string key, uint classId, Vector3 position, double rotation, UseObjectState initial,
            int windupMs, Action<DynamicObject> init = null)
        {
            var existing = map.DynamicObjects.FirstOrDefault(obj => obj.ScenarioKey == key);

            if (existing != null)
                return existing;

            var obj = DynamicObjectManager.Instance.CreateScenarioDynamicObject(map, (EntityClasses)classId, position, rotation, key, true, (uint)windupMs);
            obj.StateId = initial;
            init?.Invoke(obj);
            map.DynamicObjects.Add(obj);
            CellManager.Instance.AddToWorld(map, obj);
            obj.IsInWorld = true;
            return obj;
        }

        /// <summary>A piece the client shows in its state and nobody uses: the chamber's door, as a tunnel's (TunnelDoors).</summary>
        private static DynamicObject Scenery(MapChannel map, string key, uint classId, Vector3 position, float yaw, UseObjectState state)
        {
            var existing = map.DynamicObjects.FirstOrDefault(obj => obj.ScenarioKey == key);

            if (existing != null)
                return existing;

            var obj = new DynamicObject
            {
                EntityClassId = (EntityClasses)classId,
                DynamicObjectType = DynamicObjectType.Scenery,
                StateId = state,
                Position = position,
                Rotation = yaw,
                MapContextId = D.Donn,
                RuntimeMapChannel = map,
                TargetCategory = TargetCategory.Object,
                ScenarioKey = key,
                Comment = "Caves of Donn: the Eloh door of the hidden chamber",
                IsInWorld = true
            };

            map.DynamicObjects.Add(obj);
            CellManager.Instance.AddToWorld(map, obj);
            return obj;
        }

        private static void SetEnabled(MapChannel map, DynamicObject obj, bool enabled)
        {
            if (obj.IsEnabled == enabled)
                return;

            DynamicObjectManager.Instance.SetScenarioInteractionEnabled(map, obj, enabled);
        }

        #endregion

        #region Use

        /// <summary>Whether a finished use is of one of the copy's usables (ActorActionManager.PerformRecovery).</summary>
        public static bool IsUseOf(MapChannel map, ActionData action) =>
            action != null && StateOf(map) is { } state && state.Usables.Any(usable => usable.Object.EntityId == action.SourceId);

        /// <summary>
        /// A use about to start (DynamicObjectManager.RequestUseObjectPacket): the message it is
        /// refused with, or null. A cave-in wants a bomb, the gate the three Logos.
        /// </summary>
        public static PlayerMessage? Refusal(Client client, DynamicObject obj)
        {
            var map = client?.Player?.MapChannel;
            var usable = UsableOf(StateOf(map), obj);

            if (usable == null)
                return null;

            switch (usable.Kind)
            {
                case Kind.CaveIn when Bombs(client) == 0:
                    CommunicatorManager.Instance.SystemMessage(client, "The cave-in is too much to dig through. A bomb would clear it.");
                    return PlayerMessage.PmUseObjectNotUsable;
                case Kind.Gate when !KnowsTheThree(client.Player):
                    return PlayerMessage.PmUseObjectUsableLocked;
                default:
                    return null;
            }
        }

        /// <summary>A finished use of one of the copy's usables: what it does.</summary>
        public static void UseRecovery(MapChannel map, ActionData action)
        {
            var state = StateOf(map);
            var usable = state?.Usables.FirstOrDefault(entry => entry.Object.EntityId == action.SourceId);

            if (usable == null)
                return;

            var obj = usable.Object;
            var client = obj.TriggeredByPlayers.FirstOrDefault(c => c.Player == action.Actor);

            if (client == null)
                return;

            obj.TriggeredByPlayers.Remove(client);

            if (action.IsInrerrupted)
                return;

            if (!MapInstanceScope.Share(map, client.Player, obj) ||
                Vector3.Distance(client.Player.Position, obj.Position) > DynamicObjectManager.MaxUseDistance)
            {
                Logger.WriteLog(LogType.Security, $"{client.Player.FamilyName} was no longer at object {obj.EntityId} when the use finished; nothing done.");
                return;
            }

            var now = Now();

            switch (usable.Kind)
            {
                case Kind.Brazier:
                    Brazier(map, state, usable, client);
                    break;
                case Kind.CaveIn:
                    BlowCaveIn(map, state, usable, client, now);
                    break;
                case Kind.FallenSoldier:
                    if (Wants(client.Player, D.WhatEvilLurks, 4) && Bombs(client) == 0)
                        GiveBombs(client, 1);
                    else
                        CommunicatorManager.Instance.SystemMessage(client, "There is nothing more of use on him.");
                    break;
                case Kind.Signal:
                    state.SignalFound = true;
                    Show(map, obj, UseObjectState.TsState1, client.Player.EntityId);
                    foreach (var member in Members(map).Where(member => Wants(member.Player, D.TooQuiet, 9)))
                        Record(state, member, D.TooQuiet, D.SignalFound, now);
                    break;
                case Kind.Gate:
                    OpenGate(map, state, client, now);
                    break;
                case Kind.Statue:
                    FindStatue(map, state, usable, now);
                    break;
                case Kind.Reliquary:
                    if (!state.ReliquaryOpened)
                    {
                        state.ReliquaryOpened = true;
                        Show(map, obj, UseObjectState.TdStateOpened, client.Player.EntityId);
                    }

                    foreach (var member in Members(map).Where(member => Wants(member.Player, D.TheReasonWhy, 14)))
                        Record(state, member, D.TheReasonWhy, D.ReliquaryOpened, now);
                    break;
                case Kind.Teleporter:
                    DynamicObjectManager.Instance.TakePassage(client, D.Corridor, CorridorFacing);
                    break;
            }
        }

        private static Usable UsableOf(State state, DynamicObject obj) =>
            state == null || obj == null ? null : state.Usables.FirstOrDefault(usable => ReferenceEquals(usable.Object, obj));

        private static void Show(MapChannel map, DynamicObject obj, UseObjectState next, ulong actor)
        {
            obj.StateId = next;
            CellManager.Instance.CellCallMethod(map, obj, new UsePacket(actor, next, 0));
        }

        #endregion

        #region The tests

        /// <summary>A brazier used: lit if it is the next in its site's order, else the site's go out.</summary>
        private static void Brazier(MapChannel map, State state, Usable usable, Client client)
        {
            var site = usable.Site;

            if (state.Solved[site] || usable.Index < state.Lit[site])
                return;

            if (usable.Index != state.Lit[site])
            {
                foreach (var lit in state.Usables.Where(entry => entry.Kind == Kind.Brazier && entry.Site == site &&
                             entry.Object.StateId == UseObjectState.TsState1))
                    Show(map, lit.Object, UseObjectState.TsState0, client.Player.EntityId);

                state.Lit[site] = 0;
                CommunicatorManager.Instance.SystemMessage(client, "The flames gutter and die. That was not the way.");
                return;
            }

            Show(map, usable.Object, UseObjectState.TsState1, client.Player.EntityId);
            state.Lit[site]++;

            if (state.Lit[site] < D.Tests[site].Braziers.Length)
                return;

            state.Solved[site] = true;

            foreach (var brazier in state.Usables.Where(entry => entry.Kind == Kind.Brazier && entry.Site == site))
                SetEnabled(map, brazier.Object, false);

            foreach (var shrine in state.Shrines.Where(shrine => shrine.Id == D.Tests[site].Logos))
                SetEnabled(map, shrine, true);

            foreach (var member in Members(map).Where(member => Vector3.Distance(member.Player.Position, D.Tests[site].Site) <= RuinRadius * 2))
                CommunicatorManager.Instance.SystemMessage(member, $"The test of {D.Tests[site].Name} is passed: its Logos stirs.");

            Logger.WriteLog(LogType.Debug, $"Caves of Donn {map.InstanceId}: the test of {D.Tests[site].Name} is passed.");
        }

        #endregion

        #region Cave-ins and bombs

        private static uint Bombs(Client client) =>
            client?.Player == null ? 0 : InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)29777);

        private static void GiveBombs(Client client, uint count)
        {
            var bomb = ItemManager.Instance.CreateFromTemplateId(D.BombTemplate, count);

            if (bomb != null)
                InventoryManager.Instance.GrantItemToInventory(client, bomb);
        }

        private static void BlowCaveIn(MapChannel map, State state, Usable usable, Client client, long now)
        {
            if (state.Blown[usable.Index])
                return;

            if (InventoryManager.Instance.RemoveItemsByClass(client, (EntityClasses)29777, 1) > 0)
            {
                CommunicatorManager.Instance.SystemMessage(client, "The cave-in is too much to dig through. A bomb would clear it.");
                return;
            }

            var obj = usable.Object;

            state.Blown[usable.Index] = true;
            state.ClearAt[usable.Index] = now + CaveInClearMs;
            CellManager.Instance.CellCallMethod(map, obj, new PerformObjectDamagePacket(PlacedUsables.ExplosionLarge, obj.EntityId));
            Show(map, obj, UseObjectState.DoorStateOpen, client.Player.EntityId);
            SetEnabled(map, obj, false);

            Logger.WriteLog(LogType.Debug, $"Caves of Donn {map.InstanceId}: {client.Player.FamilyName} blew the cave-in at {D.CaveIns[usable.Index].Comment}.");
        }

        /// <summary>A cave-in come down is taken away once its dust has settled.</summary>
        private static void ClearCaveIns(MapChannel map, State state, long now)
        {
            foreach (var usable in state.Usables.Where(entry => entry.Kind == Kind.CaveIn).ToList())
            {
                var i = usable.Index;

                if (!state.Blown[i] || state.ClearAt[i] == 0 || now < state.ClearAt[i])
                    continue;

                // Out of the copy's usables too: its entity id goes back to be handed out again.
                state.ClearAt[i] = 0;
                state.Usables.Remove(usable);
                map.DynamicObjects.Remove(usable.Object);
                CellManager.Instance.RemoveFromWorld(map, usable.Object);
                usable.Object.IsInWorld = false;
            }
        }

        /// <summary>Whether a cave-in of the copy is down: 0 Spirit Cave, 1 the Chantry tunnel, 2 the shrine tunnel.</summary>
        public static bool IsBlown(MapChannel map, int caveIn) => StateOf(map)?.Blown[caveIn] == true;

        #endregion

        #region Too Quiet

        private static void TooQuiet(MapChannel map, State state, List<Client> members, long now)
        {
            foreach (var client in members.Where(client => Wants(client.Player, D.TooQuiet, 3) &&
                         Vector3.Distance(client.Player.Position, D.HillBottom) <= HillRadius))
                Record(state, client, D.TooQuiet, D.HillScouted, now);

            Ambush(map, state, members, now, state.FirstAmbush, ref state.FirstBeaten, 4, D.FirstAmbushBeaten, D.MarFend,
                FirstAmbushRadius, D.FirstAmbushSpots, new[] { D.ShieldDrone, D.Technician, D.Soldier, D.Soldier, D.ForeanMachina, D.MachinaChampion });

            Ambush(map, state, members, now, state.SecondAmbush, ref state.SecondBeaten, 8, D.SecondAmbushBeaten, D.Village,
                SecondAmbushRadius, D.SecondAmbushSpots, new[] { D.Soldier, D.Soldier, D.Grenadier, D.Lightbender, D.Technician });

            if (state.SignalFound)
                foreach (var client in members.Where(client => Wants(client.Player, D.TooQuiet, 9)))
                    Record(state, client, D.TooQuiet, D.SignalFound, now);

            foreach (var client in members.Where(client => Wants(client.Player, D.TooQuiet, 11) &&
                         Vector3.Distance(client.Player.Position, D.Pyramid) <= PyramidRadius))
                Record(state, client, D.TooQuiet, D.ReachedPyramid, now);
        }

        /// <summary>
        /// An ambush: down on the spot when a member on its objective comes near, all of it dead
        /// the objective for everyone on it - and for anyone who comes to it later.
        /// </summary>
        private static void Ambush(MapChannel map, State state, List<Client> members, long now, List<Creature> ambush, ref bool beaten,
            uint objective, uint eventId, Vector3 spot, float radius, Vector3[] spots, uint[] lineup)
        {
            var wanting = members.Where(client => Wants(client.Player, D.TooQuiet, objective)).ToList();

            if (!beaten && ambush.Count == 0)
            {
                var near = wanting.Where(client => Vector3.Distance(client.Player.Position, spot) <= radius)
                    .OrderBy(client => Vector3.Distance(client.Player.Position, spot)).FirstOrDefault();

                if (near == null)
                    return;

                for (var i = 0; i < lineup.Length; i++)
                    if (Spawn(map, lineup[i], spots[i % spots.Length], 0f, near.Player.Position) is { } bane)
                    {
                        bane.Hate.Ensure(near.Player.EntityId, Threat.NoticedThreat);
                        ambush.Add(bane);
                    }

                Logger.WriteLog(LogType.Debug, $"Caves of Donn {map.InstanceId}: {ambush.Count} Bane ambush {near.Player.FamilyName} at {spot}.");
                return;
            }

            if (!beaten && ambush.Count > 0 && !ambush.Any(Alive))
            {
                beaten = true;
                ambush.Clear();
            }

            if (beaten)
                foreach (var client in wanting)
                    Record(state, client, D.TooQuiet, eventId, now);
        }

        /// <summary>Where the copy's progress wants Stone.</summary>
        public static StoneStage StoneStageOf(MapChannel map) => StateOf(map)?.Stone ?? StoneStage.Entrance;

        public static Vector3 SpotOf(StoneStage stage) => stage switch
        {
            StoneStage.MarFend => D.StoneAtMarFend,
            StoneStage.Village => D.StoneAtVillage,
            StoneStage.Pyramid => D.StoneAtPyramid,
            _ => D.StoneAtEntrance
        };

        private static void MoveStone(MapChannel map, State state)
        {
            var stage = state.Stone;

            if (state.FirstBeaten && stage < StoneStage.MarFend)
                stage = StoneStage.MarFend;

            if (state.SecondBeaten && stage < StoneStage.Village)
                stage = StoneStage.Village;

            if (Members(map).Any(client => Wants(client.Player, D.TooQuiet, 11)))
                stage = StoneStage.Pyramid;

            state.Stone = stage;

            var stone = Find(map, D.Stone);

            if (stone == null || stone.Hate.Ranked().Any())
                return;

            var spot = SpotOf(stage);

            // Walked there, and his own again once he has arrived.
            if (stone.Controller.ScriptedMove is { Arrived: true } && Vector3.Distance(stone.Position, spot) < 3f)
            {
                stone.Controller.ScriptedMove = null;
                stone.Controller.CurrentAction = BehaviorManager.BehaviorActionIdle;
                return;
            }

            if (Vector3.Distance(stone.Position, spot) < 3f || stone.Controller.ScriptedMove is { Arrived: false } move && move.Destination == spot)
                return;

            var facing = stage == StoneStage.Entrance ? Math.PI : Math.Atan2(D.Pyramid.X - spot.X, D.Pyramid.Z - spot.Z);

            if (stone.SpawnPool != null)
            {
                stone.SpawnPool.Position = spot;
                stone.SpawnPool.Rotation = facing;
            }

            if (!BehaviorManager.Instance.SetActionScriptedMove(map, stone, spot, facing) &&
                BehaviorManager.Instance.RestoreScriptedPose(map, stone, spot, facing))
            {
                stone.Controller.ScriptedMove = null;
                stone.Controller.CurrentAction = BehaviorManager.BehaviorActionIdle;
            }
        }

        /// <summary>Nekala: at Mar Fend, or by the entrance once the gate is open and nobody in the copy still wants her on the hill.</summary>
        private static void MoveNekala(MapChannel map, State state, List<Client> members)
        {
            state.NekalaAtEntrance = state.GateOpen && !members.Any(client =>
                Wants(client.Player, D.TooQuiet, 5) || !Active(client.Player, D.TheReasonWhy) && !Done(client.Player, D.TheReasonWhy));

            var nekala = Find(map, D.Nekala);

            if (nekala == null || nekala.Hate.Ranked().Any())
                return;

            var spot = state.NekalaAtEntrance ? D.NekalaAtEntrance : D.NekalaAtMarFend;

            if (Vector3.Distance(nekala.Position, spot) < 3f)
                return;

            var facing = state.NekalaAtEntrance ? -Math.PI / 2 : 2.0;

            if (nekala.SpawnPool != null)
            {
                nekala.SpawnPool.Position = spot;
                nekala.SpawnPool.Rotation = facing;
            }

            if (BehaviorManager.Instance.RestoreScriptedPose(map, nekala, spot, facing))
            {
                nekala.Controller.ScriptedMove = null;
                nekala.Controller.CurrentAction = BehaviorManager.BehaviorActionIdle;
            }
        }

        #endregion

        #region What Evil Lurks

        private static void WhatEvilLurks(MapChannel map, State state, List<Client> members, long now)
        {
            if (state.Blown[0])
                foreach (var client in members.Where(client => Wants(client.Player, D.WhatEvilLurks, 4)))
                    Record(state, client, D.WhatEvilLurks, D.CaveOpened, now);

            var found = state.Extracted ? D.Extraction : D.SpiritCave;

            foreach (var client in members.Where(client => Wants(client.Player, D.WhatEvilLurks, 3) &&
                         Vector3.Distance(client.Player.Position, found) <= LostRadius))
                Record(state, client, D.WhatEvilLurks, D.LostFound, now);

            Escort(map, state, members, now);

            if (state.Extracted)
                foreach (var client in members.Where(client => Wants(client.Player, D.WhatEvilLurks, 2)))
                    Record(state, client, D.WhatEvilLurks, D.Extracted, now);

            if (Find(map, D.Xang) == null)
                foreach (var client in members.Where(client => Wants(client.Player, D.WhatEvilLurks, 1)))
                    Record(state, client, D.WhatEvilLurks, D.XangDown, now);
        }

        /// <summary>The Lost Foreans: put down in the cave, walked out behind a member to the extraction point.</summary>
        private static void Escort(MapChannel map, State state, List<Client> members, long now)
        {
            if (state.Extracted)
                return;

            if (state.Survivors.Count == 0)
            {
                if (now < state.SurvivorsBackAt)
                    return;

                for (var i = 0; i < D.LostForeanSpots.Length; i++)
                    if (Spawn(map, i == 0 ? D.LostForeanLeader : D.LostForean, D.LostForeanSpots[i], 0f, D.Extraction) is { } survivor)
                        state.Survivors.Add(survivor);

                state.Escorting = false;
                state.EscortLead = 0;
                return;
            }

            var escorting = members.Where(client => Wants(client.Player, D.WhatEvilLurks, 2)).ToList();

            if (state.Escorting && state.Survivors.Any(survivor => !Alive(survivor)))
            {
                foreach (var client in escorting)
                    Record(state, client, D.WhatEvilLurks, D.SurvivorLost, now);

                foreach (var survivor in state.Survivors.Where(Alive))
                    CellManager.Instance.RemoveCreatureFromWorld(map, survivor);

                state.Survivors.Clear();
                state.Escorting = false;
                state.EscortLead = 0;
                state.SurvivorsBackAt = now + SurvivorsBackMs;
                Logger.WriteLog(LogType.Debug, $"Caves of Donn {map.InstanceId}: a Lost Forean died on the way out.");
                return;
            }

            if (!state.Escorting)
            {
                // Lost before anyone was to walk them out: back in the cave in a while.
                if (state.Survivors.Any(survivor => !Alive(survivor)))
                {
                    foreach (var survivor in state.Survivors.Where(Alive))
                        CellManager.Instance.RemoveCreatureFromWorld(map, survivor);

                    state.Survivors.Clear();
                    state.SurvivorsBackAt = now + SurvivorsBackMs;
                    return;
                }

                var lead = escorting.Where(client => Vector3.Distance(client.Player.Position, state.Survivors[0].Position) <= EscortPickupRadius)
                    .OrderBy(client => Vector3.Distance(client.Player.Position, state.Survivors[0].Position)).FirstOrDefault();

                if (lead == null)
                    return;

                state.Escorting = true;
                Follow(state, lead.Player);
                Logger.WriteLog(LogType.Debug, $"Caves of Donn {map.InstanceId}: the Lost Foreans follow {lead.Player.FamilyName} out.");
                return;
            }

            if (state.Survivors.All(survivor => Vector3.Distance(survivor.Position, D.Extraction) <= ExtractionRadius))
            {
                state.Extracted = true;
                state.Escorting = false;

                for (var i = 0; i < state.Survivors.Count; i++)
                {
                    var survivor = state.Survivors[i];

                    if (i == 0)
                    {
                        survivor.Controller.ActionFollow.FollowTargetId = 0;
                        BehaviorManager.Instance.RestoreScriptedPose(map, survivor, D.Extraction, Math.PI);
                        continue;
                    }

                    CellManager.Instance.RemoveCreatureFromWorld(map, survivor);
                }

                state.Survivors.RemoveRange(1, state.Survivors.Count - 1);
                Logger.WriteLog(LogType.Debug, $"Caves of Donn {map.InstanceId}: the Lost Foreans are at the extraction point.");
                return;
            }

            // The one they follow gone, dead or no longer on the objective: the nearest who is.
            var current = escorting.FirstOrDefault(client => client.Player.EntityId == state.EscortLead);

            if (current == null)
            {
                var next = escorting.OrderBy(client => Vector3.Distance(client.Player.Position, state.Survivors[0].Position)).FirstOrDefault();

                if (next == null)
                {
                    state.Escorting = false;
                    state.EscortLead = 0;

                    for (var i = 0; i < state.Survivors.Count; i++)
                    {
                        state.Survivors[i].Controller.ActionFollow.FollowTargetId = 0;
                        BehaviorManager.Instance.RestoreScriptedPose(map, state.Survivors[i], D.LostForeanSpots[i], 0);
                    }

                    return;
                }

                Follow(state, next.Player);
            }
        }

        private static void Follow(State state, Manifestation lead)
        {
            state.EscortLead = lead.EntityId;

            foreach (var survivor in state.Survivors.Where(Alive))
            {
                survivor.Controller.ScriptedMove = null;
                BehaviorManager.Instance.SetActionFollow(survivor, lead.EntityId);
            }
        }

        /// <summary>The copy's Lost Foreans, for a test or a GM.</summary>
        public static IReadOnlyList<Creature> Survivors(MapChannel map) => StateOf(map)?.Survivors ?? new List<Creature>();

        #endregion

        #region The Reason Why

        private static void TheReasonWhy(MapChannel map, State state, List<Client> members, long now)
        {
            for (var site = 0; site < D.Tests.Length; site++)
            {
                var test = D.Tests[site];
                var objective = (uint)site + 1;

                foreach (var client in members.Where(client => Wants(client.Player, D.TheReasonWhy, objective) &&
                             Vector3.Distance(client.Player.Position, test.Site) <= RuinRadius))
                    Record(state, client, D.TheReasonWhy, test.RuinEvent, now);
            }

            if (!state.GateOpen)
                return;

            foreach (var client in members.Where(client => Wants(client.Player, D.TheReasonWhy, 10)))
                Record(state, client, D.TheReasonWhy, D.GateOpened, now);

            foreach (var client in members.Where(client => Wants(client.Player, D.TheReasonWhy, 16) && InChamber(client.Player.Position)))
                Record(state, client, D.TheReasonWhy, D.ChamberFound, now);

            foreach (var statue in state.StatuesFound)
                foreach (var client in members.Where(client => Wants(client.Player, D.TheReasonWhy, 19)))
                    Record(state, client, D.TheReasonWhy, D.FirstStatue + (uint)statue, now, D.StatueScenario);

            if (state.ReliquaryOpened)
                foreach (var client in members.Where(client => Wants(client.Player, D.TheReasonWhy, 14)))
                    Record(state, client, D.TheReasonWhy, D.ReliquaryOpened, now);

            if (state.DrakSpawned && !state.DrakDead && !Alive(state.Drak))
            {
                state.DrakDead = true;

                foreach (var client in members.Where(client => Active(client.Player, D.TheReasonWhy) &&
                             InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)10666) == 0))
                    if (ItemManager.Instance.CreateFromTemplateId(D.OrdersTemplate, 1) is { } orders)
                        InventoryManager.Instance.GrantItemToInventory(client, orders);
            }

            if (state.DrakDead)
                foreach (var client in members.Where(client => Wants(client.Player, D.TheReasonWhy, 17)))
                    Record(state, client, D.TheReasonWhy, D.DrakDown, now);
        }

        /// <summary>Whether a position is in the hidden chamber, past its door.</summary>
        public static bool InChamber(Vector3 position) =>
            position.X > D.ChamberDoor.X + 2f && Vector2.Distance(new Vector2(position.X, position.Z), new Vector2(D.Chamber.X, D.Chamber.Z)) <= ChamberRadius &&
            Math.Abs(position.Y - D.Chamber.Y) < 8f;

        public static bool KnowsTheThree(Manifestation player) =>
            player?.Logos != null && new[] { D.TrapLogos, D.FeelingLogos, D.HealLogos }.All(logos => player.Logos.Contains(logos));

        public static bool IsGateOpen(MapChannel map) => StateOf(map)?.GateOpen == true;

        private static void OpenGate(MapChannel map, State state, Client client, long now)
        {
            if (state.GateOpen || !KnowsTheThree(client.Player))
                return;

            state.GateOpen = true;
            state.Gate.Lock.Unlocked = true;
            Show(map, state.Gate, UseObjectState.DoorStateOpen, client.Player.EntityId);
            SetEnabled(map, state.Gate, false);
            Show(map, state.ChamberDoor, UseObjectState.DoorStateOpen, client.Player.EntityId);

            foreach (var member in Members(map).Where(member => Vector3.Distance(member.Player.Position, D.Gate) <= GateCameraRadius))
                CameraScripts.Run(member, GateCameraScript);

            foreach (var member in Members(map).Where(member => Wants(member.Player, D.TheReasonWhy, 10)))
                Record(state, member, D.TheReasonWhy, D.GateOpened, now);

            // D'rak has been waiting for someone to open it for him.
            state.DrakSpawned = true;
            state.Drak = Spawn(map, D.Drak, D.DrakSpot, 0f, D.Gate);
            state.Drak?.Hate.Ensure(client.Player.EntityId, Threat.NoticedThreat);

            foreach (var spot in D.DrakGuardSpots)
                Spawn(map, D.Soldier, spot, 0f, D.Gate)?.Hate.Ensure(client.Player.EntityId, Threat.NoticedThreat);

            Logger.WriteLog(LogType.Debug, $"Caves of Donn {map.InstanceId}: {client.Player.FamilyName} opened the gate; D'rak comes out of the dig.");
        }

        private static void FindStatue(MapChannel map, State state, Usable usable, long now)
        {
            state.StatuesFound.Add(usable.Index);

            foreach (var member in Members(map).Where(member => Wants(member.Player, D.TheReasonWhy, 19)))
            {
                var classId = (EntityClasses)(29766 + usable.Index);

                if (InventoryManager.Instance.CountItemsByClass(member, classId) == 0 &&
                    ItemManager.Instance.CreateFromTemplateId(D.StatueTemplates[usable.Index], 1) is { } statue)
                    InventoryManager.Instance.GrantItemToInventory(member, statue);

                Record(state, member, D.TheReasonWhy, D.FirstStatue + (uint)usable.Index, now, D.StatueScenario);
            }
        }

        /// <summary>
        /// A mission changed for a player (SceneApplication.MissionChanged): The Reason Why taken
        /// from Nekala comes with her two bombs.
        /// </summary>
        public static void MissionChanged(Client client, uint missionId, string change)
        {
            if (missionId == D.TheReasonWhy && change == "Accepted" && client?.Player != null)
                GiveBombs(client, NekalaBombs);
        }

        #endregion

        #region Bugged to Death

        /// <summary>A world_destructible destroyed (PlacedDestructibles.Hit): one of the egg clusters, for the copy.</summary>
        public static void Destroyed(MapChannel map, uint rowId)
        {
            var state = StateOf(map);
            var eventId = D.EggClusterEvent(rowId);

            if (state == null || eventId == 0 || !state.EggsDestroyed.Add(eventId))
                return;

            var now = Now();

            foreach (var client in Members(map).Where(client => Wants(client.Player, D.BuggedToDeath, 1)))
                Record(state, client, D.BuggedToDeath, eventId, now);
        }

        #endregion

        #region Helpers

        /// <summary>The living players in the copy.</summary>
        private static List<Client> Members(MapChannel map) =>
            map.ClientList?.Where(client => client?.Player != null && client.State == ClientState.Ingame &&
                    client.Player.MapChannel == map && client.Player.State != CharacterState.Dead).ToList()
            ?? new List<Client>();

        private static IEnumerable<Creature> Creatures(MapChannel map) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().Where(Alive).ToList();

        /// <summary>A creature of the row alive in the copy; null if there is none.</summary>
        public static Creature Find(MapChannel map, uint dbId) => Creatures(map).FirstOrDefault(creature => creature.DbId == dbId);

        private static bool Active(Manifestation player, uint missionId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active;

        private static bool Done(Manifestation player, uint missionId) =>
            player.MissionHistory.TryGetValue(missionId, out var state) && state == MissionState.Completed;

        /// <summary>Whether the player has the mission with the objective open.</summary>
        public static bool Wants(Manifestation player, uint missionId, uint objectiveId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Incomplete;

        /// <summary>The event to the player's mission: once for an assignment, or again a while after one that changed nothing.</summary>
        private static void Record(State state, Client client, uint missionId, uint eventId, long now, uint scenarioId = D.Scenario)
        {
            if (!client.Player.Missions.TryGetValue(missionId, out var log))
                return;

            var key = $"{client.Player.Id}:{log.AssignmentId}:{missionId}:{scenarioId}:{eventId}";

            if (state.Given.TryGetValue(key, out var retryAt) && now < retryAt)
                return;

            state.Given[key] = MissionApplication.Instance.TryEmitScenarioProgressEvent(client, missionId, scenarioId, eventId)
                ? long.MaxValue
                : now + RetryMs;
        }

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

            at = NavMeshManager.SnapToGround(map, at);

            var facing = Math.Atan2(facingTo.X - at.X, facingTo.Z - at.Z);

            CreatureManager.Instance.SetLocation(creature, at, facing, map.MapInfo.MapContextId);
            creature.LastYaw = (float)facing;
            CellManager.Instance.AddToWorld(map, creature);
            return creature;
        }

        /// <summary>Alive and still in the world as itself (as FithikEggClusters counts its hatchlings).</summary>
        private static bool Alive(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying &&
            EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) && ReferenceEquals(registered, creature);

        /// <summary>Forgets every copy; for tests.</summary>
        public static void Reset() => States.Clear();

        #endregion
    }
}
