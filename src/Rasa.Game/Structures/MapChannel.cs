using System.Collections.Generic;

namespace Rasa.Structures
{
    using Game;

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
        // queue
        public readonly Queue<Client> QueuedClients = new Queue<Client>();
        public int PlayerQueueReadIndex { get; set; }
        public int PlayerQueueWriteIndex { get; set; }
        // cell
        public MapCellInfo MapCellInfo = new MapCellInfo();
        // effect
        public int CurrentEffectId { get; set; } // increases with every spawned game effect
    }
}
