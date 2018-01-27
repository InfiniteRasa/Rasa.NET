using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public static class CharacterTable
    {
        private static readonly MySqlCommand CreateCharacterCommand = new MySqlCommand(
            "INSERT INTO characters" +
            "(accountId, characterSlot, name, familyName, gender, scale, raceId, classId, mapContextId, posX, posY, posZ, rotation, level, logos) VALUES" +
            "(@AccountId, @CharacterSlot, @Name, @FamilyName, @Gender, @Scale, @RaceId, @ClassId, @MapContextId, @PosX, @PosY, @PosZ, @Rotation, @Level, @Logos)"
            );
        private static readonly MySqlCommand DeleteCharacterCommand = new MySqlCommand(
            "DELETE FROM characters WHERE accountId = @AccountId AND characterSlot = @CharacterSlot; "+
            "DELETE FROM character_abilitydrawer WHERE accountId = @AccountId AND characterSlot = @CharacterSlot; "+
            "DELETE FROM character_appearance WHERE accountId = @AccountId AND characterSlot = @CharacterSlot; "+
            "DELETE FROM character_inventory WHERE accountId = @AccountId AND characterSlot = @CharacterSlot; "+
            "DELETE FROM character_missions WHERE accountId = @AccountId AND characterSlot = @CharacterSlot; "+
            "DELETE FROM character_logos WHERE accountId = @AccountId AND characterSlot = @CharacterSlot; "+
            "DELETE FROM character_skills WHERE accountId = @AccountId AND characterSlot = @CharacterSlot; " +
            "DELETE FROM character_titles WHERE accountId = @AccountId AND characterSlot = @CharacterSlot;"
            );
        private static readonly MySqlCommand GetCharacterCountCommand = new MySqlCommand("SELECT COUNT(*) FROM characters WHERE accountId = @AccountId;");
        private static readonly MySqlCommand GetCharacterDataCommand = new MySqlCommand("SELECT name, familyName, accountId, gender, scale, raceId, classId, mapContextId, posX, posY, posZ, rotation, experience, level, body, mind, spirit, cloneCredits, " +
                                                                                        "numLogins, totalTimePlayed, TIMESTAMPDIFF(MINUTE , timeSinceLastPlayed, NOW()) AS timeSinceLastPlayed, clanId, clanName, credits, prestige, currentAbilityDrawer, logos FROM characters WHERE accountId = @AccountId AND characterSlot = @CharacterSlot");
        private static readonly MySqlCommand GetCharacterFamilyCommand = new MySqlCommand("SELECT familyName FROM characters WHERE accountId = @AccountId");
        private static readonly MySqlCommand IsFamilyNameAvailableCommand = new MySqlCommand("SELECT familyName FROM characters WHERE familyName = @FamilyName");
        private static readonly MySqlCommand IsNameAvailableCommand = new MySqlCommand("SELECT name FROM characters WHERE name = @Name");
        private static readonly MySqlCommand IsSlotAvailableCommand = new MySqlCommand("SELECT * FROM characters WHERE accountId = @AccountId AND characterSlot = @CharacterSlot");
        private static readonly MySqlCommand UpdateCharacterCreditsCommand = new MySqlCommand("UPDATE characters SET credits = @Credits WHERE accountId = @AccountId AND characterSlot = @CharacterSlot");
        private static readonly MySqlCommand UpdateCharacterLoginCommand = new MySqlCommand("UPDATE characters SET numLogins = numLogins + 1, totalTimePlayed = totalTimePlayed + @Value, timeSinceLastPlayed = NOW() WHERE accountId = @AccountId AND characterSlot = @CharacterSlot");
        private static readonly MySqlCommand UpdateCharacterPosCommand = new MySqlCommand("UPDATE characters SET posX = @PosX, posY = @PosY, posZ = @PosZ, rotation = @Rotation, mapContextId = @MapContextId WHERE accountId = @AccountId AND characterSlot = @CharacterSlot");
        
        public static void Initialize()
        {
            CreateCharacterCommand.Connection = GameDatabaseAccess.CharConnection;
            CreateCharacterCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            CreateCharacterCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            CreateCharacterCommand.Parameters.Add("@Name", MySqlDbType.VarChar);
            CreateCharacterCommand.Parameters.Add("@FamilyName", MySqlDbType.VarChar);
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
            DeleteCharacterCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            DeleteCharacterCommand.Prepare();

            GetCharacterCountCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterCountCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetCharacterCountCommand.Prepare();

            GetCharacterDataCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterDataCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetCharacterDataCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            GetCharacterDataCommand.Prepare();

            GetCharacterFamilyCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterFamilyCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetCharacterFamilyCommand.Prepare();

            IsFamilyNameAvailableCommand.Connection = GameDatabaseAccess.CharConnection;
            IsFamilyNameAvailableCommand.Parameters.Add("@FamilyName", MySqlDbType.VarChar);
            IsFamilyNameAvailableCommand.Prepare();

            IsNameAvailableCommand.Connection = GameDatabaseAccess.CharConnection;
            IsNameAvailableCommand.Parameters.Add("@Name", MySqlDbType.VarChar);
            IsNameAvailableCommand.Prepare();

            IsSlotAvailableCommand.Connection = GameDatabaseAccess.CharConnection;
            IsSlotAvailableCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            IsSlotAvailableCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            IsSlotAvailableCommand.Prepare();

            UpdateCharacterCreditsCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterCreditsCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            UpdateCharacterCreditsCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            UpdateCharacterCreditsCommand.Parameters.Add("@Credits", MySqlDbType.Int32);
            UpdateCharacterCreditsCommand.Prepare();

            UpdateCharacterLoginCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterLoginCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            UpdateCharacterLoginCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            UpdateCharacterLoginCommand.Parameters.Add("@Value", MySqlDbType.Int32);
            UpdateCharacterLoginCommand.Prepare();

            UpdateCharacterPosCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateCharacterPosCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            UpdateCharacterPosCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            UpdateCharacterPosCommand.Parameters.Add("@PosX", MySqlDbType.Double);
            UpdateCharacterPosCommand.Parameters.Add("@PosY", MySqlDbType.Double);
            UpdateCharacterPosCommand.Parameters.Add("@PosZ", MySqlDbType.Double);
            UpdateCharacterPosCommand.Parameters.Add("@Rotation", MySqlDbType.Double);
            UpdateCharacterPosCommand.Parameters.Add("@MapContextId", MySqlDbType.Int32);
            UpdateCharacterPosCommand.Prepare();
        }

        public static void CreateCharacter(uint accountId, uint characterSlot, string name, string familyName, int gender, double scale, int raceId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                CreateCharacterCommand.Parameters["@AccountId"].Value = accountId;
                CreateCharacterCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                CreateCharacterCommand.Parameters["@Name"].Value = name;
                CreateCharacterCommand.Parameters["@FamilyName"].Value = familyName;
                CreateCharacterCommand.Parameters["@Gender"].Value = gender;
                CreateCharacterCommand.Parameters["@Scale"].Value = scale;
                CreateCharacterCommand.Parameters["@RaceId"].Value = raceId;
                // ToDo maybe get data from elsewhere, insted of hardcore input
                CreateCharacterCommand.Parameters["@ClassId"].Value = 1;    // recruit
                CreateCharacterCommand.Parameters["@MapContextId"].Value = 1220;
                CreateCharacterCommand.Parameters["@PosX"].Value = 841;
                CreateCharacterCommand.Parameters["@PosY"].Value = 377;
                CreateCharacterCommand.Parameters["@PosZ"].Value = 293;
                CreateCharacterCommand.Parameters["@Rotation"].Value = 1;
                CreateCharacterCommand.Parameters["@Level"].Value = 1;
                CreateCharacterCommand.Parameters["@Logos"].Value = "00000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                                                                    "00000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                                                                    "00000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                                                                    "00000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                                                                    "00000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                                                                    "0000000000";   // 410 logos
                CreateCharacterCommand.ExecuteNonQuery();
            }
        }

        public static void DeleteCharacter(uint accountId, uint characterSlot)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                DeleteCharacterCommand.Parameters["@AccountId"].Value = accountId;
                DeleteCharacterCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                DeleteCharacterCommand.ExecuteNonQuery();
            }
        }

        public static long GetCharacterCount(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterCountCommand.Parameters["@AccountId"].Value = accountId;
                return (long)GetCharacterCountCommand.ExecuteScalar();
            }
        }

        public static CharacterEntry GetCharacterData(uint accountId, uint characterSlot)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterDataCommand.Parameters["@AccountId"].Value = accountId;
                GetCharacterDataCommand.Parameters["@CharacterSlot"].Value = characterSlot;

                using (var reader = GetCharacterDataCommand.ExecuteReader())
                    return CharacterEntry.Read(reader);
            }
        }

        public static string GetCharacterFamily(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterFamilyCommand.Parameters["@AccountId"].Value = accountId;
                using (var reader = GetCharacterFamilyCommand.ExecuteReader())
                    if (reader.Read())
                        return reader.GetString("familyName");

                return "";
            }
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

        public static bool IsSlotAvailable(uint accountId, uint characterSlot)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                IsSlotAvailableCommand.Parameters["@AccountId"].Value = accountId;
                IsSlotAvailableCommand.Parameters["@CharacterSlot"].Value = characterSlot;

                using (var reader = IsSlotAvailableCommand.ExecuteReader())
                    if (reader.Read())
                        return false;
            }

            return true;
        }

        public static void UpdateCharacterCredits(uint accountId, uint characterSlot, int credits)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterCreditsCommand.Parameters["@AccountId"].Value = accountId;
                UpdateCharacterCreditsCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                UpdateCharacterCreditsCommand.Parameters["@Credits"].Value = credits;
                UpdateCharacterCreditsCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterLogin(uint accountId, uint characterSlot, int value)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterLoginCommand.Parameters["@AccountId"].Value = accountId;
                UpdateCharacterLoginCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                UpdateCharacterLoginCommand.Parameters["@Value"].Value = value;
                UpdateCharacterLoginCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterPos(uint accountId, uint characterSlot, double posX, double posY, double posZ, double rotation, int mapContextId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateCharacterPosCommand.Parameters["@AccountId"].Value = accountId;
                UpdateCharacterPosCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                UpdateCharacterPosCommand.Parameters["@PosX"].Value = posX;
                UpdateCharacterPosCommand.Parameters["@PosY"].Value = posY;
                UpdateCharacterPosCommand.Parameters["@PosZ"].Value = posZ;
                UpdateCharacterPosCommand.Parameters["@Rotation"].Value = rotation;
                UpdateCharacterPosCommand.Parameters["@MapContextId"].Value = mapContextId;
                UpdateCharacterPosCommand.ExecuteNonQuery();
            }
        }
    }
}
