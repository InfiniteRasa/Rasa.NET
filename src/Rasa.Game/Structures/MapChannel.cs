using System.Collections.Generic;

namespace Rasa.Structures
{
    using Game;

    public class MapChannel
    {
        // ToDo
        public MapInfo MapInfo { get; set; }
        // timers
        //public int TimerClientEffectUpdate { get; set; }
        //public int TimerMissileUpdate { get; set; }
        //public int TimerDynObjUpdate { get; set; }
        public long MapChannelElapsed { get; set; }
        //public int TimerController { get; set; }
        //public int TimerPlayerUpdate { get; set; }
        // player
        public int PlayerLimit { get; set; }
        public List<Client> ClientList { get; set; }
        // queue
        public readonly Queue<Client> QueuedClients = new Queue<Client>();
        // action
        public readonly List<ActionData> PerformRecovery = new List<ActionData>();
        // cell
        public MapCellInfo MapCellInfo = new MapCellInfo();
        // effect
        public int CurrentEffectId { get; set; } // increases with every spawned game effect

        // Dynamic Object List
        public List<DynamicObject> DynamicObjects = new List<DynamicObject>();

        // Dictionary<uniqueControlPointId, dataAboutdynamicObject> ControlPoints
        public Dictionary<uint, DynamicObject> ControlPoints = new Dictionary<uint, DynamicObject>();

        // Dictionary<uniqueFootlockerId, dataAboutdynamicObject> Footlockers
        public Dictionary<uint, DynamicObject> FootLockers = new Dictionary<uint, DynamicObject>();

        // Dictionary<uniqueTeleporterId, dataAboutdynamicObject> Teleporters
        public Dictionary<uint,DynamicObject> Teleporters = new Dictionary<uint, DynamicObject>();

        // Dictionary<uniqueTriggerId, TriggerData> MapTriggers
        public Dictionary<uint, MapTrigger> MapTriggers = new Dictionary<uint, MapTrigger>();

        // Missiles on this mapChannel
        public List<Missile> QueuedMissiles = new List<Missile>();
    }
}
