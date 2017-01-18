using System.Collections.Generic;

namespace Rasa.Managers
{
    using Structures;
    public class EntityManager
    {
        public static Dictionary<uint, MapChannelClient> EntytyTable = new Dictionary<uint, MapChannelClient>();
        public static Dictionary<uint, Item> ItemTable = new Dictionary<uint, Item>();

        // Players

        public static void RegisterEntity(uint entityId, MapChannelClient entity)
        {
            EntytyTable.Add(entityId, entity);
        }

        public static void UnregisterEntity(uint entityId)
        {
            EntytyTable.Remove(entityId);
        }

        // Items
        public static Item GetItem(uint entityId)
        {
            return ItemTable[entityId];
        }

        public static void RegisterItem(uint entityId, Item item)
        {
            if (!ItemTable.ContainsKey(entityId))
                ItemTable.Add(entityId, item);
        }

        public static void UnregisterItem(uint entityId)
        {
            ItemTable.Remove(entityId);
        }
    }
}
