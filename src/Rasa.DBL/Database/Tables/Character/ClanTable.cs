using System;
using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{    
    using Structures;

    public static class ClanTable
    {
        private static readonly MySqlCommand GetClanDataByCharacterIdCommand = new MySqlCommand("SELECT * FROM `clan` WHERE `id` = (SELECT `clan_id` FROM `clan_member` WHERE `character_id` = @CharacterId)");
        private static readonly MySqlCommand GetClanByNameCommand = new MySqlCommand("SELECT * FROM `clan` WHERE `name` = @Name");
        private static readonly MySqlCommand GetClanByIdCommand = new MySqlCommand("SELECT * FROM `clan` WHERE `id` = @ClanId");
        private static readonly MySqlCommand GetClansCommand = new MySqlCommand("SELECT * FROM `clan`");
        private static readonly MySqlCommand GetDefaultClanRankTitlesCommand = new MySqlCommand("SELECT * FROM `clan_ranks`");
        private static readonly MySqlCommand GetAllClanMembersByClanIdCommand = new MySqlCommand("SELECT * FROM `clan_member` WHERE `clan_id` = @ClanId");
        private static readonly MySqlCommand GetClanMemberByCharacterIdCommand = new MySqlCommand("SELECT * FROM `clan_member` WHERE `character_id` = @CharacterId");
        private static readonly MySqlCommand InsertClanCommand = new MySqlCommand("INSERT INTO `clan` (`name`, `created_at`, `ispvp`, `rank_title_0`, `rank_title_1`, `rank_title_2`, `rank_title_3`) VALUES (@Name, @CreatedAt, @IsPvP, @RankTitle0, @RankTitle1, @RankTitle2, @RankTitle3);");
        private static readonly MySqlCommand InsertClanMemberCommand = new MySqlCommand("INSERT INTO `clan_member` (`clan_id`, `character_id`, `rank`, `note`) VALUES (@ClanId, @CharacterId, @Rank, @Note);");
        private static readonly MySqlCommand DeleteClanMemberCommand = new MySqlCommand("DELETE FROM `clan_member` WHERE `character_id` = @CharacterId");
        private static readonly MySqlCommand DeleteClanMembersCommand = new MySqlCommand("DELETE FROM `clan_member` WHERE `clan_id` = @ClanId");
        private static readonly MySqlCommand DeleteClanCommand = new MySqlCommand("DELETE FROM `clan` WHERE `id` = @ClanId");
        private static readonly MySqlCommand UpdateLastPvPClanTimeForMembersCommand = new MySqlCommand("UPDATE `character` c INNER JOIN `clan_member` cm ON c.id = cm.character_id SET c.last_pvp_clan = @LastPvpClanTime WHERE cm.clan_id = @ClanId");
        private static readonly MySqlCommand UpdateClanMemberRankCommand = new MySqlCommand("UPDATE `clan_member` SET `rank` = @Rank WHERE `character_id` = @CharacterId");

        // Matches game client limits
        public static readonly uint MinClanNameLength = 3;
        public static readonly uint MaxClanNameLength = 20;
        public static readonly uint ClankRankLeader = 3;
        public static readonly int RequiredCreditsForClanCreation = 10000;

        // Arbitrary limit right now
        public static readonly uint MaxClanMembers = 100;

        public static void Initialize()
        {
            GetClanDataByCharacterIdCommand.Connection = GameDatabaseAccess.CharConnection;
            GetClanDataByCharacterIdCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetClanDataByCharacterIdCommand.Prepare();

            GetDefaultClanRankTitlesCommand.Connection = GameDatabaseAccess.CharConnection;            
            GetDefaultClanRankTitlesCommand.Prepare();

            GetClanByIdCommand.Connection = GameDatabaseAccess.CharConnection;
            GetClanByIdCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            GetClanByIdCommand.Prepare();

            GetClanByNameCommand.Connection = GameDatabaseAccess.CharConnection;
            GetClanByNameCommand.Parameters.Add("@Name", MySqlDbType.VarChar);
            GetClanByNameCommand.Prepare();

            GetAllClanMembersByClanIdCommand.Connection = GameDatabaseAccess.CharConnection;
            GetAllClanMembersByClanIdCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            GetAllClanMembersByClanIdCommand.Prepare();
            
            GetClanMemberByCharacterIdCommand.Connection = GameDatabaseAccess.CharConnection;
            GetClanMemberByCharacterIdCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetClanMemberByCharacterIdCommand.Prepare();

            GetClansCommand.Connection = GameDatabaseAccess.CharConnection;
            GetClansCommand.Prepare();

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

            DeleteClanMemberCommand.Connection = GameDatabaseAccess.CharConnection;
            DeleteClanMemberCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            DeleteClanMemberCommand.Prepare();

            DeleteClanMembersCommand.Connection = GameDatabaseAccess.CharConnection;
            DeleteClanMembersCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            DeleteClanMembersCommand.Prepare();

            DeleteClanCommand.Connection = GameDatabaseAccess.CharConnection;
            DeleteClanCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            DeleteClanCommand.Prepare();

            UpdateLastPvPClanTimeForMembersCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateLastPvPClanTimeForMembersCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            UpdateLastPvPClanTimeForMembersCommand.Parameters.Add("@LastPvpClanTime", MySqlDbType.Timestamp);
            UpdateLastPvPClanTimeForMembersCommand.Prepare();
           
            UpdateClanMemberRankCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateClanMemberRankCommand.Parameters.Add("@Rank", MySqlDbType.UInt32);
            UpdateClanMemberRankCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            UpdateClanMemberRankCommand.Prepare();
        }

        public static List<ClanEntry> GetClans()
        {
            var clans = new List<ClanEntry>();

            lock (GameDatabaseAccess.CharLock)
            {
                using (var reader = GetClansCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var clan = ClanEntry.Read(reader, false);

                        clans.Add(clan);
                    }
                }
            }

            return clans;
        }

        public static ClanEntry GetClanByCharacterId(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetClanDataByCharacterIdCommand.Parameters["@CharacterId"].Value = characterId;

                using (var reader = GetClanDataByCharacterIdCommand.ExecuteReader())
                    return ClanEntry.Read(reader);
            }
        }

        public static ClanEntry GetClanById(uint clanId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetClanByIdCommand.Parameters["@ClanId"].Value = clanId;

                using (var reader = GetClanByIdCommand.ExecuteReader())
                    return ClanEntry.Read(reader);
            }
        }

        public static ClanEntry GetClanByName(string clanName)
        {            
            lock (GameDatabaseAccess.CharLock)
            {
                GetClanByNameCommand.Parameters["@Name"].Value = clanName;

                ClanEntry clan = null;
                using (var reader = GetClanByNameCommand.ExecuteReader())
                    clan = ClanEntry.Read(reader);

                return clan;
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
                Logger.WriteLog(LogType.Error, $"ClanTable.{nameof(CreateClan)}(): {ex.Message}");
            }

            return newClanEntry;
        }

        public static bool UpdateRankTitleByClanId(uint clanId, uint rank, string title)
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
                Logger.WriteLog(LogType.Error, $"ClanTable.{nameof(UpdateRankTitleByClanId)}(): {ex.Message}");
            }

            return result > 0;
        }

        public static void UpdateRankByCharacterId(uint rank, uint characterId)
        {
            try
            {
                UpdateClanMemberRankCommand.Parameters["@Rank"].Value = rank;
                UpdateClanMemberRankCommand.Parameters["@CharacterId"].Value = characterId;
                UpdateClanMemberRankCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(LogType.Error, $"ClanTable.{nameof(UpdateRankByCharacterId)}(): {ex.Message}");
            }
        }

        public static List<ClanMemberEntry> GetAllClanMembersByClanId(uint clanId)
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

        public static ClanMemberEntry GetClanMemberByCharacterId(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetClanMemberByCharacterIdCommand.Parameters["@CharacterId"].Value = characterId;

                using (var reader = GetClanMemberByCharacterIdCommand.ExecuteReader())
                    return ClanMemberEntry.Read(reader);
            }
        }

        public static bool InsertClanMemberData(uint clanId, ulong characterid, uint rank, string note)
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
                Logger.WriteLog(LogType.Error, $"ClanTable.{nameof(InsertClanMemberData)}(): {ex.Message}");
            }

            return result > 0;
        }

        public static bool DeleteClanMember(ClanMemberEntry member)
        {
            int result = 0;

            try
            {
                DeleteClanMemberCommand.Parameters["@CharacterId"].Value = member.CharacterId;
                result = DeleteClanMemberCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(LogType.Error, $"ClanTable.{nameof(DeleteClanMember)}(): {ex.Message}");
            }

            return result > 0;
        }

        public static void UpdateLastPvPClanTimeForMembers(uint clanId, DateTime lastPvPClanTimestamp)
        {
            try
            {
                UpdateLastPvPClanTimeForMembersCommand.Parameters["@ClanId"].Value = clanId;
                UpdateLastPvPClanTimeForMembersCommand.Parameters["@LastPvpClanTime"].Value = lastPvPClanTimestamp;
                UpdateLastPvPClanTimeForMembersCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(LogType.Error, $"ClanTable.{nameof(UpdateLastPvPClanTimeForMembers)}(): {ex.Message}");
            }
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

        public static bool DeleteClan(uint clanId)
        {
            int result = 0;

            try
            {
                DeleteClanCommand.Parameters["@ClanId"].Value = clanId;
                result = DeleteClanCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(LogType.Error, $"ClanTable.{nameof(DeleteClan)}(): {ex.Message}");
            }

            return result > 0;
        }

        public static bool DeleteClanMembers(uint clanId)
        {
            int result = 0;

            try
            {
                DeleteClanMembersCommand.Parameters["@ClanId"].Value = clanId;
                result = DeleteClanMembersCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(LogType.Error, $"ClanTable.{nameof(DeleteClanMembers)}(): {ex.Message}");
            }

            return result > 0;
        }
    }
}
