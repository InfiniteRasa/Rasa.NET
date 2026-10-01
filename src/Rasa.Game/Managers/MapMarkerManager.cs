using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.World;

    /// <summary>
    /// The status a map-screen marker carries: whether the player has found that waypoint or
    /// hospital, and whether a crafting station is working.
    ///
    /// The map window and the minimap both draw a status dot on these markers and put a line in
    /// the tooltip - *Acquired* / *Not Acquired*, *Controlled By: AFS* - and neither has ever
    /// shown anything, because nothing sent the state. <c>GetMarkerState</c> answers None for
    /// every marker, and the dot is hidden on None.
    ///
    /// What makes it worth sending is that one field of it is per-character.
    /// <c>DynamicObjectManager.CheckPlayerWaypoint</c> writes a character_teleporter row the first
    /// time a player walks into a waypoint *or a hospital*, and until now the only way to see
    /// which you had was to open the travel window and read the list. On the map it is a colour.
    ///
    /// The markers are keyed by the client's own entity ids, which are compiled into its
    /// uimapmarker table and are not ids this server ever mints; map_marker is what ties each one
    /// to the object here whose state it shows. See MapMarkerEntry for where that came from.
    /// </summary>
    public class MapMarkerManager
    {
        private static MapMarkerManager _instance;
        private static readonly object InstanceLock = new object();

        /// <summary>Markers by the map they are on, which is how they are always read.</summary>
        private readonly Dictionary<uint, List<MapMarkerEntry>> _byMap = new Dictionary<uint, List<MapMarkerEntry>>();

        /// <summary>Teleporter row id to the marker that stands for it, for the discovery push.</summary>
        private readonly Dictionary<uint, MapMarkerEntry> _byTeleporter = new Dictionary<uint, MapMarkerEntry>();

        public static MapMarkerManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new MapMarkerManager();
                    }
                }

                return _instance;
            }
        }

        private MapMarkerManager()
        {
        }

        public void MapMarkerInit()
        {
            _byMap.Clear();
            _byTeleporter.Clear();

            using var unitOfWork = Server.GameUnitOfWorkFactory.CreateWorld();

            foreach (var marker in unitOfWork.MapMarkers.GetMapMarkers())
            {
                if (!_byMap.TryGetValue(marker.MapContextId, out var list))
                    _byMap[marker.MapContextId] = list = new List<MapMarkerEntry>();

                list.Add(marker);

                if (marker.Source == MapMarkerSource.Teleporter)
                    _byTeleporter[marker.ObjectId] = marker;
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded {_byMap.Values.Sum(l => l.Count)} map markers on {_byMap.Count} maps.");
        }

        /// <summary>
        /// Everything the player should see on the map they have just arrived on. Sent on every
        /// arrival, not once per session: the client empties its dictionary on each map change.
        /// </summary>
        public void PlayerEnteredMap(Client client)
        {
            if (client?.Player?.MapChannel == null)
                return;

            var mapContextId = client.Player.MapChannel.MapInfo.MapContextId;

            if (!_byMap.TryGetValue(mapContextId, out var markers))
                return;

            var found = new HashSet<uint>(client.Player.GainedWaypoints.Select(w => w.WaypointId));
            var state = new Dictionary<ulong, MapMarkerState>();

            foreach (var marker in markers)
            {
                var value = StateOf(marker, found);

                if (value != null)
                    state[marker.MarkerEntityId] = value;
            }

            if (state.Count > 0)
                client.CallMethod(SysEntity.ClientMapStateId, new MapMarkerInfoPacket(state));
        }

        /// <summary>Whether the map screen marks this teleporter a safe zone (Map_SafeZone): a hospital everyone may go back to (Hospitals).</summary>
        public bool IsSafeZone(uint teleporterId)
        {
            return _byTeleporter.TryGetValue(teleporterId, out var marker) && marker.MarkerType == MapMarkerType.SafeZone;
        }

        /// <summary>
        /// A waypoint or hospital has just been found. The client is already being told it gained
        /// the waypoint; this is the same news for the map, and it refreshes that one marker
        /// rather than the whole map.
        /// </summary>
        public void WaypointDiscovered(Client client, uint waypointId)
        {
            if (client?.Player?.MapChannel == null)
                return;

            if (!_byTeleporter.TryGetValue(waypointId, out var marker))
                return;

            // Only if it is a marker on the map they are looking at. The client keeps one map's
            // worth of state and throws it away on the way out, so an id from anywhere else would
            // sit in the dictionary unread until the next map change dropped it.
            if (marker.MapContextId != client.Player.MapChannel.MapInfo.MapContextId)
                return;

            var state = StateOf(marker, new HashSet<uint> { waypointId });

            if (state != null)
                client.CallMethod(SysEntity.ClientMapStateId, new UpdateMapMarkerPacket(marker.MarkerEntityId, state));
        }

        /// <summary>
        /// The state for one marker, or null for a kind this server has nothing to say about yet.
        /// Null means the marker is left out, and a marker with no state is what every one of them
        /// gets today - so the ones that are not covered look exactly as they always have.
        /// </summary>
        private static MapMarkerState StateOf(MapMarkerEntry marker, HashSet<uint> foundWaypoints)
        {
            switch (marker.MarkerType)
            {
                case MapMarkerType.WaypointTeleporter:
                case MapMarkerType.Hospital:
                case MapMarkerType.SafeZone:
                    // isFriendly and isSafe are constants until there is a faction that can hold
                    // ground and a PvP rule that says where it is not safe. They are sent as what
                    // is true on this server rather than left out, because the client draws the
                    // dot from isFriendly and the tooltip line from all three.
                    return MapMarkerState.Teleporter(marker.MarkerType, foundWaypoints.Contains(marker.ObjectId));

                case MapMarkerType.CraftingStation:
                    // Every station in the table is one the crafting handlers will serve.
                    return MapMarkerState.CraftingStation(true);

                default:
                    return null;
            }
        }
    }
}
