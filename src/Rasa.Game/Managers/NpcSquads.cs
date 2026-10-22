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
    ///    (a wedge, a column or a line abreast, so many metres apart); and how far its members look
    ///    for a fight (0 for each creature's own aggro range).
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
    /// falls is replaced by the next of them. Each map channel has its own squads - every copy of
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

        public enum Phase { Moving, Pausing, Holding, Fighting, Regrouping, Gone }

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

        #region Definitions

        public sealed class SquadType
        {
            public uint Id { get; init; }
            public string Name { get; set; }
            public Formation Formation { get; set; }
            public float Spacing { get; set; }
            public float EngageRange { get; set; }
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

        public sealed record PathPoint(Vector3 Position, uint PauseMs);

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
                Spacing = row.Spacing > 0 ? (float)row.Spacing : (float)DefaultSpacing, EngageRange = (float)Math.Max(0, row.EngageRange)
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
                    path.Points.Add(new PathPoint(row.Position, row.PauseMs));
                else
                    Logger.WriteLog(LogType.Error, $"npc_squad_path_point {row.PathId}/{row.Step}: no such path, or not a place; left out");

            _spawns = (spawns ?? Enumerable.Empty<NpcSquadSpawnEntry>()).ToDictionary(row => row.Id, row => new SquadSpawn
            {
                Id = row.Id, MapContextId = row.MapContextId, TypeId = row.TypeId, PathId = row.PathId, Position = row.Position,
                Rotation = (float)row.Rotation, RespawnSeconds = row.RespawnSeconds, Enabled = row.Enabled != 0, Comment = row.Comment ?? string.Empty
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

        /// <summary>How far the creature, one of a squad's, looks for a fight.</summary>
        public static float EngageRange(Creature creature)
        {
            var type = creature?.SquadMember != null && _types.TryGetValue(creature.SquadMember.Squad.Spawn.TypeId, out var t) ? t : null;

            return type != null && type.EngageRange > 0 ? type.EngageRange : creature?.AggroRange ?? 0;
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

            foreach (var dbId in creatures)
            {
                var creature = CreatureManager.Instance.CreateCreature(dbId, null);

                if (creature == null)
                {
                    Logger.WriteLog(LogType.Error, $"npc_squad_spawn {spawn.Id}: creature {dbId} could not be made; left out");
                    continue;
                }

                var member = new Member(squad, creature) { Place = squad.Members.Count };
                var at = PlaceOf(map, type, member.Place, spawn.Position, squad.Heading);

                CreatureManager.Instance.SetLocation(creature, at, squad.Heading, spawn.MapContextId);
                creature.LastYaw = squad.Heading;
                creature.SquadMember = member;
                squad.Members.Add(member);

                CellManager.Instance.AddToWorld(map, creature);
                BehaviorManager.Instance.SetActionSquad(creature);
            }

            squad.PathIndex = -1;
            squad.Direction = 1;
            squad.Phase = squad.Members.Count == 0 ? Phase.Gone : Phase.Moving;

            if (squad.Members.Count == 0)
                squad.RespawnAt = now + Math.Max(10u, spawn.RespawnSeconds) * 1000L;

            Logger.WriteLog(LogType.Debug, $"Squad spawn {spawn.Id} ({type.Name}) sent {squad.Members.Count} on map {spawn.MapContextId}.");

            return squad.Members.Count;
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
            squad.Phase = Phase.Gone;
            squad.RespawnAt = 0;
        }

        /// <summary>One think of a squad with someone left in it.</summary>
        private static void Think(MapChannel map, LiveSquad squad, long now)
        {
            var spawn = squad.Spawn;
            _types.TryGetValue(spawn.TypeId, out var type);
            var living = squad.Living.OrderBy(member => member.Place).ToList();

            squad.Members.RemoveAll(member => !living.Contains(member));

            // The places closed up about whoever leads now.
            for (var i = 0; i < living.Count; i++)
                living[i].Place = i;

            var leader = living[0];
            var spacing = type?.Spacing ?? (float)DefaultSpacing;
            var movers = living.Where(member => member.Creature.WalkSpeed >= 0.01f).ToList();
            var pace = movers.Count > 0 ? Math.Max(1f, movers.Min(member => member.Creature.WalkSpeed)) : 1f;

            // In a fight: the squad stands, and whoever is not yet in it joins it.
            var fighting = living.Where(member => member.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting).ToList();

            if (fighting.Count > 0)
            {
                squad.Phase = Phase.Fighting;

                var target = fighting.Select(member => member.Creature.Controller.ActionFighting.TargetEntityId).FirstOrDefault(id => id != 0);

                foreach (var member in living.Except(fighting))
                {
                    member.Goal = null;

                    if (target != 0 && member.Creature.Controller.CurrentAction == BehaviorManager.BehaviorActionSquad &&
                        EntityManager.Instance.GetActor(target) is Actor enemy &&
                        Vector3.Distance(enemy.Position, member.Creature.Position) <= AssistRange)
                    {
                        member.Creature.Hate.Ensure(target, Threat.NoticedThreat);
                        BehaviorManager.Instance.SetActionFighting(member.Creature, target);
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

                    var flat = Flat(point.Position) - Flat(leader.Creature.Position);

                    if (flat.LengthSquared() > 1f)
                        squad.Heading = AbilityManager.YawTowards(leader.Creature.Position, point.Position);

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
            ".squad [types|paths|spawns|export] | .squad type new <name> | .squad type <id> [add <creature> [min] [max]|remove <slot>|formation wedge|column|line|spacing <m>|engage <m>|name <name>|delete] | " +
            ".squad path new <name> [loop|back|once] | .squad path <id> [record|add [pause s]|undo|remove <step>|mode loop|back|once|name <name>|delete] | .squad point [pause s] | " +
            ".squad spawn add <type> [path] [respawn s] [comment] | .squad spawn <id> [type|path|respawn <v>|comment <text>|move|enable|disable|send|delete]";

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
                        say.Add($"  {i}: .teleport {Coords(found.Points[i].Position)} {found.MapContextId}" +
                            (found.Points[i].PauseMs > 0 ? $"  stands {found.Points[i].PauseMs / 1000.0:0.#} s" : ""));
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

                    var pause = args.Length > 2 ? (uint)(double.Parse(args[2], CultureInfo.InvariantCulture) * 1000) : 0u;
                    found.Points.Add(new PathPoint(gm.Position, pause));
                    say.Add($"{Saved(SavePath(found))}Path {id}: point {found.Points.Count - 1} at {Coords(gm.Position)}" + (pause > 0 ? $", stands {pause / 1000.0:0.#} s" : "") + ".");
                    return;

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
            $"Type {type.Id} {type.Name}: {type.Formation}, {type.Spacing:0.#} m apart, engages at {(type.EngageRange > 0 ? $"{type.EngageRange:0.#} m" : "its creatures' range")}; " +
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

            return $"Spawn {spawn.Id} {spawn.Comment}: type {spawn.TypeId} {type}, {path}, respawn {spawn.RespawnSeconds} s, {state}, " +
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
                new NpcSquadTypeEntry { Id = type.Id, Name = type.Name, Formation = (byte)type.Formation, Spacing = type.Spacing, EngageRange = type.EngageRange },
                type.Members.Select(m => new NpcSquadMemberEntry { TypeId = type.Id, Slot = m.Slot, CreatureId = m.CreatureId, MinCount = m.MinCount, MaxCount = m.MaxCount }).ToList()));

        private static bool SavePath(SquadPath path) =>
            Write(squads => squads.SavePath(
                new NpcSquadPathEntry { Id = path.Id, MapContextId = path.MapContextId, Name = path.Name, Mode = (byte)path.Mode },
                path.Points.Select((point, i) => new NpcSquadPathPointEntry
                {
                    PathId = path.Id, Step = (uint)i, PosX = point.Position.X, PosY = point.Position.Y, PosZ = point.Position.Z, PauseMs = point.PauseMs
                }).ToList()));

        private static bool SaveSpawn(SquadSpawn spawn) =>
            Write(squads => squads.SaveSpawn(new NpcSquadSpawnEntry
            {
                Id = spawn.Id, MapContextId = spawn.MapContextId, TypeId = spawn.TypeId, PathId = spawn.PathId,
                PosX = spawn.Position.X, PosY = spawn.Position.Y, PosZ = spawn.Position.Z, Rotation = spawn.Rotation,
                RespawnSeconds = spawn.RespawnSeconds, Enabled = (byte)(spawn.Enabled ? 1 : 0), Comment = spawn.Comment ?? string.Empty
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
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO npc_squad_type (id, name, formation, spacing, engage_range) VALUES ({type.Id}, {Quote(type.Name)}, {(byte)type.Formation}, {N(type.Spacing)}, {N(type.EngageRange)});"));

                foreach (var line in type.Members)
                    sql.AppendLine(FormattableString.Invariant($"INSERT INTO npc_squad_member (type_id, slot, creature_id, min_count, max_count) VALUES ({type.Id}, {line.Slot}, {line.CreatureId}, {line.MinCount}, {line.MaxCount});"));
            }

            foreach (var path in paths)
            {
                sql.AppendLine(FormattableString.Invariant($"INSERT INTO npc_squad_path (id, map_context_id, name, mode) VALUES ({path.Id}, {path.MapContextId}, {Quote(path.Name)}, {(byte)path.Mode});"));

                for (var i = 0; i < path.Points.Count; i++)
                {
                    var point = path.Points[i];
                    sql.AppendLine(FormattableString.Invariant($"INSERT INTO npc_squad_path_point (path_id, step, pos_x, pos_y, pos_z, pause_ms) VALUES ({path.Id}, {i}, {N(point.Position.X)}, {N(point.Position.Y)}, {N(point.Position.Z)}, {point.PauseMs});"));
                }
            }

            foreach (var spawn in spawns)
                sql.AppendLine(FormattableString.Invariant(
                    $"INSERT INTO npc_squad_spawn (id, map_context_id, type_id, path_id, pos_x, pos_y, pos_z, rotation, respawn_seconds, enabled, comment) VALUES ({spawn.Id}, {spawn.MapContextId}, {spawn.TypeId}, {spawn.PathId}, {N(spawn.Position.X)}, {N(spawn.Position.Y)}, {N(spawn.Position.Z)}, {N(spawn.Rotation)}, {spawn.RespawnSeconds}, {(spawn.Enabled ? 1 : 0)}, {Quote(spawn.Comment)});"));

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
