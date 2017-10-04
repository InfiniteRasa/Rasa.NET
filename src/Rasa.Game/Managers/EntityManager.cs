﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rasa.Managers
{
    using Data;
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
        public Dictionary<uint, MapChannelClient> MapClients = new Dictionary<uint, MapChannelClient>();
        public Dictionary<uint, Actor> Actors = new Dictionary<uint, Actor>();

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
        public void DestroyPhysicalEntity(MapChannelClient mapClient, uint entityId, EntityType entityType)
        {
            // destroy entity
            mapClient.Player.Client.SendPacket(5, new DestroyPhysicalEntityPacket { EntityId = entityId });
            //free entity
            switch (entityType)
            {
                case EntityType.MapClient:
                    FreeEntity(entityId);
                    UnregisterEntity(entityId);
                    UnregisterMapClient(entityId);
                    break;
                case EntityType.Player:
                    break;
                case EntityType.Entity:
                    break;
                case EntityType.Object:
                    break;
                case EntityType.Npc:
                    break;
                case EntityType.Creature:
                    break;
                case EntityType.Item:
                    FreeEntity(entityId);
                    UnregisterEntity(entityId);
                    UnregisterItem(entityId);
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
            return Items[entityId];
        }

        public void RegisterItem(uint entityId, Item item)
        {
            Items.Add(entityId, item);
        }

        public void UnregisterItem(uint entityId)
        {
            Items.Remove(entityId);
        }

        // MapClients
        public MapChannelClient GetMapClient(uint entityId)
        {
            return MapClients[entityId];
        }

        public void RegisterMapClient(uint entityId, MapChannelClient mapClient)
        {
            MapClients.Add(entityId, mapClient);
        }

        public void UnregisterMapClient(uint entityId)
        {
            MapClients.Remove(entityId);
        }
    }
}