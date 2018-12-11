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
        private uint _entityId = 1000;
        private object _entityIdLock = new object();
        private List<uint> _freeEntityIds = new List<uint>();

        public Dictionary<uint, EntityType> RegisteredEntities = new Dictionary<uint, EntityType>();
        public Dictionary<uint, Item> Items = new Dictionary<uint, Item>();
        public Dictionary<uint, MapChannelClient> Players = new Dictionary<uint, MapChannelClient>();
        public Dictionary<uint, Actor> Actors = new Dictionary<uint, Actor>();
        public Dictionary<uint, Creature> Creatures = new Dictionary<uint, Creature>();
        public Dictionary<uint, List<uint>> VendorItems = new Dictionary<uint, List<uint>>();

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
        public void DestroyPhysicalEntity(Client client, uint entityId, EntityType entityType)
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

        public uint GetEntityId
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

        public EntityClassId GetEntityClassId(uint entityId)
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
                    Logger.WriteLog(LogType.Error, $"Not implemented, {GetEntityType(entityId)} ToDo");
                    return 0;

                case EntityType.VendorItem:
                    Logger.WriteLog(LogType.Error, $"Not implemented, {GetEntityType(entityId)} ToDo");
                    return 0;

                default:
                    Logger.WriteLog(LogType.Error, $"Unknown entityType {GetEntityType(entityId)}");
                    return 0;
            }
        }

        public EntityType GetEntityType(uint entityId)
        {
            return RegisteredEntities[entityId];
        }

        public void FreeEntity(uint id)
        {
            lock (_entityIdLock)
                if(!_freeEntityIds.Contains(id))
                    _freeEntityIds.Add(id);
        }

        public void RegisterEntity(uint entityId, EntityType type)
        {
            RegisteredEntities.Add(entityId, type);
        }

        public void UnregisterEntity(uint entityId)
        {
            RegisteredEntities.Remove(entityId);
        }
        // Actors
        public Actor GetActor(uint entityId)
        {
            return Actors[entityId];
        }

        public void RegisterActor(uint entityId, Actor actor)
        {
            Actors.Add(entityId, actor);
        }

        public void UnregisterActor(uint entityId)
        {
            Actors.Remove(entityId);
        }

        // Items
        public Item GetItem(uint entityId)
        {
            if (Items.ContainsKey(entityId))
                return Items[entityId];

            return null;
        }

        public void RegisterItem(uint entityId, Item item)
        {
            Items.Add(entityId, item);
        }

        public void UnregisterItem(uint entityId)
        {
            Items.Remove(entityId);
        }

        // Players
        public MapChannelClient GetPlayer(uint entityId)
        {
            return Players[entityId];
        }

        public void RegisterPlayer(uint entityId, MapChannelClient mapClient)
        {
            Players.Add(entityId, mapClient);
        }

        public void UnregisterPlayer(uint entityId)
        {
            Players.Remove(entityId);
        }

        // Creatures
        public Creature GetCreature(uint entityId)
        {
            return Creatures[entityId];
        }

        public void RegisterCreature(Creature creature)
        {
            Creatures.Add(creature.Actor.EntityId, creature);
        }

        public void UnregisterCreature(uint entityId)
        {
            Creatures.Remove(entityId);
        }

        // VendorItems
        public void RegisterVendorItem(uint vendorEntityId, List<uint> itemEntityIds)
        {
            VendorItems.Add(vendorEntityId, itemEntityIds);
        }

        public void UnRegisterVendorItem(uint clientEntityId)
        {
            // ToDO
        }

    }
}
