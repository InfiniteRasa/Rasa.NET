using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    using Data;
    using Managers;

    public class DynamicObject
    {
        public DynamicObject()
        {
            EntityId = EntityManager.Instance.GetEntityId;
        }

        public uint EntityId { get; set; }
        public DynamicObjectFunctions Functions { get; set; }
        public EntityClassId EntityClassId { get; set; }
        public delegate void ObjectData();
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public uint MapContextId { get; set; }
        public uint Faction { get; set; }
        public bool InUse = false;
        public bool IsInWorld = false;
        public long RespawnTime { get; set; }
    }

    public class DynamicObjectFunctions
    {
        public delegate void Destroy(MapChannel mapChannel, DynamicObject dynObject);
        public delegate void AppearForPlayers(MapChannel mapChannel, DynamicObject dynObject, List<MapChannelClient> playerList);       // enter sight, created
        public delegate void DisappearForPlayers(MapChannel mapChannel, DynamicObject dynObject, List<MapChannelClient> playerList);    // leave sight, deleted
        public delegate bool PeriodicCallback(MapChannel mapChannel, DynamicObject dynObject, uint timerID, long timePassed);           // timerId can be used to distinguish between multiple timers on the same object, timePassed is the time since the last callback in MS
        public delegate void UseObject(MapChannel mapChannel, DynamicObject dynObject, MapChannelClient player, int actionId, int actionArg);
        public delegate void InterruptUse(MapChannel mapChannel, DynamicObject dynObject, MapChannelClient player, int actionID, int actionArg);
    }
}
