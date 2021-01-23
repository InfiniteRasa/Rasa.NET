using System.Collections.Generic;
using System.Diagnostics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    public class EntityManager
    {
        private static EntityManager _instance;
        private static readonly object InstanceLock = new object();
        private ulong _entityId = 1000;
        private object _entityIdLock = new object();
        private List<ulong> _freeEntityIds = new List<ulong>();

        public Dictionary<ulong, EntityType> RegisteredEntities = new Dictionary<ulong, EntityType>();
        public Dictionary<ulong, Item> Items = new Dictionary<ulong, Item>();
        public Dictionary<ulong, MapChannelClient> Players = new Dictionary<ulong, MapChannelClient>();
        public Dictionary<ulong, Actor> Actors = new Dictionary<ulong, Actor>();
        public Dictionary<ulong, Creature> Creatures = new Dictionary<ulong, Creature>();
        public Dictionary<ulong, DynamicObject> DynamicObjects = new Dictionary<ulong, DynamicObject>();
        public Dictionary<ulong, List<ulong>> VendorItems = new Dictionary<ulong, List<ulong>>();

        public static EntityManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new EntityManager();
                    }
                }

                return _instance;
            }
        }
        
        private EntityManager()
        {
        }

        // All Entities (everything in game)
        public void DestroyPhysicalEntity(Client client, ulong entityId, EntityType entityType)
        {
            client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(entityId));
            //free entity
            switch (entityType)
            {
                case EntityType.Player:
                    FreeEntity(entityId);
                    UnregisterEntity(entityId);
                    UnregisterPlayer(entityId);
                    break;
                case EntityType.Npc:
                    break;
                case EntityType.Creature:
                    FreeEntity(entityId);
                    UnregisterEntity(entityId);
                    UnregisterCreature(entityId);
                    break;
                case EntityType.Item:
                    FreeEntity(entityId);
                    UnregisterEntity(entityId);
                    UnregisterItem(entityId);
                    break;
                case EntityType.Object:
                    break;
                case EntityType.VendorItem:
                    break;
                default:
                    Debugger.Break();
                    break;
            }
                    
        }

        public ulong GetEntityId
        {
            get
            {
                lock (_entityIdLock)
                {
                    if (_freeEntityIds.Count > 0)
                    {
                        var freeEntityId = _freeEntityIds[0];

                        _freeEntityIds.RemoveAt(0);

                        return freeEntityId;
                    }

                    return _entityId++;
                }
            }
        }

        public EntityClassId GetEntityClassId(ulong entityId)
        {
            switch (GetEntityType(entityId))
            {
                case EntityType.Player:
                    return Players[entityId].Player.Actor.EntityClassId;

                case EntityType.Creature:
                    return Creatures[entityId].EntityClassId;

                case EntityType.Npc:
                    Logger.WriteLog(LogType.Error, $"Not implemented, {GetEntityType(entityId)} ToDo");
                    return 0;

                case EntityType.Item:
                    return Items[entityId].ItemTemplate.Class;

                case EntityType.Object:
                    return DynamicObjects[entityId].EntityClassId;

                case EntityType.VendorItem:
                    Logger.WriteLog(LogType.Error, $"Not implemented, {GetEntityType(entityId)} ToDo");
                    return 0;

                default:
                    Logger.WriteLog(LogType.Error, $"Unknown entityType {GetEntityType(entityId)}");
                    return 0;
            }
        }

        public EntityType GetEntityType(ulong entityId)
        {
            if (entityId != 0)
                return RegisteredEntities[entityId];

            return 0;
        }

        public void FreeEntity(ulong id)
        {
            lock (_entityIdLock)
                if(!_freeEntityIds.Contains(id))
                    _freeEntityIds.Add(id);
        }

        public void RegisterEntity(ulong entityId, EntityType type)
        {
            RegisteredEntities.Add(entityId, type);
        }

        public void UnregisterEntity(ulong entityId)
        {
            RegisteredEntities.Remove(entityId);
        }
        // Actors
        public Actor GetActor(ulong entityId)
        {
            return Actors[entityId];
        }

        public void RegisterActor(ulong entityId, Actor actor)
        {
            Actors.Add(entityId, actor);
        }

        public void UnregisterActor(ulong entityId)
        {
            Actors.Remove(entityId);
        }

        // Items
        public Item GetItem(ulong entityId)
        {
            if (Items.ContainsKey(entityId))
                return Items[entityId];

            return null;
        }

        public void RegisterItem(ulong entityId, Item item)
        {
            Items.Add(entityId, item);
        }

        public void UnregisterItem(ulong entityId)
        {
            Items.Remove(entityId);
        }

        // DynamicObjects

        internal DynamicObject GetObject(ulong entityId)
        {
            return DynamicObjects[entityId];
        }

        public void RegisterDynamicObject(DynamicObject dynamicObject)
        {
            DynamicObjects.Add(dynamicObject.EntityId, dynamicObject);
        }

        // Players
        public MapChannelClient GetPlayer(ulong entityId)
        {
            return Players[entityId];
        }

        public void RegisterPlayer(ulong entityId, MapChannelClient mapClient)
        {
            Players.Add(entityId, mapClient);
        }

        public void UnregisterPlayer(ulong entityId)
        {
            Players.Remove(entityId);
        }

        // Creatures
        public Creature GetCreature(ulong entityId)
        {
            return Creatures[entityId];
        }

        public void RegisterCreature(Creature creature)
        {
            Creatures.Add(creature.Actor.EntityId, creature);
        }

        public void UnregisterCreature(ulong entityId)
        {
            Creatures.Remove(entityId);
        }

        // VendorItems
        public void RegisterVendorItem(ulong vendorEntityId, List<ulong> itemEntityIds)
        {
            VendorItems.Add(vendorEntityId, itemEntityIds);
        }

        public void UnRegisterVendorItem(ulong clientEntityId)
        {
            // ToDO
        }

    }
}
