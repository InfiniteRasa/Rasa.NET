using System.Collections.Generic;

namespace Rasa.Structures
{
    public class MapChannel
    {
        // ToDo
        public MapInfo MapInfo { get; set; }
        public Dictionary<int, MapChannelClient> SocketToClient { get; set; }
        // timers
        public int TimerClientEffectUpdate { get; set; }
        public int TimerMissileUpdate { get; set; }
        public int TimerDynObjUpdate { get; set; }
        public int TimerGeneralTimer { get; set; }
        public int TimerController { get; set; }
        public int TimerPlayerUpdate { get; set; }
        // player
        public int PlayerCount { get; set; }
        public int PlayerLimit { get; set; }
        public List<MapChannelClient> PlayerList { get; set; }
        //
        public MapCellInfo MapCellInfo = new MapCellInfo();
    }
}
