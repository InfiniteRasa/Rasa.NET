using System.Numerics;

namespace Rasa.Structures
{
    internal sealed class PlayerTransfer
    {
        internal MapChannel OriginMap { get; init; }
        internal Vector3 OriginPosition { get; init; }
        internal double OriginRotation { get; init; }
        internal MapChannel DestinationMap { get; init; }
        internal Vector3 DestinationPosition { get; init; }
        internal double DestinationRotation { get; init; }
        internal long Deadline { get; init; }
        internal bool IsDropship { get; init; }
        internal bool IsMapLink { get; init; }
        internal bool HasDeparted { get; set; }
        internal ulong DropshipId { get; set; }
        internal uint ReleaseOwnedPrivateInstancesForCharacterId { get; init; }

        /// <summary>A secret passage's teleport (DynamicObjectManager.TakePassage).</summary>
        internal bool IsPassage { get; init; }

        /// <summary>
        /// The local teleporter a passage lands the player on (DynamicObjectManager
        /// .LocalTeleporterWorker): they are put on its list as they arrive, so it does not take
        /// them straight back.
        /// </summary>
        internal DynamicObject ArrivalPad { get; init; }

        /// <summary>
        /// Held in its teleport-out effect before it left (a passage, or a waypoint's teleport
        /// with DynamicObjectManager.WaypointHoldMs): the arrival is shown to everyone at the far
        /// end too, as the departure was to everyone where they stood.
        /// </summary>
        internal bool IsHeld { get; init; }

        /// <summary>
        /// When a teleport held for its effect leaves (DynamicObjectManager.PassagesWorker), on
        /// the manager's clock; 0 once it has left, or for one that left at once.
        /// </summary>
        internal long DepartAt { get; set; }
    }
}
