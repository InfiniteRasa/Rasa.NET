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

    using M = Services.Preloader.MagmaCavernsContent;

    /// <summary>
    /// What Magma Caverns (1977) does that its rows do not: each copy of it - a squad's
    /// instance, or the map's own channel - takes one side, and runs that side's story for
    /// whoever is in it (MagmaCavernsContent has the people, the creatures and the missions).
    ///
    ///  - The side. Until it has one the Rebels and Penumbra stand Neutral to everyone. The
    ///    members on Consensus vote at Kane; once every one of them has, the majority decides,
    ///    a tie the first to vote, and every member waiting has the copy's decision. A member who
    ///    comes in on Final Chance or Executioner decides an undecided copy for their arc's side.
    ///    Then the copy's side stands Friendly, the other Hostile - so the two fight each other -
    ///    and the officers of the copy's side and Kane take no harm. Each side's hospital is
    ///    offered to those who die in a copy of that side.
    ///  - The way out of the station: Arkhara's teleporter takes a Rebel copy to the camp and
    ///    back; Penumbra's way is the airlock out the back.
    ///  - The assaults: Penumbra's two on a Rebel copy, the Rebels' on a Penumbra one. Each
    ///    begins when a member has its objective open: its wormhole opens and sends
    ///    <see cref="WaveSize"/> every <see cref="WaveEveryMs"/>, no more than
    ///    <see cref="MaxWaves"/> waves, which walk its route, fighting what they meet; the client's
    ///    message goes out as the first reaches each stage. One reaching the last stage (the camp,
    ///    the door, a landing zone) loses the bonus that stage guards. Shutting the wormhole is its
    ///    objective; shut with none of it left, the assault is broken, and each bonus not lost is
    ///    won, its credits or experience given.
    ///  - The doors between the oasis and the Grounds and the Grounds and the east corridor: a
    ///    Rebel copy's open once the assault its officer holds is broken, refused with the
    ///    client's "speak to Sudevi" or "Nelby" before; a Penumbra copy's are blown open by a
    ///    member on their objective.
    ///  - The optional wormholes: a member on one sets it up, and after that it takes anyone of
    ///    the copy's side to its far end.
    ///  - The GPS beacons at the landing zones, Salyard's laptop, the prison block's forcefield
    ///    (its subjects Hostile once it is down, and all of them dead an objective), the places to reach, Creature Strike's Mantas
    ///    and Ticks, and Ember, whose death shows his hoard.
    /// What is done stays done while the copy stands. Members who come later are given what the
    /// copy has done. A copy that is closed and opened again starts over, undecided.
    /// </summary>
    public static class MagmaCaverns
    {
        public const int UseWindupMs = 2_000;

        public const long WaveEveryMs = 30_000;
        public const int WaveSize = 3, MaxWaves = 6;
        public const float StageRadius = 6f, WaveSpread = 3f;

        public const float BackDoorRadius = 10f, LandingZoneRadius = 45f, FrontRadius = 20f, StrongholdRadius = 35f,
            CampRadius = 25f, LairRadius = 30f, TreasureShout = 150f;

        public const int StrikeMantas = 3, StrikeTicks = 3;
        public const int TreasureCredits = 25_000;

        public const long RetryMs = 10_000;

        public enum Side { None, Rebel, Penumbra }

        public enum Kind { StationTeleporter, CampTeleporter, Door, Wormhole, Setup, Beacon, Laptop, Cells, Treasure }

        /// <summary>The doors: the west one (the oasis's) and the east one (the Grounds' east corridor).</summary>
        public const int WestDoor = 0, EastDoor = 1;

        /// <summary>The optional wormholes' bases: the Grounds', the Cauldron's.</summary>
        public const int GroundsSetup = 0, CauldronSetup = 1;

        /// <summary>A stage of an assault's route, its message, and the bonus it loses (0: none).</summary>
        public sealed record Stage(Vector3 At, PlayerMessage? Message, int Breach = 0);

        /// <summary>A bonus of an assault: the objective, the stage it guards, what it gives and says, its two events.</summary>
        public sealed record Bonus(uint Objective, int Guards, int Credits, uint Experience, PlayerMessage Message, uint Won, uint Lost);

        /// <summary>One assault: its copy's side, mission and objectives, wormhole, route, soldiers and bonuses.</summary>
        public sealed record AssaultDef(string Name, Side Side, uint Mission, uint StartObjective, uint ShutObjective, uint ShutEvent,
            uint OverObjective, uint OverEvent, PlayerMessage Begin, Vector3 Wormhole, Stage[] Route, uint[] Rows, Bonus[] Bonuses);

        public static readonly AssaultDef[] Assaults =
        {
            new("Penumbra's first assault", Side.Rebel, M.FinalChance, M.FcHoldCamp, M.FcShutWormhole1, M.FcWormhole1Shut,
                M.FcHoldCamp, M.FcAssault1Over, PlayerMessage.PmMagmaPenumbraAssault1Begin, M.OasisNorth,
                new[]
                {
                    new Stage(M.Sakra, PlayerMessage.PmMagmaPenumbraAssault1Area1),
                    new Stage(M.SudeviSpot, PlayerMessage.PmMagmaPenumbraAssault1Area2),
                    new Stage(M.TunnelOasisEnd, null),
                    new Stage(M.TunnelCampEnd, PlayerMessage.PmMagmaPenumbraAssault1Area3Camp),
                    new Stage(M.Camp, PlayerMessage.PmMagmaPenumbraAssault1Area4CampInfiltrated, 1)
                },
                new[] { M.BlackOps, M.ShockTrooper, M.Spy, M.SpecOps },
                new[] { new Bonus(M.FcBonus1, 1, M.BonusCredits, 0, PlayerMessage.PmMagmaRebelBonus1Recieved, M.FcBonus1Won, M.FcBonus1Lost) }),
            new("Penumbra's second assault", Side.Rebel, M.FinalChance, M.FcHoldDoor, M.FcShutWormhole2, M.FcWormhole2Shut,
                M.FcHoldDoor, M.FcAssault2Over, PlayerMessage.PmMagmaPenumbraAssault2Begin, M.GroundsWormhole,
                new[]
                {
                    new Stage(M.NelbySpot, PlayerMessage.PmMagmaPenumbraAssault2Area1),
                    new Stage(M.GroundsSetup, PlayerMessage.PmMagmaPenumbraAssault2Area2),
                    new Stage(M.WestDoorWay, PlayerMessage.PmMagmaPenumbraAssault2Fail, 1)
                },
                new[] { M.ShockTrooper, M.BlackOps, M.SpecOps, M.Spy },
                new[] { new Bonus(M.FcBonus2, 1, 0, M.BonusExperience, PlayerMessage.PmMagmaRebelBonus2Recieved, M.FcBonus2Won, M.FcBonus2Lost) }),
            new("The Rebel assault", Side.Penumbra, M.Executioner, M.ExStopAssault, M.ExShutWormhole, M.ExWormholeShut,
                M.ExStopAssault, M.ExAssaultOver, PlayerMessage.PmMagmaRebelAssaultBegin, M.RebelWormhole,
                new[]
                {
                    new Stage(M.RebelApproach, PlayerMessage.PmMagmaRebelAssaultLz1),
                    new Stage(M.LandingZone1, PlayerMessage.PmMagmaRebelAssaultBeacon1, 1),
                    new Stage(M.CauldronRim, PlayerMessage.PmMagmaRebelAssaultLz2),
                    new Stage(M.LandingZone2, PlayerMessage.PmMagmaRebelAssaultBeacon2, 2)
                },
                new[] { M.Guerrilla, M.Marine, M.Assassin, M.ConvertedThrax },
                new[]
                {
                    new Bonus(M.ExBonus1, 1, M.BonusCredits, 0, PlayerMessage.PmMagmaPenumbraBonus1Recieved, M.ExBonus1Won, M.ExBonus1Lost),
                    new Bonus(M.ExBonus2, 2, 0, M.BonusExperience, PlayerMessage.PmMagmaPenumbraBonus2Recieved, M.ExBonus2Won, M.ExBonus2Lost)
                })
        };

        /// <summary>The far end of each optional wormhole, by base, for a copy of each side (null: not that side's).</summary>
        public static Vector3? FarEnd(Side side, int setup) => (side, setup) switch
        {
            (Side.Rebel, GroundsSetup) => M.RebelWormhole,
            (Side.Penumbra, GroundsSetup) => M.OasisNorth,
            (Side.Penumbra, CauldronSetup) => M.EastCorridor,
            _ => null
        };

        /// <summary>One of the copy's usables: what it is, and which.</summary>
        internal sealed class Usable
        {
            public Kind Kind;
            public int Index;
            public DynamicObject Object;
        }

        internal sealed class Walker
        {
            public Creature Creature;
            public int Stage;
        }

        internal sealed class Assault
        {
            public bool Started, Shut, Over;
            public int Waves, Breach;
            public long NextWaveAt;
            public Usable Wormhole;
            public readonly List<Walker> Walkers = new();
            public readonly HashSet<int> Announced = new();
        }

        /// <summary>One copy of the caverns, as it stands.</summary>
        internal sealed class State
        {
            public bool Set;
            public Side Side;
            public readonly List<uint> VoteOrder = new();
            public readonly List<Usable> Usables = new();
            public readonly Assault[] Assaults = { new Assault(), new Assault(), new Assault() };
            public readonly bool[] DoorOpen = new bool[2], WormholeSet = new bool[2], BeaconPlaced = new bool[2];
            public bool LaptopRead, Released, SubjectsDead;
            public readonly List<Creature> Loosed = new();

            public bool StrikeStarted, StrikeOver;
            public readonly List<Creature> Strike = new();

            public Creature Ember;
            public bool EmberDown;
            public readonly HashSet<uint> TreasureTakenBy = new();

            /// <summary>Events already given, by player, assignment and event: given once; one that changed nothing is tried again later.</summary>
            public readonly Dictionary<string, long> Given = new();
        }

        private static readonly ConditionalWeakTable<MapChannel, State> States = new();

        /// <summary>The clock; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>Whether a map channel is a copy of the caverns.</summary>
        public static bool IsCaverns(MapChannel map) => map?.MapInfo?.MapContextId == M.MagmaCaverns;

        internal static State StateOf(MapChannel map) => IsCaverns(map) ? States.GetValue(map, _ => new State()) : null;

        /// <summary>The copy's side.</summary>
        public static Side SideOf(MapChannel map) => StateOf(map)?.Side ?? Side.None;

        /// <summary>From the map channel worker.</summary>
        public static void Worker(MapChannel map)
        {
            if (!IsCaverns(map))
                return;

            var state = StateOf(map);
            var now = Now();

            if (!state.Set)
                SetUp(map, state);

            var members = Members(map);

            Decide(map, state, members);
            Ember(map, state, members);

            if (members.Count > 0)
            {
                Consensus(state, members, now);
                Places(state, members, now);

                for (var i = 0; i < Assaults.Length; i++)
                    RunAssault(map, state, Assaults[i], state.Assaults[i], members, now);

                Strike(map, state, members, now);
                Doors(map, state, members, now);
                Handled(state, members, now);
            }

            // Last, so that what came in this tick stands as its side does from the first.
            ApplySides(map, state);
        }

        #region Set up

        /// <summary>The copy's teleporters, doors, optional wormholes, beacons, laptop and cells, and Ember, put down once.</summary>
        private static void SetUp(MapChannel map, State state)
        {
            state.Set = true;

            Add(state, Kind.StationTeleporter, 0, Place(map, "magma:station-teleporter", M.TeleporterClass, M.StationTeleporter, 0, UseObjectState.TsState1));
            Add(state, Kind.CampTeleporter, 0, Place(map, "magma:camp-teleporter", M.TeleporterClass, M.CampTeleporter, 0, UseObjectState.TsState1));

            Add(state, Kind.Door, WestDoor, Place(map, "magma:west-door", M.DoorClass, M.WestDoor, 0, UseObjectState.DoorStateClosed, ground: false));
            Add(state, Kind.Door, EastDoor, Place(map, "magma:east-door", M.DoorClass, M.EastDoor, 0, UseObjectState.DoorStateClosed, ground: false));

            Add(state, Kind.Setup, GroundsSetup, Place(map, "magma:grounds-wormhole", M.WormholeClass, M.GroundsSetup, 0, UseObjectState.TsState0));
            Add(state, Kind.Setup, CauldronSetup, Place(map, "magma:cauldron-wormhole", M.WormholeClass, M.CauldronSetup, 0, UseObjectState.TsState0));

            Add(state, Kind.Beacon, 0, Place(map, "magma:beacon-1", M.BeaconClass, M.LandingZone1, 0, UseObjectState.IaStateActive));
            Add(state, Kind.Beacon, 1, Place(map, "magma:beacon-2", M.BeaconClass, M.LandingZone2, 0, UseObjectState.IaStateActive));

            Add(state, Kind.Laptop, 0, Place(map, "magma:laptop", M.LaptopClass, M.Laptop, -1.6, UseObjectState.TsState1));
            Add(state, Kind.Cells, 0, Place(map, "magma:cells", M.CellsClass, M.Cells, 0, UseObjectState.DoorStateClosed));

            state.Ember = Spawn(map, M.Ember, M.Lair, 0f, M.Treasure);

            Logger.WriteLog(LogType.Debug, $"Magma Caverns {map.InstanceId}: {state.Usables.Count} usables and Ember put down.");
        }

        private static void Add(State state, Kind kind, int index, DynamicObject obj) =>
            state.Usables.Add(new Usable { Kind = kind, Index = index, Object = obj });

        private static DynamicObject Place(MapChannel map, string key, uint classId, Vector3 position, double rotation, UseObjectState initial,
            bool ground = true)
        {
            var existing = map.DynamicObjects.FirstOrDefault(obj => obj.ScenarioKey == key);

            if (existing != null)
                return existing;

            var at = ground ? NavMeshManager.SnapToGround(map, position) : position;
            var obj = DynamicObjectManager.Instance.CreateScenarioDynamicObject(map, (EntityClasses)classId, at, rotation, key, true, UseWindupMs);
            obj.StateId = initial;
            map.DynamicObjects.Add(obj);
            CellManager.Instance.AddToWorld(map, obj);
            obj.IsInWorld = true;
            return obj;
        }

        private static void SetEnabled(MapChannel map, DynamicObject obj, bool enabled)
        {
            if (obj == null || obj.IsEnabled == enabled)
                return;

            DynamicObjectManager.Instance.SetScenarioInteractionEnabled(map, obj, enabled);
        }

        private static void Show(MapChannel map, DynamicObject obj, UseObjectState next, ulong actor)
        {
            obj.StateId = next;
            CellManager.Instance.CellCallMethod(map, obj, new UsePacket(actor, next, 0));
        }

        #endregion

        #region The side

        /// <summary>
        /// An undecided copy: a member on an arc decides it for the arc's side; else, once every
        /// member on Consensus has voted, the majority, a tie the first to vote.
        /// </summary>
        private static void Decide(MapChannel map, State state, List<Client> members)
        {
            if (state.Side != Side.None)
                return;

            var arc = members.FirstOrDefault(client => Active(client.Player, M.FinalChance) || Active(client.Player, M.Executioner));

            if (arc != null)
            {
                Choose(map, state, members, Active(arc.Player, M.FinalChance) ? Side.Rebel : Side.Penumbra);
                return;
            }

            var on = members.Where(client => Active(client.Player, M.Consensus)).ToList();
            var votes = new Dictionary<uint, Side>();

            foreach (var client in on)
            {
                var player = client.Player;

                if (!Done(player, M.Consensus, M.SpeakToKane) || !Wants(player, M.Consensus, M.WaitForSquad))
                    continue;

                var vote = VoteOf(player);

                if (vote == Side.None)
                    continue;

                votes[player.Id] = vote;

                if (!state.VoteOrder.Contains(player.Id))
                    state.VoteOrder.Add(player.Id);
            }

            if (on.Count == 0 || votes.Count < on.Count(client => !Done(client.Player, M.Consensus, M.WaitForSquad)))
                return;

            Choose(map, state, members, Tally(votes, state.VoteOrder));
        }

        /// <summary>The majority of the votes; a tie, the first of them to have voted.</summary>
        public static Side Tally(IReadOnlyDictionary<uint, Side> votes, IReadOnlyList<uint> order)
        {
            var rebels = votes.Values.Count(vote => vote == Side.Rebel);
            var penumbra = votes.Values.Count(vote => vote == Side.Penumbra);

            if (rebels != penumbra)
                return rebels > penumbra ? Side.Rebel : Side.Penumbra;

            var first = order.FirstOrDefault(votes.ContainsKey);
            return votes.TryGetValue(first, out var side) ? side : Side.None;
        }

        /// <summary>A member's answer to Kane, as their flag holds it.</summary>
        public static Side VoteOf(Manifestation player)
        {
            var flags = player.PlayerFlags;

            if (flags == null || !flags.TryGetValue(M.VoteFlag, out var value))
                return Side.None;

            return value switch
            {
                M.RebelSide => Side.Rebel,
                M.PenumbraSide => Side.Penumbra,
                _ => Side.None
            };
        }

        private static void Choose(MapChannel map, State state, List<Client> members, Side side)
        {
            if (side == Side.None)
                return;

            state.Side = side;

            foreach (var member in members)
                CommunicatorManager.Instance.SystemMessage(member, side == Side.Rebel
                    ? "Your squad has chosen to fight for the Rebels."
                    : "Your squad has chosen to join Penumbra against the Rebels.");

            Logger.WriteLog(LogType.Debug, $"Magma Caverns {map.InstanceId}: the copy is the {side}'.");
        }

        /// <summary>For a test or a GM: the copy's side set.</summary>
        public static void SetSide(MapChannel map, Side side)
        {
            if (StateOf(map) is { } state)
                state.Side = side;
        }

        /// <summary>The members waiting on the vote are given the copy's decision.</summary>
        private static void Consensus(State state, List<Client> members, long now)
        {
            if (state.Side == Side.None)
                return;

            var sided = state.Side == Side.Rebel ? M.SidedRebel : M.SidedPenumbra;

            foreach (var client in members.Where(client => Wants(client.Player, M.Consensus, M.WaitForSquad)))
                Record(state, client, M.Consensus, sided, now);
        }

        /// <summary>What each side's people stand as to the copy, and who of them takes no harm.</summary>
        private static void ApplySides(MapChannel map, State state)
        {
            foreach (var creature in Creatures(map))
            {
                var id = creature.DbId;
                TargetCategory? category = null;

                if (id == M.Kane)
                    category = TargetCategory.Friendly;
                else if (Array.IndexOf(M.RebelRows, id) >= 0)
                    category = Standing(state.Side, Side.Rebel);
                else if (Array.IndexOf(M.PenumbraRows, id) >= 0)
                    category = Standing(state.Side, Side.Penumbra);
                else if (id == M.PacifiedThrax)
                    category = state.Side == Side.Penumbra && state.Released ? TargetCategory.Hostile : TargetCategory.Neutral;

                if (category == null)
                    continue;

                SetCategory(creature, category.Value);

                var immune = id == M.Kane || Array.IndexOf(M.Officers, id) >= 0 && category != TargetCategory.Hostile;

                if (creature.ImmuneToAllDamage != immune)
                    creature.ImmuneToAllDamage = immune;
            }
        }

        /// <summary>How one side's people stand in a copy of a side: its own Friendly, the other's Hostile; Neutral undecided.</summary>
        public static TargetCategory Standing(Side copy, Side theirs) =>
            copy == Side.None ? TargetCategory.Neutral : copy == theirs ? TargetCategory.Friendly : TargetCategory.Hostile;

        private static void SetCategory(Creature creature, TargetCategory category)
        {
            if (creature.TargetCategory == category)
                return;

            creature.TargetCategory = category;
            creature.Hate.Clear();
            CellManager.Instance.CellCallMethod(creature, new TargetCategoryPacket(category));
            BehaviorManager.Instance.StopFighting(creature);
        }

        /// <summary>The hospital of the copy's side, offered beside its entrance; none undecided.</summary>
        public static Hospitals.Hospital SideHospital(MapChannel map)
        {
            var side = SideOf(map);

            if (side == Side.None)
                return null;

            var teleporter = side == Side.Rebel ? M.RebelHospital : M.PenumbraHospital;
            return Hospitals.OnMap(M.MagmaCaverns).FirstOrDefault(hospital => hospital.TeleporterId == teleporter);
        }

        /// <summary>Whether a hospital of the caverns may be gone back to in this copy: not the other side's (Hospitals.AvailableTo).</summary>
        public static bool Allows(MapChannel map, Hospitals.Hospital hospital)
        {
            if (hospital == null || hospital.MapContextId != M.MagmaCaverns ||
                hospital.TeleporterId != M.RebelHospital && hospital.TeleporterId != M.PenumbraHospital)
                return true;

            return SideHospital(map)?.TeleporterId == hospital.TeleporterId;
        }

        #endregion

        #region Use

        /// <summary>Whether a finished use is of one of the copy's usables (ActorActionManager.PerformRecovery).</summary>
        public static bool IsUseOf(MapChannel map, ActionData action) =>
            action != null && StateOf(map) is { } state && state.Usables.Any(usable => usable.Object.EntityId == action.SourceId);

        /// <summary>A use about to start (DynamicObjectManager.RequestUseObjectPacket): the message it is refused with, or null.</summary>
        public static PlayerMessage? Refusal(Client client, DynamicObject obj)
        {
            var map = client?.Player?.MapChannel;
            var state = StateOf(map);
            var usable = UsableOf(state, obj);

            if (usable == null)
                return null;

            var player = client.Player;
            var why = WhyNot(state, usable, player, out var message);

            if (why == null)
                return null;

            if (message != null)
                CommunicatorManager.Instance.SystemMessage(client, message);

            return why;
        }

        /// <summary>Why the member may not use it now, and what they are told; null when they may.</summary>
        private static PlayerMessage? WhyNot(State state, Usable usable, Manifestation player, out string message)
        {
            message = null;
            var side = state.Side;

            switch (usable.Kind)
            {
                case Kind.StationTeleporter:
                case Kind.CampTeleporter:
                    if (side == Side.Rebel)
                        return null;
                    message = side == Side.None
                        ? "The teleporter will not answer. Speak with Lieutenant Kane."
                        : "The Rebels have shut their teleporter down from the camp.";
                    return PlayerMessage.PmUseObjectNotUsable;

                case Kind.Door when state.DoorOpen[usable.Index]:
                    return PlayerMessage.PmUseObjectNotUsable;

                case Kind.Door when side == Side.Rebel:
                {
                    var (officer, closed) = usable.Index == WestDoor
                        ? (M.FcSudevi, PlayerMessage.PmMagmaRebelDoor1Closed)
                        : (M.FcNelby, PlayerMessage.PmMagmaRebelDoor2Closed);

                    if (!Done(player, M.FinalChance, officer))
                        return closed;
                    message = "The door stays shut until the Penumbra assault here is broken.";
                    return PlayerMessage.PmUseObjectNotUsable;
                }

                case Kind.Door when side == Side.Penumbra:
                    if (Wants(player, M.Executioner, usable.Index == EastDoor ? M.ExDoor1 : M.ExDoor2))
                        return null;
                    message = "The door is sealed from the Rebels' side. You have no orders to blow it yet.";
                    return PlayerMessage.PmUseObjectNotUsable;

                case Kind.Door:
                    message = "The door is sealed from the other side.";
                    return PlayerMessage.PmUseObjectNotUsable;

                case Kind.Wormhole:
                    return Array.Find(state.Assaults, assault => assault.Wormhole == usable) is { Started: true, Shut: false }
                        ? null
                        : PlayerMessage.PmUseObjectNotUsable;

                case Kind.Setup:
                {
                    if (FarEnd(side, usable.Index) == null)
                    {
                        message = "The wormhole's controls do not answer you.";
                        return PlayerMessage.PmUseObjectNotUsable;
                    }

                    if (state.WormholeSet[usable.Index] || SetupWanted(player, side, usable.Index))
                        return null;
                    message = "You have no orders to set this wormhole up.";
                    return PlayerMessage.PmUseObjectNotUsable;
                }

                case Kind.Beacon:
                    return side == Side.Rebel && !state.BeaconPlaced[usable.Index] &&
                           Wants(player, M.FinalChance, usable.Index == 0 ? M.FcBeacon1 : M.FcBeacon2)
                        ? null
                        : PlayerMessage.PmUseObjectNotUsable;

                case Kind.Laptop:
                    return side == Side.Penumbra && Wants(player, M.Executioner, M.ExLaptop) ? null : PlayerMessage.PmUseObjectNotUsable;

                case Kind.Cells:
                    return side == Side.Penumbra && !state.Released && Wants(player, M.Executioner, M.ExRelease)
                        ? null
                        : PlayerMessage.PmUseObjectNotUsable;

                case Kind.Treasure:
                    return state.TreasureTakenBy.Contains(player.Id) ? PlayerMessage.PmUseObjectNotUsable : null;
            }

            return null;
        }

        private static bool SetupWanted(Manifestation player, Side side, int setup) => (side, setup) switch
        {
            (Side.Rebel, GroundsSetup) => Wants(player, M.FinalChance, M.FcRebelWormhole),
            (Side.Penumbra, GroundsSetup) => Wants(player, M.Executioner, M.ExWormhole2),
            (Side.Penumbra, CauldronSetup) => Wants(player, M.Executioner, M.ExWormhole1),
            _ => false
        };

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

            // Whatever changed while it was used: asked again.
            if (WhyNot(state, usable, client.Player, out _) != null)
                return;

            Used(map, state, usable, client, Now());
        }

        private static void Used(MapChannel map, State state, Usable usable, Client client, long now)
        {
            var obj = usable.Object;

            switch (usable.Kind)
            {
                case Kind.StationTeleporter:
                    // The objective before the trip: it is the use, wherever the trip ends.
                    if (Wants(client.Player, M.Consensus, M.TakeTeleporter))
                    {
                        Record(state, client, M.Consensus, M.UsedTeleporter, now);
                        Record(state, client, M.Consensus, M.Crossed, now);
                    }
                    Show(map, obj, UseObjectState.TsState1, client.Player.EntityId);
                    DynamicObjectManager.Instance.TakePassage(client, M.Camp, Math.PI / 2);
                    break;

                case Kind.CampTeleporter:
                    Show(map, obj, UseObjectState.TsState1, client.Player.EntityId);
                    DynamicObjectManager.Instance.TakePassage(client, M.StationArrival, Math.PI);
                    break;

                case Kind.Door:
                    OpenDoor(map, state, usable.Index, client.Player.EntityId);
                    CommunicatorManager.Instance.SystemMessage(client, "The charges go off and blow the door open.");
                    break;

                case Kind.Wormhole:
                    var assault = Array.Find(state.Assaults, entry => entry.Wormhole == usable);
                    assault.Shut = true;
                    Show(map, obj, UseObjectState.TsState0, client.Player.EntityId);
                    SetEnabled(map, obj, false);
                    CommunicatorManager.Instance.SystemMessage(client, "The wormhole collapses: no more come through it.");
                    break;

                case Kind.Setup:
                    if (!state.WormholeSet[usable.Index])
                    {
                        state.WormholeSet[usable.Index] = true;
                        Show(map, obj, UseObjectState.TsState1, client.Player.EntityId);
                        CommunicatorManager.Instance.SystemMessage(client, "The wormhole opens: it will take your squad through.");
                        break;
                    }
                    DynamicObjectManager.Instance.TakePassage(client, FarEnd(state.Side, usable.Index).Value, 0);
                    break;

                case Kind.Beacon:
                    state.BeaconPlaced[usable.Index] = true;
                    Show(map, obj, UseObjectState.IaStateAccepted, client.Player.EntityId);
                    SetEnabled(map, obj, false);
                    CommunicatorManager.Instance.SystemMessage(client, "The Rebel GPS beacon is placed.");
                    break;

                case Kind.Laptop:
                    state.LaptopRead = true;
                    CommunicatorManager.Instance.SystemMessage(client, "You copy Salyard's research from the laptop.");
                    break;

                case Kind.Cells:
                    state.Released = true;
                    state.Loosed.AddRange(Creatures(map).Where(creature => creature.DbId == M.PacifiedThrax));
                    Show(map, obj, UseObjectState.DoorStateOpen, client.Player.EntityId);
                    SetEnabled(map, obj, false);
                    CommunicatorManager.Instance.SystemMessage(client, "The forcefield drops: the research subjects are loose!");
                    break;

                case Kind.Treasure:
                    state.TreasureTakenBy.Add(client.Player.Id);
                    ManifestationManager.Instance.GainCredits(client, TreasureCredits);
                    CommunicatorManager.Instance.SystemMessage(client, "You take your share of Ember's hoard.");
                    break;
            }

            Handled(state, Members(map), now);
        }

        private static Usable UsableOf(State state, DynamicObject obj) =>
            state == null || obj == null ? null : state.Usables.FirstOrDefault(usable => ReferenceEquals(usable.Object, obj));

        private static Usable UsableOf(State state, Kind kind, int index = 0) =>
            state.Usables.FirstOrDefault(usable => usable.Kind == kind && usable.Index == index);

        private static void OpenDoor(MapChannel map, State state, int index, ulong actor)
        {
            if (state.DoorOpen[index])
                return;

            state.DoorOpen[index] = true;
            var door = UsableOf(state, Kind.Door, index).Object;
            Show(map, door, UseObjectState.DoorStateOpen, actor);
            SetEnabled(map, door, false);
            Logger.WriteLog(LogType.Debug, $"Magma Caverns {map.InstanceId}: the {(index == WestDoor ? "west" : "east")} door is open.");
        }

        /// <summary>Whether a door of the copy is open, for a test or a GM.</summary>
        public static bool DoorOpen(MapChannel map, int door) => StateOf(map)?.DoorOpen[door] == true;

        #endregion

        #region The story

        /// <summary>A Rebel copy's doors open once the assault before them is broken; blown ones are their objective's.</summary>
        private static void Doors(MapChannel map, State state, List<Client> members, long now)
        {
            if (state.Side == Side.Rebel)
            {
                if (!state.DoorOpen[WestDoor] && members.Any(client => Done(client.Player, M.FinalChance, M.FcHoldCamp)))
                    OpenDoor(map, state, WestDoor, 0);

                if (!state.DoorOpen[EastDoor] && members.Any(client => Done(client.Player, M.FinalChance, M.FcHoldDoor)))
                    OpenDoor(map, state, EastDoor, 0);
            }
        }

        /// <summary>What the copy has done, to every member it is an objective of.</summary>
        private static void Handled(State state, List<Client> members, long now)
        {
            // Every research subject the forcefield held, loosed, dead.
            if (state.Released && !state.SubjectsDead && state.Loosed.All(subject => !Alive(subject)))
                state.SubjectsDead = true;

            foreach (var client in members)
            {
                var player = client.Player;

                if (state.Side == Side.Penumbra)
                {
                    if (state.DoorOpen[EastDoor] && Wants(player, M.Executioner, M.ExDoor1))
                        Record(state, client, M.Executioner, M.ExDoor1Blown, now);
                    if (state.DoorOpen[WestDoor] && Wants(player, M.Executioner, M.ExDoor2))
                        Record(state, client, M.Executioner, M.ExDoor2Blown, now);
                    if (state.WormholeSet[CauldronSetup] && Wants(player, M.Executioner, M.ExWormhole1))
                        Record(state, client, M.Executioner, M.ExWormhole1Set, now);
                    if (state.WormholeSet[GroundsSetup] && Wants(player, M.Executioner, M.ExWormhole2))
                        Record(state, client, M.Executioner, M.ExWormhole2Set, now);
                    if (state.LaptopRead && Wants(player, M.Executioner, M.ExLaptop))
                        Record(state, client, M.Executioner, M.ExLaptopRead, now);
                    if (state.Released && Wants(player, M.Executioner, M.ExRelease))
                        Record(state, client, M.Executioner, M.ExSubjectsReleased, now);
                    if (state.SubjectsDead && Wants(player, M.Executioner, M.ExSubjects))
                        Record(state, client, M.Executioner, M.ExSubjectsDead, now);
                }
                else if (state.Side == Side.Rebel)
                {
                    if (state.WormholeSet[GroundsSetup] && Wants(player, M.FinalChance, M.FcRebelWormhole))
                        Record(state, client, M.FinalChance, M.FcRebelWormholeSet, now);
                    if (state.BeaconPlaced[0] && Wants(player, M.FinalChance, M.FcBeacon1))
                        Record(state, client, M.FinalChance, M.FcBeacon1Placed, now);
                    if (state.BeaconPlaced[1] && Wants(player, M.FinalChance, M.FcBeacon2))
                        Record(state, client, M.FinalChance, M.FcBeacon2Placed, now);
                }
            }
        }

        /// <summary>The places a member's objective is to reach.</summary>
        private static void Places(State state, List<Client> members, long now)
        {
            foreach (var client in members)
            {
                var player = client.Player;
                var at = player.Position;

                if (state.Side == Side.Penumbra && Wants(player, M.Consensus, M.TakeBackDoor) &&
                    Vector3.Distance(at, M.BackDoor) <= BackDoorRadius)
                {
                    Record(state, client, M.Consensus, M.UsedBackDoor, now);
                    Record(state, client, M.Consensus, M.Crossed, now);
                }

                if (state.Side == Side.Rebel && Wants(player, M.FinalChance, M.FcInfiltrate) &&
                    (Vector3.Distance(at, M.LandingZone1) <= LandingZoneRadius || Vector3.Distance(at, M.LandingZone2) <= LandingZoneRadius))
                    Record(state, client, M.FinalChance, M.FcInfiltrated, now);

                if (state.Side == Side.Penumbra)
                {
                    if (Wants(player, M.Executioner, M.ExFrontLine) &&
                        (Vector3.Distance(at, M.EastCorridor) <= FrontRadius || Vector3.Distance(at, M.EastDoorWay) <= FrontRadius))
                        Record(state, client, M.Executioner, M.ExFrontFound, now);

                    if (Wants(player, M.Executioner, M.ExStronghold) && Vector3.Distance(at, M.SudeviSpot) <= StrongholdRadius)
                        Record(state, client, M.Executioner, M.ExStrongholdFound, now);

                    if (Wants(player, M.Executioner, M.ExCamp) &&
                        (Vector3.Distance(at, M.Camp) <= CampRadius || Vector3.Distance(at, M.SalyardSpot) <= CampRadius))
                        Record(state, client, M.Executioner, M.ExCampEntered, now);
                }

                if (Wants(player, M.LavaBeast, M.FindLair) && Vector3.Distance(at, M.Lair) <= LairRadius)
                    Record(state, client, M.LavaBeast, M.LairFound, now);
            }
        }

        #endregion

        #region The assaults

        /// <summary>Whether an assault of the copy has begun, is shut, is broken; for a test or a GM.</summary>
        public static (bool Started, bool Shut, bool Over, int Breach, int Waves) AssaultOf(MapChannel map, int assault) =>
            StateOf(map)?.Assaults[assault] is { } a ? (a.Started, a.Shut, a.Over, a.Breach, a.Waves) : default;

        /// <summary>The living soldiers of an assault of the copy, for a test or a GM.</summary>
        public static List<Creature> AssaultSoldiers(MapChannel map, int assault) =>
            StateOf(map)?.Assaults[assault].Walkers.Select(walker => walker.Creature).Where(Alive).ToList() ?? new List<Creature>();

        private static void RunAssault(MapChannel map, State state, AssaultDef def, Assault assault, List<Client> members, long now)
        {
            if (state.Side != def.Side)
                return;

            if (!assault.Started)
            {
                if (!members.Any(client => Wants(client.Player, def.Mission, def.StartObjective)))
                    return;

                assault.Started = true;
                assault.NextWaveAt = now;
                var key = $"magma:assault-{Array.IndexOf(Assaults, def)}";
                assault.Wormhole = new Usable
                {
                    Kind = Kind.Wormhole,
                    Index = Array.IndexOf(Assaults, def),
                    Object = Place(map, key, M.WormholeClass, def.Wormhole, 0, UseObjectState.TsState1)
                };
                state.Usables.Add(assault.Wormhole);

                foreach (var member in members)
                    CommunicatorManager.ShowPlayerMessage(member, def.Begin);

                Logger.WriteLog(LogType.Debug, $"Magma Caverns {map.InstanceId}: {def.Name} begins.");
            }

            assault.Walkers.RemoveAll(walker => !Alive(walker.Creature));

            if (!assault.Shut && assault.Waves < MaxWaves && now >= assault.NextWaveAt)
            {
                assault.NextWaveAt = now + WaveEveryMs;

                for (var i = 0; i < WaveSize; i++)
                {
                    var row = def.Rows[(assault.Waves * WaveSize + i) % def.Rows.Length];

                    if (Spawn(map, row, def.Wormhole, WaveSpread, def.Route[0].At) is { } soldier)
                        assault.Walkers.Add(new Walker { Creature = soldier });
                }

                assault.Waves++;
            }

            foreach (var walker in assault.Walkers)
                March(map, state, def, assault, walker, members, now);

            if (assault.Shut)
                foreach (var client in members.Where(client => Wants(client.Player, def.Mission, def.ShutObjective)))
                    Record(state, client, def.Mission, def.ShutEvent, now);

            foreach (var bonus in def.Bonuses.Where(bonus => assault.Breach >= bonus.Guards))
                foreach (var client in members.Where(client => Wants(client.Player, def.Mission, bonus.Objective)))
                    Record(state, client, def.Mission, bonus.Lost, now);

            if (!assault.Over && assault.Shut && assault.Walkers.Count == 0)
            {
                assault.Over = true;
                Logger.WriteLog(LogType.Debug, $"Magma Caverns {map.InstanceId}: {def.Name} is broken.");
            }

            if (!assault.Over)
                return;

            foreach (var client in members)
            {
                foreach (var bonus in def.Bonuses.Where(bonus => assault.Breach < bonus.Guards))
                    if (Wants(client.Player, def.Mission, bonus.Objective) && Record(state, client, def.Mission, bonus.Won, now))
                    {
                        if (bonus.Credits > 0)
                            ManifestationManager.Instance.GainCredits(client, bonus.Credits);
                        if (bonus.Experience > 0)
                            ManifestationManager.Instance.GainExperience(client, bonus.Experience);
                        CommunicatorManager.ShowPlayerMessage(client, bonus.Message);
                    }

                if (Wants(client.Player, def.Mission, def.OverObjective))
                    Record(state, client, def.Mission, def.OverEvent, now);
            }
        }

        /// <summary>A soldier on to the next stage of its route, unless it is fighting; each stage's message as the first reaches it.</summary>
        private static void March(MapChannel map, State state, AssaultDef def, Assault assault, Walker walker, List<Client> members, long now)
        {
            var soldier = walker.Creature;

            if (walker.Stage >= def.Route.Length)
                return;

            // The furthest stage it stands at, from the one it is making for: those before it are passed.
            for (var at = def.Route.Length - 1; at >= walker.Stage; at--)
            {
                if (Vector3.Distance(soldier.Position, def.Route[at].At) > StageRadius)
                    continue;

                for (var passed = walker.Stage; passed <= at; passed++)
                    Reached(map, def, assault, passed, members);

                walker.Stage = at + 1;
                break;
            }

            if (walker.Stage >= def.Route.Length)
                return;

            var stage = def.Route[walker.Stage];

            if (soldier.Hate.Ranked().Any())
                return;

            if (soldier.Controller.CurrentAction == BehaviorManager.BehaviorActionScriptedMove &&
                soldier.Controller.ScriptedMove is { Arrived: false } move && move.Destination == stage.At)
                return;

            var facing = Math.Atan2(stage.At.X - soldier.Position.X, stage.At.Z - soldier.Position.Z);

            if (BehaviorManager.Instance.SetActionScriptedMove(map, soldier, stage.At, facing) && soldier.Controller.ScriptedMove != null)
                soldier.Controller.ScriptedMove.FightsBack = true;
        }

        private static void Reached(MapChannel map, AssaultDef def, Assault assault, int stage, List<Client> members)
        {
            if (!assault.Announced.Add(stage))
                return;

            var at = def.Route[stage];

            if (at.Message is { } message)
                foreach (var member in members)
                    CommunicatorManager.ShowPlayerMessage(member, message);

            if (at.Breach > assault.Breach)
            {
                assault.Breach = at.Breach;
                Logger.WriteLog(LogType.Debug, $"Magma Caverns {map.InstanceId}: {def.Name} reached stage {stage} (breach {at.Breach}).");
            }
        }

        #endregion

        #region Creature Strike

        /// <summary>The Mantas and Ticks of the copy's Creature Strike, alive; for a test or a GM.</summary>
        public static List<Creature> StrikeOf(MapChannel map) => StateOf(map)?.Strike.Where(Alive).ToList() ?? new List<Creature>();

        private static void Strike(MapChannel map, State state, List<Client> members, long now)
        {
            if (state.Side != Side.Rebel)
                return;

            var wanting = members.Where(client => Wants(client.Player, M.CreatureStrike, M.StrikeDefeat)).ToList();

            if (!state.StrikeStarted)
            {
                if (wanting.Count == 0)
                    return;

                state.StrikeStarted = true;

                for (var i = 0; i < StrikeMantas + StrikeTicks; i++)
                    if (Spawn(map, i < StrikeMantas ? M.BeamManta : M.BarbTick, M.Sakra, 6f, M.Defenses) is { } creature)
                    {
                        state.Strike.Add(creature);
                        var facing = Math.Atan2(M.Defenses.X - creature.Position.X, M.Defenses.Z - creature.Position.Z);

                        if (BehaviorManager.Instance.SetActionScriptedMove(map, creature, M.Defenses, facing) && creature.Controller.ScriptedMove != null)
                            creature.Controller.ScriptedMove.FightsBack = true;
                    }

                foreach (var client in wanting)
                    CommunicatorManager.Instance.SystemMessage(client, "Beam Mantas and Barb Ticks are coming out of the oasis at the defenses!");

                Logger.WriteLog(LogType.Debug, $"Magma Caverns {map.InstanceId}: Creature Strike: {state.Strike.Count} from the oasis.");
            }

            if (!state.StrikeOver && state.Strike.All(creature => !Alive(creature)))
                state.StrikeOver = true;

            if (!state.StrikeOver)
                return;

            foreach (var client in wanting)
                Record(state, client, M.CreatureStrike, M.StrikeOver, now);
        }

        #endregion

        #region Ember

        /// <summary>The copy's Ember, alive; null if he is not.</summary>
        public static Creature EmberOf(MapChannel map) => StateOf(map)?.Ember is { } ember && Alive(ember) ? ember : null;

        /// <summary>Whether Ember's hoard is out in the copy, for a test or a GM.</summary>
        public static bool TreasureOut(MapChannel map) => StateOf(map)?.EmberDown == true;

        private static void Ember(MapChannel map, State state, List<Client> members)
        {
            if (state.EmberDown || state.Ember == null || Alive(state.Ember))
                return;

            state.EmberDown = true;

            var crate = Place(map, "magma:ember-hoard", M.TreasureClass, M.Treasure, 0.4, UseObjectState.TdStateClosed);
            Add(state, Kind.Treasure, 0, crate);

            foreach (var member in members.Where(member => Vector3.Distance(member.Player.Position, M.Lair) <= TreasureShout))
                CommunicatorManager.ShowPlayerMessage(member, PlayerMessage.PmMagmaEmberTreasureBoxRevealed);

            Logger.WriteLog(LogType.Debug, $"Magma Caverns {map.InstanceId}: Ember is dead; his hoard is out.");
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

        private static bool Active(Manifestation player, uint missionId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active;

        /// <summary>Whether the player has the mission with the objective open.</summary>
        public static bool Wants(Manifestation player, uint missionId, uint objectiveId) =>
            player.Missions.TryGetValue(missionId, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Incomplete;

        /// <summary>Whether the player has the mission with the objective done.</summary>
        private static bool Done(Manifestation player, uint missionId, uint objectiveId) =>
            player.Missions.TryGetValue(missionId, out var log) &&
            log.Objectives.TryGetValue(objectiveId, out var objective) && objective.State == MissionObjectiveState.Completed;

        /// <summary>
        /// The event to the player's mission: once for an assignment, or again a while after one
        /// that changed nothing. True when this call gave it.
        /// </summary>
        private static bool Record(State state, Client client, uint missionId, uint eventId, long now)
        {
            if (!client.Player.Missions.TryGetValue(missionId, out var log))
                return false;

            var key = $"{client.Player.Id}:{log.AssignmentId}:{missionId}:{eventId}";

            if (state.Given.TryGetValue(key, out var retryAt) && now < retryAt)
                return false;

            var given = MissionApplication.Instance.TryEmitScenarioProgressEvent(client, missionId, M.Scenario, eventId);
            state.Given[key] = given ? long.MaxValue : now + RetryMs;
            return given;
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

        /// <summary>Alive and still in the world as itself.</summary>
        private static bool Alive(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying &&
            EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) && ReferenceEquals(registered, creature);

        /// <summary>Forgets every copy; for tests.</summary>
        public static void Reset() => States.Clear();

        #endregion
    }
}
