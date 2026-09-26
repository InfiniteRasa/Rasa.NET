using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Structures;

    public sealed class PrivateMapInstanceService
    {
        private static PrivateMapInstanceService _instance;
        private static readonly object InstanceLock = new object();
        private readonly object _syncRoot = new object();
        private readonly Dictionary<PrivateMapInstanceKey, MapChannel> _instancesByKey = new();
        private readonly Dictionary<(uint ContextId, uint OwnerCharacterId), PrivateMapInstanceKey> _keysByOwner = new();
        private uint _nextInstanceId = 2;

        internal static PrivateMapInstanceService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new PrivateMapInstanceService();
                    }
                }

                return _instance;
            }
        }

        internal PrivateMapInstanceService()
        {
        }

        internal MapChannel GetOrCreate(MapChannel template, uint ownerCharacterId, System.Action<MapChannel> initialize = null)
        {
            if (template == null || ownerCharacterId == 0)
                return null;

            lock (_syncRoot)
            {
                var ownerKey = (template.MapInfo.MapContextId, ownerCharacterId);
                if (_keysByOwner.TryGetValue(ownerKey, out var existingKey) &&
                    _instancesByKey.TryGetValue(existingKey, out var existing))
                    return existing;

                var instanceId = _nextInstanceId++;
                var privateKey = new PrivateMapInstanceKey(template.MapInfo.MapContextId, instanceId);
                var map = new MapChannel
                {
                    MapInfo = template.MapInfo,
                    InstanceId = instanceId,
                    PlayerLimit = template.PlayerLimit,
                    ClientList = new List<Game.Client>(),
                    NavMesh = template.NavMesh,
                    IsPrivateInstance = true,
                    OwnerCharacterId = ownerCharacterId
                };
                initialize?.Invoke(map);

                _instancesByKey.Add(privateKey, map);
                _keysByOwner.Add(ownerKey, privateKey);
                return map;
            }
        }

        internal MapChannel FindByContextAndInstance(uint mapContextId, uint instanceId)
        {
            lock (_syncRoot)
                return _instancesByKey.TryGetValue(
                    new PrivateMapInstanceKey(mapContextId, instanceId), out var map) ? map : null;
        }

        internal MapChannel FindOwnedInstance(uint mapContextId, uint ownerCharacterId)
        {
            lock (_syncRoot)
                return _keysByOwner.TryGetValue((mapContextId, ownerCharacterId), out var key) &&
                    _instancesByKey.TryGetValue(key, out var map)
                    ? map
                    : null;
        }

        internal IReadOnlyList<MapChannel> Snapshot()
        {
            lock (_syncRoot)
                return _instancesByKey.Values.ToArray();
        }

        internal IReadOnlyList<MapChannel> ReleaseOwned(uint ownerCharacterId)
        {
            lock (_syncRoot)
            {
                var owned = _keysByOwner
                    .Where(entry => entry.Key.OwnerCharacterId == ownerCharacterId)
                    .Select(entry => entry.Value)
                    .Distinct()
                    .ToArray();

                var released = new List<MapChannel>(owned.Length);
                foreach (var key in owned)
                    if (_instancesByKey.Remove(key, out var map))
                        released.Add(map);

                foreach (var ownerKey in _keysByOwner.Keys
                             .Where(entry => entry.OwnerCharacterId == ownerCharacterId)
                             .ToArray())
                    _keysByOwner.Remove(ownerKey);

                return released;
            }
        }
    }
}
