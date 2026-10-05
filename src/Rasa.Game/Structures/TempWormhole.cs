using System.Numerics;

namespace Rasa.Structures
{
    /// <summary>
    /// A row of EnteredWaypoint's tempWormholes: a Personal Waypoint the player may return to
    /// (PersonalWaypoints). The window lists it as "Temp Wormhole:" and its owner's name, puts
    /// it on the map at its position, and sends its id back with ReturnToWormhole.
    /// </summary>
    public readonly struct TempWormhole
    {
        public ulong Id { get; }
        public Vector3 Position { get; }
        public string OwnerName { get; }

        public TempWormhole(ulong id, Vector3 position, string ownerName)
        {
            Id = id;
            Position = position;
            OwnerName = ownerName;
        }
    }
}
