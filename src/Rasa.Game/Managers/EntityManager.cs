using System.Collections.Generic;

namespace Rasa.Managers
{
    using Structures;
    public class EntityManager
    {
        public static Dictionary<uint, MapChannelClient> EntytyTable = new Dictionary<uint, MapChannelClient>();
        public static uint GetFreeEntityIdForPlayer()
        {
            return 4096; // ToDo Generate unique EntetyId for every player
        }

        public static void RegisterEntity(uint entityId, MapChannelClient entity)
        {
            EntytyTable.Add(entityId, entity);
        }
    }
}
