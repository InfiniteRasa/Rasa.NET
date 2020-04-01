using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public class ClanLockboxTable
    {
        private static readonly MySqlCommand AddLockboxInfoCommand = new MySqlCommand("INSERT INTO clan_lockbox (clanId) VALUES (@ClanId)");
        private static readonly MySqlCommand GetLockboxInfoCommand = new MySqlCommand("SELECT credits, prestige, purchasedTabs FROM clan_lockbox WHERE clanId = @ClanId");
        private static readonly MySqlCommand UpdateCreditsCommand = new MySqlCommand("UPDATE clan_lockbox SET credits = @Credits WHERE clanId = @ClanId");
        private static readonly MySqlCommand UpdatePrestigeCommand = new MySqlCommand("UPDATE clan_lockbox SET prestige = @Prestige WHERE clanId = @ClanId");
        //private static readonly MySqlCommand UpdatePurashedTabsCommand = new MySqlCommand("UPDATE clan_lockbox SET purashedTabs = @PurchasedTabs WHERE clanId = @ClanId");

        public static void Initialize()
        {
            AddLockboxInfoCommand.Connection = GameDatabaseAccess.CharConnection;
            AddLockboxInfoCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            AddLockboxInfoCommand.Prepare();

            GetLockboxInfoCommand.Connection = GameDatabaseAccess.CharConnection;
            GetLockboxInfoCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            GetLockboxInfoCommand.Prepare();

            UpdateCreditsCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCreditsCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            UpdateCreditsCommand.Parameters.Add("@Credits", MySqlDbType.UInt32);
            UpdateCreditsCommand.Prepare();

            UpdatePrestigeCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdatePrestigeCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            UpdatePrestigeCommand.Parameters.Add("@Prestige", MySqlDbType.UInt32);
            UpdatePrestigeCommand.Prepare();

            //UpdatePurashedTabsCommand.Connection = GameDatabaseAccess.CharConnection;
            //UpdatePurashedTabsCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            //UpdatePurashedTabsCommand.Parameters.Add("@PurchasedTabs", MySqlDbType.UInt32);
            //UpdatePurashedTabsCommand.Prepare();
        }

        public static void AddLockboxInfo(uint clanId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                AddLockboxInfoCommand.Parameters["@ClanId"].Value = clanId;
                AddLockboxInfoCommand.ExecuteNonQuery();
            }
        }

        public static List<uint> GetLockboxInfo(uint clanId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var lockboxInfo = new List<uint>();

                GetLockboxInfoCommand.Parameters["@ClanId"].Value = clanId;

                using (var reader = GetLockboxInfoCommand.ExecuteReader())
                    while (reader.Read())
                    {
                        lockboxInfo.Add(reader.GetUInt32("credits"));
                        lockboxInfo.Add(reader.GetUInt32("prestige"));
                        lockboxInfo.Add(reader.GetUInt32("purchasedTabs"));
                    }

                return lockboxInfo;
            }
        }

        public static void UpdateCredits(uint clanId, uint credits)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCreditsCommand.Parameters["@ClanId"].Value = clanId;
                UpdateCreditsCommand.Parameters["@Credits"].Value = credits;
                UpdateCreditsCommand.ExecuteNonQuery();
            }
        }

        public static void UpdatePrestige(uint clanId, uint prestige)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdatePrestigeCommand.Parameters["@ClanId"].Value = clanId;
                UpdatePrestigeCommand.Parameters["@Prestige"].Value = prestige;
                UpdatePrestigeCommand.ExecuteNonQuery();
            }
        }

        //public static void UpdatePurashedTabs(uint accountId, uint purashedTabs)
        //{
        //    lock (GameDatabaseAccess.CharLock)
        //    {
        //        UpdatePurashedTabsCommand.Parameters["@AccountId"].Value = accountId;
        //        UpdatePurashedTabsCommand.Parameters["@PurashedTabs"].Value = purashedTabs;
        //        UpdatePurashedTabsCommand.ExecuteNonQuery();
        //    }
        //}
    }
}
