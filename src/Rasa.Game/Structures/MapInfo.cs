namespace Rasa.Structures
{
    using World;
    public class MapInfo
    {
        public uint MapContextId { get; set; }
        public string MapName { get; set; }
        public uint MapVersion { get; set; }
        public uint BaseRegionId { get; set; }

        public MapInfo(uint mapContextId, string mapName, uint mapVersion, uint baseRegionId)
        {
            MapContextId = mapContextId;
            MapName = mapName;
            MapVersion = mapVersion;
            BaseRegionId = baseRegionId;
        }

        public MapInfo(MapInfoEntry map)
        {
            MapContextId = map.Id;
            MapName = map.MapName;
            MapVersion = map.MapVersion;
            BaseRegionId = map.BaseRegion;
        }
    }
}
