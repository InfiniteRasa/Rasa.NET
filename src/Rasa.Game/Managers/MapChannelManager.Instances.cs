using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Config;
    using Data;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Which maps run in several shared copies, from appsettings.json's MapInstances
    /// (MapInstanceConfig). Replaced whole when the file changes.
    /// </summary>
    public static class MapInstancePolicies
    {
        private static volatile Dictionary<uint, MapInstanceConfig> _configured = new Dictionary<uint, MapInstanceConfig>();

        /// <summary>Takes the configured maps: an entry whose key is not a map context id is dropped.</summary>
        public static void Apply(IDictionary<string, MapInstanceConfig> configured)
        {
            var next = new Dictionary<uint, MapInstanceConfig>();

            if (configured != null)
                foreach (var entry in configured)
                    if (uint.TryParse(entry.Key, out var mapContextId) && mapContextId > 0 && entry.Value != null)
                        next[mapContextId] = entry.Value;

            _configured = next;
        }

        /// <summary>The map's entry, or null for a map with none.</summary>
        public static MapInstanceConfig For(uint mapContextId) =>
            _configured.TryGetValue(mapContextId, out var policy) ? policy : null;
    }

    /// <summary>
    /// Shared copies of a map, and the client's instance picker.
    ///
    /// The client's: Recv_ChooseInstanceList opens the waypoint window as "Instance Selection -
    /// Select the instance of the world map to enter", one row a copy, named as the map with its
    /// number and a population word (Low, Medium, High, Full: POPULATION_*); the player's pick
    /// comes back as SelectInstance(instanceId, startGroup) and closing the window as
    /// SelectInstanceCancel. A copy is told to the client as the instance id of its Wonkavate.
    ///
    /// Ours, the server's side of it being gone:
    ///  - a map with a MapInstances entry (MapInstanceConfig) runs in up to MaxCopies copies of
    ///    Capacity players each. The first is the map's own channel, instance 1, which always
    ///    stands; the others are made from it as a private instance is (spawn pools, objects,
    ///    triggers and links cloned), are nobody's, and are public places in every other way;
    ///  - a copy opens when every copy before it is full, as somebody walks into a door to the
    ///    map (<see cref="EnterMap"/>, from MapLinkManager). While there is one copy the door
    ///    leads straight in. Once there are more the picker lists them all, the fullest one with
    ///    room first, and the player goes where they pick;
    ///  - a player on the way to a copy counts towards how full it is from the moment they are
    ///    sent (MapChannel.Arriving), so a crowd at the door does not overfill it;
    ///  - a player who left a battleground's match while it was being played is led by the door
    ///    back to the copy they left, with no picker, and into no other until their lockout has
    ///    run out (Battlegrounds.LockoutFor);
    ///  - a copy other than the map's own closes once it has stood empty for IdleCloseSeconds;
    ///  - a summon brings a player into the copy the summoner stands in, full or not; a game
    ///    master's teleport goes to the map's own channel, as before;
    ///  - a character entering the world on such a map goes back into the copy it left
    ///    (<see cref="PlaceLogin"/>), if that one still stands, has room and is not shut to
    ///    them. If not, or after a restart, which forgets who was where, it enters the world
    ///    outside the map, where its way out leads, and comes in by the door as anybody does:
    ///    nobody logs in to a copy they did not leave.
    /// </summary>
    public partial class MapChannelManager
    {
        /// <summary>How long an instance picker's answer counts.</summary>
        public const int InstanceChoiceSeconds = 120;

        /// <summary>How far from where the picker opened a player may have gone and still be sent on by it.</summary>
        public const float InstanceChoiceReach = 12f;

        /// <summary>The configured copies of a map (MapInstancePolicies); replaceable for tests.</summary>
        public Func<uint, MapInstanceConfig> InstancePolicy { get; set; } = MapInstancePolicies.For;

        /// <summary>The map links of the world (MapLinkManager); replaceable for tests.</summary>
        public Func<IEnumerable<MapLink>> MapLinks { get; set; } = () => MapLinkManager.Instance.Links;

        /// <summary>The copy of a map each character was last taken out of, by character id. Not kept over a restart.</summary>
        private readonly Dictionary<uint, (uint MapContextId, uint InstanceId)> _copyLeft = new Dictionary<uint, (uint, uint)>();

        /// <summary>The map's entry if it runs in more than one copy, else null.</summary>
        public MapInstanceConfig SharedPolicyOf(uint mapContextId)
        {
            var policy = InstancePolicy?.Invoke(mapContextId);

            return policy != null && policy.MaxCopies > 1 && policy.Capacity > 0 ? policy : null;
        }

        /// <summary>
        /// Notes the copy a player is being taken out of, for <see cref="PlaceLogin"/>: on every
        /// way off a map that runs in copies (ManifestationManager.RemovePlayerCharacter).
        /// </summary>
        public void RememberCopy(Client client)
        {
            var player = client?.Player;
            var map = player?.MapChannel;

            if (map?.MapInfo == null || player.Id == 0 || map.IsPrivateInstance)
                return;

            // A squad's instance is remembered as a shared copy is (PlaceSquadLogin).
            if (!map.IsSquadInstance && SharedPolicyOf(map.MapInfo.MapContextId) == null)
                return;

            _copyLeft[player.Id] = (map.MapInfo.MapContextId, map.InstanceId);
        }

        /// <summary>The copy of a map a character was last taken out of, if it still stands; else null.</summary>
        public MapChannel CopyLeftBy(uint characterId, uint mapContextId)
        {
            if (!_copyLeft.TryGetValue(characterId, out var left) || left.MapContextId != mapContextId)
                return null;

            return CopiesOf(mapContextId).FirstOrDefault(copy => copy.InstanceId == left.InstanceId);
        }

        /// <summary>
        /// Where a character entering the world on a map that runs in copies goes, their map
        /// being its own channel as their row has it: back into the copy they left, if it
        /// stands, has room and no lockout shuts it to them; and otherwise out of the map, to
        /// where its way out leads (or, a map with none, to the door into it), with a word for
        /// when they have arrived. Never into a copy they did not leave. Called between the
        /// character being loaded and the client being told where to (CharacterManager).
        /// </summary>
        public void PlaceLogin(Client client)
        {
            var player = client?.Player;
            var own = player?.MapChannel;

            if (own?.MapInfo == null || own.IsPrivateInstance)
                return;

            var mapContextId = own.MapInfo.MapContextId;

            // A map entered as a squad's instance: the one they left, or outside its door.
            if (IsSquadInstanceMap(mapContextId))
            {
                PlaceSquadLogin(client, mapContextId);
                return;
            }

            var policy = SharedPolicyOf(mapContextId);

            if (policy == null)
                return;

            var left = CopyLeftBy(player.Id, mapContextId);
            Battlegrounds.Lockout lockout = null;

            if (left != null && !IsFull(left, policy) && !Battlegrounds.Instance.BarredFrom(client, left, out lockout))
            {
                player.MapChannel = left;
                left.EmptySince = 0;
                return;
            }

            if (!PlaceOutside(client, mapContextId, left == null ? "The instance you were in has closed."
                    : lockout != null ? Battlegrounds.Instance.LockoutText(lockout)
                    : "The instance you were in is full."))
            {
                Logger.WriteLog(LogType.Error,
                    $"Map {mapContextId} runs in copies and has no map link out of it or into it: {player.FamilyName} entered the world on its first copy.");
                return;
            }

            Logger.WriteLog(LogType.Debug,
                $"{player.FamilyName} entered the world outside map {mapContextId}: the copy they left {(left == null ? "is gone" : "is full or shut to them")}.");
        }

        /// <summary>
        /// Puts a character entering the world outside a map instead of on it: where the map's
        /// way out leads or, a map with none, at the door into it - with why, for when they have
        /// arrived. False, and nothing done, for a map with neither.
        /// </summary>
        private bool PlaceOutside(Client client, uint mapContextId, string why)
        {
            var player = client.Player;
            var links = (MapLinks?.Invoke() ?? Enumerable.Empty<MapLink>()).OrderBy(link => link.Id).ToList();
            var exit = links.FirstOrDefault(link => link.MapContextId == mapContextId && link.DestMapContextId != mapContextId
                                                    && MapChannelArray.ContainsKey(link.DestMapContextId));
            var door = exit == null
                ? links.FirstOrDefault(link => link.DestMapContextId == mapContextId && link.MapContextId != mapContextId
                                               && MapChannelArray.ContainsKey(link.MapContextId))
                : null;

            if (exit == null && door == null)
                return false;

            var outside = exit?.DestMapContextId ?? door.MapContextId;

            player.MapChannel = MapChannelArray[outside];
            player.MapContextId = outside;
            player.PlaceAt(exit?.DestPosition ?? door.Position);
            player.Rotation = exit?.DestRotation ?? 0;
            client.ArrivalNotice = why + " You are outside its door.";

            return true;
        }

        /// <summary>What <see cref="PlaceLogin"/> left to be said, now that there is a client in the world to say it to.</summary>
        internal static void ShowArrivalNotice(Client client)
        {
            var notice = client?.ArrivalNotice;

            if (notice == null)
                return;

            client.ArrivalNotice = null;
            CommunicatorManager.Instance.SystemMessage(client, notice);
        }

        /// <summary>The copies of a map that anyone may enter: its own channel first, then the shared ones, oldest first.</summary>
        public List<MapChannel> CopiesOf(uint mapContextId)
        {
            var copies = new List<MapChannel>();

            if (MapChannelArray.TryGetValue(mapContextId, out var own))
                copies.Add(own);

            copies.AddRange(_privateInstances.SharedOf(mapContextId));

            return copies;
        }

        /// <summary>How many players a channel holds, counting those sent to it who have not arrived.</summary>
        public int PopulationOf(MapChannel map)
        {
            if (map == null)
                return 0;

            map.Arriving.RemoveWhere(client => client?.PendingTransfer == null || !ReferenceEquals(client.PendingTransfer.DestinationMap, map));

            return map.ClientList.Concat(map.QueuedClients).Concat(map.Arriving).Where(client => client != null).Distinct().Count();
        }

        /// <summary>The population word the picker shows for a channel holding that many of a capacity.</summary>
        public static MapInstanceStatus StatusOf(int population, int capacity)
        {
            if (capacity <= 0 || population >= capacity)
                return MapInstanceStatus.Full;

            if (population * 4 >= capacity * 3)
                return MapInstanceStatus.High;

            return population * 2 >= capacity ? MapInstanceStatus.Medium : MapInstanceStatus.Low;
        }

        private bool IsFull(MapChannel map, MapInstanceConfig policy) => PopulationOf(map) >= policy.Capacity;

        /// <summary>
        /// Opens another shared copy of a map. Null when the map is not loaded, runs in one copy
        /// only, or already has all the copies it may.
        /// </summary>
        public MapChannel OpenSharedCopy(uint mapContextId)
        {
            var policy = SharedPolicyOf(mapContextId);

            if (policy == null || !MapChannelArray.TryGetValue(mapContextId, out var template) || CopiesOf(mapContextId).Count >= policy.MaxCopies)
                return null;

            var map = _privateInstances.CreateShared(template, copy => InitializePrivateMapChannel(template, copy));

            if (map != null)
                Logger.WriteLog(LogType.Debug, $"Map {mapContextId}: shared copy opened, instance {map.InstanceId} ({CopiesOf(mapContextId).Count} of {policy.MaxCopies}).");

            return map;
        }

        /// <summary>Closes a shared copy that has nobody in it or on the way. False when it has, or is not a shared copy.</summary>
        public bool CloseSharedCopy(MapChannel map)
        {
            if (map == null || !map.IsSharedInstance || PopulationOf(map) > 0 || !_privateInstances.ReleaseShared(map))
                return false;

            CleanupPrivateMapChannel(map);
            Battlegrounds.Instance.Forget(map);

            Logger.WriteLog(LogType.Debug, $"Map {map.MapInfo.MapContextId}: shared copy closed, instance {map.InstanceId}.");

            return true;
        }

        /// <summary>Once a second, from the map channel worker: the shared copies that have stood empty long enough are closed.</summary>
        internal void SharedInstanceWorker()
        {
            var now = _clock();

            foreach (var map in _privateInstances.Snapshot().Where(map => map.IsSharedInstance).ToArray())
            {
                if (PopulationOf(map) > 0)
                {
                    map.EmptySince = 0;
                    continue;
                }

                if (map.EmptySince == 0)
                {
                    map.EmptySince = now;
                    continue;
                }

                var idleMs = Math.Max(0, InstancePolicy?.Invoke(map.MapInfo.MapContextId)?.IdleCloseSeconds ?? 0) * 1000L;

                if (now - map.EmptySince >= idleMs)
                    CloseSharedCopy(map);
            }
        }

        /// <summary>
        /// A player has walked into a door to a map (MapLinkManager). A map of one copy is entered
        /// as before. One that runs in several is entered directly while it has one copy, a new
        /// one being opened when that is full; with more than one the player is shown the picker.
        /// Returns whether it was dealt with: they are on their way, have been asked, or have
        /// been told there is no room.
        /// </summary>
        public bool EnterMap(Client client, uint mapContextId, Vector3 position, float rotation)
        {
            // An Operation: into the instance of their squad, by its door.
            if (IsSquadInstanceMap(mapContextId))
                return EnterSquadInstance(client, mapContextId, position, rotation, byDoor: true);

            var policy = SharedPolicyOf(mapContextId);

            if (policy == null)
                return ChangeMap(client, mapContextId, position, rotation);

            if (client?.Player == null || client.State != ClientState.Ingame || client.PendingTransfer != null)
                return false;

            var copies = CopiesOf(mapContextId);

            if (copies.Count == 0)
                return false;

            // A player who left a match that was being played goes back to the copy they left,
            // and to no other, until their time is up (Battlegrounds).
            var lockout = Battlegrounds.Instance.LockoutFor(client, mapContextId);

            if (lockout != null)
            {
                var left = copies.FirstOrDefault(copy => copy.InstanceId == lockout.InstanceId);

                client.PendingInstanceChoice = null;

                if (left != null && !IsFull(left, policy))
                    return Send(client, left, position, rotation);

                CommunicatorManager.Instance.SystemMessage(client,
                    $"You left a match in progress and that instance {(left == null ? "has closed" : "is full")}: no other is open to you for {Battlegrounds.Instance.TimeLeftOf(lockout)}.");
                return true;
            }

            if (copies.All(copy => IsFull(copy, policy)))
            {
                var opened = OpenSharedCopy(mapContextId);

                if (opened != null)
                    copies.Add(opened);
            }

            var open = copies.Where(copy => !IsFull(copy, policy))
                .OrderByDescending(PopulationOf)
                .ThenBy(copy => copy.InstanceId)
                .ToList();

            if (open.Count == 0)
            {
                client.PendingInstanceChoice = null;
                CommunicatorManager.Instance.SystemMessage(client, "Every instance of that map is full. Try again shortly.");
                return true;
            }

            var templateId = MapTemplates.Of(mapContextId);

            // One copy, or a map the client has no template to name the rows by: straight in.
            if (copies.Count == 1 || templateId == 0)
            {
                client.PendingInstanceChoice = null;
                return Send(client, open[0], position, rotation);
            }

            var rows = open.Concat(copies.Where(copy => !open.Contains(copy)))
                .Select(copy => new ChooseInstanceListPacket.Row
                {
                    Ordinal = copies.IndexOf(copy) + 1,
                    InstanceId = copy.InstanceId,
                    MapTemplateId = templateId,
                    StartGroup = 0,
                    Status = StatusOf(PopulationOf(copy), policy.Capacity)
                })
                .ToList();

            client.PendingInstanceChoice = new InstanceChoice
            {
                MapContextId = mapContextId,
                Position = position,
                Rotation = rotation,
                Origin = client.Player.MapChannel,
                OriginPosition = client.Player.Position,
                Offered = copies.Select(copy => copy.InstanceId).ToHashSet(),
                Deadline = _clock() + InstanceChoiceSeconds * 1000L
            };

            client.CallMethod(SysEntity.ClientMethodId, new ChooseInstanceListPacket(rows));

            return true;
        }

        /// <summary>
        /// SelectInstance: the copy picked in the instance picker. It must be one they were
        /// offered, by the picker they still have open, from where they were offered it.
        /// A copy that has filled or closed meanwhile is refused and the picker shown afresh.
        /// </summary>
        public void SelectInstance(Client client, SelectInstancePacket packet)
        {
            var choice = client?.PendingInstanceChoice;

            if (choice == null || packet == null || client.Player == null)
                return;

            client.PendingInstanceChoice = null;

            if (_clock() > choice.Deadline || client.State != ClientState.Ingame || !ReferenceEquals(client.Player.MapChannel, choice.Origin)
                || client.Player.State == CharacterState.Dead || client.Player.LogoutPending
                || Vector3.Distance(client.Player.Position, choice.OriginPosition) > InstanceChoiceReach)
                return;

            if (!choice.Offered.Contains(packet.InstanceId))
            {
                Logger.WriteLog(LogType.Security,
                    $"{client.Player.FamilyName} picked instance {packet.InstanceId} of map {choice.MapContextId}, which they were not offered.");
                return;
            }

            var policy = SharedPolicyOf(choice.MapContextId);
            var map = CopiesOf(choice.MapContextId).FirstOrDefault(copy => copy.InstanceId == packet.InstanceId);

            if (ReferenceEquals(map, client.Player.MapChannel))
            {
                CommunicatorManager.Instance.SystemMessage(client, "You are in that instance.");
                return;
            }

            if (map != null && Battlegrounds.Instance.BarredFrom(client, map, out var lockout))
            {
                CommunicatorManager.Instance.SystemMessage(client, Battlegrounds.Instance.LockoutText(lockout));
                return;
            }

            if (policy == null || map == null || IsFull(map, policy))
            {
                CommunicatorManager.Instance.SystemMessage(client, map == null ? "That instance has closed." : "That instance is full.");
                EnterMap(client, choice.MapContextId, choice.Position, choice.Rotation);
                return;
            }

            if (!Send(client, map, choice.Position, choice.Rotation))
                Logger.WriteLog(LogType.Error, $"{client.Player.FamilyName} could not be moved to instance {map.InstanceId} of map {choice.MapContextId}.");
        }

        /// <summary>SelectInstanceCancel: the instance picker closed without a pick.</summary>
        public void SelectInstanceCancel(Client client)
        {
            if (client != null)
                client.PendingInstanceChoice = null;
        }

        /// <summary>Sends a player into a copy, counted there from now.</summary>
        internal bool Send(Client client, MapChannel map, Vector3 position, float rotation)
        {
            if (!ChangeMap(client, map, position, rotation))
                return false;

            map.Arriving.Add(client);
            map.EmptySince = 0;

            if (map.IsSquadInstance)
                NoteVisitor(client.Player.Id, map);

            return true;
        }
    }
}
