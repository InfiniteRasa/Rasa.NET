using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class CharacterAppearanceTable
    {        
        private static readonly MySqlCommand GetAppearanceCommand = new MySqlCommand("SELECT slotId, classId, color FROM character_appearance WHERE characterId = @CharacterId");
        private static readonly MySqlCommand SetAppearanceCommand = new MySqlCommand("INSERT INTO character_appearance (characterId, slotId, classId, color) VALUES (@CharacterId, @SlotId, @ClassId, @Color)");
        private static readonly MySqlCommand UpdateAppearanceCommand = new MySqlCommand("UPDATE character_appearance SET classId = @ClassId, color = @Color WHERE characterId = @CharacterId AND slotId = @SlotId");
        
        public static void Initialize()
        {
            GetAppearanceCommand.Connection = GameDatabaseAccess.CharConnection;
            GetAppearanceCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetAppearanceCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            GetAppearanceCommand.Prepare();

            SetAppearanceCommand.Connection = GameDatabaseAccess.CharConnection;
            SetAppearanceCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            SetAppearanceCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            SetAppearanceCommand.Parameters.Add("@ClassId", MySqlDbType.Int32);
            SetAppearanceCommand.Parameters.Add("@Color", MySqlDbType.Int32);
            SetAppearanceCommand.Prepare();

            UpdateAppearanceCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateAppearanceCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            UpdateAppearanceCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            UpdateAppearanceCommand.Parameters.Add("@ClassId", MySqlDbType.Int32);
            UpdateAppearanceCommand.Parameters.Add("@Color", MySqlDbType.Int32);
            UpdateAppearanceCommand.Prepare();            
        }

        public static List<AppearanceEntry> GetAppearance(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var playerAppearance = new List<AppearanceEntry>();

                GetAppearanceCommand.Parameters["@CharacterId"].Value = characterId;
                using (var reader = GetAppearanceCommand.ExecuteReader())
                    while (reader.Read())
                        playerAppearance.Add(AppearanceEntry.Read(reader));

                return playerAppearance;
            }
        }

        public static void SetAppearance(uint characterId, int slotId, int classId, int color)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                SetAppearanceCommand.Parameters["@CharacterId"].Value = characterId;
                SetAppearanceCommand.Parameters["@SlotId"].Value = slotId;
                SetAppearanceCommand.Parameters["@ClassId"].Value = classId;
                SetAppearanceCommand.Parameters["@Color"].Value = color;
                SetAppearanceCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterAppearance(uint characterId, int slotId, int classId, int color)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateAppearanceCommand.Parameters["@CharacterId"].Value = characterId;
                UpdateAppearanceCommand.Parameters["@SlotId"].Value =  slotId;
                UpdateAppearanceCommand.Parameters["@ClassId"].Value = classId;
                UpdateAppearanceCommand.Parameters["@Color"].Value = color;
                UpdateAppearanceCommand.ExecuteNonQuery();
            }
        }
    }
}