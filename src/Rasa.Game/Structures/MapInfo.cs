namespace Rasa.Structures
{
    public class MapInfo
    {
        public int MapId { get; set; }
        public string MapName { get; set; }
        public int MapVersion { get; set; }
        public int BaseRegionId { get; set; }

        public MapInfo(int mapId, string mapName, int mapVersion, int baseRegionId)
        {
            MapId = mapId;
            MapName = mapName;
            MapVersion = mapVersion;
            BaseRegionId = baseRegionId;
        }
    }
}
