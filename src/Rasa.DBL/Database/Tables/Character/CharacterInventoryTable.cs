using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public class CharacterInventoryTable
    {
        private static readonly MySqlCommand AddInvItemCommand = new MySqlCommand("INSERT INTO character_inventory (characterId, slotId, itemId, stackSize) VALUES (@CharacterId, @SlotId, @ItemId, @StackSize)");
        private static readonly MySqlCommand GetInvItemCommand = new MySqlCommand("SELECT itemId, stackSize FROM character_inventory WHERE characterId = @CharacterId AND slotId = @SlotId");
        private static readonly MySqlCommand UpdateInvItemCommand = new MySqlCommand("UPDATE character_inventory SET itemId = @ItemId, stackSize = @StackSize WHERE characterId = @CharacterId AND slotId = @SlotId,");

        public static void Initialize()
        {
            AddInvItemCommand.Connection = GameDatabaseAccess.CharConnection;
            AddInvItemCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            AddInvItemCommand.Parameters.Add("@SlotID", MySqlDbType.Int32);
            AddInvItemCommand.Parameters.Add("@ItemId", MySqlDbType.UInt32);
            AddInvItemCommand.Parameters.Add("@StackSize", MySqlDbType.Int32);
            AddInvItemCommand.Prepare();

            GetInvItemCommand.Connection = GameDatabaseAccess.CharConnection;
            GetInvItemCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            GetInvItemCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            GetInvItemCommand.Prepare();

            UpdateInvItemCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateInvItemCommand.Parameters.Add("@CharacterId", MySqlDbType.UInt32);
            UpdateInvItemCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            UpdateInvItemCommand.Parameters.Add("@ItemId", MySqlDbType.UInt32);
            UpdateInvItemCommand.Parameters.Add("@StackSize", MySqlDbType.Int32);
            UpdateInvItemCommand.Prepare();
        }

        public static void AddInvItem(uint characterId, int slotId, uint itemId, int stackSize)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                AddInvItemCommand.Parameters["@CharacterId"].Value = characterId;
                AddInvItemCommand.Parameters["@SlotId"].Value = slotId;
                AddInvItemCommand.Parameters["@ItemId"].Value = itemId;
                AddInvItemCommand.Parameters["@StackSize"].Value = stackSize;
                AddInvItemCommand.ExecuteNonQuery();
            }
        }

        public static List<uint> GetItem(uint characterId, int slotId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetInvItemCommand.Parameters["@CharacterId"].Value = characterId;
                GetInvItemCommand.Parameters["@SlotId"].Value = slotId;

                var itemData = new List<uint>();
                using (var reader = GetInvItemCommand.ExecuteReader())
                    if (reader.Read())
                        for (var i = 0; i < 3; i++)
                            itemData.Add((uint)reader[i].GetHashCode());

                return itemData;
            }
        }

        public static void UpdateInvItem(uint characterId, int slotId, uint itemId, int stackSize)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateInvItemCommand.Parameters["@CharacterId"].Value = characterId;
                UpdateInvItemCommand.Parameters["@SlotId"].Value = slotId;
                UpdateInvItemCommand.Parameters["@ItemId"].Value = itemId;
                UpdateInvItemCommand.Parameters["@StackSize"].Value = stackSize;
                UpdateInvItemCommand.ExecuteNonQuery();
            }
        }
    }
}
