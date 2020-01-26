using System;
using System.Linq;
using System.Net;

namespace Rasa.Database.Tables.Auth
{
    using Structures;

    public static class AccountTable
    {

        public static AuthAccountEntry GetAccount(string accountName)
        {
            lock (AuthDatabaseAccess.Lock)
            {
                return AuthDatabaseAccess.Connection.Account.FirstOrDefault(acc => acc.Username == accountName);
            }
        }

        public static AuthAccountEntry GetAccount(uint accountId)
        {
            lock (AuthDatabaseAccess.Lock)
            {
                return AuthDatabaseAccess.Connection.Account.FirstOrDefault(acc => acc.Id == accountId);
            }
        }

        public static void UpdateLoginData(uint accountId, IPAddress ipa)
        {
            lock (AuthDatabaseAccess.Lock)
            {
                var account = GetAccount(accountId);
                account.LastIp = ipa.ToString();
                account.LastLogin = DateTime.Now;
                AuthDatabaseAccess.Connection.SaveChanges();
            }
        }

        public static void UpdateLastServer(uint accountId, byte lastServerId)
        {
            lock (AuthDatabaseAccess.Lock)
            {
                var account = GetAccount(accountId);
                account.LastServerId = lastServerId;
                AuthDatabaseAccess.Connection.SaveChanges();
            }
        }

        public static void InsertAccount(AuthAccountEntry newAccount)
        {
            lock (AuthDatabaseAccess.Lock)
            {
                AuthDatabaseAccess.Connection.Account.Add(newAccount);
                AuthDatabaseAccess.Connection.SaveChanges();
            }
        }
    }
}
