using System.Collections.Generic;

using MySql.Data.MySqlClient;


namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class UserOptionsTable
    {
        private static readonly MySqlCommand DeleteUserOptionsCommand = new MySqlCommand("DELETE FROM user_options WHERE account_id = @AccountId");
        private static readonly MySqlCommand GetUserOptionsCommand = new MySqlCommand("SELECT * FROM user_options WHERE account_id = @AccountId");

        public static void Initialize()
        {
            DeleteUserOptionsCommand.Connection = GameDatabaseAccess.CharConnection;
            DeleteUserOptionsCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            DeleteUserOptionsCommand.Prepare();

            GetUserOptionsCommand.Connection = GameDatabaseAccess.CharConnection;
            GetUserOptionsCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetUserOptionsCommand.Prepare();
        }

        public static void AddUserOption(string value)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                MySqlCommand AddUserOptionsCommand = new MySqlCommand("INSERT INTO user_options (account_id, option_id, value) VALUES" + value)
                {
                    Connection = GameDatabaseAccess.CharConnection
                };
                AddUserOptionsCommand.ExecuteNonQuery();
            }
        }

        public static void DeleteUserOptions(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                DeleteUserOptionsCommand.Parameters["@AccountId"].Value = accountId;
                DeleteUserOptionsCommand.ExecuteNonQuery();
            }
        }

        public static List<UserOptionsEntry> GetUserOptions(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetUserOptionsCommand.Parameters["@AccountId"].Value = accountId;

                var userOptions = new List<UserOptionsEntry>();

                using (var reader = GetUserOptionsCommand.ExecuteReader())
                    while (reader.Read())
                        userOptions.Add(UserOptionsEntry.Read(reader));

                return userOptions;
            }
        }

    }
}
