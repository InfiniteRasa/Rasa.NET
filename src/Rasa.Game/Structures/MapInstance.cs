namespace Rasa.Structures
{
    public class MapInstance
    {
        internal MapInfo MapInfo { get; set; }
        internal uint InstanceId { get; set; } // we don't use instances, 1 instance per map for now

        public MapInstance(MapInfo mapInfo, uint instanceId = 1)
        {
            MapInfo = mapInfo;
            InstanceId = instanceId;
        }
    }
}
