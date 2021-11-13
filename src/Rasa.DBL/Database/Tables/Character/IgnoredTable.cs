using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public class IgnoredTable
    {
        private static readonly MySqlCommand AddIgnoredCommand = new MySqlCommand("INSERT INTO ignored (account_Id, ignored_account_id) VALUES (@AccountId, @IgnoredAccountId)");
        private static readonly MySqlCommand RemoveIgnoredCommand = new MySqlCommand("DELETE FROM ignored WHERE account_id = @AccountId AND ignored_account_id = @IgnoredAccountId");
        private static readonly MySqlCommand GetIgnoredCommand = new MySqlCommand("SELECT ignored_account_id FROM ignored WHERE account_id = @AccountId");

        public static void Initialize()
        {
            AddIgnoredCommand.Connection = GameDatabaseAccess.CharConnection;
            AddIgnoredCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            AddIgnoredCommand.Parameters.Add("@IgnoredAccountId", MySqlDbType.UInt32);
            AddIgnoredCommand.Prepare();

            GetIgnoredCommand.Connection = GameDatabaseAccess.CharConnection;
            GetIgnoredCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetIgnoredCommand.Prepare();

            RemoveIgnoredCommand.Connection = GameDatabaseAccess.CharConnection;
            RemoveIgnoredCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            RemoveIgnoredCommand.Parameters.Add("@IgnoredAccountId", MySqlDbType.UInt32);
            RemoveIgnoredCommand.Prepare();
        }

        public static void AddIgnored(uint accountId, uint ignoredAccountId)
        {

            lock (GameDatabaseAccess.CharLock)
            {
                AddIgnoredCommand.Parameters["@AccountId"].Value = accountId;
                AddIgnoredCommand.Parameters["@IgnoredAccountId"].Value = ignoredAccountId;
                AddIgnoredCommand.ExecuteNonQuery();
            }
        }

        public static List<uint> GetIgnored(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var ignoredAccountIds = new List<uint>();

                GetIgnoredCommand.Parameters["@AccountID"].Value = accountId;

                using (var reader = GetIgnoredCommand.ExecuteReader())
                    while (reader.Read())
                        ignoredAccountIds.Add(reader.GetUInt32("ignored_account_id"));

                return ignoredAccountIds;
            }
        }

        public static void RemoveIgnored(uint accountId, uint ignoredAccountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                RemoveIgnoredCommand.Parameters["@AccountID"].Value = accountId;
                RemoveIgnoredCommand.Parameters["@IgnoredAccountId"].Value = ignoredAccountId;
                RemoveIgnoredCommand.ExecuteNonQuery();
            }
        }
    }
}
