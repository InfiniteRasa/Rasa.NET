using System.Linq;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class GameAccountTable
    {
        public static void CreateAccountDataIfNeeded(uint id, string name, string email)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var account = GameDatabaseAccess.CharConnection.Account.FirstOrDefault(acc => acc.Id == id);

                if (account != null)
                    return;

                GameDatabaseAccess.CharConnection.Account.Add(new GameAccountEntry
                {
                    Id = id,
                    Name = name,
                    Email = email
                });
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static GameAccountEntry GetAccount(uint id)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                return GameDatabaseAccess.CharConnection.Account.First(acc => acc.Id == id);
            }
        }
        public static GameAccountEntry GetAccountByFamilyName(string familyName)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                return GameDatabaseAccess.CharConnection.Account.FirstOrDefault(acc => acc.FamilyName == familyName);
            }
        }

        public static void UpdateAccount(GameAccountEntry entry)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var account = GameDatabaseAccess.CharConnection.Account.First(acc => acc.Id == entry.Id);
                account.Level = entry.Level;
                account.FamilyName = entry.FamilyName;
                account.SelectedSlot = entry.SelectedSlot;
                account.CanSkipBootcamp = entry.CanSkipBootcamp;
                account.LastIP = entry.LastIP;
                account.LastLogin = entry.LastLogin;
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }
    }
}
