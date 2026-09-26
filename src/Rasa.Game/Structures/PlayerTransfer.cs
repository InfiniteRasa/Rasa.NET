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
    }
}
