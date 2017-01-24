using System.Collections.Generic;

namespace Rasa.Managers
{
    using Structures;
    public class EntityManager
    {
        private static EntityManager _instance;
        private static readonly object InstanceLock = new object();
        public Dictionary<uint, MapChannelClient> EntityTable = new Dictionary<uint, MapChannelClient>();
        public Dictionary<uint, Item> ItemTable = new Dictionary<uint, Item>();

        private uint _entityId = 1000;
        private object _entityIdLock = new object();
        private List<uint> _freeEntityIds = new List<uint>();

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
        public uint NextEntityId
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

        public void FreeEntity(uint id)
        {
            lock (_entityIdLock)
                _freeEntityIds.Add(id);
        }

        // Players

        public void RegisterEntity(uint entityId, MapChannelClient entity)
        {
            EntityTable.Add(entityId, entity);
        }

        public void UnregisterEntity(uint entityId)
        {
            EntityTable.Remove(entityId);
        }

        // Items
        public Item GetItem(uint entityId)
        {
            return ItemTable[entityId];
        }

        public void RegisterItem(uint entityId, Item item)
        {
            if (!ItemTable.ContainsKey(entityId))
                ItemTable.Add(entityId, item);
        }

        public void UnregisterItem(uint entityId)
        {
            ItemTable.Remove(entityId);
        }
    }
}
