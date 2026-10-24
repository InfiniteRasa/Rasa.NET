using System.Collections.Generic;

namespace Rasa.Managers
{
    using Structures;

    /// <summary>
    /// The copies of maps that apartments stand in (Managers.Apartments): one copy an apartment,
    /// opened as the first person goes in and closed by Apartments once it has stood empty.
    ///
    /// A copy is blank: none of the map's spawn pools, objects, doors, triggers or placed
    /// usables are put in it, unlike a private, shared or squad copy - the client draws the
    /// map's own scenery from its .map, and the server adds only the apartment's items. It is
    /// nobody's to the rest of the server (not IsPrivateInstance), so kills, loot and missions
    /// treat it as any public place; there is nothing in it to kill.
    /// </summary>
    public partial class MapChannelManager
    {
        /// <summary>
        /// The apartment's copy of a map: the one that stands, or a new one. Null when the map
        /// is not loaded.
        /// </summary>
        public MapChannel OpenApartmentCopy(uint mapContextId, uint apartmentId)
        {
            if (!MapChannelArray.TryGetValue(mapContextId, out var template))
                return null;

            var map = _privateInstances.CreateApartment(template, apartmentId);

            if (map != null && map.MapInfo.MapContextId != mapContextId)
            {
                // The apartment's copy stands on another map: its plot has changed. The caller
                // closes it first; this is not that copy.
                return null;
            }

            return map;
        }

        /// <summary>The apartment's copy that stands, or null.</summary>
        public MapChannel ApartmentCopyOf(uint apartmentId) => _privateInstances.FindApartment(apartmentId);

        /// <summary>Every apartment copy that stands, oldest first.</summary>
        public IReadOnlyList<MapChannel> ApartmentCopies() => _privateInstances.Apartments();

        /// <summary>
        /// Takes an apartment's copy apart: off the lists, its objects out of the world. False
        /// when somebody is in it or on the way, or it is not a listed apartment copy.
        /// </summary>
        public bool CloseApartmentCopy(MapChannel map)
        {
            if (map == null || !map.IsApartment || PopulationOf(map) > 0 || !_privateInstances.ReleaseApartment(map))
                return false;

            CleanupPrivateMapChannel(map);

            Logger.WriteLog(LogType.Debug, $"Map {map.MapInfo.MapContextId}: apartment {map.ApartmentId}'s copy closed, instance {map.InstanceId}.");

            return true;
        }
    }
}
