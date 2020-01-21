using System.Collections.Generic;
using System.Linq;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class UserOptionsTable
    {

        public static void AddUserOption(IEnumerable<UserOptionsEntry> userOptions)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GameDatabaseAccess.CharConnection.UserOptions.AddRange(userOptions);
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static void DeleteUserOptions(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var options = GameDatabaseAccess.CharConnection.UserOptions.Where(opt => opt.AccountId == accountId);
                GameDatabaseAccess.CharConnection.RemoveRange(options);
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static List<UserOptionsEntry> GetUserOptions(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                return GameDatabaseAccess.CharConnection.UserOptions.Where(opt => opt.AccountId == accountId).ToList();
            }
        }

    }
}
