namespace Rasa.Structures
{
    public class MapInfo
    {
        public uint ContextId { get; set; }
        public string Name { get; set; }
        public uint Version { get; set; }
        public uint BaseRegionId { get; set; }

        public MapInfo(uint contextId, string name, uint version, uint baseRegionId)
        {
            ContextId = contextId;
            Name = name;
            Version = version;
            BaseRegionId = baseRegionId;
        }
    }
}