using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class CharacterLogosTable
    {
        public static List<int> GetLogos(uint accountId, uint characterSlot)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                return (from characterLogos in GameDatabaseAccess.CharConnection.CharacterLogos
                    where characterLogos.AccountId == accountId && characterLogos.CharacterSlot == characterSlot
                    select characterLogos.LogosId).ToList();
            }
        }

        public static void SetLogos(uint accountId, uint characterSlot, int logosId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GameDatabaseAccess.CharConnection.CharacterLogos.Add(new CharacterLogosEntry
                {
                    AccountId = accountId,
                    CharacterSlot = characterSlot,
                    LogosId = logosId
                });
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }
    }
}
