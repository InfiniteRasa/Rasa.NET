using System.Linq;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public static class ClanTable
    {
        public static ClanEntry GetClanData(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                return (from clanMember in GameDatabaseAccess.CharConnection.ClanMember
                    where clanMember.CharacterId == characterId
                    select clanMember.Clan).FirstOrDefault();
            }
        }
    }
}
