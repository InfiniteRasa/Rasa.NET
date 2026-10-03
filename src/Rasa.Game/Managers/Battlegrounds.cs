using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Config;
    using Data;
    using Game;
    using Models;
    using Packets.Communicator.Server;
    using Packets.MapChannel.Server;
    using Packets.Protocol;
    using Packets.Team.Server;
    using Structures;
    using Structures.Char;

    /// <summary>
    /// The Edmund Range match: Red Team against Blue Team for three control points.
    ///
    /// From the client:
    ///  - the rules (its help page and the range's own briefing): "players divide themselves into
    ///    two teams"; "choose a team by going through the properly colored teleporter. Once
    ///    you're inside, there will be a short prep time until the teams have enough people";
    ///    a minute of preparation, and "the doors open as soon as the match begins"; a team wins
    ///    by holding all three control points once the minimum time (10 minutes) has passed, or
    ///    the most of them at the maximum (20), and if they are level by its kills; "Simulated
    ///    Bane are at each of the Control Points. All of the Bane at a Control Point must be
    ///    destroyed before that Control Point can be taken"; each team's base is "protected from
    ///    the enemy team" and has a hospital;
    ///  - nothing to queue with or sign up by: no method, no window. A player walks in;
    ///  - teams (client/team.py): JoinedTeam, LeftTeam, AddTeamMember, RemoveTeamMember and
    ///    SetNumberOfTeams, none of which the client asks for, and RED_TEAM 1, BLUE_TEAM 2;
    ///  - the scorekeeper (scorekeeperclient.py): ScoreBoardActive, ScoreBoardGameScore - the
    ///    clock and who holds each control point, by its own ids for them - and
    ///    ScoreBoardIndividualUpdate, a row a player; WonBattleground and LostBattleground;
    ///  - a team wargame generates prestige and has no death penalty (WARGAME_FLAGS_TEAM:
    ///    GENERATE_PRESTIGE, BLOCK_INTERACTIONS, IS_AGGRESSIVE - no REZ_SICKNESS, no WEAPON_DECAY);
    ///  - its messages: "You deserted Blue Team in the middle of a match. You cannot join Red Team
    ///    until you have finished a match on Blue Team", "You cannot join Blue Team.", "Wargame
    ///    has begun.", "Minimum game time has elapsed.", "A Control Point has been taken.",
    ///    "Wargame has ended. Preparation for the next match begins now.", "You must be level 45
    ///    or higher to enter this map.", and the squad's "The resulting Squad contains members on
    ///    different Teams";
    ///  - a control point's marker takes TEAM_OWNED with the team's id, and the ownable control
    ///    point object (OWNABLECONTROLPOINT, class 10000071) has two states, nobody's and a
    ///    team's, and shows whose by SetOwnerId - 1 red, 2 blue, as the team ids are.
    ///
    /// The rest was the server's, and is ours:
    ///  - A match belongs to a map channel: the map's own, and every shared copy of it
    ///    (MapChannelManager.Instances), each with its own teams, clock and control points.
    ///  - The teleporters are map links inside the map (MapLinkKind.TeamRed / TeamBlue /
    ///    TeamLeave): walking into a team's puts the player on it and in its base. A team that
    ///    already has more players than the other allows cannot be joined
    ///    (<see cref="BattlegroundConfig.MaxImbalance"/>).
    ///  - A match waits until each team has <see cref="BattlegroundConfig.MinPlayersPerTeam"/>,
    ///    prepares for <see cref="BattlegroundConfig.PrepSeconds"/>, and runs. Players may join
    ///    one that is running.
    ///  - The doors are a rule, the map having none: until the match runs a team's players are
    ///    kept inside their base - within <see cref="BattlegroundConfig.BaseRadius"/> of its
    ///    hospital - and at any time a player who goes into the other team's base is put back in
    ///    their own. Nobody is hurt by, or hurts, the other team from inside their own base.
    ///  - The teams are enemies (Pvp) only while the match runs: the wargame is the match.
    ///  - A player killed in the match dies and goes back to a hospital of their team's - the
    ///    base's, and that of a control point their team holds - with no Rez Trauma, no wear on
    ///    their gear, and PvP Safety as after any PvP death at a hospital.
    ///  - A control point is nobody's when a match begins, with its Simulated Bane standing. With
    ///    all of them dead it can be used, by a player of a team that does not hold it,
    ///    <see cref="BattlegroundConfig.CaptureSeconds"/> of an interruptible use, and is that
    ///    team's. The Bane do not come back until the next match; a point is taken from a team
    ///    the same way, over its defenders.
    ///  - Standing in the way back out of a base for <see cref="LeaveDwellMs"/> leaves the team
    ///    and returns the player to the staging area. Leaving a running match any way at all -
    ///    that, another map, a logout, a lost connection - is desertion: the other team cannot
    ///    be joined until a match on the first has been played to its end, and for
    ///    <see cref="BattlegroundConfig.LeaverLockoutMinutes"/> every other copy of the map is
    ///    closed to them: the door leads them back to the copy they left or nowhere, and no team
    ///    of another copy takes them. Both are remembered until the server restarts.
    ///  - A match with a team that has nobody left is the other team's. When the clock runs out
    ///    with the points and the kills level, nobody wins.
    ///  - Prestige: a kill generates what a clan feud's does and steals nothing
    ///    (PvpPrestige.TeamKill); a capture is worth its amount to a player once a point a
    ///    match; a win and a loss are worth theirs when the match has run its minimum time, and
    ///    nothing when it was cut short.
    ///  - A player with no team is kept to the staging area; game masters go where they like.
    ///  - No squad holds players of both teams: joining a team takes a player out of a squad
    ///    that has somebody on the other, and such a squad cannot be formed.
    ///  - The scoreboard has a row a character at all times. Before a match it lists the teams
    ///    as they form, with nothing scored. In a match a kill, a capture and a player's coming
    ///    and going send their row at once; damage and healing, counted hit by hit, go out
    ///    every <see cref="ScoreRefreshMs"/>. After it the result stays for
    ///    <see cref="ResultMs"/>, and then the teams of the next are listed. Whoever arrives is
    ///    sent the board as it stands. The client keeps a row by the player's entity id and has
    ///    nothing that removes one, so a player back from a logout - another entity - has their
    ///    old row emptied on every client before the new one is sent (<see cref="Retire"/>).
    ///  - The control points' waypoints are closed, and the two depots stand for nothing yet.
    /// </summary>
    public class Battlegrounds
    {
        private static Battlegrounds _instance;
        private static readonly object InstanceLock = new object();

        public static Battlegrounds Instance
        {
            get
            {
                if (_instance == null)
                    lock (InstanceLock)
                        _instance ??= new Battlegrounds();

                return _instance;
            }
        }

        /// <summary>teamconstants.RED_TEAM.</summary>
        public const uint Red = 1;

        /// <summary>teamconstants.BLUE_TEAM.</summary>
        public const uint Blue = 2;

        /// <summary>How long a player stands in the way back out of a base before it takes them.</summary>
        public const long LeaveDwellMs = 5000;

        /// <summary>How often the rows whose damage or healing has changed are sent while a match runs.</summary>
        public const long ScoreRefreshMs = 3000;

        /// <summary>How long a match's result stays on the scoreboard before the teams of the next are listed there.</summary>
        public const long ResultMs = 30000;

        /// <summary>The entity class of a team's teleporter in the staging area: the waypoint pad, and its red twin.</summary>
        public const uint BlueTeleporterClassId = 25651;
        public const uint RedTeleporterClassId = 29416;

        public enum Phase
        {
            /// <summary>A team has too few players.</summary>
            Waiting,

            /// <summary>Both teams have enough: the preparation before the match.</summary>
            Preparing,

            Running
        }

        /// <summary>A map that has a match, and what of it only the server knows.</summary>
        public sealed class Definition
        {
            public uint MapContextId { get; set; }

            /// <summary>The teleporter rows of the two teams' hospitals, in their bases.</summary>
            public uint RedHospitalId { get; set; }
            public uint BlueHospitalId { get; set; }

            /// <summary>The client's own id (controlpointdata) for each of the map's control points, by the server's.</summary>
            public Dictionary<uint, uint> ClientPointIds { get; } = new Dictionary<uint, uint>();

            /// <summary>The staging area is everything below this height; the field and the bases are above it.</summary>
            public float StagingMaxY { get; set; }

            /// <summary>Where a player with no team who is found outside the staging area is put.</summary>
            public Vector3 StagingArrival { get; set; }
            public float StagingRotation { get; set; }

            public uint HospitalOf(uint team) => team == Red ? RedHospitalId : BlueHospitalId;
        }

        /// <summary>Edmund Range, adv_wargame_edmundrange2.</summary>
        public static Definition EdmundRange()
        {
            var definition = new Definition
            {
                MapContextId = 2374,
                RedHospitalId = 405,
                BlueHospitalId = 404,
                StagingMaxY = 400f,
                StagingArrival = new Vector3(-62.5f, 364.2f, -412f),
                StagingRotation = 3.1416f
            };

            // Whiskey, Charlie, Echo: control_point 101-103, controlpointdata 12, 10, 11.
            definition.ClientPointIds[101] = 12;
            definition.ClientPointIds[102] = 10;
            definition.ClientPointIds[103] = 11;

            return definition;
        }

        /// <summary>One player's row of the scoreboard for the match being played.</summary>
        public sealed class Score
        {
            public uint CharacterId { get; set; }
            public ulong EntityId { get; set; }
            public string Name { get; set; }

            /// <summary>The character's first name and clan, for the match's record (PvpRecords).</summary>
            public string FirstName { get; set; }
            public uint ClanId { get; set; }
            public uint ClassId { get; set; }
            public uint Team { get; set; }

            /// <summary>Still on their team: a row that is not is hidden.</summary>
            public bool Active { get; set; }

            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int Damage { get; set; }
            public int Healing { get; set; }
            public int Captures { get; set; }
            public int Prestige { get; set; }

            /// <summary>The points this player has been given prestige for capturing in this match: once each.</summary>
            public HashSet<uint> Rewarded { get; } = new HashSet<uint>();

            /// <summary>
            /// Changed since the row was last sent. Damage and healing are counted hit by hit and
            /// sent on the clock (<see cref="ScoreRefreshMs"/>), not a row a hit.
            /// </summary>
            public bool Dirty { get; set; }
        }

        public sealed class Member
        {
            public Client Client { get; set; }
            public uint Team { get; set; }
            public Score Score { get; set; }

            /// <summary>The way out of the base they are standing in, and when it takes them; null when in none.</summary>
            public MapLink LeaveLink { get; set; }
            public long LeavesAt { get; set; }
        }

        public sealed class Point
        {
            public ControlPoints.Point Source { get; set; }
            public DynamicObject Object { get; set; }

            /// <summary>The team that holds it; 0 for nobody.</summary>
            public uint Owner { get; set; }

            /// <summary>The client's id for it, in the scoreboard's game score; 0 if it has none.</summary>
            public uint ClientId { get; set; }

            public uint Id => Source.Id;
            public string Name => Source.Name;
        }

        public sealed class Match
        {
            public MapChannel Map { get; set; }
            public Definition Definition { get; set; }
            public Phase Phase { get; set; }

            /// <summary>When the preparation ends, or the match at its longest.</summary>
            public long PhaseEnds { get; set; }

            /// <summary>When a team holding every point has won.</summary>
            public long MinEndsAt { get; set; }

            /// <summary>The wargame the two teams are enemies in while the match runs.</summary>
            public uint WargameId { get; set; }

            /// <summary>When the match began, UTC: for its record.</summary>
            public DateTime StartedAt { get; set; }

            /// <summary>Started by a game master: it runs whatever the teams have in them.</summary>
            public bool Forced { get; set; }

            public bool MinAnnounced { get; set; }
            public bool FiveAnnounced { get; set; }
            public bool OneAnnounced { get; set; }

            /// <summary>
            /// The scoreboard is showing the last match as it ended: for <see cref="ResultMs"/> from
            /// its end, or until the next begins. The teams as they form are not listed over it.
            /// </summary>
            public bool ShowsResult { get; set; }

            /// <summary>When the last match's result gives way to the teams of the next.</summary>
            public long ResultEndsAt { get; set; }

            /// <summary>When the rows that have changed are next sent.</summary>
            public long ScoresDueAt { get; set; }

            public List<Member> Members { get; } = new List<Member>();

            /// <summary>
            /// The scoreboard, a row a character: of the match being played; after it, as it ended;
            /// and before one, the teams as they stand, with nothing scored.
            /// </summary>
            public Dictionary<uint, Score> Scores { get; } = new Dictionary<uint, Score>();
            public List<Point> Points { get; } = new List<Point>();

            public int Count(uint team) => Members.Count(m => m.Team == team);
            public IEnumerable<Member> Team(uint team) => Members.Where(m => m.Team == team).ToList();
            public Member Find(Client client) => client == null ? null : Members.Find(m => m.Client == client);
            public Member Find(Manifestation player) => player == null ? null : Members.Find(m => m.Client?.Player == player);
            public int Held(uint team) => Points.Count(p => p.Owner == team);
            public int Kills(uint team) => Scores.Values.Where(s => s.Team == team).Sum(s => s.Kills);
        }

        /// <summary>
        /// A player who left a match that was being played: the copy of the map they left, and
        /// until when every other copy of it is closed to them.
        /// </summary>
        public sealed class Lockout
        {
            public uint MapContextId { get; set; }
            public uint InstanceId { get; set; }
            public long Until { get; set; }
        }

        private readonly Dictionary<MapChannel, Match> _matches = new Dictionary<MapChannel, Match>();

        /// <summary>Who is kept to the copy they left, by character id.</summary>
        private readonly Dictionary<uint, Lockout> _lockouts = new Dictionary<uint, Lockout>();

        /// <summary>Who left a team in the middle of a match, by character id, and which.</summary>
        private readonly Dictionary<uint, uint> _deserters = new Dictionary<uint, uint>();

        /// <summary>The maps that have a match, by context id.</summary>
        public Dictionary<uint, Definition> Definitions { get; } = new Dictionary<uint, Definition> { [2374] = EdmundRange() };

        /// <summary>appsettings.json's Battleground; replaced when the file changes.</summary>
        public BattlegroundConfig Config { get; set; } = new BattlegroundConfig();

        /// <summary>The clock matches run on, in milliseconds. Replaceable for tests.</summary>
        public Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>A map's control points as the world database has them. Replaceable for tests.</summary>
        public Func<uint, IEnumerable<ControlPoints.Point>> PointsOn { get; set; } = id => ControlPoints.Instance.OnMap(id).ToList();

        /// <summary>How a point's Simulated Bane stand on a channel. Replaceable for tests.</summary>
        public Func<MapChannel, ControlPoints.Point, ControlPoints.Garrison> GarrisonOf { get; set; } =
            (map, point) => ControlPoints.Instance.GarrisonOf(map, point, ControlPoints.Bane);

        /// <summary>Puts a player somewhere else on the map they are on. Replaceable for tests.</summary>
        public Action<Client, Vector3, float> Teleport { get; set; } = MoveWithinMap;

        /// <summary>The id of a match's wargame: the duels' counter, clear of the feuds'. Replaceable for tests.</summary>
        public Func<uint> NextWargameId { get; set; } = () => Duels.Instance.NextWargameId();

        /// <summary>A match has ended, with its winner (0 for nobody): after everything else of its ending. For tests.</summary>
        public Action<Match, uint> MatchEnded { get; set; }

        /// <summary>
        /// Where the record of each match that ends is kept - who won and how, the two teams'
        /// points and kills, and every player's row of the scoreboard, deserters too. One with
        /// no store, as in a test, keeps nothing.
        /// </summary>
        public PvpRecords Records { get; set; } = PvpRecords.Instance;

        /// <summary>Whether a player goes where they like on a battleground map: a game master. Replaceable for tests.</summary>
        public Func<Client, bool> IsExempt { get; set; } = client => ChatCommandsManager.HasLevel(client, GmLevel.GameMaster);

        #region Lookups

        public static uint Other(uint team) => team == Red ? Blue : Red;

        public static string TeamName(uint team) => team == Red ? "Red Team" : team == Blue ? "Blue Team" : "nobody";

        public bool IsBattleground(uint mapContextId) => Definitions.ContainsKey(mapContextId);

        public IReadOnlyCollection<Match> Matches => _matches.Values.ToList();

        /// <summary>The match of a channel of a battleground map, made the first time it is asked for; null for any other.</summary>
        public Match MatchOf(MapChannel map)
        {
            if (map?.MapInfo == null || !Definitions.TryGetValue(map.MapInfo.MapContextId, out var definition))
                return null;

            if (_matches.TryGetValue(map, out var match))
                return match;

            match = new Match { Map = map, Definition = definition, Phase = Phase.Waiting };
            _matches[map] = match;

            Bind(match);
            ResetField(match, announce: false);

            return match;
        }

        /// <summary>The match a player is on a team of, if any.</summary>
        public Match MatchOf(Manifestation player)
        {
            var map = player?.MapChannel;

            if (map == null || !_matches.TryGetValue(map, out var match))
                return null;

            return match.Find(player) != null ? match : null;
        }

        public Member MemberOf(Client client)
        {
            var map = client?.Player?.MapChannel;

            return map != null && _matches.TryGetValue(map, out var match) ? match.Find(client) : null;
        }

        /// <summary>The team a player is on; 0 for none.</summary>
        public uint TeamOf(Manifestation player) => MatchOf(player)?.Find(player)?.Team ?? 0;

        /// <summary>The team a character deserted and has yet to finish a match on; 0 for none.</summary>
        public uint DesertedTeamOf(uint characterId) => _deserters.TryGetValue(characterId, out var team) ? team : 0;

        /// <summary>The lockout a character is under, if it has not run out; null for none.</summary>
        public Lockout LockoutOf(uint characterId)
        {
            if (!_lockouts.TryGetValue(characterId, out var lockout))
                return null;

            if (Now() < lockout.Until)
                return lockout;

            _lockouts.Remove(characterId);

            return null;
        }

        /// <summary>
        /// The lockout that keeps a player to one copy of a map: they left a match there that
        /// was being played, and its time has not run out. Null for a player with none, for
        /// another map's, and for a game master.
        /// </summary>
        public Lockout LockoutFor(Client client, uint mapContextId)
        {
            var player = client?.Player;

            if (player == null || _lockouts.Count == 0)
                return null;

            var lockout = LockoutOf(player.Id);

            return lockout != null && lockout.MapContextId == mapContextId && !IsExempt(client) ? lockout : null;
        }

        /// <summary>Whether a channel is closed to a player by their lockout: a copy of the map other than the one they left.</summary>
        public bool BarredFrom(Client client, MapChannel map, out Lockout lockout)
        {
            lockout = map?.MapInfo == null ? null : LockoutFor(client, map.MapInfo.MapContextId);

            return lockout != null && lockout.InstanceId != map.InstanceId;
        }

        /// <summary>The time left of a lockout in words, by the minute begun: "12 more minutes".</summary>
        public string TimeLeftOf(Lockout lockout)
        {
            var minutes = Math.Max(1, (lockout.Until - Now() + 59999) / 60000);

            return $"{minutes} more minute{(minutes == 1 ? "" : "s")}";
        }

        /// <summary>What a player kept out of a copy by a lockout is told.</summary>
        public string LockoutText(Lockout lockout)
        {
            return $"You left a match in progress: for {TimeLeftOf(lockout)} you can only play in the instance you left.";
        }

        /// <summary>
        /// Keeps a character to one copy of a map for <see cref="BattlegroundConfig.LeaverLockoutMinutes"/>
        /// from now, in place of any lockout they had; nothing when that is no time at all.
        /// </summary>
        internal void Lock(uint characterId, uint mapContextId, uint instanceId)
        {
            var minutes = Config.LeaverLockoutMinutes;

            if (characterId == 0 || minutes <= 0)
                return;

            _lockouts[characterId] = new Lockout { MapContextId = mapContextId, InstanceId = instanceId, Until = Now() + minutes * 60000L };
        }

        /// <summary>Forgets a character's desertion and lockout: a game master's. False if they had neither.</summary>
        public bool Forgive(uint characterId)
        {
            var deserted = _deserters.Remove(characterId);

            return _lockouts.Remove(characterId) || deserted;
        }

        /// <summary>The point a control point object is, on whichever channel it stands; null if it is none of a match's.</summary>
        public Point PointOf(DynamicObject obj)
        {
            if (obj == null)
                return null;

            foreach (var match in _matches.Values)
                foreach (var point in match.Points)
                    if (ReferenceEquals(point.Object, obj))
                        return point;

            return null;
        }

        private Match MatchWith(Point point) => _matches.Values.FirstOrDefault(m => m.Points.Contains(point));

        /// <summary>A copy of a map has closed (MapChannelManager.CloseSharedCopy): its match goes with it.</summary>
        public void Forget(MapChannel map)
        {
            if (map != null)
                _matches.Remove(map);
        }

        /// <summary>Forgets every match and every deserter. For tests.</summary>
        public void Reset()
        {
            _matches.Clear();
            _deserters.Clear();
            _lockouts.Clear();
        }

        #endregion

        #region Setting up

        /// <summary>
        /// At startup, after the control points and the map links: the match of every battleground
        /// map's own channel, its control points set down, and the teleporters of its staging
        /// area given something to be seen by.
        /// </summary>
        public void Init()
        {
            foreach (var mapChannel in MapChannelManager.Instance.MapChannelArray.Values.ToList())
            {
                var match = MatchOf(mapChannel);

                if (match == null)
                    continue;

                PlaceTeleporters(mapChannel);

                Logger.WriteLog(LogType.Initialize, $"Battleground on map {mapChannel.MapInfo.MapContextId}: {match.Points.Count} control point(s).");

                if (match.Points.Count == 0)
                    MapErrorManager.Instance.Record(mapChannel.MapInfo.MapContextId, "this map has a battleground and no control points (control_point): no match can be won on it");
            }
        }

        /// <summary>The match's points, each with its object on the match's channel: the one the channel has, or a new one.</summary>
        private void Bind(Match match)
        {
            var map = match.Map;

            foreach (var source in PointsOn(map.MapInfo.MapContextId).OrderBy(p => p.Id))
            {
                if (!map.ControlPoints.TryGetValue(source.Id, out var obj))
                {
                    obj = new DynamicObject
                    {
                        Position = NavMeshManager.SnapToGround(map, source.Position),
                        Rotation = source.Rotation,
                        MapContextId = source.MapContextId,
                        EntityClassId = source.ClassId,
                        DynamicObjectType = DynamicObjectType.ControlPoint,
                        Comment = $"Control Point: {source.Name}"
                    };

                    map.ControlPoints[source.Id] = obj;
                }

                // Out of service until the match runs and its Bane are dead (Garrisons).
                obj.RuntimeMapChannel = map;
                obj.IsEnabled = false;

                match.Points.Add(new Point
                {
                    Source = source,
                    Object = obj,
                    ClientId = match.Definition.ClientPointIds.TryGetValue(source.Id, out var clientId) ? clientId : 0
                });
            }
        }

        /// <summary>
        /// A waypoint pad at each team's teleporter in the staging area - the red one for Red -
        /// so there is something to walk into. On the map's own channel; a copy has copies.
        /// </summary>
        private void PlaceTeleporters(MapChannel map)
        {
            foreach (var link in LinksOn(map).Where(l => l.Kind == MapLinkKind.TeamRed || l.Kind == MapLinkKind.TeamBlue))
                map.DynamicObjects.Add(new DynamicObject
                {
                    EntityClassId = (EntityClasses)(link.Kind == MapLinkKind.TeamRed ? RedTeleporterClassId : BlueTeleporterClassId),
                    DynamicObjectType = DynamicObjectType.TeamTeleporter,
                    Position = link.Position,
                    MapContextId = map.MapInfo.MapContextId,
                    RuntimeMapChannel = map,
                    TargetCategory = TargetCategory.Friendly,
                    StateId = UseObjectState.TsState1,
                    Comment = link.Kind == MapLinkKind.TeamRed ? "Red Team teleporter" : "Blue Team teleporter"
                });
        }

        /// <summary>The map links on a channel.</summary>
        public static List<MapLink> LinksOn(MapChannel map)
        {
            if (map?.MapCellInfo?.Cells == null)
                return new List<MapLink>();

            return map.MapCellInfo.Cells.Values.SelectMany(cell => cell.MapLinks).Where(l => l != null).Distinct().ToList();
        }

        /// <summary>Where a team's players arrive in their base: where its teleporter leads.</summary>
        public (Vector3 Position, float Rotation)? BaseArrival(Match match, uint team)
        {
            var kind = team == Red ? MapLinkKind.TeamRed : MapLinkKind.TeamBlue;
            var link = LinksOn(match.Map).FirstOrDefault(l => l.Kind == kind && l.Enabled);

            if (link != null)
                return (link.DestPosition, link.DestRotation);

            var hospital = BaseCentre(match, team);

            return hospital.HasValue ? (hospital.Value, 0f) : ((Vector3, float)?)null;
        }

        /// <summary>The middle of a team's base: its hospital.</summary>
        public Vector3? BaseCentre(Match match, uint team)
        {
            var id = match.Definition.HospitalOf(team);
            var hospital = Hospitals.OnMap(match.Map.MapInfo.MapContextId).FirstOrDefault(h => h.TeleporterId == id);

            if (hospital != null)
                return hospital.Position;

            var kind = team == Red ? MapLinkKind.TeamRed : MapLinkKind.TeamBlue;

            return LinksOn(match.Map).FirstOrDefault(l => l.Kind == kind)?.DestPosition;
        }

        /// <summary>Whether a position is inside a team's base, on the ground.</summary>
        public bool InBase(Match match, uint team, Vector3 position)
        {
            var centre = BaseCentre(match, team);

            if (!centre.HasValue || position.Y <= match.Definition.StagingMaxY)
                return false;

            var dx = position.X - centre.Value.X;
            var dz = position.Z - centre.Value.Z;

            return dx * dx + dz * dz <= (float)Config.BaseRadius * Config.BaseRadius;
        }

        /// <summary>
        /// The field as a match finds it: every point nobody's, out of service until its Bane
        /// are dead, and the Bane set down afresh.
        /// </summary>
        private void ResetField(Match match, bool announce)
        {
            foreach (var point in match.Points)
            {
                point.Owner = 0;
                point.Object.WindupTime = CaptureMs;
                DynamicObjectManager.Instance.SetEnabled(point.Object, false);

                var pools = PoolsOf(match.Map, point);

                ControlPoints.TakeOffLiving(match.Map, pools);

                foreach (var pool in pools)
                {
                    pool.IsGarrison = true;
                    pool.Suspended = false;
                    pool.HasSpawned = false;
                    pool.UpdateTimer = pool.RespawnTime;
                }

                // The waypoints are closed: whose they are is not something a waypoint can say.
                foreach (var id in point.Source.Waypoints)
                    if (match.Map.Teleporters.TryGetValue(id, out var teleporter) && teleporter.ObjectData is WaypointInfo info)
                        info.Contested = true;

                ShowOwner(match, point, announce);
            }

            if (announce)
                SendGameScore(match);
        }

        private static List<SpawnPool> PoolsOf(MapChannel map, Point point)
        {
            if (map?.SpawnPools == null || point.Source.BanePools.Count == 0)
                return new List<SpawnPool>();

            return map.SpawnPools.Where(p => p != null && point.Source.BanePools.Contains(p.DbId)).ToList();
        }

        private uint CaptureMs => (uint)Math.Max(1, Config.CaptureSeconds) * 1000u;

        #endregion

        #region Teams

        /// <summary>
        /// Whether a player may walk into a map: a battleground has a level
        /// (<see cref="BattlegroundConfig.MinLevel"/>), "You must be level 45 or higher to enter
        /// this map." Any other map is always entered.
        /// </summary>
        public bool MayEnter(Client client, uint mapContextId)
        {
            if (!IsBattleground(mapContextId) || client?.Player == null || client.Player.Level >= Config.MinLevel || IsExempt(client))
                return true;

            CommunicatorManager.Instance.DisplaySystemMessage(client, PlayerMessage.EdmundrangeNotHighEnoughLevelBigtext);
            CommunicatorManager.Instance.DisplaySystemMessage(client, PlayerMessage.EdmundrangeNotHighEnoughLevelInfo);

            return false;
        }

        /// <summary>
        /// A player has walked into a map link of a battleground's (MapLinkManager): a team's
        /// teleporter, or the way back out of a base. Returns whether the link was one of ours.
        /// </summary>
        public bool TakeLink(Client client, MapLink link)
        {
            if (link == null || link.Kind < MapLinkKind.TeamRed || link.Kind > MapLinkKind.TeamLeave)
                return false;

            var match = MatchOf(client?.Player?.MapChannel);

            if (match == null)
                return true;

            if (link.Kind == MapLinkKind.TeamLeave)
            {
                var member = match.Find(client);

                if (member != null && member.LeaveLink == null)
                {
                    member.LeaveLink = link;
                    member.LeavesAt = Now() + LeaveDwellMs;

                    Say(client, $"Stay here for {LeaveDwellMs / 1000} seconds to leave {TeamName(member.Team)} and return to the Staging Area."
                        + (match.Phase == Phase.Running ? " Leaving a match that is being played is desertion." : ""));
                }

                return true;
            }

            Join(client, link.Kind == MapLinkKind.TeamRed ? Red : Blue, force: false);

            return true;
        }

        /// <summary>
        /// Puts a player on a team of the match of the channel they are on, and in its base.
        /// Refused, with the client's own words, to a deserter of the other team, to a player
        /// who left a match on another copy of the map within their lockout, and when the team
        /// has too many more players than the other; <paramref name="force"/> is a game
        /// master's, and refuses nothing. Returns whether they are on the team.
        /// </summary>
        public bool Join(Client client, uint team, bool force)
        {
            var player = client?.Player;
            var match = MatchOf(player?.MapChannel);

            if (match == null || team != Red && team != Blue)
                return false;

            var current = match.Find(client);

            if (current != null)
            {
                if (current.Team == team)
                    return true;

                // Changing sides, which only a game master's command does: off the one first.
                Leave(match, current, deserting: false, toStaging: false);
            }

            if (!force)
            {
                // They left a match that is, or was, being played on another copy of the map.
                if (BarredFrom(client, match.Map, out var lockout))
                {
                    CommunicatorManager.Instance.DisplaySystemMessage(client,
                        team == Red ? PlayerMessage.EdmundrangeCanNotJoinRedTeamBigtext : PlayerMessage.EdmundrangeCanNotJoinBlueTeamBigtext);
                    Say(client, LockoutText(lockout));
                    return false;
                }

                if (_deserters.TryGetValue(player.Id, out var deserted) && deserted != team)
                {
                    CommunicatorManager.Instance.DisplaySystemMessage(client,
                        team == Red ? PlayerMessage.EdmundrangeCanNotJoinRedTeamBigtext : PlayerMessage.EdmundrangeCanNotJoinBlueTeamBigtext);
                    CommunicatorManager.Instance.DisplaySystemMessage(client,
                        team == Red ? PlayerMessage.EdmundrangeCanNotJoinRedTeamInfo : PlayerMessage.EdmundrangeCanNotJoinBlueTeamInfo);
                    return false;
                }

                if (match.Count(team) > match.Count(Other(team)) + Math.Max(0, Config.MaxImbalance))
                {
                    CommunicatorManager.Instance.DisplaySystemMessage(client,
                        team == Red ? PlayerMessage.EdmundrangeCanNotJoinRedTeamBigtext : PlayerMessage.EdmundrangeCanNotJoinBlueTeamBigtext);
                    Say(client, $"{TeamName(team)} has more players than {TeamName(Other(team))}: join {TeamName(Other(team))}.");
                    return false;
                }
            }

            var member = new Member { Client = client, Team = team };

            match.Members.Add(member);

            if (match.Phase == Phase.Running)
                member.Score = ScoreFor(match, member);

            Logger.WriteLog(LogType.Debug, $"Battleground {match.Map.MapInfo.MapContextId}/{match.Map.InstanceId}: {player.FamilyName} joined {TeamName(team)} ({match.Count(Red)} red, {match.Count(Blue)} blue).");

            // No squad holds both teams.
            PartyManager.Instance.SeparateTeams(client);

            var mates = match.Team(team).ToList();

            client.CallMethod(SysEntity.ClientTeamManagerId, new JoinedTeamPacket(team, mates.Select(m => m.Client.Player.EntityId).ToList()));

            foreach (var mate in mates)
                if (mate != member)
                    mate.Client.CallMethod(SysEntity.ClientTeamManagerId, new AddTeamMemberPacket(player.EntityId));

            Tell(match, team == Red ? PlayerMessage.PmPvpRedTeam : PlayerMessage.PmPvpBlueTeam, ("player", player.FamilyName ?? ""));

            var arrival = BaseArrival(match, team);

            if (arrival.HasValue)
                Teleport(client, arrival.Value.Position, arrival.Value.Rotation);

            if (match.Phase == Phase.Running)
            {
                SendScore(match, member.Score);

                // Now an enemy of the other team, to everyone who sees them.
                Wargames.Show(client);
            }
            else if (!match.ShowsResult)
            {
                // Before a match the scoreboard lists the teams as they form.
                SendScore(match, ScoreFor(match, member));
            }

            ShowHospitals(match, client);

            return true;
        }

        /// <summary>
        /// Takes a player off their team: leaving a match that is running is desertion. Back to
        /// the staging area with <paramref name="toStaging"/>; a player who is leaving the map
        /// is going somewhere else already.
        /// </summary>
        private void Leave(Match match, Member member, bool deserting, bool toStaging)
        {
            if (!match.Members.Remove(member))
                return;

            var client = member.Client;
            var player = client?.Player;

            if (deserting && player != null)
            {
                Lock(player.Id, match.Map.MapInfo.MapContextId, match.Map.InstanceId);
                _deserters[player.Id] = member.Team;
                Logger.WriteLog(LogType.Debug, $"Battleground {match.Map.MapInfo.MapContextId}/{match.Map.InstanceId}: {player.FamilyName} deserted {TeamName(member.Team)}.");
            }

            if (member.Score != null)
            {
                member.Score.Active = false;
                SendScore(match, member.Score);
            }
            else if (match.Phase != Phase.Running && !match.ShowsResult && player != null && match.Scores.TryGetValue(player.Id, out var listed))
            {
                // Off the list of the teams as they form.
                listed.Active = false;
                SendScore(match, listed);
            }

            if (player != null)
            {
                foreach (var mate in match.Team(member.Team))
                    mate.Client.CallMethod(SysEntity.ClientTeamManagerId, new RemoveTeamMemberPacket(player.EntityId));

                Tell(match, member.Team == Red ? PlayerMessage.PmPvpRedTeamLeave : PlayerMessage.PmPvpBlueTeamLeave, ("player", player.FamilyName ?? ""));

                if (client.State != ClientState.Disconnected)
                {
                    client.CallMethod(SysEntity.ClientTeamManagerId, new LeftTeamPacket());

                    if (toStaging)
                    {
                        var link = member.LeaveLink;

                        Teleport(client, link?.DestPosition ?? match.Definition.StagingArrival, link?.DestRotation ?? match.Definition.StagingRotation);
                    }

                    // No longer anybody's enemy.
                    if (match.Phase == Phase.Running)
                        Wargames.Show(client);
                }
            }
        }

        /// <summary>Takes a player off their team and back to the staging area, with no desertion: a game master's. False if they had none.</summary>
        public bool LeaveTeam(Client client)
        {
            var match = MatchOf(client?.Player?.MapChannel);
            var member = match?.Find(client);

            if (member == null)
                return false;

            member.LeaveLink = null;
            Leave(match, member, deserting: false, toStaging: true);

            return true;
        }

        /// <summary>
        /// A player is leaving the map or the world (ManifestationManager.RemovePlayerCharacter):
        /// off their team, a deserter if the match is running, and told the map they are going to
        /// has no teams.
        /// </summary>
        public void PlayerLeft(Client client)
        {
            var map = client?.Player?.MapChannel;

            if (map == null || !_matches.TryGetValue(map, out var match))
                return;

            var member = match.Find(client);

            if (member != null)
                Leave(match, member, deserting: match.Phase == Phase.Running, toStaging: false);

            if (client.State != ClientState.Disconnected)
                client.CallMethod(SysEntity.ClientTeamManagerId, new SetNumberOfTeamsPacket(0));
        }

        /// <summary>
        /// A player has arrived on a map (MapChannelManager): on a battleground they are told it
        /// has two teams, and shown the match as it stands - the clock, the control points and
        /// the scoreboard: the match being played, the last one as it ended, or the teams as they
        /// form.
        /// </summary>
        public void PlayerEntered(Client client)
        {
            var match = MatchOf(client?.Player?.MapChannel);

            if (match == null)
                return;

            client.CallMethod(SysEntity.ClientTeamManagerId, new SetNumberOfTeamsPacket(2));

            // Here by a way that is not the door, which no player has (a login goes back into
            // the copy left, or outside: MapChannelManager.PlaceLogin): no team of this copy
            // takes them, and they hear why before they try one.
            if (BarredFrom(client, match.Map, out var lockout))
                Say(client, LockoutText(lockout) + " Leave by the door and come back in to return to it.");

            if (match.Phase == Phase.Running)
                client.CallMethod(SysEntity.ClientTeamManagerId, new ScoreBoardActivePacket(true));

            foreach (var score in match.Scores.Values)
                client.CallMethod(SysEntity.ClientTeamManagerId, Row(score));

            client.CallMethod(SysEntity.ClientTeamManagerId, GameScore(match));
        }

        /// <summary>
        /// Whether a hit between two players of a match is stopped by a base: the one hit, or the
        /// one hitting, stands inside their own (Pvp.Shielded). It lands as Immune.
        /// </summary>
        public bool Sheltered(Actor source, Actor target)
        {
            if (_matches.Count == 0 || !(target is Manifestation victim) || !(Pvp.Controller(source) is Manifestation attacker) || ReferenceEquals(attacker, victim))
                return false;

            var match = MatchOf(victim);

            if (match == null || !ReferenceEquals(MatchOf(attacker), match))
                return false;

            return InBase(match, match.Find(victim).Team, victim.Position) || InBase(match, match.Find(attacker).Team, attacker.Position);
        }

        /// <summary>Whether two sides - each a list of players - have somebody on each team of a match between them.</summary>
        public bool OnDifferentTeams(IEnumerable<Client> first, IEnumerable<Client> second)
        {
            if (_matches.Count == 0)
                return false;

            var teams = (first ?? Enumerable.Empty<Client>()).Concat(second ?? Enumerable.Empty<Client>())
                .Select(c => (Match: MatchOf(c?.Player), Team: TeamOf(c?.Player)))
                .Where(t => t.Team != 0)
                .ToList();

            return teams.Any(a => teams.Any(b => ReferenceEquals(a.Match, b.Match) && a.Team != b.Team));
        }

        #endregion

        #region The wargame

        /// <summary>The player's WargameData for a match that is running: the match's id, and whether they are Red (Wargames.DataOf).</summary>
        public Dictionary<uint, bool> WargameDataOf(Manifestation player)
        {
            var data = new Dictionary<uint, bool>();

            if (_matches.Count == 0)
                return data;

            var match = MatchOf(player);

            if (match != null && match.Phase == Phase.Running && match.WargameId != 0)
                data[match.WargameId] = match.Find(player).Team == Red;

            return data;
        }

        /// <summary>
        /// A player has been killed by an enemy player (Pvp.CountKill): if they are on the two
        /// teams of a match, the kill and the death are counted and the kill's prestige given.
        /// </summary>
        public void Kill(Client killer, Client victim)
        {
            if (_matches.Count == 0 || killer?.Player == null || victim?.Player == null)
                return;

            var match = MatchOf(killer.Player);

            if (match == null || match.Phase != Phase.Running || !ReferenceEquals(MatchOf(victim.Player), match))
                return;

            var by = match.Find(killer);
            var of = match.Find(victim);

            if (by.Team == of.Team || by.Score == null || of.Score == null)
                return;

            by.Score.Kills++;
            of.Score.Deaths++;
            by.Score.Prestige += PvpPrestige.TeamKill(killer, victim);

            SendScore(match, by.Score);
            SendScore(match, of.Score);
        }

        /// <summary>Damage a player of a match has done to one of the other team, for their row.</summary>
        public void Damaged(Actor source, Actor target, int amount)
        {
            if (_matches.Count == 0 || amount <= 0 || !(target is Manifestation victim) || !(Pvp.Controller(source) is Manifestation attacker))
                return;

            var match = MatchOf(attacker);
            var score = match?.Find(attacker)?.Score;

            if (score == null || match.Phase != Phase.Running || match.Find(victim) == null || match.Find(victim).Team == match.Find(attacker).Team)
                return;

            score.Damage += amount;
            score.Dirty = true;
        }

        /// <summary>Healing a player of a match has done to one of their own team, themselves included, for their row.</summary>
        public void Healed(ulong sourceEntityId, Actor target, int amount)
        {
            if (_matches.Count == 0 || amount <= 0 || sourceEntityId == 0 || !(target is Manifestation healed)
                || !EntityManager.Instance.Players.TryGetValue(sourceEntityId, out var healer))
                return;

            var match = MatchOf(healer);
            var score = match?.Find(healer)?.Score;

            if (score == null || match.Phase != Phase.Running || match.Find(healed) == null || match.Find(healed).Team != match.Find(healer).Team)
                return;

            score.Healing += amount;
            score.Dirty = true;
        }

        /// <summary>Whether a player who dies here comes back without the penalties of a death: on a team of a match.</summary>
        public bool NoDeathPenalty(Manifestation player) => _matches.Count > 0 && MatchOf(player) != null;

        /// <summary>
        /// The hospitals a player of a match goes back to: their team's own, and that of every
        /// control point their team holds. Null for anybody else, who has the map's as usual.
        /// </summary>
        public List<Hospitals.Hospital> HospitalsFor(Manifestation player)
        {
            if (_matches.Count == 0)
                return null;

            var match = MatchOf(player);

            if (match == null)
                return null;

            var team = match.Find(player).Team;
            var open = new HashSet<uint> { match.Definition.HospitalOf(team) };

            foreach (var point in match.Points.Where(p => p.Owner == team))
                foreach (var id in point.Source.Hospitals)
                    open.Add(id);

            var hospitals = Hospitals.OnMap(match.Map.MapInfo.MapContextId).Where(h => open.Contains(h.TeleporterId)).ToList();

            return hospitals.Count > 0 ? hospitals : null;
        }

        #endregion

        #region Control points

        /// <summary>How long a capture's use takes on a point's object.</summary>
        public uint CaptureMsOf(Point point) => point?.Object?.WindupTime > 0 ? point.Object.WindupTime : CaptureMs;

        /// <summary>
        /// Whether a player may start to capture a point now: the match is running, they are on
        /// a team that does not hold it, and none of its Simulated Bane stands.
        /// </summary>
        public bool MayCapture(Client client, Point point)
        {
            var match = MatchWith(point);
            var member = match?.Find(client);

            return member != null && match.Phase == Phase.Running && member.Team != point.Owner
                   && GarrisonOf(match.Map, point.Source) != ControlPoints.Garrison.Standing;
        }

        /// <summary>A player has begun the use that captures a point: everyone on the map is told.</summary>
        public void Claiming(Client client, Point point)
        {
            var match = MatchWith(point);
            var member = match?.Find(client);

            if (member != null)
                Tell(match, PlayerMessage.PmControlpointClaiming, ("faction", TeamName(member.Team)), ("cpName", point.Name));
        }

        /// <summary>
        /// A player's use of a point has run its time (DynamicObjectManager): the point is their
        /// team's, unless it no longer may be. Returns whether it changed hands.
        /// </summary>
        public bool Captured(Client client, Point point)
        {
            if (!MayCapture(client, point))
                return false;

            var match = MatchWith(point);
            var member = match.Find(client);

            SetOwner(match, point, member.Team);

            if (member.Score != null)
            {
                member.Score.Captures++;

                // Once a point a match: a point handed back and forth is worth nothing more.
                if (member.Score.Rewarded.Add(point.Id))
                    member.Score.Prestige += Award(client, Config.CapturePrestige);

                SendScore(match, member.Score);
            }

            return true;
        }

        /// <summary>Gives a point to a team, or to nobody: shown, told, and on the scoreboard. False if that is who has it.</summary>
        public bool SetOwner(Match match, Point point, uint team)
        {
            if (match == null || point == null || point.Owner == team)
                return false;

            point.Owner = team;

            Logger.WriteLog(LogType.Debug, $"Battleground {match.Map.MapInfo.MapContextId}/{match.Map.InstanceId}: {point.Name} is {TeamName(team)}'s.");

            ShowOwner(match, point, announce: true);

            if (team != 0)
            {
                Tell(match, PlayerMessage.PmBattlegroundCpTaken);
                Tell(match, PlayerMessage.PmControlpointOwned, ("faction", TeamName(team)), ("cpName", point.Name));
            }

            SendGameScore(match);

            // Its hospital is now somebody else's.
            foreach (var member in match.Members)
                ShowHospitals(match, member.Client);

            return true;
        }

        /// <summary>
        /// The state of a point's object: the ownable control point (OWNABLECONTROLPOINT) has
        /// two, nobody's and a team's, and takes which team's from its owner (SetOwnerId).
        /// </summary>
        public static UseObjectState StateOf(uint owner) =>
            owner == 0 ? UseObjectState.OcpStateUncontrolled : UseObjectState.OcpStateTeamControlled;

        /// <summary>The owner id a point's object is told: the team's own, or -1 for nobody, which has no effect on it.</summary>
        public static int OwnerIdOf(uint owner) => owner == 0 ? -1 : (int)owner;

        /// <summary>What a client meeting a point's object is told besides its creation: whose it is (DynamicObjectManager).</summary>
        internal void ShowTo(Client client, DynamicObject obj)
        {
            if (_matches.Count == 0)
                return;

            var point = PointOf(obj);

            if (point != null)
                client.CallMethod(obj.EntityId, new SetOwnerIdPacket(OwnerIdOf(point.Owner)));
        }

        private void ShowOwner(Match match, Point point, bool announce)
        {
            var obj = point.Object;

            obj.StateId = StateOf(point.Owner);

            if (!announce)
                return;

            if (obj.IsInWorld)
            {
                // The owner before the state: the state's effect is picked by it.
                CellManager.Instance.CellCallMethod(match.Map, obj, new SetOwnerIdPacket(OwnerIdOf(point.Owner)));
                CellManager.Instance.CellCallMethod(match.Map, obj, new ForceStatePacket(obj.StateId, 100));
                CellManager.Instance.CellCallMethod(match.Map, obj, new UsableInfoPacket(obj.IsEnabled, obj.StateId, 0, obj.WindupTime, 0));
            }

            if (point.Source.MarkerEntityId != 0)
                foreach (var client in Present(match))
                    client.CallMethod(SysEntity.ClientMapStateId, new UpdateMapMarkerPacket(point.Source.MarkerEntityId, MapMarkerState.TeamControlPoint(point.Owner)));
        }

        /// <summary>The marker state of a battleground's control point on a channel, for a client that has just arrived (MapMarkerManager).</summary>
        public MapMarkerState MarkerStateOf(MapChannel map, ControlPoints.Point source)
        {
            var point = MatchOf(map)?.Points.Find(p => p.Source == source);

            return MapMarkerState.TeamControlPoint(point?.Owner ?? 0);
        }

        /// <summary>Whether a hospital or waypoint is one of a battleground's control points', or a team's own: no walking up to it gains it.</summary>
        public bool OwnsTeleporter(uint mapContextId, uint teleporterId)
        {
            if (!Definitions.TryGetValue(mapContextId, out var definition))
                return false;

            return definition.RedHospitalId == teleporterId || definition.BlueHospitalId == teleporterId
                   || PointsOn(mapContextId).Any(p => p.Hospitals.Contains(teleporterId) || p.Waypoints.Contains(teleporterId));
        }

        /// <summary>A point's object has been stood somewhere else (ControlPoints.Move): on every channel that has it.</summary>
        public void PointMoved(ControlPoints.Point source)
        {
            foreach (var match in _matches.Values)
            {
                var obj = match.Points.Find(p => p.Source == source)?.Object;

                if (obj == null)
                    continue;

                if (obj.IsInWorld)
                    CellManager.Instance.RemoveFromWorld(match.Map, obj);

                obj.Position = source.Position;
                obj.Rotation = source.Rotation;

                if (obj.IsInWorld)
                    CellManager.Instance.AddToWorld(match.Map, obj);
            }
        }

        #endregion

        #region The match

        /// <summary>Once a second for each channel, from the map channel worker: the match of a battleground's, moved on.</summary>
        public void Worker(MapChannel map)
        {
            var match = MatchOf(map);

            if (match == null)
                return;

            var now = Now();

            Police(match, now);
            Garrisons(match);

            // The last match has been on the scoreboard long enough: the teams of the next.
            if (match.ShowsResult && match.Phase != Phase.Running && now >= match.ResultEndsAt)
                ListTeams(match);

            switch (match.Phase)
            {
                case Phase.Waiting:
                    if (Enough(match))
                    {
                        match.Phase = Phase.Preparing;
                        match.PhaseEnds = now + Math.Max(0, Config.PrepSeconds) * 1000L;

                        Say(match, $"Both teams are ready. The match begins in {Math.Max(0, Config.PrepSeconds)} seconds.");
                        SendGameScore(match);
                    }

                    break;

                case Phase.Preparing:
                    if (!Enough(match))
                    {
                        match.Phase = Phase.Waiting;

                        Say(match, "A team is short of players. The match waits for more.");
                        SendGameScore(match);
                    }
                    else if (now >= match.PhaseEnds)
                        Start(match, forced: false);

                    break;

                case Phase.Running:
                    SendChangedScores(match, now);
                    Referee(match, now);
                    break;
            }
        }

        private bool Enough(Match match)
        {
            var needed = Math.Max(1, Config.MinPlayersPerTeam);

            return match.Count(Red) >= needed && match.Count(Blue) >= needed;
        }

        /// <summary>
        /// Begins a match now: the teams enemies, the scoreboard on and empty, the clock at its
        /// longest. <paramref name="forced"/> is a game master's, and runs with a team of nobody.
        /// </summary>
        public void Start(Match match, bool forced)
        {
            if (match == null || match.Phase == Phase.Running)
                return;

            var now = Now();

            match.Phase = Phase.Running;
            match.Forced = forced;
            match.WargameId = NextWargameId();
            match.MinEndsAt = now + Math.Max(0, Config.MinMinutes) * 60000L;
            match.PhaseEnds = now + Math.Max(1, Math.Max(Config.MinMinutes, Config.MaxMinutes)) * 60000L;
            match.MinAnnounced = false;
            match.FiveAnnounced = false;
            match.OneAnnounced = false;
            match.ShowsResult = false;
            match.ScoresDueAt = now + ScoreRefreshMs;
            match.Scores.Clear();
            match.StartedAt = (Records ?? PvpRecords.Instance).UtcNow();

            Logger.WriteLog(LogType.Debug, $"Battleground {match.Map.MapInfo.MapContextId}/{match.Map.InstanceId}: match {match.WargameId} begins, {match.Count(Red)} red against {match.Count(Blue)} blue.");

            foreach (var client in Present(match))
                client.CallMethod(SysEntity.ClientTeamManagerId, new ScoreBoardActivePacket(true));

            foreach (var member in match.Members)
            {
                member.Score = ScoreFor(match, member);
                SendScore(match, member.Score);
            }

            SendGameScore(match);

            Tell(match, PlayerMessage.PmEdmundrangeBattlegroundStart);
            Tell(match, PlayerMessage.PmBattlegroundBegins);

            foreach (var member in match.Members)
                Wargames.Show(member.Client);
        }

        /// <summary>Who has won as the match stands: the team with more points, then with more kills; 0 when they are level.</summary>
        public static uint Leader(Match match)
        {
            var red = match.Held(Red);
            var blue = match.Held(Blue);

            if (red != blue)
                return red > blue ? Red : Blue;

            red = match.Kills(Red);
            blue = match.Kills(Blue);

            return red == blue ? 0 : red > blue ? Red : Blue;
        }

        private void Referee(Match match, long now)
        {
            // A team with nobody left has lost.
            if (!match.Forced)
            {
                if (match.Count(Red) == 0 || match.Count(Blue) == 0)
                {
                    End(match, match.Count(Red) > 0 ? Red : match.Count(Blue) > 0 ? Blue : 0, "forfeit");
                    return;
                }
            }

            if (now >= match.PhaseEnds)
            {
                End(match, Leader(match), "time");
                return;
            }

            if (now >= match.MinEndsAt)
            {
                if (!match.MinAnnounced)
                {
                    match.MinAnnounced = true;
                    Tell(match, PlayerMessage.PmBattlegroundMinimumTimeExpired);
                }

                foreach (var team in new[] { Red, Blue })
                    if (match.Points.Count > 0 && match.Held(team) == match.Points.Count)
                    {
                        End(match, team, "points");
                        return;
                    }
            }

            var left = match.PhaseEnds - now;

            if (!match.FiveAnnounced && left <= 300000 && match.PhaseEnds - match.MinEndsAt > 0)
            {
                match.FiveAnnounced = true;
                Tell(match, PlayerMessage.PmBattleground5MinuteWarning);
            }

            if (!match.OneAnnounced && left <= 60000)
            {
                match.OneAnnounced = true;
                Tell(match, PlayerMessage.PmBattleground1MinuteWarning);
            }
        }

        /// <summary>
        /// Ends the match with a winner, or with none: everyone told, the prestige given, the
        /// deserters who saw it through forgiven, the field reset and everyone back in their base
        /// for the next.
        /// </summary>
        public void End(Match match, uint winner, string reason = null)
        {
            if (match == null || match.Phase != Phase.Running)
                return;

            Logger.WriteLog(LogType.Debug, $"Battleground {match.Map.MapInfo.MapContextId}/{match.Map.InstanceId}: match {match.WargameId} ended, {(winner == 0 ? "nobody" : TeamName(winner))} won ({match.Held(Red)}:{match.Held(Blue)} points, {match.Kills(Red)}:{match.Kills(Blue)} kills).");

            // A match cut short - a team walked out of it, a game master ended it - is worth
            // nothing: its prestige is for one played to its minimum time.
            var now = Now();
            var played = now >= match.MinEndsAt;

            match.Phase = Phase.Waiting;
            match.Forced = false;
            match.ShowsResult = true;
            match.ResultEndsAt = now + ResultMs;

            foreach (var member in match.Members)
            {
                var client = member.Client;
                var player = client.Player;
                var won = winner != 0 && member.Team == winner;
                var prestige = played ? Award(client, won ? Config.WinPrestige : Config.LossPrestige) : 0;

                if (member.Score != null)
                {
                    member.Score.Prestige += prestige;
                    SendScore(match, member.Score);
                }

                if (winner != 0)
                    client.CallMethod(player.EntityId, won ? (Packets.PythonPacket)new WonBattlegroundPacket() : new LostBattlegroundPacket());

                // They finished a match on the team they once left.
                if (_deserters.TryGetValue(player.Id, out var deserted) && deserted == member.Team)
                    _deserters.Remove(player.Id);

                // No longer enemies.
                Wargames.Show(client);
            }

            if (winner == 0)
                Say(match, "Match over! Neither team wins.");
            else
                Tell(match, winner == Red ? PlayerMessage.PmCocpTestMatchOverRedWin : PlayerMessage.PmCocpTestMatchOverBlueWin);

            Tell(match, PlayerMessage.PmBattlegroundEnds);
            Tell(match, PlayerMessage.PmEdmundrangeBattlegroundEnd);

            foreach (var client in Present(match))
                client.CallMethod(SysEntity.ClientTeamManagerId, new ScoreBoardActivePacket(false));

            // On record before the field is reset: the points as they were held at the end.
            Keep(match, winner, reason);

            foreach (var member in match.Members)
                member.Score = null;

            ResetField(match, announce: true);

            // Behind their doors again.
            foreach (var member in match.Members)
                ToBase(match, member);

            foreach (var member in match.Members)
                ShowHospitals(match, member.Client);

            MatchEnded?.Invoke(match, winner);
        }

        /// <summary>
        /// Where everyone may be: a player with no team in the staging area, a team's players in
        /// their base until the match runs and out of the other team's always, and whoever has
        /// stood their time in the way out of a base on their way.
        /// </summary>
        private void Police(Match match, long now)
        {
            foreach (var member in match.Members.ToList())
            {
                var client = member.Client;
                var player = client?.Player;

                // Gone without saying so.
                if (player == null || client.State == ClientState.Disconnected || !ReferenceEquals(player.MapChannel, match.Map))
                {
                    Leave(match, member, deserting: match.Phase == Phase.Running, toStaging: false);
                    continue;
                }

                if (client.State != ClientState.Ingame || client.PendingTransfer != null)
                    continue;

                if (member.LeaveLink != null)
                {
                    if (!MapLinkManager.Contains(member.LeaveLink, player.Position) || player.State == CharacterState.Dead)
                    {
                        member.LeaveLink = null;
                        Say(client, $"You stay with {TeamName(member.Team)}.");
                    }
                    else if (now >= member.LeavesAt)
                    {
                        Leave(match, member, deserting: match.Phase == Phase.Running, toStaging: true);
                        continue;
                    }
                }

                if (player.State == CharacterState.Dead || player.State == CharacterState.Dying || IsExempt(client))
                    continue;

                if (InBase(match, Other(member.Team), player.Position))
                {
                    ToBase(match, member);
                    Say(client, $"{TeamName(Other(member.Team))}'s base is closed to you.");
                }
                else if (match.Phase != Phase.Running && !InBase(match, member.Team, player.Position))
                {
                    ToBase(match, member);
                    Say(client, "The doors open when the match begins.");
                }
            }

            foreach (var client in match.Map.ClientList.ToArray())
            {
                var player = client?.Player;

                if (player == null || client.State != ClientState.Ingame || client.PendingTransfer != null || match.Find(client) != null
                    || player.Position.Y <= match.Definition.StagingMaxY || IsExempt(client))
                    continue;

                Teleport(client, match.Definition.StagingArrival, match.Definition.StagingRotation);
                Say(client, "Choose a team in the Staging Area to go onto the range.");
            }
        }

        private void ToBase(Match match, Member member)
        {
            var arrival = BaseArrival(match, member.Team);

            if (!arrival.HasValue || member.Client?.Player == null || member.Client.State != ClientState.Ingame || member.Client.PendingTransfer != null
                || member.Client.Player.State == CharacterState.Dead)
                return;

            member.LeaveLink = null;
            Teleport(member.Client, arrival.Value.Position, arrival.Value.Rotation);
        }

        /// <summary>
        /// The Simulated Bane and the points they hold shut: a pool that has set its creatures
        /// down sets down no more until the field is reset, and a point is in service while the
        /// match runs and none of its Bane stands.
        /// </summary>
        private void Garrisons(Match match)
        {
            foreach (var point in match.Points)
            {
                foreach (var pool in PoolsOf(match.Map, point))
                    if (pool.HasSpawned)
                        pool.Suspended = true;

                var open = match.Phase == Phase.Running && GarrisonOf(match.Map, point.Source) != ControlPoints.Garrison.Standing;

                DynamicObjectManager.Instance.SetEnabled(point.Object, open);
            }
        }

        #endregion

        #region Showing it

        /// <summary>
        /// Puts a match that has ended on record (PvpRecords): Red Team is side 1 and Blue Team
        /// side 2, a team's score is the control points it held and its kills are beside it,
        /// and every row of the scoreboard goes with it - a deserter's too, as one who was not
        /// there at the end. A match nobody scored in (a game master's, with no teams) leaves none.
        /// </summary>
        private void Keep(Match match, uint winner, string reason)
        {
            var records = Records;

            if (records?.Store == null || match.Scores.Count == 0)
                return;

            records.Record(new PvpMatchEntry
            {
                Kind = (byte)PvpMatchKind.Battleground,
                WargameId = match.WargameId,
                MapContextId = match.Map?.MapInfo?.MapContextId ?? 0,
                InstanceId = match.Map?.InstanceId ?? 0,
                StartedAt = match.StartedAt,
                Outcome = (byte)(winner == Red || winner == Blue ? PvpMatchOutcome.Won : PvpMatchOutcome.Tied),
                WinnerSide = winner == Red ? (byte)1 : winner == Blue ? (byte)2 : (byte)0,
                Reason = reason ?? "",
                Side1Name = TeamName(Red),
                Side1Score = match.Held(Red),
                Side1Kills = match.Kills(Red),
                Side2Name = TeamName(Blue),
                Side2Score = match.Held(Blue),
                Side2Kills = match.Kills(Blue)
            }, match.Scores.Values.Where(score => score.Team == Red || score.Team == Blue).Select(score => new PvpMatchPlayerEntry
            {
                CharacterId = score.CharacterId,
                Side = score.Team == Red ? (byte)1 : (byte)2,
                Name = score.FirstName ?? "",
                FamilyName = score.Name ?? "",
                ClanId = score.ClanId,
                Kills = score.Kills,
                Deaths = score.Deaths,
                Damage = score.Damage,
                Healing = score.Healing,
                Captures = score.Captures,
                Prestige = score.Prestige,
                PresentAtEnd = score.Active
            }).ToList());
        }

        private Score ScoreFor(Match match, Member member)
        {
            var player = member.Client.Player;

            if (!match.Scores.TryGetValue(player.Id, out var score))
            {
                score = new Score { CharacterId = player.Id };
                match.Scores[player.Id] = score;
            }
            else if (score.EntityId != 0 && score.EntityId != player.EntityId)
            {
                // Back as another entity - they logged out and in again.
                Retire(match, score);
            }

            score.EntityId = player.EntityId;
            score.Name = player.FamilyName ?? "";
            score.FirstName = player.Name ?? "";
            score.ClanId = player.ClanId;
            score.ClassId = player.Class;
            score.Team = member.Team;
            score.Active = true;

            return score;
        }

        private static ScoreBoardIndividualUpdatePacket Row(Score score) => new ScoreBoardIndividualUpdatePacket(score.EntityId)
        {
            Name = score.Name,
            ClassId = score.ClassId,
            TeamId = score.Team,
            Active = score.Active,
            Kills = score.Kills,
            Deaths = score.Deaths,
            Damage = score.Damage,
            Healing = score.Healing,
            Captures = score.Captures,
            Prestige = score.Prestige
        };

        private void SendScore(Match match, Score score)
        {
            if (score == null)
                return;

            score.Dirty = false;

            foreach (var client in Present(match))
                client.CallMethod(SysEntity.ClientTeamManagerId, Row(score));
        }

        /// <summary>
        /// The rows that have changed with nothing to send them - damage and healing, which no
        /// kill or capture need follow - every <see cref="ScoreRefreshMs"/> of a match.
        /// </summary>
        private void SendChangedScores(Match match, long now)
        {
            if (now < match.ScoresDueAt)
                return;

            match.ScoresDueAt = now + ScoreRefreshMs;

            foreach (var score in match.Scores.Values.Where(s => s.Dirty).ToList())
                SendScore(match, score);
        }

        /// <summary>
        /// Takes off the clients' scoreboards the row they have for a character who has come back
        /// as another entity. The client keeps a row by the entity id it came under and shows it
        /// by the player's name (scoreboardwindow.py), and has nothing that removes one: left as
        /// it is, the old row and the new are one line on the board, which the old one - not
        /// active - can hide, and two in the team's kills. So the old one is sent once more with
        /// no name, not active and with nothing scored: a line of its own that is never shown
        /// and adds nothing.
        /// </summary>
        private void Retire(Match match, Score score)
        {
            var gone = new ScoreBoardIndividualUpdatePacket(score.EntityId)
            {
                Name = "",
                ClassId = score.ClassId,
                TeamId = score.Team,
                Active = false
            };

            foreach (var client in Present(match))
                client.CallMethod(SysEntity.ClientTeamManagerId, gone);
        }

        /// <summary>
        /// Puts the teams of the next match on the scoreboard in place of the last one's result:
        /// every row with nothing scored, those of players who have left since not active.
        /// </summary>
        private void ListTeams(Match match)
        {
            match.ShowsResult = false;

            var members = match.Members.Where(m => m.Client?.Player != null).Select(m => m.Client.Player.Id).ToHashSet();

            foreach (var score in match.Scores.Values.ToList())
            {
                score.Kills = score.Deaths = score.Damage = score.Healing = score.Captures = score.Prestige = 0;
                score.Rewarded.Clear();

                if (members.Contains(score.CharacterId))
                    continue;

                score.Active = false;
                SendScore(match, score);
            }

            foreach (var member in match.Members.Where(m => m.Client?.Player != null))
                SendScore(match, ScoreFor(match, member));
        }

        /// <summary>The seconds left on a match's clock: of the preparation, of the match, or none while it waits.</summary>
        public int SecondsLeft(Match match)
        {
            if (match.Phase == Phase.Waiting)
                return 0;

            return (int)Math.Max(0, (match.PhaseEnds - Now() + 999) / 1000);
        }

        private ScoreBoardGameScorePacket GameScore(Match match)
        {
            return new ScoreBoardGameScorePacket(SecondsLeft(match),
                match.Points.Where(p => p.ClientId != 0).ToDictionary(p => p.ClientId, p => p.Owner));
        }

        private void SendGameScore(Match match)
        {
            foreach (var client in Present(match))
                client.CallMethod(SysEntity.ClientTeamManagerId, GameScore(match));
        }

        /// <summary>
        /// The map's hospital markers as a player of a team has them: friendly where their team
        /// may go back to, and not where it may not.
        /// </summary>
        private void ShowHospitals(Match match, Client client)
        {
            if (client?.Player == null || client.State != ClientState.Ingame)
                return;

            MapMarkerManager.Instance.TeamHospitalsChanged(client, match.Map.MapInfo.MapContextId);
        }

        /// <summary>Everyone on the match's channel who is in the world.</summary>
        private static List<Client> Present(Match match)
        {
            return match.Map.ClientList.Where(c => c?.Player != null && c.State == ClientState.Ingame).ToList();
        }

        private static void Tell(Match match, PlayerMessage message, params (string Key, string Value)[] args)
        {
            var packet = new DisplayClientMessagePacket(message, args.ToDictionary(a => a.Key, a => a.Value), MsgFilterId.GeneralSystemMessages);

            foreach (var client in Present(match))
                client.CallMethod(SysEntity.CommunicatorId, packet);
        }

        private static void Say(Match match, string text)
        {
            foreach (var client in Present(match))
                Say(client, text);
        }

        private static void Say(Client client, string text)
        {
            if (client != null && client.State != ClientState.Disconnected)
                CommunicatorManager.Instance.SystemMessage(client, text);
        }

        /// <summary>Gives a player prestige and says so; what they got.</summary>
        private static int Award(Client client, int amount)
        {
            if (amount <= 0 || client?.Player == null)
                return 0;

            try
            {
                if (!PvpPrestige.Change(client, amount))
                    return 0;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Battleground: {amount} prestige for {client.Player.FamilyName} failed: {e.Message}");
                return 0;
            }

            client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmPrestigePointsReceived,
                new Dictionary<string, string> { { "amount", amount.ToString() } }, MsgFilterId.PrestigeGainLose));

            return amount;
        }

        /// <summary>The player somewhere else on their map, facing a way, for them and everyone who sees them.</summary>
        private static void MoveWithinMap(Client client, Vector3 position, float rotation)
        {
            var player = client?.Player;

            if (player?.MapChannel == null)
                return;

            player.PlaceAt(position);
            player.Rotation = rotation;

            client.CellMoveObject(client, new MoveObjectMessage(player.EntityId, new Movement(position, new Vector2(rotation, 0f))), false);
        }

        #endregion
    }
}
