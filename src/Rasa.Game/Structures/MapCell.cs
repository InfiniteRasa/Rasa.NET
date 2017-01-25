using System.Collections.Generic;

namespace Rasa.Structures
{
    public class MapCell
    {
        public uint CellPosX { get; set; }
        public uint CellPosY { get; set; }
        public List<MapChannelClient> PlayerList = new List<MapChannelClient>();
        public List<MapChannelClient> PlayerNotifyList = new List<MapChannelClient>();
        //public List<DynamicObject> DynamicObjectList = new List<DynamicObject>();
        //public List<Creature> CreatureList = new List<Creature>();
    }
}
