using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using System;
    using System.Collections.Generic;
    using Structures;

    public static class ClanTable
    {
        private static readonly MySqlCommand GetClanDataCommand = new MySqlCommand("SELECT * FROM `clan` WHERE `id` = (SELECT `clan_id` FROM `clan_member` WHERE `character_id` = @CharacterId)");
        private static readonly MySqlCommand GetClanByNameCommand = new MySqlCommand("SELECT * FROM `clan` WHERE `name` = @Name");
        private static readonly MySqlCommand GetDefaultClanRankTitlesCommand = new MySqlCommand("SELECT * FROM `clan_ranks`");
        private static readonly MySqlCommand GetAllClanMembersByClanIdCommand = new MySqlCommand("SELECT * FROM `clan_member` WHERE `clan_id` = @ClanId");
        private static readonly MySqlCommand InsertClanCommand = new MySqlCommand("INSERT INTO `clan` (`name`, `created_at`, `ispvp`, `rank_title_0`, `rank_title_1`, `rank_title_2`, `rank_title_3`) VALUES (@Name, @CreatedAt, @IsPvP, @RankTitle0, @RankTitle1, @RankTitle2, @RankTitle3);");
        private static readonly MySqlCommand InsertClanMemberCommand = new MySqlCommand("INSERT INTO `clan_member` (`clan_id`, `character_id`, `rank`, `note`) VALUES (@ClanId, @CharacterId, @Rank, @Note);");

        public static readonly uint MaxClanNameLength = 16;
        public static readonly uint ClankRankLeader = 3;

        public static void Initialize()
        {
            GetClanDataCommand.Connection = GameDatabaseAccess.CharConnection;
            GetClanDataCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetClanDataCommand.Prepare();

            GetDefaultClanRankTitlesCommand.Connection = GameDatabaseAccess.CharConnection;            
            GetDefaultClanRankTitlesCommand.Prepare();

            GetClanByNameCommand.Connection = GameDatabaseAccess.CharConnection;
            GetClanByNameCommand.Parameters.Add("@Name", MySqlDbType.VarChar);
            GetClanByNameCommand.Prepare();

            GetAllClanMembersByClanIdCommand.Connection = GameDatabaseAccess.CharConnection;
            GetAllClanMembersByClanIdCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            GetAllClanMembersByClanIdCommand.Prepare();

            InsertClanCommand.Connection = GameDatabaseAccess.CharConnection;
            InsertClanCommand.Parameters.Add("@Name", MySqlDbType.VarChar);
            InsertClanCommand.Parameters.Add("@CreatedAt", MySqlDbType.Timestamp);
            InsertClanCommand.Parameters.Add("@IsPvP", MySqlDbType.Byte);
            InsertClanCommand.Parameters.Add("@RankTitle0", MySqlDbType.VarChar);
            InsertClanCommand.Parameters.Add("@RankTitle1", MySqlDbType.VarChar);
            InsertClanCommand.Parameters.Add("@RankTitle2", MySqlDbType.VarChar);
            InsertClanCommand.Parameters.Add("@RankTitle3", MySqlDbType.VarChar);
            InsertClanCommand.Prepare();

            InsertClanMemberCommand.Connection = GameDatabaseAccess.CharConnection;
            InsertClanMemberCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            InsertClanMemberCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            InsertClanMemberCommand.Parameters.Add("@Rank", MySqlDbType.UInt32);
            InsertClanMemberCommand.Parameters.Add("@Note", MySqlDbType.VarChar);            
            InsertClanCommand.Prepare();
        }

        public static ClanEntry GetClanData(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetClanDataCommand.Parameters["@CharacterId"].Value = characterId;

                using (var reader = GetClanDataCommand.ExecuteReader())
                    return ClanEntry.Read(reader);
            }
        }

        public static bool ClanNameAlreadyExists(string clanName)
        {            
            lock (GameDatabaseAccess.CharLock)
            {
                GetClanByNameCommand.Parameters["@Name"].Value = clanName;

                ClanEntry existingClan = null;
                using (var reader = GetClanByNameCommand.ExecuteReader())
                    existingClan = ClanEntry.Read(reader);

                return existingClan != null;
            }
        }

        public static ClanEntry CreateClan(string clanName, bool isPvP)
        {
            ClanEntry newClanEntry = null;

            try
            {
                List<ClanRankEntry> defaultRankTitles = GetDefaultClanRanks();

                if(defaultRankTitles.Count != 4)
                {
                    throw new Exception($"Unexpected number of default clan ranks. Expecting 4, found {defaultRankTitles.Count}.");
                }

                lock (GameDatabaseAccess.CharLock)
                {
                    var createdAt = DateTime.UtcNow;
                    InsertClanCommand.Parameters["@Name"].Value = clanName;
                    InsertClanCommand.Parameters["@CreatedAt"].Value = createdAt;
                    InsertClanCommand.Parameters["@IsPvP"].Value = isPvP;
                    InsertClanCommand.Parameters["@RankTitle0"].Value = defaultRankTitles[0].Title;
                    InsertClanCommand.Parameters["@RankTitle1"].Value = defaultRankTitles[1].Title;
                    InsertClanCommand.Parameters["@RankTitle2"].Value = defaultRankTitles[2].Title;
                    InsertClanCommand.Parameters["@RankTitle3"].Value = defaultRankTitles[3].Title;
                    int result = InsertClanCommand.ExecuteNonQuery();

                    if (result > 0)
                    {
                        long id = InsertClanCommand.LastInsertedId;

                        newClanEntry = new ClanEntry
                        {
                            Id = (uint)id,
                            Name = clanName,
                            CreatedAt = createdAt,
                            IsPvP = isPvP,
                            RankTitle0 = defaultRankTitles[0].Title,
                            RankTitle1 = defaultRankTitles[1].Title,
                            RankTitle2 = defaultRankTitles[2].Title,
                            RankTitle3 = defaultRankTitles[3].Title,
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(LogType.Error, $"ClanTable.CreateClan(): {ex.Message}");
            }

            return newClanEntry;
        }

        public static bool UpdateRankTitleForClanId(uint clanId, uint rank, string title)
        {
            int result = 0;

            try
            {
                var queryString = $"UPDATE `clan` SET `rank_title_{rank}` = @Title WHERE `id` = @ClanId";
                var command = new MySqlCommand(queryString);
                command.Connection = GameDatabaseAccess.CharConnection;
                command.Parameters.Add("@Title", MySqlDbType.VarChar);
                command.Parameters.Add("@ClanId", MySqlDbType.UInt32);
                command.Parameters["@Title"].Value = title;
                command.Parameters["@ClanId"].Value = clanId;
                result = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(LogType.Error, $"ClanTable.UpdateRankTitleForClanId(): {ex.Message}");
            }

            return result > 0;
        }

        public static List<ClanMemberEntry> GetAllClanMembersForClanId(uint clanId)
        {
            var clanMembers = new List<ClanMemberEntry>();

            lock (GameDatabaseAccess.CharLock)
            {
                GetAllClanMembersByClanIdCommand.Parameters["@ClanId"].Value = clanId;

                using (var reader = GetAllClanMembersByClanIdCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var clanMember = ClanMemberEntry.Read(reader, false);

                        clanMembers.Add(clanMember);
                    }
                }
            }

            return clanMembers;
        }

        public static bool InsertClanMemberData(uint clanId, uint characterid, uint rank, string note)
        {
            int result = 0;

            try
            {
                lock (GameDatabaseAccess.CharLock)
                {
                    var createdAt = DateTime.UtcNow;
                    InsertClanMemberCommand.Parameters["@ClanId"].Value = clanId;
                    InsertClanMemberCommand.Parameters["@CharacterId"].Value = characterid;
                    InsertClanMemberCommand.Parameters["@Rank"].Value = rank;
                    InsertClanMemberCommand.Parameters["@Note"].Value = note;
                    result = InsertClanMemberCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(LogType.Error, $"ClanTable.InsertClanMemberData(): {ex.Message}");
            }

            return result > 0;
        }

        static List<ClanRankEntry> GetDefaultClanRanks()
        {
            var defaultClanRanks = new List<ClanRankEntry>();

            lock (GameDatabaseAccess.CharLock)
            {
                using (var reader = GetDefaultClanRankTitlesCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var clanRankEntry = ClanRankEntry.Read(reader, false);

                        defaultClanRanks.Add(clanRankEntry);
                    }
                }
            }

            return defaultClanRanks;
        }
    }
}
