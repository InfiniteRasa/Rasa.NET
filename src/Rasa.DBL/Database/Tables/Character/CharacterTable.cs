using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;
    public static class CharacterTable
    {
        private static readonly MySqlCommand CreateCharacterCommand = new MySqlCommand(
                "INSERT INTO characters" +
                "(name, familyName, accountId, slotId, gender, scale, raceId, classId, mapContextId, posX, posY, posZ, rotation) VALUES" +
                "(@Name, @FamilyName, @AccountId, @SlotId, @Gender, @Scale, @RaceId, @ClassId, @MapContextId, @PosX, @PosY, @PosZ, @Rotation)");
        private static readonly MySqlCommand GetCharacterDataCommand = new MySqlCommand("SELECT name, familyName, slotId, gender, scale, raceId, classId, mapContextId, posX, posY, posZ, rotation FROM characters WHERE guid = @Guid");
		private static readonly MySqlCommand IsNameAvailableCommand = new MySqlCommand("SELECT name FROM characters WHERE name = @Name");
        private static readonly MySqlCommand IsSlotAvailableCommand = new MySqlCommand("SELECT slotId FROM characters WHERE accountId = @AccountID AND slotId = @SlotId");

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

            GetCharacterDataCommand.Connection = GameDatabaseAccess.CharConnection;
            GetCharacterDataCommand.Parameters.Add("@Guid", MySqlDbType.UInt64);
            GetCharacterDataCommand.Prepare();

            IsNameAvailableCommand.Connection = GameDatabaseAccess.CharConnection;
            IsNameAvailableCommand.Parameters.Add("@Name", MySqlDbType.VarChar);
            IsNameAvailableCommand.Prepare();

            IsSlotAvailableCommand.Connection = GameDatabaseAccess.CharConnection;
            IsSlotAvailableCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            IsSlotAvailableCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            IsSlotAvailableCommand.Prepare();
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

        public static CharacterEntry GetCharacterData(uint id)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                CreateCharacterCommand.Parameters["@Id"].Value = id;

                using (var reader = GetCharacterDataCommand.ExecuteReader())
                    return CharacterEntry.Read(reader);
            }
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
