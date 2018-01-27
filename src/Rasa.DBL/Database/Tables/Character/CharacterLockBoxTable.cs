using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public class CharacterLockboxTable
    {
        private static readonly MySqlCommand GetLockboxInfoCommand = new MySqlCommand("SELECT credits, purashedTabs FROM character_lockbox WHERE accountId = @AccountId");
        private static readonly MySqlCommand UpdateCreditsCommand = new MySqlCommand("UPDATE character_lockbox SET credits = @Credits WHERE accountId = @AccountId");

        public static void Initialize()
        {
            GetLockboxInfoCommand.Connection = GameDatabaseAccess.CharConnection;
            GetLockboxInfoCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetLockboxInfoCommand.Prepare();

            UpdateCreditsCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCreditsCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            UpdateCreditsCommand.Parameters.Add("@Credits", MySqlDbType.UInt32);
            UpdateCreditsCommand.Prepare();
        }

        public static List<uint> GetLockboxInfo(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var lockboxInfo = new List<uint>();

                GetLockboxInfoCommand.Parameters["@AccountId"].Value = accountId;

                using (var reader = GetLockboxInfoCommand.ExecuteReader())
                    while (reader.Read())
                    {
                        lockboxInfo.Add(reader.GetUInt32("credits"));
                        lockboxInfo.Add(reader.GetUInt32("purashedTabs"));
                    }

                return lockboxInfo;
            }
        }

        public static void UpdateCredits(uint accountId, uint credits)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCreditsCommand.Parameters["@AccountId"].Value = accountId;
                UpdateCreditsCommand.Parameters["@Credits"].Value = credits;
                UpdateCreditsCommand.ExecuteNonQuery();
            }
        }
    }
}
