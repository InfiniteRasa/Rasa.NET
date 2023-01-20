namespace Rasa.Structures
{
    public class Map
    {
        internal MapInfo MapInfo { get; set; }
        internal uint InstanceId { get; set; } = 0; // we don't use instances, 1 instance per map for now
    }
}
