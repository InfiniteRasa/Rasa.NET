using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    /// <summary>
    /// An instance picker a client has open (MapChannelManager.EnterMap): where the door they
    /// walked into leads, which copies they were offered, and until when their answer counts.
    /// </summary>
    internal sealed class InstanceChoice
    {
        internal uint MapContextId { get; init; }
        internal Vector3 Position { get; init; }
        internal float Rotation { get; init; }

        /// <summary>The channel they stood on when it was offered: a choice made from anywhere else is void.</summary>
        internal MapChannel Origin { get; init; }

        /// <summary>Where they stood then: the door. A choice made from far off it is void.</summary>
        internal Vector3 OriginPosition { get; init; }

        /// <summary>The instance ids listed to them.</summary>
        internal HashSet<uint> Offered { get; init; } = new HashSet<uint>();

        internal long Deadline { get; init; }
    }
}
