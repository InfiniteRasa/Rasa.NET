using System.Collections.Generic;

namespace Rasa.Structures
{
    using Game;

    public class MapCell
    {
        public uint CellSeed { get; set; }
        public uint CellPosX { get; set; }
        public uint CellPosZ { get; set; }
        public List<Creature> CreatureList = new List<Creature>();
        public List<Client> ClientList = new List<Client>();
        public List<Client> ClientNotifyList = new List<Client>();
        public List<DynamicObject> DynamicObjectList = new List<DynamicObject>();
        public List<MapTrigger> MapTriggers = new List<MapTrigger>();
    }
}
