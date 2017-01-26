using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public class CharacterAppearanceTable
    {        
        private static readonly MySqlCommand GetAppearanceCommand = new MySqlCommand("SELECT slotItem, slotHue FROM character_appearance WHERE characterId = @CharacterId AND slotId = @SlotId");
        private static readonly MySqlCommand SetAppearanceCommand = new MySqlCommand("INSERT INTO character_appearance (characterId, slotId, slotItem, slotHue) VALUES (@CharacterId, @SlotId, @SlotItem, @SlotHue)");
        private static readonly MySqlCommand UpdateAppearanceCommand = new MySqlCommand("UPDATE character_appearance SET slotItem = @ClassId, slotHue = @Hue WHERE characterId = @CharacterId AND slotId = @SlotId");
        
        public static void Initialize()
        {
            GetAppearanceCommand.Connection = GameDatabaseAccess.CharConnection;
            GetAppearanceCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetAppearanceCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            GetAppearanceCommand.Prepare();

            SetAppearanceCommand.Connection = GameDatabaseAccess.CharConnection;
            SetAppearanceCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            SetAppearanceCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            SetAppearanceCommand.Parameters.Add("@SlotItem", MySqlDbType.Int32);
            SetAppearanceCommand.Parameters.Add("@SlotHue", MySqlDbType.Int32);
            SetAppearanceCommand.Prepare();

            UpdateAppearanceCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateAppearanceCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            UpdateAppearanceCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            UpdateAppearanceCommand.Parameters.Add("@ClassId", MySqlDbType.Int32);
            UpdateAppearanceCommand.Parameters.Add("@Hue", MySqlDbType.Int32);
            UpdateAppearanceCommand.Prepare();            
        }

        public static List<int> GetAppearance(uint characterId, int slotId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var playerAppearance = new List<int>();
                GetAppearanceCommand.Parameters["@CharacterId"].Value = characterId;
                GetAppearanceCommand.Parameters["@SlotId"].Value = slotId;

                using (var reader = GetAppearanceCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        playerAppearance.Add(reader[0].GetHashCode());
                        playerAppearance.Add(reader[1].GetHashCode());
                    }
                }

                return playerAppearance;
            }
        }

        public static void SetAppearance(uint characterId, int slotId, int slotItem, int slotHue)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                SetAppearanceCommand.Parameters["@CharacterId"].Value = characterId;
                SetAppearanceCommand.Parameters["@SlotId"].Value = slotId;
                SetAppearanceCommand.Parameters["@SlotItem"].Value = slotItem;
                SetAppearanceCommand.Parameters["@SlotHue"].Value = slotHue;
                SetAppearanceCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateCharacterAppearance(uint characterId, int slotId, int classId, int hue)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateAppearanceCommand.Parameters["@CharacterId"].Value = characterId;
                UpdateAppearanceCommand.Parameters["@SlotId"].Value =  slotId;
                UpdateAppearanceCommand.Parameters["@ClassId"].Value = classId;
                UpdateAppearanceCommand.Parameters["@Hue"].Value = hue;
                UpdateAppearanceCommand.ExecuteNonQuery();
            }
        }
    }
}