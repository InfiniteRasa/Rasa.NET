using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public static class CharacterTable
    {
        private static readonly MySqlCommand CreateCharacterCommand = new MySqlCommand("INSERT INTO characters" +
                                                                                       "(name, familyName, accountId, slotId, gender, scale, raceId, classId, mapContextId, posX, posY, posZ, rotation, level, logos) VALUES" +
                                                                                       "(@Name, @FamilyName, @AccountId, @SlotId, @Gender, @Scale, @RaceId, @ClassId, @MapContextId, @PosX, @PosY, @PosZ, @Rotation, @Level, @Logos)");
        private static readonly MySqlCommand DeleteCharacterCommand = new MySqlCommand("DELETE FROM characters WHERE accountId = @AccountId AND slotId = @SlotId");
        private static readonly MySqlCommand GetCharacterCountCommand = new MySqlCommand("SELECT COUNT(*) FROM characters WHERE accountId = @AccountId;");
        private static readonly MySqlCommand GetCharacterDataCommand = new MySqlCommand("SELECT characterId, name, familyName, accountId, slotId, gender, scale, raceId, classId, mapContextId, posX, posY, posZ, rotation, experience, level, body, mind, spirit, cloneCredits, " +
                                                                                        "numLogins, totalTimePlayed, TIMESTAMPDIFF(MINUTE , timeSinceLastPlayed, NOW()) AS timeSinceLastPlayed, clanId, clanName, credits, prestige, currentAbilityDrawer, logos FROM characters WHERE accountId = @AccountId AND slotId = @SlotId");
        private static readonly MySqlCommand GetCharacterFamilyCommand = new MySqlCommand("SELECT COUNT(*), familyName FROM characters WHERE accountId = @AccountId");
        private static readonly MySqlCommand GetCharacterSkillsCommand = new MySqlCommand("SELECT skills FROM characters WHERE characterId = @CharacterId");
        private static readonly MySqlCommand IsFamilyNameAvailableCommand = new MySqlCommand("SELECT familyName FROM characters WHERE familyName = @FamilyName");
        private static readonly MySqlCommand IsNameAvailableCommand = new MySqlCommand("SELECT name FROM characters WHERE name = @Name");
        private static readonly MySqlCommand IsSlotAvailableCommand = new MySqlCommand("SELECT slotId FROM characters WHERE accountId = @AccountId AND slotId = @SlotId");
        private static readonly MySqlCommand UpdateCharacterLoginCommand = new MySqlCommand("UPDATE characters SET numLogins = numLogins + 1, totalTimePlayed = totalTimePlayed + @Value, timeSinceLastPlayed = NOW() WHERE characterId = @CharacterId");
        private static readonly MySqlCommand UpdateCharacterPosCommand = new MySqlCommand("UPDATE characters SET posX = @PosX, posY = @PosY, posZ = @PosZ, rotation = @Rotation, mapContextId  =@MapContextId WHERE characterId = @CharacterId");
        private static readonly MySqlCommand UpdateCharacterSkillsCommand = new MySqlCommand("UPDATE characters SET skills = @Skills WHERE characterId = @CharacterId");

        public static void Initialize()
        {
            CreateCharacterCommand.Connection = GameDatabaseAccess.CharConnection;
            CreateCharacterCommand.Parameters.Add("@Name", MySqlDbType.VarChar);
            CreateCharacterCommand.Parameters.Add("@FamilyName", MySqlDbType.VarChar);
            CreateCharacterCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            CreateCharacterCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            CreateCharacterCommand.Parameters.Add("@Gender", MySqlDbType.Int32);
            CreateCharacterCommand.Parameters.Add("@Scale", MySqlDbType.Double);
            CreateCharacterCommand.Parameters.Add("@RaceId", MySqlDbType.Int32);
            CreateCharacterCommand.Parameters.Add("@ClassId", MySqlDbType.Int32);
            CreateCharacterCommand.Parameters.Add("@MapContextId", MySqlDbType.Int32);
            CreateCharacterCommand.Parameters.Add("@PosX", MySqlDbType.Double);
            CreateCharacterCommand.Parameters.Add("@PosY", MySqlDbType.Double);
            CreateCharacterCommand.Parameters.Add("@PosZ", MySqlDbType.Double);
            CreateCharacterCommand.Parameters.Add("@Rotation", MySqlDbType.Double);
            CreateCharacterCommand.Parameters.Add("@Level", MySqlDbType.Int32);
            CreateCharacterCommand.Parameters.Add("@Logos", MySqlDbType.VarChar);
            CreateCharacterCommand.Prepare();

            DeleteCharacterCommand.Connection = GameDatabaseAccess.CharConnection;
            DeleteCharacterCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            DeleteCharacterCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            DeleteCharacterCommand.Prepare();

            GetCharacterCountCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterCountCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetCharacterCountCommand.Prepare();

            GetCharacterDataCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterDataCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetCharacterDataCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            GetCharacterDataCommand.Prepare();

            GetCharacterFamilyCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterFamilyCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetCharacterFamilyCommand.Prepare();

            GetCharacterSkillsCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterSkillsCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetCharacterSkillsCommand.Prepare();

            IsFamilyNameAvailableCommand.Connection = GameDatabaseAccess.CharConnection;
            IsFamilyNameAvailableCommand.Parameters.Add("@FamilyName", MySqlDbType.VarChar);
            IsFamilyNameAvailableCommand.Prepare();

            IsNameAvailableCommand.Connection = GameDatabaseAccess.CharConnection;
            IsNameAvailableCommand.Parameters.Add("@Name", MySqlDbType.VarChar);
            IsNameAvailableCommand.Prepare();

            IsSlotAvailableCommand.Connection = GameDatabaseAccess.CharConnection;
            IsSlotAvailableCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            IsSlotAvailableCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            IsSlotAvailableCommand.Prepare();

            UpdateCharacterLoginCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterLoginCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            UpdateCharacterLoginCommand.Parameters.Add("@Value", MySqlDbType.Int32);
            UpdateCharacterLoginCommand.Prepare();

            UpdateCharacterPosCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterPosCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            UpdateCharacterPosCommand.Parameters.Add("@PosX", MySqlDbType.Double);
            UpdateCharacterPosCommand.Parameters.Add("@PosY", MySqlDbType.Double);
            UpdateCharacterPosCommand.Parameters.Add("@PosZ", MySqlDbType.Double);
            UpdateCharacterPosCommand.Parameters.Add("@Rotation", MySqlDbType.Double);
            UpdateCharacterPosCommand.Parameters.Add("@MapContextId", MySqlDbType.Int32);
            UpdateCharacterPosCommand.Prepare();

            UpdateCharacterSkillsCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterSkillsCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            UpdateCharacterSkillsCommand.Parameters.Add("@Skills", MySqlDbType.VarChar);
            UpdateCharacterSkillsCommand.Prepare();
        }
        
        public static uint CreateCharacter(uint accountId, string name, string familyName, int slotId, int gender, double scale, int raceId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                CreateCharacterCommand.Parameters["@Name"].Value = name;
                CreateCharacterCommand.Parameters["@FamilyName"].Value = familyName;
                CreateCharacterCommand.Parameters["@AccountId"].Value = accountId;
                CreateCharacterCommand.Parameters["@SlotId"].Value = slotId;
                CreateCharacterCommand.Parameters["@Gender"].Value = gender;
                CreateCharacterCommand.Parameters["@Scale"].Value = scale;
                CreateCharacterCommand.Parameters["@RaceId"].Value = raceId;
                // ToDo maybe get data from elsewhere, insted of hardcore input
                CreateCharacterCommand.Parameters["@ClassId"].Value = 1;    // recruit
                CreateCharacterCommand.Parameters["@MapContextId"].Value = 1220;
                CreateCharacterCommand.Parameters["@PosX"].Value = 894;
                CreateCharacterCommand.Parameters["@PosY"].Value = 347;
                CreateCharacterCommand.Parameters["@PosZ"].Value = 306;
                CreateCharacterCommand.Parameters["@Rotation"].Value = 1;
                CreateCharacterCommand.Parameters["@Level"].Value = 1;
                CreateCharacterCommand.Parameters["@Logos"].Value = "00000000000000000000000000000000000000000000000000000000000000000000000000000000"+
                                                                    "00000000000000000000000000000000000000000000000000000000000000000000000000000000"+
                                                                    "00000000000000000000000000000000000000000000000000000000000000000000000000000000"+
                                                                    "00000000000000000000000000000000000000000000000000000000000000000000000000000000"+
                                                                    "00000000000000000000000000000000000000000000000000000000000000000000000000000000"+
                                                                    "0000000000";   // 410 logos
                CreateCharacterCommand.ExecuteNonQuery();
                return (uint)CreateCharacterCommand.LastInsertedId;
            }
        }

        public static void DeleteCharacter(uint accountId, int slotId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                DeleteCharacterCommand.Parameters["@AccountId"].Value = accountId;
                DeleteCharacterCommand.Parameters["@SlotId"].Value = slotId;
                DeleteCharacterCommand.ExecuteNonQuery();
            }
        }

        public static int GetCharacterCount(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterCountCommand.Parameters["@AccountId"].Value = accountId;
                return (int)(long) GetCharacterCountCommand.ExecuteScalar();
            }
        }

        public static CharacterEntry GetCharacterData(uint accountId, int slotId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterDataCommand.Parameters["@AccountId"].Value = accountId;
                GetCharacterDataCommand.Parameters["@SlotId"].Value = slotId;

                using (var reader = GetCharacterDataCommand.ExecuteReader())
                    return CharacterEntry.Read(reader);
            }
        }

        public static string GetCharacterFamily(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterFamilyCommand.Parameters["@AccountId"].Value = accountId;
                if ((int)(long)GetCharacterFamilyCommand.ExecuteScalar() != 0)
                {
                    using (var reader = GetCharacterFamilyCommand.ExecuteReader())
                        if (reader.Read())
                            return reader.GetString("familyName");
                }

                return "";
            }
        }

        public static string GetCharacterSkills(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterSkillsCommand.Parameters["@CharacterId"].Value = characterId;
                using (var reader = GetCharacterSkillsCommand.ExecuteReader())
                    if (reader.Read())
                        return reader.GetString("skills");
            }
            return "";
        }

        public static string IsFamilyNameAvailable(string familyName)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                IsFamilyNameAvailableCommand.Parameters["@FamilyName"].Value = familyName;

                using (var reader = IsFamilyNameAvailableCommand.ExecuteReader())
                    if (reader.Read())
                        return reader.GetString("familyName");
            }

            return null;
        }

        public static string IsNameAvailable(string name)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                IsNameAvailableCommand.Parameters["@Name"].Value = name;

                using (var reader = IsNameAvailableCommand.ExecuteReader())
                    if (reader.Read())
                        return reader.GetString("name");
            }

            return null;
        }

        public static int IsSlotAvailable(uint accountId, int slotId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                IsSlotAvailableCommand.Parameters["@AccountId"].Value = accountId;
                IsSlotAvailableCommand.Parameters["@SlotId"].Value = slotId;

                using (var reader = IsSlotAvailableCommand.ExecuteReader())
                    if (reader.Read())
                        return reader.GetInt32("slotId");
            }

            return 0;
        }

        public static void UpdateCharacterLogin(uint characterId, int value)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterLoginCommand.Parameters["@CharacterId"].Value = characterId;
                UpdateCharacterLoginCommand.Parameters["@Value"].Value = value;
                UpdateCharacterLoginCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterPos(uint characterId, double posX, double posY, double posZ, double rotation, int mapContextId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterPosCommand.Parameters["@CharacterId"].Value = characterId;
                UpdateCharacterPosCommand.Parameters["@PosX"].Value = posX;
                UpdateCharacterPosCommand.Parameters["@PosY"].Value = posY;
                UpdateCharacterPosCommand.Parameters["@PosZ"].Value = posZ;                
                UpdateCharacterPosCommand.Parameters["@Rotation"].Value = rotation;
                UpdateCharacterPosCommand.Parameters["@MapContextId"].Value = mapContextId;
                UpdateCharacterPosCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterSkills(uint characterId, string skills)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterSkillsCommand.Parameters["@CharacterId"].Value = characterId;
                UpdateCharacterSkillsCommand.Parameters["@Skills"].Value = skills;
                UpdateCharacterSkillsCommand.ExecuteNonQuery();
            }
        }
    }
}
