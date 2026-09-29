using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Models;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The client's GM map pickers, /gotomap, /gotostartgroup and /killmap. None is a local slash
    /// command, so both reach the server as PrivilegedCommand (ChatCommandsManager):
    ///
    ///  - /gotomap: no arg is a request for the map list, answered with GmGotoMapAck; the client
    ///    shows it in DebugMapSelectWindow (inputstate/gotomap.py) with a start group box that
    ///    defaults to "default", and sends the pick back as ('gotomap', '&lt;mapId&gt; &lt;startGroup&gt;').
    ///    Typed by hand, the map can also be a name, or a unique part of one.
    ///  - /gotostartgroup: no arg is a request for the start groups on the GM's map, answered
    ///    with GmGotoStartGroupAck; the waypoint window lists them and sends the pick back as
    ///    ('gotostartgroup', name).
    ///  - /killmap: no arg is a request for the map list, answered with GmKillMapAck and shown in
    ///    the same picker (inputstate/killmap.py), which sends the pick back as ('killmap',
    ///    '&lt;mapId&gt;'); the map is reset (MapReset).
    ///
    /// The maps' own start groups were authored in the map files and are not in anything we have,
    /// so a map's start groups are the places the server already knows a player arrives at on it,
    /// in this order: its waypoints ("Waypoint &lt;id&gt;"), its hospitals ("Hospital &lt;id&gt;"), each
    /// 1 m above the pad as SelectWaypoint puts a player, and the arrival of every enabled map
    /// link into it ("Entrance &lt;link id&gt;"). "default" (or nothing) is the first of them. A name
    /// matches whole and without regard to case, or a bare number matches the waypoint, hospital
    /// or link of that id.
    /// </summary>
    public static class GmMapCommands
    {
        public const string DefaultStartGroup = "default";

        /// <summary>The map every character passes through on login; nobody is sent to it.</summary>
        public const string CharacterSelectionMap = "characterselection";

        /// <summary>How far above a waypoint or hospital pad a player lands (as SelectWaypoint does).</summary>
        public const float PadHeight = 1.0f;

        public sealed class StartGroup
        {
            public string Name { get; set; }
            public uint Id { get; set; }
            public Vector3 Position { get; set; }
            public float Rotation { get; set; }
        }

        #region Lists

        /// <summary>The maps a GM can go to: every loaded one but character selection, by context id.</summary>
        public static List<MapChannel> Maps(IEnumerable<MapChannel> loaded)
        {
            return loaded
                .Where(m => m?.MapInfo != null && !string.Equals(m.MapInfo.MapName, CharacterSelectionMap, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.MapInfo.MapContextId)
                .ToList();
        }

        /// <summary>
        /// The maps a token names: the one with that context id, the one with that whole name,
        /// or else every map whose name has it in it.
        /// </summary>
        public static List<MapChannel> MapsMatching(IEnumerable<MapChannel> maps, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return new List<MapChannel>();

            var list = maps.ToList();

            if (uint.TryParse(token, out var contextId))
                return list.Where(m => m.MapInfo.MapContextId == contextId).ToList();

            var exact = list.Where(m => string.Equals(m.MapInfo.MapName, token, StringComparison.OrdinalIgnoreCase)).ToList();

            if (exact.Count > 0)
                return exact;

            return list.Where(m => m.MapInfo.MapName != null && m.MapInfo.MapName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        /// <summary>The one map a token names, or null for none or more than one.</summary>
        public static MapChannel FindMap(IEnumerable<MapChannel> maps, string token)
        {
            var matching = MapsMatching(maps, token);

            return matching.Count == 1 ? matching[0] : null;
        }

        /// <summary>The start groups on a map, in the order the class summary gives.</summary>
        public static List<StartGroup> StartGroupsOf(uint mapContextId, IEnumerable<DynamicObject> teleporters, IEnumerable<MapLink> links)
        {
            var pads = (teleporters ?? Enumerable.Empty<DynamicObject>())
                .Where(t => t != null && t.MapContextId == mapContextId && t.ObjectData is WaypointInfo)
                .Select(t => (Object: t, Info: (WaypointInfo)t.ObjectData))
                .ToList();

            var groups = new List<StartGroup>();

            foreach (var type in new[] { WaypointType.Waypoint, WaypointType.Hospital })
                foreach (var (pad, info) in pads.Where(p => p.Info.WaypointType == type).OrderBy(p => p.Info.WaypointId))
                    groups.Add(new StartGroup
                    {
                        Name = $"{type} {info.WaypointId}",
                        Id = info.WaypointId,
                        Position = pad.Position + new Vector3(0f, PadHeight, 0f),
                        Rotation = (float)pad.Rotation
                    });

            foreach (var link in (links ?? Enumerable.Empty<MapLink>())
                .Where(l => l != null && l.Enabled && l.DestMapContextId == mapContextId)
                .OrderBy(l => l.Id))
                groups.Add(new StartGroup
                {
                    Name = $"Entrance {link.Id}",
                    Id = link.Id,
                    Position = link.DestPosition,
                    Rotation = link.DestRotation
                });

            return groups;
        }

        /// <summary>The start group asked for, or null when none matches.</summary>
        public static StartGroup FindStartGroup(List<StartGroup> groups, string name)
        {
            if (groups == null || groups.Count == 0)
                return null;

            name = name?.Trim();

            if (string.IsNullOrEmpty(name) || string.Equals(name, DefaultStartGroup, StringComparison.OrdinalIgnoreCase))
                return groups[0];

            var named = groups.FirstOrDefault(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));

            if (named != null)
                return named;

            return uint.TryParse(name, out var id) ? groups.FirstOrDefault(g => g.Id == id) : null;
        }

        /// <summary>'&lt;map&gt; [startGroup]': the map and the start group, which can have spaces in it.</summary>
        public static (string Map, string StartGroup) SplitGotoMapArgs(string args)
        {
            var parts = (args ?? "").Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

            return parts.Length switch
            {
                0 => (null, null),
                1 => (parts[0], null),
                _ => (parts[0], parts[1].Trim())
            };
        }

        #endregion

        #region Commands

        /// <summary>PrivilegedCommand gotomap.</summary>
        public static void GotoMap(Client client, string args)
        {
            var maps = Maps(MapChannelManager.Instance.MapChannelArray.Values);
            var (mapToken, groupName) = SplitGotoMapArgs(args);

            if (mapToken == null)
            {
                client.CallMethod(SysEntity.ClientMethodId, new GmGotoMapAckPacket(maps.Select(m => m.MapInfo.MapContextId).ToList()));
                return;
            }

            var map = OneMap(client, maps, mapToken, "/gotomap");

            if (map == null)
                return;

            var groups = StartGroupsOf(map.MapInfo.MapContextId, map.Teleporters.Values, MapLinkManager.Instance.Links);
            var group = FindStartGroup(groups, groupName);

            if (group == null)
            {
                Say(client, groups.Count == 0
                    ? $"{map.MapInfo.MapName} ({map.MapInfo.MapContextId}) has no waypoint, hospital or entrance to arrive at; .teleport x y z {map.MapInfo.MapContextId} goes anywhere on it."
                    : $"{map.MapInfo.MapName} has no start group '{groupName}'. It has: {string.Join(", ", groups.Select(g => g.Name))}.");
                return;
            }

            if (map == client.Player.MapChannel)
            {
                MoveWithinMap(client, group);
                return;
            }

            if (!MapChannelManager.Instance.ChangeMap(client, map.MapInfo.MapContextId, group.Position, group.Rotation))
                Say(client, "You cannot change maps right now.");
        }

        /// <summary>
        /// PrivilegedCommand killmap: no arg is a request for the map list (GmKillMapAck, shown in
        /// the same picker by inputstate/killmap.py), '&lt;map&gt;' the pick or one typed by hand -
        /// the map reset (MapReset) on its next tick.
        /// </summary>
        public static void KillMap(Client client, string args)
        {
            var maps = Maps(MapChannelManager.Instance.MapChannelArray.Values);
            var (mapToken, _) = SplitGotoMapArgs(args);

            if (mapToken == null)
            {
                client.CallMethod(SysEntity.ClientMethodId, new GmKillMapAckPacket(maps.Select(m => m.MapInfo.MapContextId).ToList()));
                return;
            }

            var map = OneMap(client, maps, mapToken, "/killmap");

            if (map == null)
                return;

            if (MapReset.IsPending(map))
            {
                Say(client, $"{map.MapInfo.MapName} is already being reset.");
                return;
            }

            MapReset.Request(map, client);
            Say(client, $"Resetting {map.MapInfo.MapName} ({map.MapInfo.MapContextId}): its spawn pools' creatures go and the pools start over.");
        }

        /// <summary>PrivilegedCommand gotostartgroup.</summary>
        public static void GotoStartGroup(Client client, string args)
        {
            var map = client.Player.MapChannel;

            if (map?.MapInfo == null)
                return;

            var groups = StartGroupsOf(map.MapInfo.MapContextId, map.Teleporters.Values, MapLinkManager.Instance.Links);

            if (string.IsNullOrWhiteSpace(args))
            {
                client.CallMethod(SysEntity.ClientMethodId, new GmGotoStartGroupAckPacket(groups.Select(g => (g.Name, g.Position)).ToList()));
                return;
            }

            var group = FindStartGroup(groups, args);

            if (group == null)
            {
                Say(client, $"No start group '{args.Trim()}' on {map.MapInfo.MapName}. /gotostartgroup with nothing after it lists them.");
                return;
            }

            MoveWithinMap(client, group);
        }

        /// <summary>The one map the token names, or null after saying there is none or which it could be.</summary>
        private static MapChannel OneMap(Client client, List<MapChannel> maps, string mapToken, string command)
        {
            var matching = MapsMatching(maps, mapToken);

            if (matching.Count == 1)
                return matching[0];

            Say(client, matching.Count == 0
                ? $"No map '{mapToken}'. {command} with nothing after it lists them."
                : $"'{mapToken}' is in more than one map's name: {string.Join(", ", matching.Take(8).Select(m => $"{m.MapInfo.MapName} ({m.MapInfo.MapContextId})"))}{(matching.Count > 8 ? ", ..." : "")}.");

            return null;
        }

        /// <summary>Onto the start group on the map the GM is on, as .tele moves them.</summary>
        private static void MoveWithinMap(Client client, StartGroup group)
        {
            client.Player.PlaceAt(group.Position);
            client.Player.Rotation = group.Rotation;
            client.MoveObject(client.Player.EntityId, new Movement(group.Position, new Vector2(group.Rotation, 0f)));
        }

        private static void Say(Client client, string text) => CommunicatorManager.Instance.SystemMessage(client, text);

        #endregion
    }
}
