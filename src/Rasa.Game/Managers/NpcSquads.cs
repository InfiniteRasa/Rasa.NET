using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.World;

    /// <summary>
    /// Squads of NPCs out in the world: an AFS rifle squad walking a road between two outposts,
    /// a Forean war party, a Bane patrol. What side a squad is on is its creatures', so it fights
    /// whatever its creatures fight.
    ///
    ///  - A squad type (npc_squad_type, npc_squad_member) is what it is made of: lines of creature
    ///    rows, each from a least to a most of it, drawn each time it is sent; how it stands
    ///    (a wedge, a column or a line abreast, so many metres apart); how far its members look
    ///    for a fight (0 for each creature's own aggro range); and how far from the leader they go
    ///    in one (0 for no tether).
    ///  - A path (npc_squad_path, npc_squad_path_point) is a route on one map: points the leader
    ///    goes to in turn across the navmesh, each with a stand there if it says so, and at the end
    ///    round again from the first, back the way it came, or a stop there for good.
    ///  - A spawn (npc_squad_spawn) sends a squad of a type at a place, facing a way, to walk a
    ///    path - or to hold the place - and sends it again a set time after the last of it falls.
    ///
    /// On the move the first of the squad is its leader: he walks the path at the pace of the
    /// slowest of them and the rest keep their places about him, facing the way he goes, running
    /// to catch up when they are far from them. He waits for anyone left well behind. When one
    /// of them is in a fight the squad stops and the others join it, on the same target; when it
    /// is over they gather on him again and go on from where they left the path. A leader who
    /// falls is replaced by the next of them. A squad type with a tether keeps its members by
    /// their leader in a fight (<see cref="TetherOf"/>); one whose leader is a bystander keeps him out of
    /// every fight and its members fight by him (<see cref="IsBystander"/>). Each map channel has its own squads - every copy of
    /// an instance its own - and they are sent when the first player comes onto the map.
    ///
    /// GMs make them in game (.squad): types, paths walked and recorded point by point, spawns put
    /// down where they stand. Every change is saved to the world database at once and takes
    /// effect in the world; .squad export writes the rows as SQL for a migration.
    /// </summary>
    public static class NpcSquads
    {
        public enum Formation : byte { Wedge = 0, Column = 1, Line = 2 }

        public enum PathMode : byte { Loop = 0, Back = 1, Once = 2 }

        public enum Phase { Arriving, Moving, Pausing, Holding, Fighting, Regrouping, Gone }

        /// <summary>How a squad comes into the world (npc_squad_spawn.arrival).</summary>
        public enum Arrival : byte
        {
            /// <summary>All of it at once, in its places.</summary>
            None = 0,

            /// <summary>
            /// On the spot one after another, <see cref="ArrivalSpacingMs"/> apart, each with the
            /// teleporter arrival effect on its body, as players arriving by a waypoint are seen.
            /// </summary>
            Teleport = 1
        }

        /// <summary>How long after one member teleports in the next one does. Ours.</summary>
        public const long ArrivalSpacingMs = 700;

        /// <summary>How far off the spot a member teleports in, toward his place: the leader on it.</summary>
        public const float ArrivalOffset = 1.2f;

        /// <summary>The most creatures one squad sends, whatever its lines say.</summary>
        public const int MaxMembers = 12;

        public const double DefaultSpacing = 3, DefaultRespawnSeconds = 120;

        /// <summary>How near a point, across the ground, the leader has reached it; and up or down.</summary>
        public const float PointReached = 1.5f, PointReachedHeight = 4f;

        /// <summary>Further than this from his place a member runs; nearer, he walks at the squad's pace.</summary>
        public const float CatchUpDistance = 4f;

        /// <summary>Further than this from his place, and nearer than <see cref="CatchUpDistance"/>, a member steps out a little faster than the squad.</summary>
        public const float CloseUpDistance = 1f, CloseUpPace = 1.35f;

        /// <summary>A member further than this from his place stops the leader until he has caught up.</summary>
        public const float WaitDistance = 12f;

        /// <summary>After a fight, how long the squad gathers before it goes on regardless.</summary>
        public const long RegroupTimeoutMs = 20_000;

        /// <summary>How far a member goes to join a fight another of them is in.</summary>
        public const float AssistRange = 60f;

        /// <summary>Within this of the tether's end a member counts as at it.</summary>
        public const float TetherSlack = 1.5f;

        /// <summary>How long a member holds at its tether's end, evading, before it gives the fight up and goes back to its place.</summary>
        public const long TetherHoldMs = 10_000;

        /// <summary>
        /// On a tether, a member joins its squad's fight only with a target this much nearer the
        /// leader than the tether's length: inside it, so it can get to it, and short of its end,
        /// so a target there does not draw it in and out of the fight. On a short tether, no
        /// nearer than half of it (<see cref="RejoinDistance"/>).
        /// </summary>
        public const float TetherRejoinMargin = 5f;

        #region Definitions

        public sealed class SquadType
        {
            public uint Id { get; init; }
            public string Name { get; set; }
            public Formation Formation { get; set; }
            public float Spacing { get; set; }
            public float EngageRange { get; set; }

            /// <summary>How far from the leader a member goes in a fight; 0 for no tether (<see cref="TetherOf"/>).</summary>
            public float Tether { get; set; }

            /// <summary>Whether the leader, as the squad is sent, is a bystander its members guard (<see cref="IsBystander"/>).</summary>
            public bool LeaderBystander { get; set; }

            public List<MemberLine> Members { get; } = new();
        }

        public sealed class MemberLine
        {
            public byte Slot { get; set; }
            public uint CreatureId { get; set; }
            public byte MinCount { get; set; }
            public byte MaxCount { get; set; }
        }

        public sealed class SquadPath
        {
            public uint Id { get; init; }
            public uint MapContextId { get; set; }
            public string Name { get; set; }
            public PathMode Mode { get; set; }
            public List<PathPoint> Points { get; } = new();
        }

        /// <summary>
        /// A point of a route: where, how long the squad stands there, whether it runs the leg to
        /// it, and how far it looks for a fight going there and standing there (0 for its type's).
        /// </summary>
        public sealed record PathPoint(Vector3 Position, uint PauseMs, bool Run = false, float EngageRange = 0);

        public sealed class SquadSpawn
        {
            public uint Id { get; init; }
            public uint MapContextId { get; set; }
            public uint TypeId { get; set; }
            public uint PathId { get; set; }
            public Vector3 Position { get; set; }
            public float Rotation { get; set; }
            public uint RespawnSeconds { get; set; }
            public bool Enabled { get; set; }
            public string Comment { get; set; }
            public Arrival Arrival { get; set; }
        }

        private static Dictionary<uint, SquadType> _types = new();
        private static Dictionary<uint, SquadPath> _paths = new();
        private static Dictionary<uint, SquadSpawn> _spawns = new();
        private static IGameUnitOfWorkFactory _unitOfWorkFactory;

        public static IReadOnlyDictionary<uint, SquadType> Types => _types;
        public static IReadOnlyDictionary<uint, SquadPath> Paths => _paths;
        public static IReadOnlyDictionary<uint, SquadSpawn> Spawns => _spawns;

        /// <summary>The clock; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>The draw of a line's count, from min to max; a test's to replace.</summary>
        public static Func<int, int, int> Roll { get; set; } = (min, max) => Random.Shared.Next(min, max + 1);

        /// <summary>Loads the squads' rows. Runs after the creatures are loaded.</summary>
        public static void Init(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _unitOfWorkFactory = gameUnitOfWorkFactory;

            try
            {
                using var unitOfWork = gameUnitOfWorkFactory.CreateWorld();
                var squads = unitOfWork.NpcSquads;

                Load(squads.GetTypes(), squads.GetMembers(), squads.GetPaths(), squads.GetPathPoints(), squads.GetSpawns());
            }
            catch (NotSupportedException)
            {
                Load(null, null, null, null, null);
            }

            Logger.WriteLog(LogType.Initialize,
                $"Loaded {_types.Count} NPC squad types, {_paths.Count} squad paths and {_spawns.Count} squad spawns");
        }

        /// <summary>Takes the rows as the squads from here on.</summary>
        public static void Load(IEnumerable<NpcSquadTypeEntry> types, IEnumerable<NpcSquadMemberEntry> members,
            IEnumerable<NpcSquadPathEntry> paths, IEnumerable<NpcSquadPathPointEntry> points, IEnumerable<NpcSquadSpawnEntry> spawns)
        {
            _types = (types ?? Enumerable.Empty<NpcSquadTypeEntry>()).ToDictionary(row => row.Id, row => new SquadType
            {
                Id = row.Id, Name = row.Name, Formation = (Formation)Math.Min(row.Formation, (byte)Formation.Line),
                Spacing = row.Spacing > 0 ? (float)row.Spacing : (float)DefaultSpacing, EngageRange = (float)Math.Max(0, row.EngageRange),
                Tether = double.IsFinite(row.Tether) ? (float)Math.Max(0, row.Tether) : 0,
                LeaderBystander = row.LeaderBystander != 0
            });

            foreach (var row in (members ?? Enumerable.Empty<NpcSquadMemberEntry>()).OrderBy(row => row.Slot))
                if (_types.TryGetValue(row.TypeId, out var type))
                    type.Members.Add(new MemberLine { Slot = row.Slot, CreatureId = row.CreatureId, MinCount = row.MinCount, MaxCount = Math.Max(row.MinCount, row.MaxCount) });
                else
                    Logger.WriteLog(LogType.Error, $"npc_squad_member {row.TypeId}/{row.Slot}: no squad type {row.TypeId}; left out");

            _paths = (paths ?? Enumerable.Empty<NpcSquadPathEntry>()).ToDictionary(row => row.Id, row => new SquadPath
            {
                Id = row.Id, MapContextId = row.MapContextId, Name = row.Name, Mode = (PathMode)Math.Min(row.Mode, (byte)PathMode.Once)
            });

            foreach (var row in (points ?? Enumerable.Empty<NpcSquadPathPointEntry>()).OrderBy(row => row.Step))
                if (_paths.TryGetValue(row.PathId, out var path) && double.IsFinite(row.PosX) && double.IsFinite(row.PosY) && double.IsFinite(row.PosZ))
                    path.Points.Add(new PathPoint(row.Position, row.PauseMs, row.Run != 0, (float)Math.Max(0, row.EngageRange)));
                else
                    Logger.WriteLog(LogType.Error, $"npc_squad_path_point {row.PathId}/{row.Step}: no such path, or not a place; left out");

            _spawns = (spawns ?? Enumerable.Empty<NpcSquadSpawnEntry>()).ToDictionary(row => row.Id, row => new SquadSpawn
            {
                Id = row.Id, MapContextId = row.MapContextId, TypeId = row.TypeId, PathId = row.PathId, Position = row.Position,
                Rotation = (float)row.Rotation, RespawnSeconds = row.RespawnSeconds, Enabled = row.Enabled != 0, Comment = row.Comment ?? string.Empty,
                Arrival = row.Arrival == (byte)Arrival.Teleport ? Arrival.Teleport : Arrival.None
            });

            States.Clear();
        }

        #endregion

        #region In the world

        public sealed class Member
        {
            internal Member(LiveSquad squad, Creature creature)
            {
                Squad = squad;
                Creature = creature;
            }

            public LiveSquad Squad { get; }
            public Creature Creature { get; }

            /// <summary>His place in the formation: 0 the leader's.</summary>
            public int Place { get; internal set; }

            /// <summary>Where he is going now; null to stand where he is.</summary>
            public Vector3? Goal { get; internal set; }

            /// <summary>How fast he goes there, metres a second.</summary>
            public float Speed { get; internal set; }

            /// <summary>The way he faces when he is there.</summary>
            public float Facing { get; internal set; }

            /// <summary>The leader the squad was sent with, of a type whose leader is a bystander (<see cref="IsBystander"/>).</summary>
            public bool Bystander { get; internal set; }

            // The walker's (BehaviorManager.AdvanceSquad).
            internal Vector3? PathGoal;
            internal long RepathMs;
            internal bool Stopped;
            internal bool Stuck;
        }

        public sealed class LiveSquad
        {
            internal LiveSquad(SquadSpawn spawn) => Spawn = spawn;

            public SquadSpawn Spawn { get; }
            public List<Member> Members { get; } = new();
            public Phase Phase { get; internal set; } = Phase.Gone;
            public int PathIndex { get; internal set; } = -1;
            public int Direction { get; internal set; } = 1;
            public float Heading { get; internal set; }
            public long RespawnAt { get; internal set; }
            internal long PauseUntil;
            internal long RegroupSince;

            /// <summary>Who is still to teleport in, in order, and when the next one does.</summary>
            internal readonly Queue<uint> Arriving = new();
            internal long NextArrivalAt;

            /// <summary>The point the squad is going to or standing at; null off its route.</summary>
            public PathPoint Point => Spawn.PathId != 0 && _paths.TryGetValue(Spawn.PathId, out var path) &&
                PathIndex >= 0 && PathIndex < path.Points.Count ? path.Points[PathIndex] : null;

            public IEnumerable<Member> Living => Members.Where(member => Alive(member.Creature));
            public Member Leader => Living.OrderBy(member => member.Place).FirstOrDefault();
        }

        private sealed class MapState
        {
            public readonly Dictionary<uint, LiveSquad> Squads = new();
        }

        private static readonly ConditionalWeakTable<MapChannel, MapState> States = new();

        private static MapState StateOf(MapChannel map) => States.GetValue(map, _ => new MapState());

        /// <summary>The squads on a map channel, by spawn.</summary>
        public static IReadOnlyDictionary<uint, LiveSquad> On(MapChannel map) => map == null ? new Dictionary<uint, LiveSquad>() : StateOf(map).Squads;

        /// <summary>
        /// Whether the creature is the bystander leader of its squad (npc_squad_type.leader_bystander): the leader it was
        /// sent with - a talkable NPC his guards walk with (Nakrinx, Valverde Pools). He takes up no fight, neither his
        /// squad's (Think) nor his scan's (<see cref="MayTakeUp"/>) nor one a hit or anything else would start - all go
        /// through BehaviorManager.CanSetActionFighting - and while they fight he stands, as a squad in a fight does, so
        /// they fight by him and a tether holds them to him. When he falls the next of them leads, and fights as any.
        /// </summary>
        public static bool IsBystander(Creature creature) => creature?.SquadMember?.Bystander == true;

        /// <summary>How far the creature, one of a squad's, looks for a fight.</summary>
        public static float EngageRange(Creature creature)
        {
            var squad = creature?.SquadMember?.Squad;

            if (squad?.Point is PathPoint point && point.EngageRange > 0)
                return point.EngageRange;

            var type = squad != null && _types.TryGetValue(squad.Spawn.TypeId, out var t) ? t : null;

            return type != null && type.EngageRange > 0 ? type.EngageRange : creature?.AggroRange ?? 0;
        }

        /// <summary>
        /// The leader the creature, one of a squad's, is tethered to in a fight, and how far:
        /// with a tether on its squad's type, its squad's leader while there is one other than
        /// itself - the next of them when he falls. Null for any other creature, which is leashed
        /// from its home (BehaviorManager.ChasedTooFar).
        ///
        /// A member on a tether is not leashed from its place: it chases no further than the
        /// tether from its leader (<see cref="Tether"/>), and at the tether's end, with nothing it
        /// can strike its target with, it holds, facing it, and evades (<see cref="HoldsAtTether"/>,
        /// <see cref="Evades"/>) - so a player with a long reach cannot shoot down one that cannot
        /// answer - until <see cref="TetherHoldMs"/> is up and it goes back to its place. It joins
        /// the fight again once the target is within <see cref="RejoinDistance"/> of the leader
        /// (Think), and takes one up on its own only then (<see cref="MayTakeUp"/>).
        /// </summary>
        public static Creature TetherOf(Creature creature, out float tether)
        {
            tether = 0;
            var squad = creature?.SquadMember?.Squad;

            if (squad == null || !_types.TryGetValue(squad.Spawn.TypeId, out var type) || type.Tether <= 0)
                return null;

            var leader = squad.Leader?.Creature;

            if (leader == null || ReferenceEquals(leader, creature))
                return null;

            tether = type.Tether;
            return leader;
        }

        /// <summary>
        /// Whether the member is at the end of its tether with its target beyond it: it can get
        /// no nearer to it. Asked once it has found no attack it can make from where it is.
        /// </summary>
        public static bool HoldsAtTether(Creature member, Creature leader, float tether, Vector3 targetPosition) =>
            Vector3.Distance(Flat(targetPosition), Flat(leader.Position)) > tether
            && Vector3.Distance(Flat(member.Position), Flat(leader.Position)) >= tether - TetherSlack;

        /// <summary>The point, or where the tether lets the member get to on the way there from its leader: no further than the tether from him.</summary>
        public static Vector3 Tether(MapChannel map, Creature leader, float tether, Vector3 point)
        {
            var across = Flat(point) - Flat(leader.Position);
            var length = across.Length();

            if (length <= tether)
                return point;

            return NavMeshManager.SnapToGround(map, leader.Position + across * (tether / length));
        }

        /// <summary>
        /// Whether the creature is a member holding at its tether's end in a fight, with no attack
        /// that reaches its target this think: it takes no damage, nor any debuff (DamageImmunity,
        /// GameEffectManager).
        /// </summary>
        public static bool Evades(Creature creature) =>
            creature?.SquadMember != null && creature.Controller?.CurrentAction == BehaviorManager.BehaviorActionFighting &&
            creature.Controller.ActionFighting.TetherHeldMs > 0 && !creature.Controller.ActionFighting.TetherAnswers;

        /// <summary>
        /// Whether the member is further from its leader than its tether and <see cref="TetherSlack"/>:
        /// left there by a leader gone on, or run home; it gives its fight up (BehaviorManager).
        /// </summary>
        public static bool PastTether(Creature member, Creature leader, float tether) =>
            Vector3.Distance(Flat(member.Position), Flat(leader.Position)) > tether + TetherSlack;

        /// <summary>
        /// Whether a target is well inside the tether of a member to its leader - by
        /// <see cref="TetherSlack"/> - so the member can get to it: one held at the tether's end
        /// is no longer held (BehaviorManager).
        /// </summary>
        public static bool WithinTether(Creature leader, float tether, Vector3 targetPosition) =>
            Vector3.Distance(Flat(targetPosition), Flat(leader.Position)) <= tether - TetherSlack;

        /// <summary>
        /// How near the leader a target is for a member on a tether to take a fight up with it:
        /// <see cref="TetherRejoinMargin"/> inside the tether, and on a tether of 10 m or less half
        /// of it, so a short one is no bar to every fight.
        /// </summary>
        public static float RejoinDistance(float tether) => Math.Max(tether - TetherRejoinMargin, tether / 2);

        /// <summary>
        /// Whether the creature takes a fight up with the target its own scan found: any creature
        /// does, but one of a squad on a tether only with a target within
        /// <see cref="RejoinDistance"/> of its leader - else it would go out to its tether's end,
        /// hold, go back, see the target again and go out again for as long as it stood there.
        /// Being shot it fights back regardless, and holds evading at its tether's end. A bystander
        /// leader takes up none (<see cref="IsBystander"/>): he walks on.
        /// </summary>
        public static bool MayTakeUp(Creature creature, ulong targetEntityId)
        {
            if (IsBystander(creature))
                return false;

            var leader = TetherOf(creature, out var tether);

            return leader == null || EntityManager.Instance.GetActor(targetEntityId) is Actor target &&
                Vector3.Distance(Flat(target.Position), Flat(leader.Position)) < RejoinDistance(tether) && !PastTether(creature, leader, tether);
        }

        /// <summary>From the map channel worker: every squad of the map sent, moved and sent again.</summary>
        public static void Worker(MapChannel map)
        {
            if (map?.MapInfo == null)
                return;

            var state = StateOf(map);
            var mapContextId = map.MapInfo.MapContextId;
            var now = Now();

            foreach (var spawn in _spawns.Values.Where(spawn => spawn.MapContextId == mapContextId))
            {
                if (!state.Squads.TryGetValue(spawn.Id, out var squad))
                    state.Squads[spawn.Id] = squad = new LiveSquad(spawn);

                if (!spawn.Enabled)
                {
                    if (squad.Living.Any())
                        TakeAway(map, squad);
                    continue;
                }

                if (!squad.Living.Any())
                {
                    squad.Arriving.Clear();

                    if (squad.Phase != Phase.Gone)
                    {
                        squad.Phase = Phase.Gone;
                        squad.RespawnAt = now + spawn.RespawnSeconds * 1000L;
                    }

                    if (now >= squad.RespawnAt)
                        Send(map, squad, now);

                    continue;
                }

                Think(map, squad, now);
            }

            // Spawns taken away by a GM: their squads go with them.
            foreach (var gone in state.Squads.Keys.Where(id => !_spawns.ContainsKey(id)).ToList())
            {
                TakeAway(map, state.Squads[gone]);
                state.Squads.Remove(gone);
            }
        }

        /// <summary>A squad of the spawn's type at its place, facing its way. Returns how many came.</summary>
        public static int Send(MapChannel map, LiveSquad squad, long now)
        {
            var spawn = squad.Spawn;

            squad.Members.RemoveAll(member => !Alive(member.Creature));
            squad.RespawnAt = 0;

            if (!_types.TryGetValue(spawn.TypeId, out var type) || type.Members.Count == 0)
            {
                squad.Phase = Phase.Gone;
                squad.RespawnAt = now + Math.Max(10u, spawn.RespawnSeconds) * 1000L;
                return 0;
            }

            var creatures = new List<uint>();

            foreach (var line in type.Members.OrderBy(line => line.Slot))
            {
                var count = Roll(line.MinCount, Math.Max(line.MinCount, line.MaxCount));

                for (var i = 0; i < count && creatures.Count < MaxMembers; i++)
                    creatures.Add(line.CreatureId);
            }

            squad.Heading = spawn.Rotation;
            squad.PathIndex = -1;
            squad.Direction = 1;
            squad.Arriving.Clear();

            if (spawn.Arrival == Arrival.Teleport)
            {
                // The leader now, the rest one after another (Arrive).
                foreach (var dbId in creatures)
                    squad.Arriving.Enqueue(dbId);

                squad.Phase = Phase.Arriving;
                squad.NextArrivalAt = now;

                while (squad.Arriving.Count > 0 && squad.Members.Count == 0)
                    Arrive(map, squad, type, now);
            }
            else
            {
                foreach (var dbId in creatures)
                    Bring(map, squad, type, dbId, false);

                squad.Phase = Phase.Moving;
            }

            if (squad.Members.Count == 0)
            {
                squad.Phase = Phase.Gone;
                squad.Arriving.Clear();
                squad.RespawnAt = now + Math.Max(10u, spawn.RespawnSeconds) * 1000L;
            }

            Logger.WriteLog(LogType.Debug, $"Squad spawn {spawn.Id} ({type.Name}) sent {squad.Members.Count} on map {spawn.MapContextId}.");

            return squad.Members.Count;
        }

        /// <summary>The next of a squad teleporting in, when he is due.</summary>
        private static void Arrive(MapChannel map, LiveSquad squad, SquadType type, long now)
        {
            if (squad.Arriving.Count == 0 || now < squad.NextArrivalAt)
                return;

            Bring(map, squad, type, squad.Arriving.Dequeue(), true);
            squad.NextArrivalAt = now + ArrivalSpacingMs;
        }

        /// <summary>
        /// One of a squad into the world, facing the squad's way: in his place, or teleporting in on
        /// the spawn's spot, a little off it toward his place. Null when the creature cannot be made.
        /// </summary>
        private static Member Bring(MapChannel map, LiveSquad squad, SquadType type, uint dbId, bool teleport)
        {
            var spawn = squad.Spawn;
            var creature = CreatureManager.Instance.CreateCreature(dbId, null);

            if (creature == null)
            {
                Logger.WriteLog(LogType.Error, $"npc_squad_spawn {spawn.Id}: creature {dbId} could not be made; left out");
                return null;
            }

            var member = new Member(squad, creature) { Place = squad.Members.Count };
            member.Bystander = type?.LeaderBystander == true && member.Place == 0;
            var spot = map?.NavMesh?.Nearest(spawn.Position) ?? spawn.Position;
            var at = PlaceOf(map, type, member.Place, spot, squad.Heading);

            if (teleport && member.Place > 0)
            {
                var off = at - spot;
                var near = off.Length() > ArrivalOffset ? spot + off / off.Length() * ArrivalOffset : at;
                at = map?.NavMesh?.WalkTo(spot, near) ?? spot;
            }

            CreatureManager.Instance.SetLocation(creature, at, squad.Heading, spawn.MapContextId);
            creature.LastYaw = squad.Heading;
            creature.SquadMember = member;
            squad.Members.Add(member);

            CellManager.Instance.AddToWorld(map, creature);
            BehaviorManager.Instance.SetActionSquad(creature);

            // Seen arriving as a player off a waypoint is (actor.py Recv_TeleportArrival,
            // _PlayTeleportArrivalFX: the DEFAULT teleporter type's arrival effect on the body).
            if (teleport)
                CellManager.Instance.CellCallMethod(map, creature, new TeleportArrivalPacket());

            return member;
        }

        /// <summary>The squad's living members taken out of the world, and it gone with no respawn due.</summary>
        public static void TakeAway(MapChannel map, LiveSquad squad)
        {
            foreach (var member in squad.Members.Where(member => Alive(member.Creature)).ToList())
            {
                member.Creature.SquadMember = null;
                CellManager.Instance.RemoveCreatureFromWorld(map, member.Creature);
            }

            squad.Members.Clear();
            squad.Arriving.Clear();
            squad.Phase = Phase.Gone;
            squad.RespawnAt = 0;
        }

        /// <summary>One think of a squad with someone left in it.</summary>
        private static void Think(MapChannel map, LiveSquad squad, long now)
        {
            var spawn = squad.Spawn;
            _types.TryGetValue(spawn.TypeId, out var type);

            Arrive(map, squad, type, now);

            var living = squad.Living.OrderBy(member => member.Place).ToList();

            squad.Members.RemoveAll(member => !living.Contains(member));

            // The places closed up about whoever leads now.
            for (var i = 0; i < living.Count; i++)
                living[i].Place = i;

            var leader = living[0];
            var spacing = type?.Spacing ?? (float)DefaultSpacing;
            var movers = living.Where(member => member.Creature.WalkSpeed >= 0.01f).ToList();
            var running = squad.Phase == Phase.Moving && squad.Point?.Run == true;
            var pace = movers.Count == 0 ? 1f :
                running ? Math.Max(1f, movers.Min(member => Math.Max(member.Creature.WalkSpeed, member.Creature.RunSpeed))) :
                Math.Max(1f, movers.Min(member => member.Creature.WalkSpeed));

            // In a fight: the squad stands, and whoever is not yet in it joins it.
            var fighting = living.Where(member => member.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting).ToList();

            if (fighting.Count > 0)
            {
                squad.Phase = Phase.Fighting;

                var target = fighting.Select(member => member.Creature.Controller.ActionFighting.TargetEntityId).FirstOrDefault(id => id != 0);
                var tether = type?.Tether ?? 0;

                foreach (var member in living.Except(fighting))
                {
                    member.Goal = null;

                    if (target != 0 && member.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionSquad &&
                        EntityManager.Instance.GetActor(target) is Actor enemy &&
                        Vector3.Distance(enemy.Position, member.Creature.Position) <= AssistRange &&
                        (tether <= 0 || member == leader || Vector3.Distance(Flat(enemy.Position), Flat(leader.Creature.Position)) < RejoinDistance(tether) &&
                         !PastTether(member.Creature, leader.Creature, tether)))
                    {
                        member.Creature.Hate.Ensure(target, Threat.NoticedThreat);
                        BehaviorManager.Instance.SetActionFighting(member.Creature, target);
                        continue;
                    }

                    // On a tether, one out of the fight - its target too far from the leader, or
                    // held at its tether's end too long - keeps its place about the leader.
                    if (tether > 0 && member != leader)
                    {
                        member.Goal = PlaceOf(map, type, member.Place, leader.Creature.Position, squad.Heading);
                        member.Speed = Math.Max(pace, member.Creature.RunSpeed);
                        member.Facing = squad.Heading;
                    }
                }

                return;
            }

            // Anyone running home from a chase gets there first.
            if (living.Any(member => member.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionReturning))
                return;

            if (squad.Phase == Phase.Fighting)
            {
                squad.Phase = Phase.Regrouping;
                squad.RegroupSince = now;
            }

            var path = spawn.PathId != 0 && _paths.TryGetValue(spawn.PathId, out var p) && p.Points.Count > 0 ? p : null;

            switch (squad.Phase)
            {
                case Phase.Arriving:
                    // On the spot, facing the spawn's way, until the last of them is through.
                    leader.Goal = null;
                    squad.Heading = spawn.Rotation;

                    if (squad.Arriving.Count == 0)
                        squad.Phase = path == null ? Phase.Holding : Phase.Moving;
                    break;

                case Phase.Regrouping:
                    leader.Goal = null;

                    if (now - squad.RegroupSince >= RegroupTimeoutMs ||
                        living.Skip(1).All(member => Vector3.Distance(Flat(member.Creature.Position), Flat(PlaceOf(map, type, member.Place, leader.Creature.Position, squad.Heading))) <= spacing + 2f))
                        squad.Phase = path == null ? Phase.Holding : Phase.Moving;
                    break;

                case Phase.Pausing:
                    leader.Goal = null;

                    if (now >= squad.PauseUntil)
                        squad.Phase = Phase.Moving;
                    break;

                case Phase.Holding:
                case Phase.Moving:
                    if (path == null)
                    {
                        // No route: the squad holds its spot, facing the spawn's way.
                        squad.Phase = Phase.Holding;
                        squad.Heading = spawn.Rotation;
                        leader.Goal = Vector3.Distance(Flat(leader.Creature.Position), Flat(spawn.Position)) > PointReached ? spawn.Position : null;
                        break;
                    }

                    if (squad.Phase == Phase.Holding)
                    {
                        // At the end of a route walked once: it stays there.
                        leader.Goal = null;
                        break;
                    }

                    if (squad.PathIndex < 0 || squad.PathIndex >= path.Points.Count)
                        squad.PathIndex = Nearest(path, leader.Creature.Position);

                    var point = path.Points[squad.PathIndex];

                    if (Reached(leader.Creature.Position, point.Position))
                    {
                        leader.Goal = null;

                        if (!Advance(squad, path))
                        {
                            squad.Phase = Phase.Holding;
                            break;
                        }

                        if (point.PauseMs > 0)
                        {
                            squad.Phase = Phase.Pausing;
                            squad.PauseUntil = now + point.PauseMs;
                        }

                        break;
                    }

                    // Facing the way he is going: the next corner of his route over the navmesh,
                    // which on a winding road is not the way to the point.
                    var toward = point.Position;
                    var controller = leader.Creature.Controller;

                    if (controller.PathIndex < controller.Path.Count &&
                        (Flat(controller.Path[controller.PathIndex]) - Flat(leader.Creature.Position)).LengthSquared() > 1f)
                        toward = controller.Path[controller.PathIndex];

                    if ((Flat(toward) - Flat(leader.Creature.Position)).LengthSquared() > 1f)
                        squad.Heading = AbilityManager.YawTowards(leader.Creature.Position, toward);

                    // Anyone well behind his place: the leader waits for him.
                    var behind = living.Skip(1).Any(member => member.Creature.WalkSpeed >= 0.01f &&
                        Vector3.Distance(Flat(member.Creature.Position), Flat(PlaceOf(map, type, member.Place, leader.Creature.Position, squad.Heading))) > WaitDistance);

                    leader.Goal = behind ? null : point.Position;
                    break;
            }

            leader.Speed = pace;
            leader.Facing = squad.Heading;

            if (leader.Goal is Vector3 going)
                leader.Creature.HomePos.Position = going;
            else
                leader.Creature.HomePos.Position = leader.Creature.Position;

            foreach (var member in living.Skip(1))
            {
                var place = PlaceOf(map, type, member.Place, leader.Creature.Position, squad.Heading);
                var gap = Vector3.Distance(Flat(member.Creature.Position), Flat(place));

                member.Goal = place;
                member.Speed = gap > CatchUpDistance ? Math.Max(pace, member.Creature.RunSpeed)
                    : gap > CloseUpDistance ? Math.Max(pace, Math.Min(member.Creature.RunSpeed, pace * CloseUpPace))
                    : pace;
                member.Facing = squad.Heading;
                member.Creature.HomePos.Position = place;
            }
        }

        /// <summary>On to the next point of the route. False when there is none: a route walked once, at its end.</summary>
        private static bool Advance(LiveSquad squad, SquadPath path)
        {
            var count = path.Points.Count;

            // One point: there is nowhere else to go, whatever the mode.
            if (count == 1)
                return false;

            switch (path.Mode)
            {
                case PathMode.Loop:
                    squad.PathIndex = (squad.PathIndex + 1) % count;
                    return true;

                case PathMode.Back:
                    if (squad.PathIndex + squad.Direction < 0 || squad.PathIndex + squad.Direction >= count)
                        squad.Direction = -squad.Direction;
                    squad.PathIndex += squad.Direction;
                    return true;

                default:
                    if (squad.PathIndex + 1 >= count)
                        return false;
                    squad.PathIndex++;
                    return true;
            }
        }

        private static int Nearest(SquadPath path, Vector3 from)
        {
            var best = 0;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < path.Points.Count; i++)
            {
                var distance = Vector3.DistanceSquared(from, path.Points[i].Position);

                if (distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static bool Reached(Vector3 at, Vector3 point) =>
            Vector3.Distance(Flat(at), Flat(point)) <= PointReached && MathF.Abs(at.Y - point.Y) <= PointReachedHeight;

        private static Vector3 Flat(Vector3 v) => new(v.X, 0, v.Z);

        /// <summary>
        /// Where the member in <paramref name="place"/> stands about a leader at <paramref name="leader"/>
        /// facing <paramref name="heading"/>: in a wedge the others fan out behind him to either side, in
        /// a column they follow him in file, in a line they stand abreast of him. On the navmesh where
        /// there is one.
        /// </summary>
        public static Vector3 PlaceOf(MapChannel map, SquadType type, int place, Vector3 leader, float heading)
        {
            if (place <= 0)
                return leader;

            var offset = Offset(type?.Formation ?? Formation.Wedge, type?.Spacing ?? (float)DefaultSpacing, place);

            // YawTowards' frame: a yaw of 0 looks along -z, and forward is (-sin, -cos).
            var forward = new Vector3(-MathF.Sin(heading), 0, -MathF.Cos(heading));
            var right = new Vector3(-forward.Z, 0, forward.X);
            var along = right * offset.X + forward * offset.Y;
            var mesh = map?.NavMesh;

            if (mesh == null)
                return leader + along;

            // Where he can walk to from the leader in a straight line: past a wall or over a drop,
            // nearer the leader on the same line, and at the last on the leader himself - never the
            // other side of a wall or the floor below, which would take him the long way round.
            foreach (var share in PlaceShares)
                if (mesh.WalkTo(leader, leader + along * share) is Vector3 walkable)
                    return walkable;

            return leader;
        }

        private static readonly float[] PlaceShares = { 1f, 0.66f, 0.33f };

        /// <summary>The place's offset from the leader: across (to his right) and ahead of him, in metres.</summary>
        public static Vector2 Offset(Formation formation, float spacing, int place)
        {
            var side = place % 2 == 1 ? -1f : 1f;
            var rank = (place + 1) / 2;

            return formation switch
            {
                Formation.Column => new Vector2(0, -place * spacing),
                Formation.Line => new Vector2(side * rank * spacing, 0),
                _ => new Vector2(side * rank * spacing, -rank * spacing)
            };
        }

        private static bool Alive(Creature creature) =>
            creature != null && creature.State != CharacterState.Dead && creature.State != CharacterState.Dying &&
            creature.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0 &&
            EntityManager.Instance.Creatures.TryGetValue(creature.EntityId, out var registered) && ReferenceEquals(registered, creature);

        /// <summary>Forgets every map's squads; for tests.</summary>
        public static void Reset() => States.Clear();

        #endregion

        #region A GM's

        /// <summary>The path each GM is recording, by character: what .squad point adds to.</summary>
        private static readonly Dictionary<uint, uint> Recording = new();

        public const string Help =
            ".squad [types|paths|spawns|export] | .squad type new <name> | .squad type <id> [add <creature> [min] [max]|remove <slot>|formation wedge|column|line|spacing <m>|engage <m>|tether <m>|bystander on|off|name <name>|delete] | " +
            ".squad path new <name> [loop|back|once] | .squad path <id> [record|add [how]|insert <step> [how]|set <step> [how]|move <step>|undo|remove <step>|mode loop|back|once|name <name>|delete] | " +
            ".squad point [how] - how: [pause s] [run|walk] [engage <m>] | " +
            ".squad spawn add <type> [path] [respawn s] [comment] | .squad spawn <id> [type|path|respawn <v>|comment <text>|arrival none|teleport|move|enable|disable|send|delete]";

        /// <summary>.squad: the squads, and a GM's hand in them. Returns what to tell the GM.</summary>
        public static List<string> Command(MapChannel map, Manifestation gm, string[] parts)
        {
            var say = new List<string>();
            var args = parts.Skip(1).ToArray();
            var sub = args.Length > 0 ? args[0].ToLowerInvariant() : string.Empty;
            var mapContextId = map?.MapInfo?.MapContextId ?? 0;

            try
            {
                switch (sub)
                {
                    case "":
                        Status(map, gm, say);
                        break;
                    case "types":
                        foreach (var type in _types.Values.OrderBy(t => t.Id))
                            say.Add(Describe(type));
                        if (_types.Count == 0)
                            say.Add("No squad types. .squad type new <name>");
                        break;
                    case "paths":
                        foreach (var path in _paths.Values.Where(p => p.MapContextId == mapContextId).OrderBy(p => p.Id))
                            say.Add(Describe(path, gm));
                        if (!_paths.Values.Any(p => p.MapContextId == mapContextId))
                            say.Add("No squad paths on this map. .squad path new <name>");
                        break;
                    case "spawns":
                        foreach (var spawn in _spawns.Values.Where(s => s.MapContextId == mapContextId).OrderBy(s => Vector3.Distance(s.Position, gm.Position)))
                            say.Add(Describe(spawn, map, gm));
                        if (!_spawns.Values.Any(s => s.MapContextId == mapContextId))
                            say.Add("No squad spawns on this map. .squad spawn add <type> [path]");
                        break;
                    case "type":
                        TypeCommand(args.Skip(1).ToArray(), say);
                        break;
                    case "path":
                        PathCommand(mapContextId, gm, args.Skip(1).ToArray(), say);
                        break;
                    case "point":
                        if (!Recording.TryGetValue(gm.Id, out var recording) || !_paths.ContainsKey(recording))
                            say.Add("You are recording no path: .squad path new <name>, or .squad path <id> record.");
                        else
                            PathCommand(mapContextId, gm, new[] { recording.ToString(), "add" }.Concat(args.Skip(1)).ToArray(), say);
                        break;
                    case "spawn":
                        SpawnCommand(map, gm, args.Skip(1).ToArray(), say);
                        break;
                    case "export":
                        Export(args.Length > 1 && args[1].Equals("all", StringComparison.OrdinalIgnoreCase) ? 0 : mapContextId, say);
                        break;
                    default:
                        say.Add(Help);
                        break;
                }
            }
            catch (FormatException)
            {
                say.Add("A number was not a number. " + Help);
            }

            return say;
        }

        private static void Status(MapChannel map, Manifestation gm, List<string> say)
        {
            var mapContextId = map?.MapInfo?.MapContextId ?? 0;
            var spawns = _spawns.Values.Where(s => s.MapContextId == mapContextId).ToList();

            say.Add($"{_types.Count} squad type(s), {_paths.Values.Count(p => p.MapContextId == mapContextId)} path(s) and {spawns.Count} spawn(s) on this map.");

            foreach (var spawn in spawns.OrderBy(s => Vector3.Distance(s.Position, gm.Position)).Take(10))
                say.Add(Describe(spawn, map, gm));

            if (Recording.TryGetValue(gm.Id, out var recording) && _paths.TryGetValue(recording, out var path))
                say.Add($"Recording path {path.Id} ({path.Name}), {path.Points.Count} point(s): .squad point [pause s]");
        }

        private static void TypeCommand(string[] args, List<string> say)
        {
            if (args.Length == 0)
            {
                say.Add(".squad type new <name> | .squad type <id> ...");
                return;
            }

            if (args[0].Equals("new", StringComparison.OrdinalIgnoreCase))
            {
                var type = new SquadType
                {
                    Id = NextId(_types.Keys), Name = Text(args.Skip(1), "Squad"), Formation = Formation.Wedge,
                    Spacing = (float)DefaultSpacing
                };

                _types[type.Id] = type;
                say.Add($"{Saved(SaveType(type))}Squad type {type.Id}: {type.Name}. Add its members: .squad type {type.Id} add <creature id> [min] [max]");
                return;
            }

            if (!uint.TryParse(args[0], out var id) || !_types.TryGetValue(id, out var found))
            {
                say.Add($"No squad type {args[0]}. .squad types");
                return;
            }

            var verb = args.Length > 1 ? args[1].ToLowerInvariant() : string.Empty;

            switch (verb)
            {
                case "":
                    say.Add(Describe(found));
                    foreach (var line in found.Members)
                        say.Add($"  slot {line.Slot}: creature {line.CreatureId} {CreatureName(line.CreatureId)} x{line.MinCount}-{line.MaxCount}");
                    return;

                case "add":
                    var creatureId = uint.Parse(args[2], CultureInfo.InvariantCulture);
                    var min = args.Length > 3 ? byte.Parse(args[3], CultureInfo.InvariantCulture) : (byte)1;
                    var max = args.Length > 4 ? byte.Parse(args[4], CultureInfo.InvariantCulture) : min;

                    if (CreatureManager.Instance.LoadedCreatures?.ContainsKey(creatureId) == false)
                    {
                        say.Add($"No creature {creatureId}.");
                        return;
                    }

                    found.Members.Add(new MemberLine
                    {
                        Slot = (byte)(found.Members.Count == 0 ? 0 : found.Members.Max(m => m.Slot) + 1),
                        CreatureId = creatureId, MinCount = min, MaxCount = Math.Max(min, max)
                    });
                    break;

                case "remove":
                    var slot = byte.Parse(args[2], CultureInfo.InvariantCulture);
                    if (found.Members.RemoveAll(m => m.Slot == slot) == 0)
                    {
                        say.Add($"Squad type {id} has no slot {slot}.");
                        return;
                    }
                    break;

                case "formation":
                    found.Formation = Enum.Parse<Formation>(args[2], true);
                    break;

                case "spacing":
                    found.Spacing = Math.Max(0.5f, float.Parse(args[2], CultureInfo.InvariantCulture));
                    break;

                case "engage":
                    found.EngageRange = Math.Max(0f, float.Parse(args[2], CultureInfo.InvariantCulture));
                    break;

                case "tether":
                    if (args.Length < 3 || !float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var tether) ||
                        !float.IsFinite(tether) || tether < 0)
                    {
                        say.Add($".squad type {id} tether <m>: how far from the leader a member goes in a fight, 0 for no tether.");
                        return;
                    }
                    found.Tether = tether;
                    break;

                case "bystander":
                    if (args.Length < 3 || args[2].ToLowerInvariant() is not ("on" or "off"))
                    {
                        say.Add($".squad type {id} bystander on|off: whether the leader it is sent with takes up no fight and its members guard him.");
                        return;
                    }
                    found.LeaderBystander = args[2].ToLowerInvariant() == "on";
                    break;

                case "name":
                    found.Name = Text(args.Skip(2), found.Name);
                    break;

                case "delete":
                    _types.Remove(id);
                    foreach (var spawn in _spawns.Values.Where(s => s.TypeId == id).ToList())
                        _spawns.Remove(spawn.Id);
                    say.Add($"{Saved(Write(squads => squads.DeleteType(id)))}Squad type {id} deleted, with its spawns.");
                    return;

                default:
                    say.Add(Help);
                    return;
            }

            say.Add($"{Saved(SaveType(found))}{Describe(found)}. Squads already out keep what they were sent with.");
        }

        private static void PathCommand(uint mapContextId, Manifestation gm, string[] args, List<string> say)
        {
            if (args.Length == 0)
            {
                say.Add(".squad path new <name> [loop|back|once] | .squad path <id> ...");
                return;
            }

            if (args[0].Equals("new", StringComparison.OrdinalIgnoreCase))
            {
                var words = args.Skip(1).ToList();
                var mode = PathMode.Loop;

                if (words.Count > 1 && Enum.TryParse<PathMode>(words[^1], true, out var given))
                {
                    mode = given;
                    words.RemoveAt(words.Count - 1);
                }

                var path = new SquadPath { Id = NextId(_paths.Keys), MapContextId = mapContextId, Name = Text(words, "Path"), Mode = mode };

                _paths[path.Id] = path;
                Recording[gm.Id] = path.Id;
                say.Add($"{Saved(SavePath(path))}Path {path.Id}: {path.Name} ({mode}), recording. Walk it: .squad point [pause s] at each point.");
                return;
            }

            if (!uint.TryParse(args[0], out var id) || !_paths.TryGetValue(id, out var found))
            {
                say.Add($"No squad path {args[0]}. .squad paths");
                return;
            }

            var verb = args.Length > 1 ? args[1].ToLowerInvariant() : string.Empty;

            switch (verb)
            {
                case "":
                    say.Add(Describe(found, gm));
                    for (var i = 0; i < found.Points.Count; i++)
                        say.Add($"  {i}: .teleport {Coords(found.Points[i].Position)} {found.MapContextId}{How(found.Points[i])}");
                    return;

                case "record":
                    Recording[gm.Id] = id;
                    say.Add($"Recording path {id} ({found.Name}): .squad point [pause s] adds where you stand.");
                    return;

                case "add":
                    if (found.MapContextId != mapContextId)
                    {
                        say.Add($"Path {id} is on map {found.MapContextId}, not this one.");
                        return;
                    }

                    var added = HowOf(args.Skip(2), new PathPoint(gm.Position, 0));
                    found.Points.Add(added);
                    say.Add($"{Saved(SavePath(found))}Path {id}: point {found.Points.Count - 1} at {Coords(gm.Position)}{How(added)}.");
                    return;

                case "insert":
                case "set":
                case "move":
                {
                    if (found.MapContextId != mapContextId && verb != "set")
                    {
                        say.Add($"Path {id} is on map {found.MapContextId}, not this one.");
                        return;
                    }

                    var at = int.Parse(args[2], CultureInfo.InvariantCulture);
                    if (at < 0 || at > found.Points.Count || (verb != "insert" && at == found.Points.Count))
                    {
                        say.Add($"Path {id} has no point {at}.");
                        return;
                    }

                    if (verb == "insert")
                        found.Points.Insert(at, HowOf(args.Skip(3), new PathPoint(gm.Position, 0)));
                    else if (verb == "set")
                        found.Points[at] = HowOf(args.Skip(3), found.Points[at]);
                    else
                        found.Points[at] = found.Points[at] with { Position = gm.Position };

                    say.Add($"{Saved(SavePath(found))}Path {id}: point {at} at {Coords(found.Points[at].Position)}{How(found.Points[at])}.");
                    return;
                }

                case "undo":
                    if (found.Points.Count == 0)
                    {
                        say.Add($"Path {id} has no points.");
                        return;
                    }
                    found.Points.RemoveAt(found.Points.Count - 1);
                    break;

                case "remove":
                    var step = int.Parse(args[2], CultureInfo.InvariantCulture);
                    if (step < 0 || step >= found.Points.Count)
                    {
                        say.Add($"Path {id} has no point {step}.");
                        return;
                    }
                    found.Points.RemoveAt(step);
                    break;

                case "mode":
                    found.Mode = Enum.Parse<PathMode>(args[2], true);
                    break;

                case "name":
                    found.Name = Text(args.Skip(2), found.Name);
                    break;

                case "delete":
                    _paths.Remove(id);
                    foreach (var spawn in _spawns.Values.Where(s => s.PathId == id))
                        spawn.PathId = 0;
                    foreach (var key in Recording.Where(r => r.Value == id).Select(r => r.Key).ToList())
                        Recording.Remove(key);
                    say.Add($"{Saved(Write(squads => squads.DeletePath(id)))}Path {id} deleted; its squads hold where they are.");
                    return;

                default:
                    say.Add(Help);
                    return;
            }

            say.Add($"{Saved(SavePath(found))}{Describe(found, gm)}");
        }

        private static void SpawnCommand(MapChannel map, Manifestation gm, string[] args, List<string> say)
        {
            var mapContextId = map?.MapInfo?.MapContextId ?? 0;

            if (args.Length == 0)
            {
                say.Add(".squad spawn add <type> [path] [respawn s] [comment] | .squad spawn <id> ...");
                return;
            }

            if (args[0].Equals("add", StringComparison.OrdinalIgnoreCase))
            {
                var typeId = uint.Parse(args[1], CultureInfo.InvariantCulture);
                var pathId = args.Length > 2 ? uint.Parse(args[2], CultureInfo.InvariantCulture) : 0u;
                var respawn = args.Length > 3 ? uint.Parse(args[3], CultureInfo.InvariantCulture) : (uint)DefaultRespawnSeconds;

                if (!_types.ContainsKey(typeId))
                {
                    say.Add($"No squad type {typeId}. .squad types");
                    return;
                }

                if (pathId != 0 && (!_paths.TryGetValue(pathId, out var path) || path.MapContextId != mapContextId))
                {
                    say.Add($"No squad path {pathId} on this map. .squad paths");
                    return;
                }

                var spawn = new SquadSpawn
                {
                    Id = NextId(_spawns.Keys), MapContextId = mapContextId, TypeId = typeId, PathId = pathId,
                    Position = gm.Position, Rotation = (float)gm.Rotation, RespawnSeconds = respawn, Enabled = true,
                    Comment = Text(args.Skip(4), _types[typeId].Name)
                };

                _spawns[spawn.Id] = spawn;
                say.Add($"{Saved(SaveSpawn(spawn))}Squad spawn {spawn.Id}: {Describe(spawn, map, gm)}");
                return;
            }

            if (!uint.TryParse(args[0], out var id) || !_spawns.TryGetValue(id, out var found))
            {
                say.Add($"No squad spawn {args[0]}. .squad spawns");
                return;
            }

            var verb = args.Length > 1 ? args[1].ToLowerInvariant() : string.Empty;
            var resend = false;

            switch (verb)
            {
                case "":
                    say.Add(Describe(found, map, gm));
                    return;

                case "type":
                    var typeId = uint.Parse(args[2], CultureInfo.InvariantCulture);
                    if (!_types.ContainsKey(typeId))
                    {
                        say.Add($"No squad type {typeId}.");
                        return;
                    }
                    found.TypeId = typeId;
                    resend = true;
                    break;

                case "path":
                    var pathId = uint.Parse(args[2], CultureInfo.InvariantCulture);
                    if (pathId != 0 && (!_paths.TryGetValue(pathId, out var path) || path.MapContextId != found.MapContextId))
                    {
                        say.Add($"No squad path {pathId} on the spawn's map.");
                        return;
                    }
                    found.PathId = pathId;
                    if (map != null && On(map).TryGetValue(id, out var walking))
                        walking.PathIndex = -1;
                    break;

                case "respawn":
                    found.RespawnSeconds = uint.Parse(args[2], CultureInfo.InvariantCulture);
                    break;

                case "comment":
                    found.Comment = Text(args.Skip(2), found.Comment);
                    break;

                case "move":
                    if (found.MapContextId != mapContextId)
                    {
                        say.Add($"Spawn {id} is on map {found.MapContextId}, not this one.");
                        return;
                    }
                    found.Position = gm.Position;
                    found.Rotation = (float)gm.Rotation;
                    resend = true;
                    break;

                case "arrival":
                    found.Arrival = Enum.Parse<Arrival>(args[2], true);
                    break;

                case "enable":
                    found.Enabled = true;
                    break;

                case "disable":
                    found.Enabled = false;
                    break;

                case "send":
                    resend = true;
                    break;

                case "delete":
                    _spawns.Remove(id);
                    say.Add($"{Saved(Write(squads => squads.DeleteSpawn(id)))}Squad spawn {id} deleted; its squad goes.");
                    return;

                default:
                    say.Add(Help);
                    return;
            }

            // Sent again now, where it is and as it is, on this map's channel.
            if (resend && map != null && On(map).TryGetValue(id, out var squad))
            {
                TakeAway(map, squad);
                squad.RespawnAt = 0;
            }

            say.Add($"{Saved(verb == "send" || SaveSpawn(found))}{Describe(found, map, gm)}");
        }

        private static string Describe(SquadType type) =>
            $"Type {type.Id} {type.Name}: {type.Formation}, {type.Spacing:0.#} m apart, engages at {(type.EngageRange > 0 ? $"{type.EngageRange:0.#} m" : "its creatures' range")}, " +
            $"{(type.Tether > 0 ? $"tethered at {type.Tether:0.#} m" : "no tether")}{(type.LeaderBystander ? ", its leader a bystander" : string.Empty)}; " +
            (type.Members.Count == 0 ? "no members" : string.Join(", ", type.Members.Select(m => $"{m.CreatureId} {CreatureName(m.CreatureId)} x{m.MinCount}-{m.MaxCount}")));

        private static string Describe(SquadPath path, Manifestation gm) =>
            $"Path {path.Id} {path.Name} on map {path.MapContextId}: {path.Mode}, {path.Points.Count} point(s), {Length(path):0} m" +
            (path.Points.Count > 0 ? $", nearest point {Nearest(path, gm.Position)} {Vector3.Distance(path.Points[Nearest(path, gm.Position)].Position, gm.Position):0} m away" : "");

        private static string Describe(SquadSpawn spawn, MapChannel map, Manifestation gm)
        {
            var type = _types.TryGetValue(spawn.TypeId, out var t) ? t.Name : "(no type)";
            var path = spawn.PathId == 0 ? "holds its spot" : _paths.TryGetValue(spawn.PathId, out var p) ? $"walks path {p.Id} {p.Name}" : $"walks missing path {spawn.PathId}";
            var live = map != null && On(map).TryGetValue(spawn.Id, out var squad) ? squad : null;
            var state = !spawn.Enabled ? "disabled" :
                live == null ? "not sent yet" :
                live.Phase == Phase.Gone ? $"gone, back in {Math.Max(0, live.RespawnAt - Now()) / 1000} s" :
                $"{live.Living.Count()} out, {live.Phase}{(live.PathIndex >= 0 && live.Phase != Phase.Holding ? $" to point {live.PathIndex}" : "")}";

            return $"Spawn {spawn.Id} {spawn.Comment}: type {spawn.TypeId} {type}, {path}, {(spawn.Arrival == Arrival.Teleport ? "teleports in, " : "")}respawn {spawn.RespawnSeconds} s, {state}, " +
                $"{Vector3.Distance(spawn.Position, gm.Position):0} m away (.teleport {Coords(spawn.Position)} {spawn.MapContextId})";
        }

        private static float Length(SquadPath path)
        {
            var length = 0f;

            for (var i = 1; i < path.Points.Count; i++)
                length += Vector3.Distance(path.Points[i - 1].Position, path.Points[i].Position);

            return length;
        }

        private static string CreatureName(uint creatureId) =>
            CreatureManager.Instance.LoadedCreatures != null && CreatureManager.Instance.LoadedCreatures.TryGetValue(creatureId, out var creature)
                ? $"(class {creature.EntityClass}{(string.IsNullOrEmpty(creature.ActorName) ? "" : ", " + creature.ActorName)})" : "";

        private static string Coords(Vector3 v) => FormattableString.Invariant($"{v.X:0.##} {v.Y:0.##} {v.Z:0.##}");

        /// <summary>A point's pause, pace and range, as words: [pause s] [run|walk] [engage m], in any order.</summary>
        private static PathPoint HowOf(IEnumerable<string> words, PathPoint point)
        {
            var list = words.ToList();

            for (var i = 0; i < list.Count; i++)
            {
                var word = list[i].ToLowerInvariant();

                if (word == "run")
                    point = point with { Run = true };
                else if (word == "walk")
                    point = point with { Run = false };
                else if (word == "engage" && i + 1 < list.Count)
                    point = point with { EngageRange = Math.Max(0f, float.Parse(list[++i], CultureInfo.InvariantCulture)) };
                else
                    point = point with { PauseMs = (uint)Math.Max(0, double.Parse(word, CultureInfo.InvariantCulture) * 1000) };
            }

            return point;
        }

        private static string How(PathPoint point) =>
            (point.Run ? ", run to" : "") +
            (point.PauseMs > 0 ? FormattableString.Invariant($", stands {point.PauseMs / 1000.0:0.#} s") : "") +
            (point.EngageRange > 0 ? FormattableString.Invariant($", engages at {point.EngageRange:0.#} m") : "");

        private static string Text(IEnumerable<string> words, string fallback)
        {
            var text = string.Join(" ", words).Trim();

            if (text.Length == 0)
                return fallback;

            return text.Length > 64 ? text.Substring(0, 64) : text;
        }

        private static uint NextId(IEnumerable<uint> ids) => ids.DefaultIfEmpty(0u).Max() + 1;

        private static string Saved(bool saved) => saved ? string.Empty : "NOT SAVED to the database (see the log): ";

        #endregion

        #region Saving and export

        private static bool SaveType(SquadType type) =>
            Write(squads => squads.SaveType(
                new NpcSquadTypeEntry { Id = type.Id, Name = type.Name, Formation = (byte)type.Formation, Spacing = type.Spacing, EngageRange = type.EngageRange, Tether = type.Tether,
                    LeaderBystander = (byte)(type.LeaderBystander ? 1 : 0) },
                type.Members.Select(m => new NpcSquadMemberEntry { TypeId = type.Id, Slot = m.Slot, CreatureId = m.CreatureId, MinCount = m.MinCount, MaxCount = m.MaxCount }).ToList()));

        private static bool SavePath(SquadPath path) =>
            Write(squads => squads.SavePath(
                new NpcSquadPathEntry { Id = path.Id, MapContextId = path.MapContextId, Name = path.Name, Mode = (byte)path.Mode },
                path.Points.Select((point, i) => new NpcSquadPathPointEntry
                {
                    PathId = path.Id, Step = (uint)i, PosX = point.Position.X, PosY = point.Position.Y, PosZ = point.Position.Z, PauseMs = point.PauseMs,
                    Run = (byte)(point.Run ? 1 : 0), EngageRange = point.EngageRange
                }).ToList()));

        private static bool SaveSpawn(SquadSpawn spawn) =>
            Write(squads => squads.SaveSpawn(new NpcSquadSpawnEntry
            {
                Id = spawn.Id, MapContextId = spawn.MapContextId, TypeId = spawn.TypeId, PathId = spawn.PathId,
                PosX = spawn.Position.X, PosY = spawn.Position.Y, PosZ = spawn.Position.Z, Rotation = spawn.Rotation,
                RespawnSeconds = spawn.RespawnSeconds, Enabled = (byte)(spawn.Enabled ? 1 : 0), Comment = spawn.Comment ?? string.Empty,
                Arrival = (byte)spawn.Arrival
            }));

        private static bool Write(Func<Repositories.World.INpcSquadRepository, bool> write)
        {
            if (_unitOfWorkFactory == null)
                return false;

            try
            {
                using var unitOfWork = _unitOfWorkFactory.CreateWorld();
                return write(unitOfWork.NpcSquads);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error writing NPC squads:");
                Logger.WriteLog(LogType.Error, e);
                return false;
            }
        }

        /// <summary>The rows of a map's squads (0 for every map's) as SQL, plain and valid on SQLite and MySQL alike.</summary>
        public static string ExportSql(uint mapContextId)
        {
            var spawns = _spawns.Values.Where(s => mapContextId == 0 || s.MapContextId == mapContextId).OrderBy(s => s.Id).ToList();
            var paths = _paths.Values.Where(p => mapContextId == 0 || p.MapContextId == mapContextId).OrderBy(p => p.Id).ToList();
            var types = _types.Values.Where(t => mapContextId == 0 || spawns.Any(s => s.TypeId == t.Id)).OrderBy(t => t.Id).ToList();
            var sql = new StringBuilder();

            sql.AppendLine(FormattableString.Invariant($"-- NPC squads {(mapContextId == 0 ? "of every map" : $"of map {mapContextId}")}: {types.Count} type(s), {paths.Count} path(s), {spawns.Count} spawn(s)"));

            foreach (var type in types)
            {
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO npc_squad_type (id, name, formation, spacing, engage_range, tether, leader_bystander) VALUES ({type.Id}, {Quote(type.Name)}, {(byte)type.Formation}, {N(type.Spacing)}, {N(type.EngageRange)}, {N(type.Tether)}, {(type.LeaderBystander ? 1 : 0)});"));

                foreach (var line in type.Members)
                    sql.AppendLine(FormattableString.Invariant($"INSERT INTO npc_squad_member (type_id, slot, creature_id, min_count, max_count) VALUES ({type.Id}, {line.Slot}, {line.CreatureId}, {line.MinCount}, {line.MaxCount});"));
            }

            foreach (var path in paths)
            {
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO npc_squad_path (id, map_context_id, name, mode) VALUES ({path.Id}, {path.MapContextId}, {Quote(path.Name)}, {(byte)path.Mode});"));

                for (var i = 0; i < path.Points.Count; i++)
                {
                    var point = path.Points[i];
                    sql.AppendLine(FormattableString.Invariant($"INSERT INTO npc_squad_path_point (path_id, step, pos_x, pos_y, pos_z, pause_ms, run, engage_range) VALUES ({path.Id}, {i}, {N(point.Position.X)}, {N(point.Position.Y)}, {N(point.Position.Z)}, {point.PauseMs}, {(point.Run ? 1 : 0)}, {N(point.EngageRange)});"));
                }
            }

            foreach (var spawn in spawns)
                sql.AppendLine(FormattableString.Invariant(
                    $"INSERT INTO npc_squad_spawn (id, map_context_id, type_id, path_id, pos_x, pos_y, pos_z, rotation, respawn_seconds, enabled, comment, arrival) VALUES ({spawn.Id}, {spawn.MapContextId}, {spawn.TypeId}, {spawn.PathId}, {N(spawn.Position.X)}, {N(spawn.Position.Y)}, {N(spawn.Position.Z)}, {N(spawn.Rotation)}, {spawn.RespawnSeconds}, {(spawn.Enabled ? 1 : 0)}, {Quote(spawn.Comment)}, {(byte)spawn.Arrival});"));

            return sql.ToString();
        }

        /// <summary>Where .squad export writes: a folder beside the server.</summary>
        public static string ExportDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "exports");

        private static void Export(uint mapContextId, List<string> say)
        {
            var sql = ExportSql(mapContextId);
            Directory.CreateDirectory(ExportDirectory);

            var file = Path.Combine(ExportDirectory, FormattableString.Invariant($"npc-squads-{(mapContextId == 0 ? "all" : mapContextId.ToString())}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.sql"));
            File.WriteAllText(file, sql);

            Logger.WriteLog(LogType.Initialize, $"NPC squads exported to {file}");
            say.Add($"{sql.Split('\n').First().TrimStart('-', ' ')} written to {file}");
        }

        private static string Quote(string text) => "'" + (text ?? string.Empty).Replace("\\", "/").Replace("'", "''") + "'";

        /// <summary>A float as SQL: its shortest round-tripping decimal.</summary>
        private static string N(float value) => ((double)value).ToString("R", CultureInfo.InvariantCulture);

        #endregion
    }
}
