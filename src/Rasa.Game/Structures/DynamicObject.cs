using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    using Data;
    using Game;
    using Managers;

    public class DynamicObject
    {
        public DynamicObject()
        {
            EntityId = EntityManager.Instance.GetEntityId;
        }

        public uint EntityId { get; set; }
        public EntityClassId EntityClassId { get; set; }
        public object ObjectData { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public uint MapContextId { get; set; }
        public uint Faction { get; set; }
        public long RespawnTime { get; set; }
        public List<Client> TrigeredByPlayers = new List<Client>();

        public bool IsInWorld = false;
        public uint StateId = 0;
    }
}
