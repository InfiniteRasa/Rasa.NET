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
    using Structures;

    using K = Services.Preloader.KardashAttaColonyContent;
    using P = Services.Preloader.PhaninResearchContent;

    /// <summary>
    /// What the Phanin Research Facility (2034) does that its rows do not, in each copy of it - a
    /// squad's instance, or the map's own channel - for the missions PhaninResearchContent
    /// authors, as the wiki and Ten Ton Hammer's walkthrough tell it:
    ///
    ///  - The way in. The hospital's way down (map_link 75) takes only a player who holds the
    ///    Phanin Research Facility Keycard or has done Key Information, which gives it: "This
    ///    facility is sealed until you complete the key missions". Refused, as the client puts it
    ///    (PmMissingReqItem). A game master goes in regardless.
    ///  - The nine mainframes. A console in each of Security Alpha, Beta, Gamma and Delta, the
    ///    three laboratories, the power plant and the transmission centre; anyone may disable one
    ///    (a <see cref="HackMs"/> use), once - it goes dark and stays so - and it is The Mind of a
    ///    Madman's objective for everyone in the copy on it.
    ///  - The doors. Brann doors shut the corridor jambs into Security Gamma and Delta (stage 1),
    ///    the lower laboratories, Neuron Processing and the Neural Lab (2), and the power plant,
    ///    the transmission centre and Phanin's quarters (3). They stop players, creatures and shots
    ///    alike (ForceFields, Kind.Door). A stage's doors open once every mainframe of the stage
    ///    before is down in the copy - or a member's Mind of a Madman has them done, or the member
    ///    has done the mission, so a squad back for the other missions finds the way open.
    ///  - The caged Atta. As each laboratory's research mainframe goes down, the Kardash Atta in its
    ///    cages are let out, and go for whoever is nearest.
    ///  - The research consoles: each gives its research to a player on Do Bugs Feel Fear? or Do
    ///    Bugs Have Feelings? who has not got it.
    ///  - The medical crates: each gives one Brann Medical Supplies to each player on Medical Supply
    ///    Recovery who still wants some.
    ///  - Coming into the facility is Do Bugs' infiltration.
    /// What the copy has done goes to every member whose mission is waiting on it. Kept by the copy
    /// and not saved.
    /// </summary>
    public static class PhaninResearch
    {
        public const int HackMs = 5_000;
        public const int UseWindupMs = 2_000;

        public enum Kind { Mainframe, Research, Crate }

        internal sealed class Usable
        {
            public Kind Kind;
            public int Index;
            public DynamicObject Object;
        }

        internal sealed class State
        {
            public bool Set;
            public readonly List<Usable> Usables = new();
            public readonly ForceFields.Field[] Doors = new ForceFields.Field[P.Doors.Length];
            public readonly bool[] Disabled = new bool[P.Mainframes.Length];
            public readonly bool[] StageOpen = new bool[4];
            public readonly HashSet<uint> Freed = new();
            public readonly List<Creature> Atta = new();

            /// <summary>(player, crate): who has had what from which crate.</summary>
            public readonly HashSet<(uint Player, int Crate)> Looted = new();

            /// <summary>The events already recorded: the assignment, the mission, the scenario, the event.</summary>
            public readonly HashSet<(string Assignment, uint Mission, uint Scenario, uint Event)> Told = new();
        }

        private static readonly ConditionalWeakTable<MapChannel, State> States = new();

        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        public static bool IsFacility(MapChannel map) => map?.MapInfo?.MapContextId == P.Facility;

        internal static State StateOf(MapChannel map) => IsFacility(map) ? States.GetValue(map, _ => new State()) : null;

        /// <summary>Whether a stage's doors are open in the copy; for a test or a GM.</summary>
        public static bool StageOpen(MapChannel map, int stage) => StateOf(map)?.StageOpen[stage] ?? false;

        /// <summary>The copy's doors, in PhaninResearchContent.Doors' order; for a test.</summary>
        public static IReadOnlyList<ForceFields.Field> Doors(MapChannel map) => StateOf(map)?.Doors ?? Array.Empty<ForceFields.Field>();

        /// <summary>The copy's usables of a kind, in their table's order; for a test.</summary>
        public static IReadOnlyList<DynamicObject> Objects(MapChannel map, Kind kind) =>
            StateOf(map)?.Usables.Where(usable => usable.Kind == kind).OrderBy(usable => usable.Index).Select(usable => usable.Object).ToList()
            ?? new List<DynamicObject>();

        /// <summary>The Atta let out of the cages; for a test.</summary>
        public static IReadOnlyList<Creature> FreedAtta(MapChannel map) => StateOf(map)?.Atta ?? (IReadOnlyList<Creature>)Array.Empty<Creature>();

        #region The way in

        /// <summary>
        /// Whether a player may take a map link into a map (MapLinkManager.Fire): into the facility,
        /// only with the keycard or Key Information done. Refused, they are told why.
        /// </summary>
        public static bool MayEnter(Client client, uint mapContextId)
        {
            if (mapContextId != P.Facility || client?.Player == null || HasWayIn(client) || ChatCommandsManager.HasLevel(client, GmLevel.GameMaster))
                return true;

            CommunicatorManager.Instance.DisplaySystemMessage(client, PlayerMessage.PmMissingReqItem);
            return false;
        }

        public static bool HasWayIn(Client client) =>
            Done(client.Player, K.KeyInformation) || InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)K.KeycardClass) > 0;

        #endregion

        /// <summary>From the map channel worker.</summary>
        public static void Worker(MapChannel map)
        {
            var state = StateOf(map);

            if (state == null)
                return;

            if (!state.Set)
                SetUp(map, state);

            var members = Members(map);

            if (members.Count == 0)
                return;

            Stages(map, state, members);

            foreach (var client in members)
            {
                var player = client.Player;

                foreach (var missionId in new[] { P.DoBugsFeelFear, P.DoBugsHaveFeelings })
                    if (Wants(player, missionId, P.InfiltrateObjective))
                        Record(state, client, missionId, P.InfiltrateScenario, P.Infiltrated);

                for (var i = 0; i < P.Mainframes.Length; i++)
                    if (state.Disabled[i] && Wants(player, P.MindOfAMadman, P.Mainframes[i].Objective))
                        Record(state, client, P.MindOfAMadman, P.MainframeScenario, P.Mainframes[i].Event);
            }
        }

        #region Set up

        private static void SetUp(MapChannel map, State state)
        {
            state.Set = true;

            for (var i = 0; i < P.Mainframes.Length; i++)
                Add(state, Kind.Mainframe, i, Place(map, $"phanin:mainframe-{i}", P.MainframeClass, P.Mainframes[i].At, UseObjectState.TsState0, HackMs));

            for (var i = 0; i < P.Research.Length; i++)
                Add(state, Kind.Research, i, Place(map, $"phanin:research-{i}", P.ResearchConsoleClass, P.Research[i].At, UseObjectState.TsState0, UseWindupMs));

            for (var i = 0; i < P.Crates.Length; i++)
                Add(state, Kind.Crate, i, Place(map, $"phanin:crate-{i}", P.CrateClass, P.Crates[i], UseObjectState.TdStateClosed, UseWindupMs));

            var doorClass = ForceFields.ClassOf("branndoor");

            for (var i = 0; i < P.Doors.Length; i++)
            {
                var door = ForceFields.Place(map, doorClass, ForceFields.Side.B, P.Doors[i].At, P.Doors[i].Yaw, 1);
                door.Sealed = true;
                state.Doors[i] = door;
            }

            state.StageOpen[0] = true;

            Logger.WriteLog(LogType.Debug, $"Phanin Research {map.InstanceId}: {state.Usables.Count} usables and {P.Doors.Length} doors put down.");
        }

        private static void Add(State state, Kind kind, int index, DynamicObject obj) =>
            state.Usables.Add(new Usable { Kind = kind, Index = index, Object = obj });

        private static DynamicObject Place(MapChannel map, string key, uint classId, Vector3 position, UseObjectState initial, int windupMs)
        {
            var existing = map.DynamicObjects.FirstOrDefault(obj => obj.ScenarioKey == key);

            if (existing != null)
                return existing;

            var at = NavMeshManager.SnapToGround(map, position);
            var obj = DynamicObjectManager.Instance.CreateScenarioDynamicObject(map, (EntityClasses)classId, at, 0, key, true, (uint)windupMs);
            obj.StateId = initial;
            map.DynamicObjects.Add(obj);
            CellManager.Instance.AddToWorld(map, obj);
            obj.IsInWorld = true;
            return obj;
        }

        #endregion

        #region Stages and doors

        /// <summary>Each stage whose doors may open, opened: the stage before all down, in the copy or by a member.</summary>
        private static void Stages(MapChannel map, State state, List<Client> members)
        {
            for (var stage = 1; stage < state.StageOpen.Length; stage++)
            {
                if (state.StageOpen[stage])
                    continue;

                var before = Enumerable.Range(0, P.Mainframes.Length).Where(i => P.Mainframes[i].Stage == stage - 1).ToList();
                var down = state.StageOpen[stage - 1] && before.All(i => state.Disabled[i]) ||
                    members.Any(client => Done(client.Player, P.MindOfAMadman) ||
                        before.All(i => Completed(client.Player, P.MindOfAMadman, P.Mainframes[i].Objective)));

                if (!down)
                    break;

                Open(map, state, stage);
            }
        }

        private static void Open(MapChannel map, State state, int stage)
        {
            state.StageOpen[stage] = true;

            for (var i = 0; i < P.Doors.Length; i++)
                if (P.Doors[i].Stage <= stage && state.Doors[i] is { IsDestroyed: false } door)
                    ForceFields.Damage(door, door.Health, 0);

            Logger.WriteLog(LogType.Debug, $"Phanin Research {map.InstanceId}: the doors of stage {stage} open.");
        }

        #endregion

        #region Uses

        /// <summary>Whether a finished action is a use of one of the copy's usables (ActorActionManager).</summary>
        public static bool IsUseOf(MapChannel map, ActionData action) =>
            action != null && StateOf(map) is { } state && state.Usables.Any(usable => usable.Object.EntityId == action.SourceId);

        private static Usable UsableOf(State state, DynamicObject obj) =>
            state == null || obj == null ? null : state.Usables.FirstOrDefault(usable => ReferenceEquals(usable.Object, obj));

        /// <summary>
        /// A use about to start (DynamicObjectManager.RequestUseObjectPacket): why it is refused, or
        /// null. A dark mainframe does nothing more; a research console is for a player on the
        /// research who has not got its piece; a crate, for one on the supplies who still wants some
        /// and has not had this crate's.
        /// </summary>
        public static PlayerMessage? Refusal(Client client, DynamicObject obj)
        {
            var state = StateOf(client?.Player?.MapChannel);
            var usable = UsableOf(state, obj);

            if (usable == null)
                return null;

            var player = client.Player;

            switch (usable.Kind)
            {
                case Kind.Mainframe when state.Disabled[usable.Index]:
                    return PlayerMessage.PmUseObjectNotUsable;
                case Kind.Research:
                {
                    var research = P.Research[usable.Index];
                    var wanted = (Wants(player, P.DoBugsFeelFear, research.Objective) || Wants(player, P.DoBugsHaveFeelings, research.Objective)) &&
                        InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)research.ItemClass) == 0;
                    return wanted ? null : PlayerMessage.PmUseObjectNotUsable;
                }
                case Kind.Crate:
                {
                    var wanted = Wants(player, P.MedicalSupplyRecovery, 1) && !state.Looted.Contains((player.Id, usable.Index)) &&
                        InventoryManager.Instance.CountItemsByClass(client, (EntityClasses)P.MedicalClass) < P.SuppliesWanted;
                    return wanted ? null : PlayerMessage.PmUseObjectNotUsable;
                }
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

            if (action.IsInrerrupted || Refusal(client, obj) != null)
                return;

            if (!MapInstanceScope.Share(map, client.Player, obj) ||
                Vector3.Distance(client.Player.Position, obj.Position) > DynamicObjectManager.MaxUseDistance)
            {
                Logger.WriteLog(LogType.Security, $"{client.Player.FamilyName} was no longer at object {obj.EntityId} when the use finished; nothing done.");
                return;
            }

            Use(map, state, client, usable);
        }

        /// <summary>What a use does, once it is allowed and finished.</summary>
        internal static void Use(MapChannel map, State state, Client client, Usable usable)
        {
            switch (usable.Kind)
            {
                case Kind.Mainframe:
                    Disable(map, state, usable, client.Player.EntityId);
                    break;
                case Kind.Research:
                    Give(client, P.Research[usable.Index].ItemTemplate);
                    break;
                case Kind.Crate:
                    state.Looted.Add((client.Player.Id, usable.Index));
                    Give(client, P.MedicalTemplate);
                    break;
            }
        }

        private static void Disable(MapChannel map, State state, Usable usable, ulong by)
        {
            if (state.Disabled[usable.Index])
                return;

            state.Disabled[usable.Index] = true;
            usable.Object.StateId = UseObjectState.TsState1;
            CellManager.Instance.CellCallMethod(map, usable.Object, new UsePacket(by, UseObjectState.TsState1, 0));

            var mainframe = P.Mainframes[usable.Index];
            Logger.WriteLog(LogType.Debug, $"Phanin Research {map.InstanceId}: the {mainframe.Room} mainframe is down.");

            foreach (var (objective, cages) in P.Cages)
                if (objective == mainframe.Objective && state.Freed.Add(objective))
                    LetOut(map, state, cages);
        }

        /// <summary>A laboratory's caged Atta, out and after whoever is nearest.</summary>
        private static void LetOut(MapChannel map, State state, IEnumerable<Vector3> cages)
        {
            var members = Members(map);
            var rows = new[] { K.Soldier, K.Worker };
            var n = 0;

            foreach (var cage in cages)
            {
                var creature = CreatureManager.Instance.CreateCreature(rows[n++ % rows.Length], null);

                if (creature == null)
                    continue;

                var at = NavMeshManager.SnapToGround(map, cage);
                CreatureManager.Instance.SetLocation(creature, at, 0, map.MapInfo.MapContextId);
                CellManager.Instance.AddToWorld(map, creature);
                state.Atta.Add(creature);

                if (members.Count > 0)
                    creature.Hate.Ensure(members.OrderBy(client => Vector3.Distance(client.Player.Position, at)).First().Player.EntityId,
                        Threat.NoticedThreat);
            }
        }

        private static void Give(Client client, uint template)
        {
            var item = ItemManager.Instance.CreateFromTemplateId(template, 1);

            if (item == null || InventoryManager.Instance.GrantItemToInventory(client, item) == null)
                Logger.WriteLog(LogType.Error, $"Phanin Research: template {template} could not go to {client.Player.FamilyName}.");
        }

        #endregion

        #region Helpers

        private static List<Client> Members(MapChannel map) =>
            map.ClientList?.Where(client => client?.Player != null && client.State == ClientState.Ingame &&
                    client.Player.MapChannel == map && client.Player.State != CharacterState.Dead &&
                    client.Player.State != CharacterState.Dying).ToList()
            ?? new List<Client>();

        private static bool Done(Manifestation player, uint missionId) =>
            player.MissionHistory.TryGetValue(missionId, out var state) && state is MissionState.Completed or MissionState.Success ||
            player.Missions.TryGetValue(missionId, out var log) && log.State is MissionState.Completed or MissionState.Success;

        public static bool Wants(Manifestation player, uint missionId, uint objectiveId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Incomplete;

        private static bool Completed(Manifestation player, uint missionId, uint objectiveId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Completed;

        private static void Record(State state, Client client, uint missionId, uint scenarioId, uint eventId)
        {
            if (!client.Player.Missions.TryGetValue(missionId, out var log))
                return;

            var key = (log.AssignmentId ?? string.Empty, missionId, scenarioId, eventId);

            if (state.Told.Contains(key))
                return;

            if (MissionApplication.Instance.TryEmitScenarioProgressEvent(client, missionId, scenarioId, eventId))
                state.Told.Add(key);
        }

        public static void Reset() => States.Clear();

        #endregion
    }
}
