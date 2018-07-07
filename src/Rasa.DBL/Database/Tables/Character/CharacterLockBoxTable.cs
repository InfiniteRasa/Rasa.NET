using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public class CharacterLockboxTable
    {
        private static readonly MySqlCommand AddLockboxInfoCommand = new MySqlCommand("INSERT INTO character_lockbox (accountId) VALUES (@AccountId)");
        private static readonly MySqlCommand GetLockboxInfoCommand = new MySqlCommand("SELECT credits, purashedTabs FROM character_lockbox WHERE accountId = @AccountId");
        private static readonly MySqlCommand UpdateCreditsCommand = new MySqlCommand("UPDATE character_lockbox SET credits = @Credits WHERE accountId = @AccountId");
        private static readonly MySqlCommand UpdatePurashedTabsCommand = new MySqlCommand("UPDATE character_lockbox SET purashedTabs = @PurashedTabs WHERE accountId = @AccountId");

        public static void Initialize()
        {
            AddLockboxInfoCommand.Connection = GameDatabaseAccess.CharConnection;
            AddLockboxInfoCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            AddLockboxInfoCommand.Prepare();

            GetLockboxInfoCommand.Connection = GameDatabaseAccess.CharConnection;
            GetLockboxInfoCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetLockboxInfoCommand.Prepare();

            UpdateCreditsCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCreditsCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            UpdateCreditsCommand.Parameters.Add("@Credits", MySqlDbType.UInt32);
            UpdateCreditsCommand.Prepare();

            UpdatePurashedTabsCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdatePurashedTabsCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            UpdatePurashedTabsCommand.Parameters.Add("@PurashedTabs", MySqlDbType.UInt32);
            UpdatePurashedTabsCommand.Prepare();
        }

        public static void AddLockboxInfo(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                AddLockboxInfoCommand.Parameters["@AccountId"].Value = accountId;
                AddLockboxInfoCommand.ExecuteNonQuery();
            }
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

        public static void UpdatePurashedTabs(uint accountId, uint purashedTabs)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdatePurashedTabsCommand.Parameters["@AccountId"].Value = accountId;
                UpdatePurashedTabsCommand.Parameters["@PurashedTabs"].Value = purashedTabs;
                UpdatePurashedTabsCommand.ExecuteNonQuery();
            }
        }
    }
}
