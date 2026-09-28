namespace Rasa.Structures
{
    internal readonly struct PrivateMapInstanceKey
    {
        internal uint MapContextId { get; }
        internal uint InstanceId { get; }

        internal PrivateMapInstanceKey(uint mapContextId, uint instanceId)
        {
            MapContextId = mapContextId;
            InstanceId = instanceId;
        }
    }
}
