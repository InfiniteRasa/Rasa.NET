using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures.World;

    /// <summary>
    /// The waypoint titles: "Wilderness Pathfinder", "Gained every AFS Waypoint on
    /// Wilderness.", and the twelve like it.
    ///
    /// A character has one when they have gained every waypoint of the battlefield, as
    /// <see cref="LogosTitles"/> gives its titles for the Logos held: no mission is asked.
    ///
    /// Which waypoints are a battlefield's is the world's teleporter table: the rows of type
    /// Waypoint (2) on the battlefield's own map. Not its dropship pad, its hospitals or its
    /// instances' waypoints - the Wilderness objective as the server's recovered definition of
    /// it has it (MissionDefinitionCatalog, mission 1449 objective 1) is exactly the seven such
    /// rows of map 1220. One of the seven, Imperial Valley (156), is a control point's, so a
    /// control point's waypoint counts with the rest: it is gained while the AFS holds the
    /// point.
    ///
    /// Two rows are not counted, Staging Point (107) and Viands Village (415) on Palisades: the
    /// client has a map marker for each and no waypoint name for either, and they show names
    /// borrowed from elsewhere in its table (ClientWaypointIds). The server does not hold the
    /// client's names, so they are left out by id (<see cref="Zone.NotCounted"/>). Every other
    /// waypoint row is under the id the client names it by.
    ///
    /// Divide and Marshes each have two titles for the same thing in the client, a Pathfinder
    /// (447, 491) and a Wanderer (462, "Gained all Divide waypoints.", and 493), and their
    /// Targets of Opportunity name the Wanderer ("Wanderer: Acquire all of the AFS Waypoints on
    /// Divide."). Both are given, by the same waypoints: a title is its own entry here, and two
    /// may be of one map. Mires, Plains, Incline and Howling Maw have none.
    ///
    /// Looked at when a waypoint is gained (DynamicObjectManager.ConvergeWaypointGrant) and when
    /// a character comes onto a map (ManifestationManager.AssignPlayer), which is where one who
    /// had them all before this was here, or was cloned from one who had, is given theirs.
    /// </summary>
    public static class WaypointTitles
    {
        /// <summary>One battlefield's title.</summary>
        public sealed class Zone
        {
            public uint TitleId { get; }
            public string Name { get; }
            public uint MapContextId { get; }

            /// <summary>Waypoint rows on the map that the title does not ask for: the client has no name of their own for them.</summary>
            public IReadOnlyList<uint> NotCounted { get; }

            internal Zone(uint titleId, string name, uint mapContextId, params uint[] notCounted)
            {
                TitleId = titleId;
                Name = name;
                MapContextId = mapContextId;
                NotCounted = notCounted;
            }
        }

        public static readonly IReadOnlyList<Zone> Zones = new[]
        {
            new Zone(362, "Wilderness", 1220),              // Wilderness Pathfinder
            new Zone(447, "Divide", 1148),                  // Divide Pathfinder
            new Zone(462, "Divide", 1148),                  // Divide Wanderer
            new Zone(499, "Palisades", 1244, 107, 415),     // Palisades Wanderer
            new Zone(402, "Plateau", 1497),                 // Plateau Pathfinder
            new Zone(469, "Pools", 1304),                   // Pools Wanderer
            new Zone(491, "Marshes", 1454),                 // Marshes Pathfinder
            new Zone(493, "Marshes", 1454),                 // Marshes Wanderer
            new Zone(517, "Descent", 2047),                 // Descent Pathfinder
            new Zone(426, "Ashen Desert", 1734),            // Desert Pathfinder
            new Zone(769, "Thunderhead", 1911),             // Thunderhead Cartographer
            new Zone(448, "Abyss", 2028),                   // Abyss Pathfinder
            new Zone(414, "Crucible", 1993)                 // Crucible Pathfinder
        };

        private static IReadOnlyDictionary<uint, HashSet<uint>> _waypoints = new Dictionary<uint, HashSet<uint>>();

        /// <summary>
        /// Works out each title's waypoints from the world's teleporters
        /// (DynamicObjectManager.InitTeleporters). A row counts whether or not its map is
        /// loaded: a battlefield the server has not got is one whose title cannot be had, not
        /// one that asks for nothing.
        /// </summary>
        public static void Load(IEnumerable<TeleporterEntry> teleporters)
        {
            var byMap = (teleporters ?? Enumerable.Empty<TeleporterEntry>())
                .Where(teleporter => teleporter.Type == (byte)WaypointType.Waypoint && teleporter.Id != 0)
                .ToLookup(teleporter => teleporter.MapContextId, teleporter => teleporter.Id);

            _waypoints = Zones.ToDictionary(
                zone => zone.TitleId,
                zone => byMap[zone.MapContextId].Where(id => !zone.NotCounted.Contains(id)).ToHashSet());
        }

        /// <summary>The waypoints a title asks for; none for a title that is not one of these, or before <see cref="Load"/>.</summary>
        public static IReadOnlyCollection<uint> WaypointsOf(uint titleId) =>
            _waypoints.TryGetValue(titleId, out var waypoints) ? waypoints : (IReadOnlyCollection<uint>)System.Array.Empty<uint>();

        /// <summary>The character has gained a waypoint: the title it is part of, if it was the last they lacked.</summary>
        public static void Gained(Client client, uint waypointId) =>
            Give(client, zone => _waypoints.TryGetValue(zone.TitleId, out var waypoints) && waypoints.Contains(waypointId));

        /// <summary>The character has come onto a map: every title their waypoints have earned and they have not got.</summary>
        public static void CatchUp(Client client) => Give(client, _ => true);

        private static void Give(Client client, System.Func<Zone, bool> concerns)
        {
            var player = client?.Player;

            if (player == null)
                return;

            lock (client.SyncRoot)
            {
                HashSet<uint> held = null;

                foreach (var zone in Zones)
                {
                    if (!concerns(zone) || !_waypoints.TryGetValue(zone.TitleId, out var waypoints) || waypoints.Count == 0)
                        continue;

                    lock (player.Titles)
                        if (player.Titles.Contains(zone.TitleId))
                            continue;

                    if (held == null)
                        lock (player.GainedWaypoints)
                            held = player.GainedWaypoints.Select(waypoint => waypoint.WaypointId).ToHashSet();

                    if (waypoints.IsSubsetOf(held))
                        ManifestationManager.Instance.GrantTitle(client, zone.TitleId);
                }
            }
        }
    }
}
