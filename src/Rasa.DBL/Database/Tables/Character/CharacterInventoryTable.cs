using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class CharacterInventoryTable
    {
        private static readonly MySqlCommand AddInvItemCommand = new MySqlCommand("INSERT INTO character_inventory (characterId, slotId, itemId) VALUES (@CharacterId, @SlotId, @ItemId)");
        private static readonly MySqlCommand DeleteInvItemCommand = new MySqlCommand("DELETE FROM character_inventory WHERE characterId = @CharacterId AND slotId = @SlotId");
        private static readonly MySqlCommand GetInvItemCommand = new MySqlCommand("SELECT itemId, slotId FROM character_inventory WHERE characterId = @CharacterId");
        private static readonly MySqlCommand MoveInvItemCommand = new MySqlCommand("UPDATE character_inventory SET slotId = @SlotId WHERE characterId = @CharacterId AND itemId = @ItemId");

        public static void Initialize()
        {
            AddInvItemCommand.Connection = GameDatabaseAccess.CharConnection;
            AddInvItemCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            AddInvItemCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            AddInvItemCommand.Parameters.Add("@ItemId", MySqlDbType.UInt32);
            AddInvItemCommand.Prepare();

            DeleteInvItemCommand.Connection = GameDatabaseAccess.CharConnection;
            DeleteInvItemCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            DeleteInvItemCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            DeleteInvItemCommand.Prepare();

            GetInvItemCommand.Connection = GameDatabaseAccess.CharConnection;
            GetInvItemCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetInvItemCommand.Prepare();

            MoveInvItemCommand.Connection = GameDatabaseAccess.CharConnection;
            MoveInvItemCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            MoveInvItemCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            MoveInvItemCommand.Parameters.Add("@ItemId", MySqlDbType.UInt32);
            MoveInvItemCommand.Prepare();
        }

        public static void AddInvItem(uint characterId, int slotId, uint itemId)
        {
            
            lock (GameDatabaseAccess.CharLock)
            {
                AddInvItemCommand.Parameters["@CharacterId"].Value = characterId;
                AddInvItemCommand.Parameters["@SlotId"].Value = slotId;
                AddInvItemCommand.Parameters["@ItemId"].Value = itemId;
                AddInvItemCommand.ExecuteNonQuery();
            }
        }

        public static void DeleteInvItem(uint characterId, int slotId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                DeleteInvItemCommand.Parameters["@CharacterId"].Value = characterId;
                DeleteInvItemCommand.Parameters["@SlotId"].Value = slotId;
                DeleteInvItemCommand.ExecuteNonQuery();
            }
        }

        public static List<CharacterInventoryEntry> GetItem(uint characterId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetInvItemCommand.Parameters["@CharacterId"].Value = characterId;
                var invItemData = new List<CharacterInventoryEntry>();
                using (var reader = GetInvItemCommand.ExecuteReader())
                    while (reader.Read())
                    {
                        invItemData.Add(new CharacterInventoryEntry
                        {
                            ItemId = reader.GetUInt32("itemId"),
                            SlotId = reader.GetInt32("slotId"),
                        } );
                    }

                return invItemData;
            }
        }

        public static void MoveInvItem(uint characterId, int slotId, uint itemId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                MoveInvItemCommand.Parameters["@CharacterId"].Value = characterId;
                MoveInvItemCommand.Parameters["@SlotId"].Value = slotId;
                MoveInvItemCommand.Parameters["@ItemId"].Value = itemId;
                MoveInvItemCommand.ExecuteNonQuery();
            }
        }
    }
}
