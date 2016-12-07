using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;
    public static class CharacterTable
    {
        private static readonly MySqlCommand CreateCharacterCommand = new MySqlCommand("INSERT INTO characters" +
                                                                                       "(name, familyName, accountId, slotId, gender, scale, raceId, classId, mapContextId, posX, posY, posZ, rotation) VALUES" +
                                                                                       "(@Name, @FamilyName, @AccountId, @SlotId, @Gender, @Scale, @RaceId, @ClassId, @MapContextId, @PosX, @PosY, @PosZ, @Rotation)");
        private static readonly MySqlCommand GetCharacterDataCommand = new MySqlCommand("SELECT id, name, familyName, slotId, gender, scale, raceId, classId, mapContextId, experience, level, body, mind, spirit, cloneCredits, " +
                                                                                        "numLogins, totalTimePlayed, timeSinceLastPlayed FROM characters WHERE accountId = @AccountId AND slotId = @SlotId");
        private static readonly MySqlCommand GetCharacterNameCommand = new MySqlCommand("SELECT name FROM characters WHERE accountId = @AccountID AND slotId = @SlotId");
        private static readonly MySqlCommand DeleteCharacterCommand = new MySqlCommand("DELETE characters, character_abilities, character_equipment, character_inventory, character_skills FROM characters " +
                                                                                       "INNER JOIN character_abilities ON characters.id = character_abilities.id " +
                                                                                       "INNER JOIN character_equipment ON characters.id = character_equipment.id " +
                                                                                       "INNER JOIN character_inventory ON characters.id = character_inventory.id " +
                                                                                       "INNER JOIN character_skills ON characters.id = character_skills.id " +
                                                                                       "WHERE accountId = @AccountId AND slotId = @SlotId");
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
            CreateCharacterCommand.Parameters.Add("@MapContextId", MySqlDbType.Int32);
            CreateCharacterCommand.Parameters.Add("@PosX", MySqlDbType.Double);
            CreateCharacterCommand.Parameters.Add("@PosY", MySqlDbType.Double);
            CreateCharacterCommand.Parameters.Add("@PosZ", MySqlDbType.Double);
            CreateCharacterCommand.Parameters.Add("@Rotation", MySqlDbType.Double);
            CreateCharacterCommand.Prepare();

            DeleteCharacterCommand.Connection = GameDatabaseAccess.CharConnection;
            DeleteCharacterCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            DeleteCharacterCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            DeleteCharacterCommand.Prepare();

            GetCharacterDataCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterDataCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetCharacterDataCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            GetCharacterDataCommand.Prepare();

            GetCharacterNameCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterNameCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetCharacterNameCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            GetCharacterNameCommand.Prepare();

            IsNameAvailableCommand.Connection = GameDatabaseAccess.CharConnection;
            IsNameAvailableCommand.Parameters.Add("@Name", MySqlDbType.VarChar);
            IsNameAvailableCommand.Prepare();

            IsSlotAvailableCommand.Connection = GameDatabaseAccess.CharConnection;
            IsSlotAvailableCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            IsSlotAvailableCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            IsSlotAvailableCommand.Prepare();
        }

        public static void DeleteCharacter(ulong accountId, int slotId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                DeleteCharacterCommand.Parameters["@AccountId"].Value = accountId;
                DeleteCharacterCommand.Parameters["@SlotId"].Value = slotId;
                DeleteCharacterCommand.ExecuteNonQuery();
               
            }
        }

        public static int CreateCharacter(ulong accountId, string name, string familyName, int slotId, int gender, double scale, int raceId)
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
                CreateCharacterCommand.Parameters["@PosX"].Value = 894.9;
                CreateCharacterCommand.Parameters["@PosY"].Value = 307.9;
                CreateCharacterCommand.Parameters["@PosZ"].Value = 347.1;
                CreateCharacterCommand.Parameters["@Rotation"].Value = 1;
                CreateCharacterCommand.ExecuteNonQuery();
                return (int)CreateCharacterCommand.LastInsertedId;
            }
        }

        public static CharacterEntry GetCharacterData(uint accountId, int slotId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterDataCommand.Parameters["@AccountId"].Value = accountId;
                GetCharacterDataCommand.Parameters["@SlotId"].Value = slotId;

                using (var reader = GetCharacterDataCommand.ExecuteReader())
                    return CharacterEntry.GetData(reader);
            }
        }

        public static string GetCharacterName(uint accountId, int slotId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetCharacterNameCommand.Parameters["@AccountId"].Value = accountId;
                GetCharacterNameCommand.Parameters["@SlotId"].Value = slotId;

                using (var reader = GetCharacterNameCommand.ExecuteReader())
                    if (reader.Read())
                        return reader.GetString("name");
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
    }
}
