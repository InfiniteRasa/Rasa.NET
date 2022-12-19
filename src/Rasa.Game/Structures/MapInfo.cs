namespace Rasa.Structures
{
    public class MapInfo
    {
        public uint MapContextId { get; set; }
        public string MapName { get; set; }
        public uint MapVersion { get; set; }
        public int BaseRegionId { get; set; }

        public MapInfo(uint mapContextId, string mapName, uint mapVersion, int baseRegionId)
        {
            MapContextId = mapContextId;
            MapName = mapName;
            MapVersion = mapVersion;
            BaseRegionId = baseRegionId;
        }

        public MapInfo(MapInfoEntry map)
        {
            MapContextId = map.MapContextId;
            MapName = map.MapName;
            MapVersion = map.MapVersion;
            BaseRegionId = map.BaseRegion;
        }
    }
}
