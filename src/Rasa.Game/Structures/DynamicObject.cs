﻿using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    using Data;
    using Game;
    using Managers;
    using Interfaces;

    public class DynamicObject : IHasPosition
    {
        public DynamicObject()
        {
            EntityId = EntityManager.Instance.GetEntityId;
        }

        public ulong EntityId { get; set; }
        public EntityClasses EntityClassId { get; set; }
        public object ObjectData { get; set; }
        public Vector3 Position { get; set; }
        public double Rotation { get; set; }
        public uint MapContextId { get; set; }
        public Factions Faction { get; set; }
        public long RespawnTime { get; set; }
        public DynamicObjectType DynamicObjectType { get; set; }
        public List<Client> TriggeredByPlayers = new List<Client>();
        public string Comment { get; set; }

        public bool IsInWorld = false;
        public UseObjectState StateId = 0;
        public bool IsEnabled = true;
        public uint WindupTime { get; internal set; }
        public uint ActivateMission { get; internal set; }
    }
}
