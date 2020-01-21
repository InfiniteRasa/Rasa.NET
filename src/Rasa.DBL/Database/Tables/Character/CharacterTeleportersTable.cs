using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class CharacterTeleportersTable
    {
        public static void AddTeleporter(uint characterId, uint waypointId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GameDatabaseAccess.CharConnection.CharacterTeleporters.Add(new CharacterTeleportersEntry
                {
                    CharacterId = characterId,
                    WaypointId = waypointId
                });
            }
        }

        public static List<uint> GetTeleporters(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                return (from charTeleporters in GameDatabaseAccess.CharConnection.CharacterTeleporters
                    where charTeleporters.CharacterId == characterId
                    select charTeleporters.WaypointId.Value).ToList();
            }
        }
    }
}
