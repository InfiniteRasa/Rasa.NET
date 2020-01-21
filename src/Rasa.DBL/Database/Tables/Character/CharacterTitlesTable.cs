using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.Character
{
    public static class CharacterTitlesTable
    {
        public static List<uint> GetCharacterTitles(uint accountId, byte characterSlot)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                return (from characterTitles in GameDatabaseAccess.CharConnection.CharacterTitles
                    where characterTitles.AccountId == accountId
                          && characterTitles.CharacterSlot == characterSlot
                    select (uint)characterTitles.TitleId).ToList();
            }
        }
    }
}