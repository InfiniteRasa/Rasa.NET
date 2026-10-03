using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Config;
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// Which maps are entered as a squad's own instance, from appsettings.json's SquadInstances
    /// (SquadInstanceConfig). Replaced whole when the file changes.
    /// </summary>
    public static class SquadInstancePolicies
    {
        /// <summary>
        /// The maps entered as a squad's instance when the file names none: the Operations, the
        /// maps the client's mission log files under "Instance (...)" and that stand off a
        /// battlefield. Not Bootcamp, the arenas and wargames, or Manhattan.
        /// </summary>
        public static readonly IReadOnlyList<uint> DefaultMaps = new uint[]
        {
            1416, 1430, 1506, 1721, 2368,   // Wilderness: Guardian Prominence, Pravus Research, Caves of Donn, Crater Lake Research Facility, Caves of Donn (epic)
            1347, 1348, 1349, 1806,         // Divide: Minos Caverns, Timora Mines, Torcastra Prison, Purgas Station
            2084,                           // Concordia: Eloh Comm Tower
            1384, 1394, 1397, 1803,         // Palisades: Warnet Caverns, Devil's Den, Treeback Camp, Eloh Temples
            1502, 1823, 1830, 2029,         // Plateau: Ustor Yard, Sanctus Grotto, Maligo Base, Temporal Chamber
            1465, 1694, 1763,               // Pools: the weapons test centre, Retread Caves, Live Target Pens
            1429, 1451, 1700, 1743,         // Marshes: the wetland refinery, Bane Supply Depot, Logos Research Facility, the village ruins
            2156, 2163,                     // Descent: The Refuge, Outpost Inferno
            2136, 2162,                     // Howling Maw: Death Burrow, Cuthah Base
            1773, 2034, 2093,               // Plains: the Atta colony, the penal research centre, Brann Water Refinery
            2107, 2115, 2125,               // Mires: the energy weapon centre, Fluxite Mines, Tahrendra Base
            1865, 2085, 2111,               // Incline: Ojasa Atta Hive, the comm tower, the Warden bot factory
            1988, 2055, 2110,               // Ashen Desert: Bane Conscription Facility, Indra Caverns, Avernus Outpost
            2103, 2105, 2112,               // Thunderhead: Fault Lever, Quasso Station, Rivasa Atta Colony
            2155, 2190, 2203,               // Abyss: Ruins of Tampeii, Dybukkar, Omega Labs
            1977, 2138, 2141, 2146          // Crucible: Magma Caverns, Staal Junkyard, Incurables Ward, the Warden bot facility
        };

        private static volatile SquadInstanceConfig _config = new SquadInstanceConfig();
        private static volatile HashSet<uint> _maps = new HashSet<uint>(DefaultMaps);

        /// <summary>Takes the configured values; a file without the section is the defaults.</summary>
        public static void Apply(SquadInstanceConfig config)
        {
            config ??= new SquadInstanceConfig();

            _maps = new HashSet<uint>((config.Maps ?? DefaultMaps).Where(id => id > 0));
            _config = config;

            if (config.Enabled && config.WeeklyReset && !TryWeeklyReset(config, out _, out _))
                Logger.WriteLog(LogType.Error,
                    $"SquadInstances: WeeklyResetDay \"{config.WeeklyResetDay}\" or WeeklyResetTime \"{config.WeeklyResetTime}\" is not a day's English name and an HH:mm time: there is no weekly reset.");
        }

        /// <summary>The values in force.</summary>
        public static SquadInstanceConfig Current => _config;

        /// <summary>Whether a map is one of those entered as a squad's instance.</summary>
        public static bool IsSquadMap(uint mapContextId) => _config.Enabled && _maps.Contains(mapContextId);

        /// <summary>The day and time of day the weekly reset is set for. False when either does not read.</summary>
        public static bool TryWeeklyReset(SquadInstanceConfig config, out DayOfWeek day, out TimeSpan time)
        {
            day = default;
            time = default;

            return config != null
                   && Enum.TryParse(config.WeeklyResetDay?.Trim(), true, out day) && Enum.IsDefined(typeof(DayOfWeek), day)
                   && TimeSpan.TryParseExact(config.WeeklyResetTime?.Trim(), new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out time)
                   && time < TimeSpan.FromDays(1);
        }

        /// <summary>The first moment after one given that falls on that day of the week at that time.</summary>
        public static DateTime NextReset(DateTime after, DayOfWeek day, TimeSpan time)
        {
            var next = after.Date.AddDays(((int)day - (int)after.DayOfWeek + 7) % 7).Add(time);

            return next > after ? next : next.AddDays(7);
        }
    }

    /// <summary>
    /// Where a squad instance is entered: where a player who dies in one is put back on their feet.
    /// </summary>
    public static class InstanceEntrances
    {
        /// <summary>The map links of the world (MapLinkManager); replaceable for tests.</summary>
        public static Func<IEnumerable<MapLink>> MapLinks { get; set; } = () => MapLinkManager.Instance.Links;

        /// <summary>
        /// The entrance of the squad instance a player is in: where the door they came in by set
        /// them down, or - having come some other way, a summon or a login - where the map's
        /// first door does. Null on any other channel, and on a map no door leads into.
        /// </summary>
        public static (Vector3 Position, float Rotation)? Of(MapChannel map, Manifestation player)
        {
            if (map?.MapInfo == null || !map.IsSquadInstance)
                return null;

            var mapContextId = map.MapInfo.MapContextId;

            if (player?.InstanceEntrance is { } entered && entered.MapContextId == mapContextId)
                return (entered.Position, entered.Rotation);

            var door = MapLinks?.Invoke()?
                .Where(link => link.DestMapContextId == mapContextId && link.MapContextId != mapContextId)
                .OrderBy(link => link.Id)
                .FirstOrDefault();

            return door == null ? null : (door.DestPosition, door.DestRotation);
        }

        /// <summary>
        /// Counts the player as standing in every door of the map that holds the position, as
        /// one who has just arrived through it is (MapLinkManager.PlayerEnteredMap): the way out
        /// they are set down in does not take them out until they have stepped off it.
        /// </summary>
        public static void StandIn(MapChannel map, Manifestation player, Vector3 position)
        {
            if (map?.MapInfo == null || player == null)
                return;

            var mapContextId = map.MapInfo.MapContextId;

            foreach (var link in MapLinks?.Invoke() ?? Enumerable.Empty<MapLink>())
                if (link.MapContextId == mapContextId && MapLinkManager.Contains(link, position))
                    player.InsideMapLinks.Add(link.Id);
        }
    }

    /// <summary>
    /// Squad instances: an Operation is entered as a copy of its map that is the squad's own.
    /// The client's help has it so: "Operations are instanced areas created for you and your
    /// squad when you enter them."
    ///
    ///  - Which maps: SquadInstancePolicies - the Operations unless the file says otherwise. A
    ///    map with a MapInstances entry runs in shared copies and is not one of these.
    ///  - Whose: an instance is its owner's, the character of the squad's leader, or of the
    ///    player themselves in no squad. Anyone who goes to the map goes into the instance of
    ///    their squad's leader - one is made if there is none, whether or not the leader is in
    ///    it - by a door, a summon to the map, or a game master's teleport. One per owner per map.
    ///  - The lead changing hands: the squad goes where its leader is. A leader standing in an
    ///    instance of the map that is not theirs - the lead passed to them in it - has the
    ///    squad come to that one, whatever instance of their own they have kept; and it becomes
    ///    theirs if they have none. Should the leader be in none and have none while members
    ///    of the squad stand in one - a member was in their own when they joined - that one is
    ///    the squad's, and becomes the leader's. A player in an instance who is no longer of
    ///    its squad stays until they leave.
    ///  - Made as a shared copy is (spawn pools, objects, triggers and doors cloned from the
    ///    map's own channel) and a public place to those in it: everybody's kills, loot and
    ///    squad credit are as on any map. The map's own channel stays, with nobody in it.
    ///  - Dying: a player who dies in one is put back at its entrance (InstanceEntrances), not
    ///    at a hospital of the map (PlayerDeath).
    ///  - Staying: an instance is kept when everybody has left it. Its owner, and their squad,
    ///    come back to the same one, as they left it: nothing in it moves while it is empty,
    ///    for an empty channel's creatures and spawn pools are not run.
    ///  - The weekly reset: on WeeklyResetDay at WeeklyResetTime, the server's clock (Tuesday
    ///    at 03:00 unless the file says otherwise), every instance with nobody in it or on the
    ///    way is closed - taken off the lists, its creatures and objects with it - and the
    ///    next to go to the map has a new one. One with players in it is left alone, and
    ///    stands until the reset after. An instance does not outlive the process: a server
    ///    restart closes them all.
    ///  - EmptyCloseSeconds, for a server that would rather not keep them: 0 or more closes an
    ///    instance once it has stood empty that long. Negative, the default, never does.
    ///  - Entering the world on such a map: back into the instance they left, if it still
    ///    stands; else outside its door, with a word; on a map with no door, into an instance
    ///    of their own where they stood.
    /// </summary>
    public partial class MapChannelManager
    {
        /// <summary>How long an instance somebody has just been sent to is kept from closing, however empty it looks.</summary>
        public const int SquadHoldMs = 5000;

        /// <summary>A player's squad as the instances see it: whose the instance is, and who is of the squad.</summary>
        public readonly struct SquadMembership
        {
            /// <summary>The character of the squad's leader; the player's own in no squad.</summary>
            public uint OwnerCharacterId { get; }

            /// <summary>The characters of the squad, the player among them; null in no squad.</summary>
            public IReadOnlyCollection<uint> Members { get; }

            public SquadMembership(uint ownerCharacterId, IReadOnlyCollection<uint> members)
            {
                OwnerCharacterId = ownerCharacterId;
                Members = members;
            }
        }

        /// <summary>Whether a map is entered as a squad's instance (SquadInstancePolicies); replaceable for tests.</summary>
        public Func<uint, bool> SquadMapPolicy { get; set; } = SquadInstancePolicies.IsSquadMap;

        /// <summary>The squad instance values in force; replaceable for tests.</summary>
        public Func<SquadInstanceConfig> SquadConfig { get; set; } = () => SquadInstancePolicies.Current;

        /// <summary>A player's squad (PartyManager); replaceable for tests.</summary>
        public Func<Client, SquadMembership> SquadFor { get; set; } = PartySquadOf;

        /// <summary>The clock the weekly reset is told by; replaceable for tests.</summary>
        public Func<DateTime> WallClock { get; set; } = () => DateTime.Now;

        /// <summary>When the weekly reset next runs; null before the first pass, and while there is none.</summary>
        public DateTime? NextWeeklyReset { get; private set; }

        private readonly object _squadLock = new object();
        private string _weeklyResetSetting;

        private static SquadMembership PartySquadOf(Client client)
        {
            var party = PartyManager.Instance.PartyOf(client);

            if (party == null)
                return new SquadMembership(client.Player.Id, null);

            var leader = party.Find(party.PartyLeaderId);

            return new SquadMembership(
                leader != null && leader.CharacterId != 0 ? leader.CharacterId : client.Player.Id,
                party.Members.Select(member => member.CharacterId).Where(id => id != 0).ToHashSet());
        }

        /// <summary>Whether a map is entered as a squad's instance: one of the configured maps, loaded, and not run in shared copies.</summary>
        public bool IsSquadInstanceMap(uint mapContextId) =>
            SquadMapPolicy?.Invoke(mapContextId) == true && MapChannelArray.ContainsKey(mapContextId) && SharedPolicyOf(mapContextId) == null;

        /// <summary>The squad instances of a map, oldest first; of every map with none given.</summary>
        public IReadOnlyList<MapChannel> SquadInstancesOf(uint? mapContextId = null) => _privateInstances.SquadOf(mapContextId);

        /// <summary>
        /// The players in a channel or on their way to it. One who was sent to it stays on its
        /// list of arrivals until the list is next counted (PopulationOf): only those still on
        /// their way there are taken from it.
        /// </summary>
        private static IEnumerable<Client> PlayersOf(MapChannel map) =>
            map.ClientList.Concat(map.QueuedClients)
                .Concat(map.Arriving.Where(client => client?.PendingTransfer != null && ReferenceEquals(client.PendingTransfer.DestinationMap, map)))
                .Where(client => client?.Player != null)
                .Distinct()
                .ToArray();

        /// <summary>
        /// The squad instance of a map that a player goes into: the one they are in, if they are
        /// in one of that map; else the one their squad's leader stands in; else the leader's
        /// own, or their own in no squad; else the one members of their squad stand in, which
        /// becomes the leader's; else, when one may be made, a new one. Null for a map that is
        /// not entered so. The instance is held from closing for a moment
        /// (<see cref="SquadHoldMs"/>), for them to be sent into it.
        /// </summary>
        public MapChannel SquadInstanceFor(Client client, uint mapContextId, bool create)
        {
            var player = client?.Player;

            if (player == null || player.Id == 0 || !IsSquadInstanceMap(mapContextId))
                return null;

            var squad = SquadFor?.Invoke(client) ?? new SquadMembership(player.Id, null);
            var owner = squad.OwnerCharacterId != 0 ? squad.OwnerCharacterId : player.Id;

            lock (_squadLock)
            {
                MapChannel map = null;

                // In one of that map already - a game master moving about in it: that one.
                if (player.MapChannel is { IsSquadInstance: true } here && here.MapInfo.MapContextId == mapContextId
                    && ReferenceEquals(_privateInstances.FindByContextAndInstance(mapContextId, here.InstanceId), here))
                    map = here;

                // Their leader is standing in one that is not the leader's own - the lead passed
                // to them in it: where the leader is, and the leader's too if they have none.
                if (map == null && squad.Members != null && owner != player.Id)
                {
                    map = _privateInstances.SquadOf(mapContextId)
                        .FirstOrDefault(candidate => candidate.SquadOwnerCharacterId != owner && PlayersOf(candidate).Any(member => member.Player.Id == owner));

                    if (map != null && _privateInstances.ReassignSquad(map, owner))
                        Logger.WriteLog(LogType.Debug, $"Map {mapContextId}: squad instance {map.InstanceId} is character {owner}'s now, the squad's leader.");
                }

                map ??= _privateInstances.FindSquad(mapContextId, owner);

                // The squad is in one, and its leader has none: that one, and the leader's from now on.
                if (map == null && squad.Members != null)
                {
                    var theirs = _privateInstances.SquadOf(mapContextId)
                        .Select(candidate => (Map: candidate, Members: PlayersOf(candidate).Count(member => member != client && squad.Members.Contains(member.Player.Id))))
                        .Where(candidate => candidate.Members > 0)
                        .OrderByDescending(candidate => candidate.Members)
                        .ThenBy(candidate => candidate.Map.InstanceId)
                        .Select(candidate => candidate.Map)
                        .FirstOrDefault();

                    if (theirs != null && _privateInstances.ReassignSquad(theirs, owner))
                    {
                        map = theirs;
                        Logger.WriteLog(LogType.Debug, $"Map {mapContextId}: squad instance {map.InstanceId} is character {owner}'s now, the squad's leader.");
                    }
                }

                if (map == null && create && MapChannelArray.TryGetValue(mapContextId, out var template))
                {
                    map = _privateInstances.CreateSquad(template, owner, copy => InitializePrivateMapChannel(template, copy));

                    if (map != null)
                        Logger.WriteLog(LogType.Debug, $"Map {mapContextId}: squad instance {map.InstanceId} opened for character {owner}.");
                }

                if (map != null)
                {
                    map.HeldUntil = _clock() + SquadHoldMs;
                    map.EmptySince = 0;
                }

                return map;
            }
        }

        /// <summary>
        /// Sends a player to a map that is entered as a squad's instance, into theirs. By a door
        /// (MapLinkManager), where the door sets them down is the instance's entrance to them.
        /// </summary>
        internal bool EnterSquadInstance(Client client, uint mapContextId, Vector3 position, float rotation, bool byDoor)
        {
            if (client?.Player == null || client.State != ClientState.Ingame || client.PendingTransfer != null)
                return false;

            var map = SquadInstanceFor(client, mapContextId, create: true);

            if (map == null)
                return false;

            client.PendingInstanceChoice = null;

            // One made for them that they could not be sent to stands empty, theirs for next time.
            if (!Send(client, map, position, rotation))
                return false;

            if (byDoor)
                client.Player.InstanceEntrance = (mapContextId, position, rotation);

            return true;
        }

        /// <summary>
        /// Closes a squad instance that has nobody in it or on the way. False when it has, when
        /// somebody has just been sent to it, or when it is not a listed squad instance.
        /// </summary>
        public bool CloseSquadInstance(MapChannel map)
        {
            if (map == null || !map.IsSquadInstance)
                return false;

            lock (_squadLock)
            {
                if (PopulationOf(map) > 0 || _clock() < map.HeldUntil || !_privateInstances.ReleaseSquad(map))
                    return false;
            }

            CleanupPrivateMapChannel(map);

            Logger.WriteLog(LogType.Debug, $"Map {map.MapInfo.MapContextId}: squad instance {map.InstanceId} of character {map.SquadOwnerCharacterId} closed.");

            return true;
        }

        /// <summary>
        /// The weekly reset, and a game master's .instance reset: every squad instance with
        /// nobody in it or on the way is closed. Those with players in them are left as they
        /// are. Returns how many were closed.
        /// </summary>
        public int ResetSquadInstances() => _privateInstances.SquadOf().Count(CloseSquadInstance);

        /// <summary>
        /// Once a second, from the map channel worker: the weekly reset is run when its time has
        /// come; and, on a server set to close empty instances (EmptyCloseSeconds), those that
        /// have stood empty long enough are closed.
        /// </summary>
        internal void SquadInstanceWorker()
        {
            var now = _clock();
            var emptyCloseSeconds = SquadConfig?.Invoke()?.EmptyCloseSeconds ?? -1;

            foreach (var map in _privateInstances.SquadOf())
            {
                if (PopulationOf(map) > 0)
                {
                    map.EmptySince = 0;
                    continue;
                }

                if (map.EmptySince == 0)
                    map.EmptySince = Math.Max(1, now);

                // Negative: an empty instance is kept, until the weekly reset.
                if (emptyCloseSeconds >= 0 && now - map.EmptySince >= emptyCloseSeconds * 1000L)
                    CloseSquadInstance(map);
            }

            WeeklyResetWorker();
        }

        private void WeeklyResetWorker()
        {
            var config = SquadConfig?.Invoke();

            if (config == null || !config.Enabled || !config.WeeklyReset || !SquadInstancePolicies.TryWeeklyReset(config, out var day, out var time))
            {
                NextWeeklyReset = null;
                _weeklyResetSetting = null;
                return;
            }

            var now = WallClock();
            var setting = $"{day} {time}";

            // The first pass, or the day or time changed in the file: from now on.
            if (NextWeeklyReset == null || _weeklyResetSetting != setting)
            {
                NextWeeklyReset = SquadInstancePolicies.NextReset(now, day, time);
                _weeklyResetSetting = setting;
                return;
            }

            if (now < NextWeeklyReset.Value)
                return;

            var standing = _privateInstances.SquadOf().Count;
            var closed = ResetSquadInstances();

            NextWeeklyReset = SquadInstancePolicies.NextReset(now, day, time);

            Logger.WriteLog(LogType.Initialize,
                $"Weekly squad instance reset: {closed} of {standing} closed, {standing - closed} left with players in them. The next is {NextWeeklyReset.Value:yyyy-MM-dd HH:mm}.");
        }

        /// <summary>
        /// Where a character entering the world on a map that is entered as a squad's instance
        /// goes, their map being its own channel as their row has it (<see cref="PlaceLogin"/>).
        /// </summary>
        private void PlaceSquadLogin(Client client, uint mapContextId)
        {
            var player = client.Player;
            MapChannel left = null;

            lock (_squadLock)
            {
                if (_copyLeft.TryGetValue(player.Id, out var was) && was.MapContextId == mapContextId)
                    left = _privateInstances.SquadOf(mapContextId).FirstOrDefault(map => map.InstanceId == was.InstanceId);

                if (left != null)
                {
                    left.HeldUntil = _clock() + SquadHoldMs;
                    left.EmptySince = 0;
                }
            }

            if (left != null)
            {
                player.MapChannel = left;
                return;
            }

            if (PlaceOutside(client, mapContextId, "The instance you were in has closed."))
            {
                Logger.WriteLog(LogType.Debug, $"{player.FamilyName} entered the world outside map {mapContextId}: the squad instance they left is gone.");
                return;
            }

            // No door in or out of the map: an instance of their own, where they stood.
            var fresh = SquadInstanceFor(client, mapContextId, create: true);

            if (fresh != null)
                player.MapChannel = fresh;
        }
    }
}
