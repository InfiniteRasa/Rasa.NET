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
        private readonly Dictionary<(uint ContextId, uint OwnerCharacterId), MapChannel> _squadByOwner = new();
        private readonly Dictionary<uint, MapChannel> _apartments = new();
        private uint _nextInstanceId = 2;

        /// <summary>
        /// Moved on by every change to the list of instances, so that a caller that keeps a copy
        /// of it (MapChannelManager.MapsToTick) knows when its copy is out of date.
        /// </summary>
        private long _version;

        /// <summary>The list's version: a different number from the last one read means the list has changed since.</summary>
        internal long Version
        {
            get
            {
                lock (_syncRoot)
                    return _version;
            }
        }

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
                    IsPrivateInstance = true,
                    OwnerCharacterId = ownerCharacterId
                };
                map.ShareNavigationOf(template, heights: false);
                initialize?.Invoke(map);

                _instancesByKey.Add(privateKey, map);
                _version++;
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

        /// <summary>
        /// A shared copy of a map: nobody's, found by its instance id like a private one, and
        /// ticked and cleaned up with them. The ids come from the same counter, so a map's
        /// private and shared copies never share one.
        /// </summary>
        internal MapChannel CreateShared(MapChannel template, System.Action<MapChannel> initialize = null)
        {
            if (template == null)
                return null;

            lock (_syncRoot)
            {
                var instanceId = _nextInstanceId++;
                var map = new MapChannel
                {
                    MapInfo = template.MapInfo,
                    InstanceId = instanceId,
                    PlayerLimit = template.PlayerLimit,
                    ClientList = new List<Game.Client>(),
                    IsSharedInstance = true
                };
                map.ShareNavigationOf(template, heights: true);
                initialize?.Invoke(map);

                _instancesByKey.Add(new PrivateMapInstanceKey(template.MapInfo.MapContextId, instanceId), map);
                _version++;
                return map;
            }
        }

        /// <summary>
        /// A squad's instance of a map: its owner's and their squad's, found by its instance id
        /// as the others are, and ticked and cleaned up with them. One per owner per map.
        /// </summary>
        internal MapChannel CreateSquad(MapChannel template, uint ownerCharacterId, System.Action<MapChannel> initialize = null)
        {
            if (template == null || ownerCharacterId == 0)
                return null;

            lock (_syncRoot)
            {
                var ownerKey = (template.MapInfo.MapContextId, ownerCharacterId);

                if (_squadByOwner.TryGetValue(ownerKey, out var existing))
                    return existing;

                var instanceId = _nextInstanceId++;
                var map = new MapChannel
                {
                    MapInfo = template.MapInfo,
                    InstanceId = instanceId,
                    PlayerLimit = template.PlayerLimit,
                    ClientList = new List<Game.Client>(),
                    IsSquadInstance = true,
                    SquadOwnerCharacterId = ownerCharacterId
                };
                map.ShareNavigationOf(template, heights: true);
                initialize?.Invoke(map);

                _instancesByKey.Add(new PrivateMapInstanceKey(template.MapInfo.MapContextId, instanceId), map);
                _version++;
                _squadByOwner.Add(ownerKey, map);
                return map;
            }
        }

        /// <summary>The squad instance of a map that is this character's, or null.</summary>
        internal MapChannel FindSquad(uint mapContextId, uint ownerCharacterId)
        {
            lock (_syncRoot)
                return _squadByOwner.TryGetValue((mapContextId, ownerCharacterId), out var map) ? map : null;
        }

        /// <summary>The squad instances of a map, oldest first; of every map with no map given.</summary>
        internal IReadOnlyList<MapChannel> SquadOf(uint? mapContextId = null)
        {
            lock (_syncRoot)
                return _squadByOwner.Values
                    .Where(map => mapContextId == null || map.MapInfo.MapContextId == mapContextId)
                    .OrderBy(map => map.InstanceId)
                    .ToArray();
        }

        /// <summary>
        /// Makes a squad instance another character's: the lead of its squad has changed hands.
        /// False when it is not a listed squad instance, or the new owner has one of that map.
        /// </summary>
        internal bool ReassignSquad(MapChannel map, uint ownerCharacterId)
        {
            if (map == null || !map.IsSquadInstance || ownerCharacterId == 0)
                return false;

            lock (_syncRoot)
            {
                var mapContextId = map.MapInfo.MapContextId;
                var oldKey = (mapContextId, map.SquadOwnerCharacterId);
                var newKey = (mapContextId, ownerCharacterId);

                if (!_squadByOwner.TryGetValue(oldKey, out var listed) || !ReferenceEquals(listed, map))
                    return false;

                if (map.SquadOwnerCharacterId == ownerCharacterId)
                    return true;

                if (_squadByOwner.ContainsKey(newKey))
                    return false;

                _squadByOwner.Remove(oldKey);
                _squadByOwner.Add(newKey, map);
                map.SquadOwnerCharacterId = ownerCharacterId;
                return true;
            }
        }

        /// <summary>Takes a squad instance off the lists. False when it is not one, or not on them.</summary>
        internal bool ReleaseSquad(MapChannel map)
        {
            if (map == null || !map.IsSquadInstance)
                return false;

            lock (_syncRoot)
            {
                var ownerKey = (map.MapInfo.MapContextId, map.SquadOwnerCharacterId);

                if (!_squadByOwner.TryGetValue(ownerKey, out var listed) || !ReferenceEquals(listed, map))
                    return false;

                _squadByOwner.Remove(ownerKey);
                _instancesByKey.Remove(new PrivateMapInstanceKey(map.MapInfo.MapContextId, map.InstanceId));
                _version++;
                return true;
            }
        }

        /// <summary>
        /// An apartment's copy of a map (Managers.Apartments): found by its instance id as the
        /// others are, ticked and cleaned up with them. One per apartment, whoever goes in.
        /// </summary>
        internal MapChannel CreateApartment(MapChannel template, uint apartmentId, System.Action<MapChannel> initialize = null)
        {
            if (template == null || apartmentId == 0)
                return null;

            lock (_syncRoot)
            {
                if (_apartments.TryGetValue(apartmentId, out var existing))
                    return existing;

                var instanceId = _nextInstanceId++;
                var map = new MapChannel
                {
                    MapInfo = template.MapInfo,
                    InstanceId = instanceId,
                    PlayerLimit = template.PlayerLimit,
                    ClientList = new List<Game.Client>(),
                    NavMesh = template.NavMesh,
                    Cover = template.Cover,
                    SafetyFloorY = template.SafetyFloorY,
                    TopWalkableY = template.TopWalkableY,
                    ApartmentId = apartmentId
                };
                initialize?.Invoke(map);

                _instancesByKey.Add(new PrivateMapInstanceKey(template.MapInfo.MapContextId, instanceId), map);
                _apartments.Add(apartmentId, map);
                return map;
            }
        }

        /// <summary>The copy of an apartment that stands, or null.</summary>
        internal MapChannel FindApartment(uint apartmentId)
        {
            lock (_syncRoot)
                return _apartments.TryGetValue(apartmentId, out var map) ? map : null;
        }

        /// <summary>The apartment copies that stand, oldest first.</summary>
        internal IReadOnlyList<MapChannel> Apartments()
        {
            lock (_syncRoot)
                return _apartments.Values.OrderBy(map => map.InstanceId).ToArray();
        }

        /// <summary>Takes an apartment's copy off the lists. False when it is not one, or not on them.</summary>
        internal bool ReleaseApartment(MapChannel map)
        {
            if (map == null || !map.IsApartment)
                return false;

            lock (_syncRoot)
            {
                if (!_apartments.TryGetValue(map.ApartmentId, out var listed) || !ReferenceEquals(listed, map))
                    return false;

                _apartments.Remove(map.ApartmentId);
                _instancesByKey.Remove(new PrivateMapInstanceKey(map.MapInfo.MapContextId, map.InstanceId));
                return true;
            }
        }

        /// <summary>The shared copies of a map, oldest first.</summary>
        internal IReadOnlyList<MapChannel> SharedOf(uint mapContextId)
        {
            lock (_syncRoot)
                return _instancesByKey.Values
                    .Where(map => map.IsSharedInstance && map.MapInfo.MapContextId == mapContextId)
                    .OrderBy(map => map.InstanceId)
                    .ToArray();
        }

        /// <summary>Takes a shared copy off the list. False when it is not one, or not on it.</summary>
        internal bool ReleaseShared(MapChannel map)
        {
            if (map == null || !map.IsSharedInstance)
                return false;

            lock (_syncRoot)
            {
                var key = new PrivateMapInstanceKey(map.MapInfo.MapContextId, map.InstanceId);

                if (!_instancesByKey.TryGetValue(key, out var listed) || !ReferenceEquals(listed, map) || !_instancesByKey.Remove(key))
                    return false;

                _version++;
                return true;
            }
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
                    {
                        released.Add(map);
                        _version++;
                    }

                foreach (var ownerKey in _keysByOwner.Keys
                             .Where(entry => entry.OwnerCharacterId == ownerCharacterId)
                             .ToArray())
                    _keysByOwner.Remove(ownerKey);

                return released;
            }
        }
    }
}
