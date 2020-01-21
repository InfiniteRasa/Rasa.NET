using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class CharacterLockboxTable
    {
        public static void AddLockboxInfo(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GameDatabaseAccess.CharConnection.CharacterLockbox.Add(new CharacterLockboxEntry
                {
                    AccountId = accountId
                });
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static List<int> GetLockboxInfo(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                List<int> lockBoxInfo = new List<int>();
                var lockBox = (from charLockBox in GameDatabaseAccess.CharConnection.CharacterLockbox
                    where charLockBox.AccountId == accountId
                    select new[] {charLockBox.Credits, charLockBox.PurashedTabs});

                foreach (var lockb in lockBox)
                {
                    lockBoxInfo.Add(lockb[0]);
                    lockBoxInfo.Add(lockb[1]);
                }

                return lockBoxInfo;
            }
        }

        public static void UpdateCredits(uint accountId, int credits)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var characterLockBox = GameDatabaseAccess.CharConnection.CharacterLockbox.First(charLockBox =>
                    charLockBox.AccountId == accountId);
                characterLockBox.Credits = credits;
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static void UpdatePurashedTabs(uint accountId, int purashedTabs)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var characterLockBox = GameDatabaseAccess.CharConnection.CharacterLockbox.First(charLockBox =>
                    charLockBox.AccountId == accountId);
                characterLockBox.PurashedTabs = purashedTabs;
            }
        }
    }
}
