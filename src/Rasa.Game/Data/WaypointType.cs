namespace Rasa.Data
{
    /// <summary>
    /// The teleporter table's type column. These are the server's own categories and are not
    /// the numbers the client uses - see <see cref="ClientWaypointType"/> and
    /// <see cref="WaypointTypeExtensions.ToClient"/> for what goes on the wire.
    /// </summary>
    public enum WaypointType
    {
        /// <summary>
        /// A shortcut or tower teleporter inside a base: never gained and in no window. It takes
        /// whoever steps onto it to the local teleporter its local_teleporter_link row names.
        /// </summary>
        LocalTeleporter = 1,
        /// <summary>A waypoint of a map's own network, gained by walking over it.</summary>
        Waypoint = 2,
        Wormhole = 3,
        /// <summary>A dropship transport pad: the map-to-map network, gained by walking into the beam.</summary>
        Dropship = 4,
        Hospital = 5
    }

    /// <summary>
    /// The client's generated.client.constant.waypointtype, the only three values its
    /// Recv_WaypointGained and waypoint window know. LOCALWAYPOINT is a waypoint of the map you
    /// are on ("You just gained local waypoint ..."), MAPWAYPOINT is the dropship network ("You
    /// just gained dropship waypoint ..."), WORMHOLE is itself. The server used to send its own
    /// table type instead, so a map waypoint (2) announced itself as a dropship waypoint and a
    /// dropship pad (4) announced nothing at all.
    /// </summary>
    public enum ClientWaypointType
    {
        LocalWaypoint = 1,
        MapWaypoint = 2,
        Wormhole = 3
    }

    public static class WaypointTypeExtensions
    {
        public static ClientWaypointType ToClient(this WaypointType type)
        {
            switch (type)
            {
                case WaypointType.Wormhole:
                    return ClientWaypointType.Wormhole;
                case WaypointType.Dropship:
                    return ClientWaypointType.MapWaypoint;
                default:
                    return ClientWaypointType.LocalWaypoint;
            }
        }
    }
}
