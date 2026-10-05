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
    using Repositories.Char.SquadInstance;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

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
    /// Where the squad instances are saved (the character database's squad_instance tables):
    /// something done with their repository, saved as it is done.
    /// </summary>
    public interface ISquadInstanceStore
    {
        T With<T>(Func<ISquadInstanceRepository, T> action);
    }

    /// <summary>The server's store: a character unit of work for each thing done.</summary>
    internal sealed class DatabaseSquadInstanceStore : ISquadInstanceStore
    {
        private readonly IGameUnitOfWorkFactory _factory;

        internal DatabaseSquadInstanceStore(IGameUnitOfWorkFactory factory) => _factory = factory;

        public T With<T>(Func<ISquadInstanceRepository, T> action)
        {
            using var unitOfWork = _factory.CreateChar();

            return action(unitOfWork.SquadInstances);
        }
    }

    /// <summary>
    /// What a squad instance keeps of itself: the row it is saved under, since when it counts,
    /// and which of its spawn pools are dead and when the last of each died. A pool comes back
    /// by the clock on the wall, a set time after that - whether or not anybody was in the
    /// instance meanwhile, and whatever the pool's own respawn time is (SpawnPoolManager).
    /// </summary>
    public sealed class SquadInstanceState
    {
        private readonly Func<long> _utcNowMs;
        private readonly Func<long> _respawnMs;
        private readonly Action<uint, uint, long> _saveCleared;
        private readonly Action<uint, uint> _saveRespawned;
        private readonly Action<uint> _saveReset;

        /// <summary>The squad_instance row; 0 for an instance that is not saved.</summary>
        public uint DbId { get; }

        /// <summary>When it was made, or when a weekly reset last passed it over: Unix milliseconds, UTC.</summary>
        public long CreatedAtUtcMs { get; internal set; }

        internal SquadInstanceState(uint dbId, long createdAtUtcMs, Func<long> utcNowMs, Func<long> respawnMs,
            Action<uint, uint, long> saveCleared, Action<uint, uint> saveRespawned, Action<uint> saveReset)
        {
            DbId = dbId;
            CreatedAtUtcMs = createdAtUtcMs;
            _utcNowMs = utcNowMs;
            _respawnMs = respawnMs;
            _saveCleared = saveCleared;
            _saveRespawned = saveRespawned;
            _saveReset = saveReset;
        }

        /// <summary>The last creature of a pool has died: it is dead from now.</summary>
        public void PoolCleared(SpawnPool pool)
        {
            if (pool == null)
                return;

            pool.ClearedAtUtcMs = Math.Max(1, _utcNowMs());

            if (DbId != 0 && pool.DbId != 0)
                _saveCleared(DbId, pool.DbId, pool.ClearedAtUtcMs);
        }

        /// <summary>
        /// Whether a pool with nothing alive may spawn: it has never been cleared, or it was
        /// cleared the respawn time ago. With no respawn time (0 or less) a cleared pool stays
        /// dead until the instance is closed.
        /// </summary>
        public bool RespawnDue(SpawnPool pool)
        {
            if (pool == null || pool.ClearedAtUtcMs == 0)
                return true;

            var respawnMs = _respawnMs();

            return respawnMs > 0 && _utcNowMs() - pool.ClearedAtUtcMs >= respawnMs;
        }

        /// <summary>A pool has creatures again, alive or on their way.</summary>
        public void PoolRespawned(SpawnPool pool)
        {
            if (pool == null || pool.ClearedAtUtcMs == 0)
                return;

            pool.ClearedAtUtcMs = 0;

            if (DbId != 0 && pool.DbId != 0)
                _saveRespawned(DbId, pool.DbId);
        }

        /// <summary>Every pool is to spawn afresh (/killmap): none is dead any more.</summary>
        public void PoolsReset(IEnumerable<SpawnPool> pools)
        {
            foreach (var pool in pools ?? Enumerable.Empty<SpawnPool>())
                pool.ClearedAtUtcMs = 0;

            if (DbId != 0)
                _saveReset(DbId);
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
    ///  - Its creatures: a spawn pool whose creatures are all dead comes back RespawnMinutes
    ///    (45) after the last of them died, by the clock on the wall - not after the pool's
    ///    own respawn time, and whether or not anybody was in the instance meanwhile
    ///    (SquadInstanceState). 0 or less: not until the instance is closed.
    ///  - Saved: the instance (its map, its owner and since when), each dead pool with when it
    ///    died, and the instance each character last went into are written to the character
    ///    database as they change (squad_instance, squad_instance_pool, squad_instance_visitor),
    ///    so an instance lasts through a restart. Not saved: a pool some of whose creatures
    ///    are dead, which comes back whole; corpses and loot on the ground; where its creatures
    ///    stood and what state its objects were in.
    ///  - In memory: an instance is loaded from its rows when somebody goes to it, and taken
    ///    out of memory once it has stood empty for UnloadEmptySeconds (300), its rows kept.
    ///    A restart is the same thing for every instance at once.
    ///  - The weekly reset: on WeeklyResetDay at WeeklyResetTime, the server's clock (Tuesday
    ///    at 03:00 unless the file says otherwise), every instance with nobody in it or on the
    ///    way is closed - its rows deleted, and what is in memory with them - and the next to
    ///    go to the map has a new one. One with players in it is left alone, counts from that
    ///    reset, and stands until the one after. A server that was down at the time closes,
    ///    on its first pass, the instances that the reset it missed would have.
    ///  - EmptyCloseSeconds, for a server that would rather not keep them: 0 or more closes an
    ///    instance once it has stood empty that long. Negative, the default, never does.
    ///  - Entering the world on such a map: back into the instance they last went into, if it
    ///    still stands; else their own of that map, if they have one; else outside its door,
    ///    with a word; on a map with no door, into an instance of their own where they stood.
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

        /// <summary>The clock what is saved is dated by, and the respawn of a pool told by; replaceable for tests.</summary>
        public Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;

        /// <summary>When the weekly reset next runs; null before the first pass, and while there is none.</summary>
        public DateTime? NextWeeklyReset { get; private set; }

        private readonly object _squadLock = new object();
        private string _weeklyResetSetting;
        private bool _missedResetLookedFor;
        private ISquadInstanceStore _squadStore;
        private bool _squadStoreGiven;

        /// <summary>
        /// Where the instances are saved: the character database, for a manager that has one.
        /// Null - a manager with no database, in a test - and nothing is saved. Replaceable.
        /// </summary>
        public ISquadInstanceStore SquadStore
        {
            get => _squadStoreGiven || _gameUnitOfWorkFactory == null
                ? _squadStore
                : _squadStore ??= new DatabaseSquadInstanceStore(_gameUnitOfWorkFactory);
            set
            {
                _squadStore = value;
                _squadStoreGiven = true;
            }
        }

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private long UtcNowMs() => (long)(UtcNow() - UnixEpoch).TotalMilliseconds;

        private long RespawnMs() => (SquadConfig?.Invoke()?.RespawnMinutes ?? 0) * 60000L;

        /// <summary>
        /// Does something with the saved instances. With nowhere to save, or the database failing
        /// it, the answer is the one given: the instances go on in memory.
        /// </summary>
        private T Stored<T>(Func<ISquadInstanceRepository, T> action, T otherwise = default)
        {
            var store = SquadStore;

            if (store == null)
                return otherwise;

            try
            {
                return store.With(action);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Squad instances: the character database could not be read or written: {e.GetBaseException().Message}");
                return otherwise;
            }
        }

        private void Stored(Action<ISquadInstanceRepository> action) => Stored(repository =>
        {
            action(repository);
            return 0;
        });

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

        /// <summary>The squad instances in memory: of a map, oldest first; of every map with none given.</summary>
        public IReadOnlyList<MapChannel> SquadInstancesOf(uint? mapContextId = null) => _privateInstances.SquadOf(mapContextId);

        /// <summary>The squad instances that are saved, in memory or not.</summary>
        public IReadOnlyList<SquadInstanceEntry> SavedSquadInstances() => Stored(repository => repository.GetAll(), new List<SquadInstanceEntry>());

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
        /// Puts an owner's instance of a map in memory: from its row, with the pools that are
        /// dead marked so; or, with no row given, a new one, which is saved. The owner's one in
        /// memory already, if there is one. Called with the squad lock held.
        /// </summary>
        private MapChannel OpenSquadInstance(uint mapContextId, uint owner, SquadInstanceEntry saved)
        {
            if (!MapChannelArray.TryGetValue(mapContextId, out var template))
                return null;

            var listed = _privateInstances.FindSquad(mapContextId, owner);

            if (listed != null)
                return listed;

            var map = _privateInstances.CreateSquad(template, owner, copy => InitializePrivateMapChannel(template, copy));

            if (map == null)
                return null;

            var now = UtcNowMs();
            var row = saved ?? Stored(repository => repository.Create(mapContextId, owner, now));

            map.SquadState = new SquadInstanceState(row?.Id ?? 0, row?.CreatedAt ?? now, UtcNowMs, RespawnMs,
                (id, pool, at) => Stored(repository => repository.SetPoolCleared(id, pool, at)),
                (id, pool) => Stored(repository => repository.RemovePool(id, pool)),
                id => Stored(repository => repository.RemovePools(id)));

            // The pools that are dead, of a row that was there: the one loaded, or - the database
            // having failed the look for it - the one that making it found.
            if (row != null)
            {
                var cleared = Stored(repository => repository.GetPools(row.Id), new Dictionary<uint, long>());

                foreach (var pool in map.SpawnPools)
                    if (pool.DbId != 0 && cleared.TryGetValue(pool.DbId, out var at))
                    {
                        pool.ClearedAtUtcMs = Math.Max(1, at);
                        pool.UpdateTimer = 0;

                        // It has spawned before: what comes back arrives as a respawn does.
                        pool.HasSpawned = true;
                    }
            }

            Logger.WriteLog(LogType.Debug, saved != null
                ? $"Map {mapContextId}: squad instance {map.InstanceId} of character {owner} loaded from row {saved.Id}."
                : $"Map {mapContextId}: squad instance {map.InstanceId} opened for character {owner}{(row != null ? $", row {row.Id}" : ", not saved")}.");

            return map;
        }

        /// <summary>Makes an instance another character's, in memory and in its row.</summary>
        private bool ReassignSquadInstance(MapChannel map, uint owner)
        {
            if (!_privateInstances.ReassignSquad(map, owner))
                return false;

            var id = map.SquadState?.DbId ?? 0;

            if (id != 0)
                Stored(repository => repository.SetOwner(id, owner));

            Logger.WriteLog(LogType.Debug, $"Map {map.MapInfo.MapContextId}: squad instance {map.InstanceId} is character {owner}'s now, the squad's leader.");

            return true;
        }

        /// <summary>
        /// The squad instance of a map that a player goes into: the one they are in, if they are
        /// in one of that map; else the one their squad's leader stands in; else the leader's
        /// own, or their own in no squad, loaded if it is saved and not in memory; else the one
        /// members of their squad stand in, which becomes the leader's; else, when one may be
        /// made, a new one. Null for a map that is not entered so. The instance is held from
        /// closing for a moment (<see cref="SquadHoldMs"/>), for them to be sent into it.
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

                var saved = map == null && _privateInstances.FindSquad(mapContextId, owner) == null
                    ? Stored(repository => repository.Find(mapContextId, owner))
                    : null;

                // Their leader is standing in one that is not the leader's own - the lead passed
                // to them in it: where the leader is, and the leader's too if they have none.
                if (map == null && squad.Members != null && owner != player.Id)
                {
                    map = _privateInstances.SquadOf(mapContextId)
                        .FirstOrDefault(candidate => candidate.SquadOwnerCharacterId != owner && PlayersOf(candidate).Any(member => member.Player.Id == owner));

                    if (map != null && saved == null)
                        ReassignSquadInstance(map, owner);
                }

                map ??= _privateInstances.FindSquad(mapContextId, owner);

                // The leader's own, saved and not in memory.
                if (map == null && saved != null)
                    map = OpenSquadInstance(mapContextId, owner, saved);

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

                    if (theirs != null && ReassignSquadInstance(theirs, owner))
                        map = theirs;
                }

                if (map == null && create)
                    map = OpenSquadInstance(mapContextId, owner, null);

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
        /// The instance is noted as the one they last went into, for when they next enter the
        /// world on that map.
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

        /// <summary>Saves the squad instance a character has gone into, for when they next enter the world on its map.</summary>
        private void NoteVisitor(uint characterId, MapChannel map)
        {
            var id = map?.SquadState?.DbId ?? 0;

            if (id != 0 && characterId != 0)
                Stored(repository => repository.SetVisitorInstance(characterId, id));
        }

        /// <summary>
        /// Closes a squad instance that has nobody in it or on the way: out of memory, and its
        /// rows deleted. False when it has, when somebody has just been sent to it, or when it
        /// is not a listed squad instance.
        /// </summary>
        public bool CloseSquadInstance(MapChannel map) => ReleaseSquadInstance(map, delete: true);

        /// <summary>
        /// Takes a squad instance that has nobody in it or on the way out of memory, its rows
        /// kept: it is loaded from them when somebody next goes to it. False as for closing, and
        /// for an instance that is not saved, which would be lost.
        /// </summary>
        public bool UnloadSquadInstance(MapChannel map) => map?.SquadState != null && map.SquadState.DbId != 0 && ReleaseSquadInstance(map, delete: false);

        private bool ReleaseSquadInstance(MapChannel map, bool delete)
        {
            if (map == null || !map.IsSquadInstance)
                return false;

            SquadInstanceState state;

            lock (_squadLock)
            {
                if (PopulationOf(map) > 0 || _clock() < map.HeldUntil || !_privateInstances.ReleaseSquad(map))
                    return false;

                // Taken apart with nothing listening: its creatures going is not their dying.
                state = map.SquadState;
                map.SquadState = null;

                CleanupPrivateMapChannel(map);

                if (delete && state != null && state.DbId != 0)
                    Stored(repository => repository.Delete(state.DbId));
            }

            Logger.WriteLog(LogType.Debug,
                $"Map {map.MapInfo.MapContextId}: squad instance {map.InstanceId} of character {map.SquadOwnerCharacterId} {(delete ? "closed" : "taken out of memory")}.");

            return true;
        }

        /// <summary>
        /// The weekly reset, and a game master's .instance reset: every squad instance with
        /// nobody in it or on the way is closed, in memory or only saved. Those with players in
        /// them are left as they are, and count from now. With a time given, only the instances
        /// from before it are closed - the reset a server missed while it was down - and those
        /// left are left as they were. Returns how many were closed.
        /// </summary>
        public int ResetSquadInstances(long? madeBeforeUtcMs = null)
        {
            lock (_squadLock)
            {
                var now = UtcNowMs();
                var keep = new List<uint>();
                var closed = 0;

                foreach (var map in _privateInstances.SquadOf())
                {
                    var state = map.SquadState;
                    var id = state?.DbId ?? 0;

                    if (madeBeforeUtcMs != null && (state == null || state.CreatedAtUtcMs >= madeBeforeUtcMs))
                    {
                        if (id != 0)
                            keep.Add(id);

                        continue;
                    }

                    if (CloseSquadInstance(map))
                    {
                        closed++;
                        continue;
                    }

                    if (id == 0)
                        continue;

                    keep.Add(id);

                    // Passed over with players in it: it counts from this reset.
                    if (madeBeforeUtcMs == null)
                    {
                        state.CreatedAtUtcMs = now;
                        Stored(repository => repository.SetCreatedAt(id, now));
                    }
                }

                return closed + Stored(repository => repository.DeleteAllExcept(keep, madeBeforeUtcMs), 0);
            }
        }

        /// <summary>
        /// Once a second, from the map channel worker: the weekly reset is run when its time has
        /// come; an instance that has stood empty long enough is taken out of memory, its rows
        /// kept (UnloadEmptySeconds); and, on a server set to close empty instances
        /// (EmptyCloseSeconds), closed.
        /// </summary>
        internal void SquadInstanceWorker()
        {
            var now = _clock();
            var config = SquadConfig?.Invoke();
            var emptyCloseSeconds = config?.EmptyCloseSeconds ?? -1;
            var unloadSeconds = config?.UnloadEmptySeconds ?? -1;

            foreach (var map in _privateInstances.SquadOf())
            {
                if (PopulationOf(map) > 0)
                {
                    map.EmptySince = 0;
                    continue;
                }

                if (map.EmptySince == 0)
                    map.EmptySince = Math.Max(1, now);

                var emptyMs = now - map.EmptySince;

                // Negative: an empty instance is kept, until the weekly reset.
                if (emptyCloseSeconds >= 0 && emptyMs >= emptyCloseSeconds * 1000L)
                    CloseSquadInstance(map);
                else if (unloadSeconds >= 0 && emptyMs >= unloadSeconds * 1000L)
                    UnloadSquadInstance(map);
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

                // The first pass of all: the reset that came round while the server was down.
                if (!_missedResetLookedFor)
                {
                    _missedResetLookedFor = true;

                    var last = NextWeeklyReset.Value.AddDays(-7);
                    var missed = ResetSquadInstances(UtcNowMs() - (long)(now - last).TotalMilliseconds);

                    if (missed > 0)
                        Logger.WriteLog(LogType.Initialize, $"Weekly squad instance reset of {last:yyyy-MM-dd HH:mm}, missed: {missed} closed.");
                }

                return;
            }

            if (now < NextWeeklyReset.Value)
                return;

            var closed = ResetSquadInstances();

            NextWeeklyReset = SquadInstancePolicies.NextReset(now, day, time);

            Logger.WriteLog(LogType.Initialize,
                $"Weekly squad instance reset: {closed} closed, {_privateInstances.SquadOf().Count} left with players in them. The next is {NextWeeklyReset.Value:yyyy-MM-dd HH:mm}.");
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
                // The one they left, still in memory.
                if (_copyLeft.TryGetValue(player.Id, out var was) && was.MapContextId == mapContextId)
                    left = _privateInstances.SquadOf(mapContextId).FirstOrDefault(map => map.InstanceId == was.InstanceId);

                // The one they last went into, as it is saved: in memory, or loaded.
                if (left == null)
                {
                    var id = Stored(repository => repository.GetVisitorInstance(player.Id), 0u);
                    var row = id != 0 ? Stored(repository => repository.Get(id)) : null;

                    if (row != null && row.MapContextId == mapContextId)
                        left = _privateInstances.SquadOf(mapContextId).FirstOrDefault(map => map.SquadState?.DbId == row.Id)
                               ?? OpenSquadInstance(mapContextId, row.OwnerCharacterId, row);
                }

                if (left != null)
                {
                    left.HeldUntil = _clock() + SquadHoldMs;
                    left.EmptySince = 0;
                }
            }

            // Their own of that map, if they have one.
            left ??= SquadInstanceFor(client, mapContextId, create: false);

            if (left != null)
            {
                player.MapChannel = left;
                NoteVisitor(player.Id, left);
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
            {
                player.MapChannel = fresh;
                NoteVisitor(player.Id, fresh);
            }
        }
    }
}
