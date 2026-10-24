using System.Collections.Generic;

namespace Rasa.Structures
{
    using Game;

    public class MapCell
    {
        public MapCell()
        {
            CreatureList = new CellList<Creature>(this);
            ClientList = new CellList<Client>(this);
        }

        public uint CellSeed { get; set; }
        public uint CellPosX { get; set; }
        public uint CellPosZ { get; set; }

        /// <summary>The creatures in this cell; each knows it is here (CellList, CellMembership).</summary>
        public CellList<Creature> CreatureList;

        /// <summary>The clients whose player is in this cell; each knows it is here (CellList, CellMembership).</summary>
        public CellList<Client> ClientList;
        public List<Client> ClientNotifyList = new List<Client>();
        public List<DynamicObject> DynamicObjectList = new List<DynamicObject>();
        public List<MapTrigger> MapTriggers = new List<MapTrigger>();
        public List<MapLink> MapLinks = new List<MapLink>();
    }
}
