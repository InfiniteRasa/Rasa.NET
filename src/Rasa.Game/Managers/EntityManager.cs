using System.Collections.Generic;
using System.Diagnostics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Game.Missions.Integration;
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
        public Dictionary<ulong, Manifestation> Players = new Dictionary<ulong, Manifestation>();
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
            ReleaseEntity(entityId, entityType);
        }

        internal void ReleaseEntity(ulong entityId, EntityType entityType)
        {
            if (!RegisteredEntities.TryGetValue(entityId, out var registeredType) || registeredType != entityType)
                return;

            switch (entityType)
            {
                case EntityType.Character:
                    UnregisterEntity(entityId);
                    UnregisterPlayer(entityId);
                    UnregisterActor(entityId);
                    FreeEntity(entityId);
                    break;
                case EntityType.Npc:
                    break;
                case EntityType.Creature:
                    UnregisterEntity(entityId);
                    UnregisterCreature(entityId);
                    UnregisterActor(entityId);
                    FreeEntity(entityId);
                    break;
                case EntityType.Item:
                    UnregisterEntity(entityId);
                    UnregisterItem(entityId);
                    FreeEntity(entityId);
                    break;
                case EntityType.Object:
                    UnregisterEntity(entityId);
                    UnregisterDynamicObject(entityId);
                    FreeEntity(entityId);
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

        internal ulong AllocateUnrecycledEntityId()
        {
            lock (_entityIdLock)
                return _entityId++;
        }

        public EntityClasses GetEntityClassId(ulong entityId)
        {
            switch (GetEntityType(entityId))
            {
                case EntityType.Character:
                    return Players[entityId].EntityClass;

                case EntityType.Creature:
                    return Creatures[entityId].EntityClass;

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
            if(RegisteredEntities.ContainsKey(entityId))
                return RegisteredEntities[entityId];

            return 0;
        }

        public void FreeEntity(ulong id)
        {
            // An id can only go back to the pool once nothing is registered under it. Handing
            // out an id that is still in RegisteredEntities gives two entities the same id,
            // and the second registration throws on the main loop. A caller that forgot to
            // unregister now leaks the id and logs, instead.
            if (RegisteredEntities.ContainsKey(id))
            {
                Logger.WriteLog(LogType.Error, $"Entity {id} ({RegisteredEntities[id]}) was freed while still registered; the id is not reused.");
                return;
            }

            lock (_entityIdLock)
                if(!_freeEntityIds.Contains(id))
                    _freeEntityIds.Add(id);
        }

        public void RegisterEntity(ulong entityId, EntityType type)
        {
            // Not Add: a duplicate used to throw out of whichever map worker was registering,
            // losing the tick. Registering the same entity twice (a creature re-added to the
            // world) is harmless; a different type under the same id means an id was reused
            // while registered, which FreeEntity now refuses, and is worth a line either way.
            if (RegisteredEntities.TryGetValue(entityId, out var existing) && existing != type)
                Logger.WriteLog(LogType.Error, $"Entity {entityId} registered as {type} while already registered as {existing}; the new registration wins.");

            RegisteredEntities[entityId] = type;
        }

        public void UnregisterEntity(ulong entityId)
        {
            InvalidateConversationTarget(entityId);
            RegisteredEntities.Remove(entityId);
        }

        private void InvalidateConversationTarget(ulong entityId)
        {
            if (Creatures.TryGetValue(entityId, out var creature))
                MissionInteractionPolicy.InvalidateTarget(creature.RuntimeMapChannel, entityId);
            if (DynamicObjects.TryGetValue(entityId, out var obj))
                MissionInteractionPolicy.InvalidateTarget(obj.RuntimeMapChannel, entityId);
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

        /// <summary>GetObject without the KeyNotFoundException: for ids that come off the wire.</summary>
        internal bool TryGetObject(ulong entityId, out DynamicObject dynamicObject)
        {
            return DynamicObjects.TryGetValue(entityId, out dynamicObject);
        }

        public void RegisterDynamicObject(DynamicObject dynamicObject)
        {
            DynamicObjects.Add(dynamicObject.EntityId, dynamicObject);
        }

        internal void UnregisterDynamicObject(ulong entityId)
        {
            InvalidateConversationTarget(entityId);
            DynamicObjects.Remove(entityId);
        }

        // Players
        public Manifestation GetPlayer(ulong entityId)
        {
            return Players[entityId];
        }

        public void RegisterPlayer(ulong entityId, Manifestation player)
        {
            Players.Add(entityId, player);
        }

        public void UnregisterPlayer(ulong entityId)
        {
            Players.Remove(entityId);
        }

        // Creatures
        /// <summary>
        /// The creature with this entity id, or null. Entity ids reach this from client packets,
        /// so an id that is not a creature - another map's, a stale one, an item's - was a
        /// KeyNotFoundException in whichever handler asked, which closes the connection.
        /// ChatCommandsManager already wrote GetCreature(entityId)?.Position, expecting the null
        /// this now returns.
        /// </summary>
        public Creature GetCreature(ulong entityId)
        {
            return Creatures.TryGetValue(entityId, out var creature) ? creature : null;
        }

        public void RegisterCreature(Creature creature)
        {
            Creatures.Add(creature.EntityId, creature);
        }

        public void UnregisterCreature(ulong entityId)
        {
            InvalidateConversationTarget(entityId);
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
