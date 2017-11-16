using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public static class CharacterTable
    {
        private static readonly MySqlCommand CreateCharacterCommand = new MySqlCommand("INSERT INTO characters" +
                                                                                       "(name, familyName, accountId, slotId, gender, scale, raceId, classId, gameContextId, posX, posY, posZ, rotation, level, logos) VALUES" +
                                                                                       "(@Name, @FamilyName, @AccountId, @SlotId, @Gender, @Scale, @RaceId, @ClassId, @GameContextId, @PosX, @PosY, @PosZ, @Rotation, @Level, @Logos)");
        private static readonly MySqlCommand DeleteCharacterCommand = new MySqlCommand("DELETE FROM characters WHERE accountId = @AccountId AND slotId = @SlotId");
        private static readonly MySqlCommand GetCharacterCountCommand = new MySqlCommand("SELECT COUNT(*) FROM characters WHERE accountId = @AccountId;");
        private static readonly MySqlCommand GetCharacterDataCommand = new MySqlCommand("SELECT characterId, name, familyName, accountId, slotId, gender, scale, raceId, classId, gameContextId, posX, posY, posZ, rotation, experience, level, body, mind, spirit, cloneCredits, " +
                                                                                        "numLogins, totalTimePlayed, TIMESTAMPDIFF(MINUTE , timeSinceLastPlayed, NOW()) AS timeSinceLastPlayed, clanId, clanName, credits, prestige, currentAbilityDrawer, logos FROM characters WHERE accountId = @AccountId AND slotId = @SlotId");
        private static readonly MySqlCommand GetCharacterFamilyCommand = new MySqlCommand("SELECT familyName FROM characters WHERE accountId = @AccountId");
        private static readonly MySqlCommand IsFamilyNameAvailableCommand = new MySqlCommand("SELECT familyName FROM characters WHERE familyName = @FamilyName");
        private static readonly MySqlCommand IsNameAvailableCommand = new MySqlCommand("SELECT name FROM characters WHERE name = @Name");
        private static readonly MySqlCommand IsSlotAvailableCommand = new MySqlCommand("SELECT slotId FROM characters WHERE accountId = @AccountId AND slotId = @SlotId");

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
            CreateCharacterCommand.Parameters.Add("@GameContextId", MySqlDbType.Int32);
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
        }

        public static uint CreateCharacter(uint accountId, string name, string familyName, uint slotId, int gender, double scale, int raceId, int classId, int gameContextId, double posX, double posY, double posZ, double rotation, int level, string logos)
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
                CreateCharacterCommand.Parameters["@ClassId"].Value = classId;
                CreateCharacterCommand.Parameters["@GameContextId"].Value = gameContextId;
                CreateCharacterCommand.Parameters["@PosX"].Value = posX;
                CreateCharacterCommand.Parameters["@PosY"].Value = posY;
                CreateCharacterCommand.Parameters["@PosZ"].Value = posZ;
                CreateCharacterCommand.Parameters["@Rotation"].Value = rotation;
                CreateCharacterCommand.Parameters["@Level"].Value = level;
                CreateCharacterCommand.Parameters["@Logos"].Value = logos;
                CreateCharacterCommand.ExecuteNonQuery();
                return (uint)CreateCharacterCommand.LastInsertedId;
            }
        }

        public static void DeleteCharacter(uint accountId, uint slotId)
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
                return (int)(long)GetCharacterCountCommand.ExecuteScalar();
            }
        }

        public static CharacterEntry GetCharacterData(uint accountId, uint slotId)
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

        public static int IsSlotAvailable(uint accountId, uint slotId)
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
    }
}
