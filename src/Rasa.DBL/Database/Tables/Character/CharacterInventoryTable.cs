using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using System;
    using Structures;

    public class CharacterInventoryTable
    {
        private static readonly MySqlCommand AddInvItemCommand = new MySqlCommand("INSERT INTO character_inventory (accountId, characterSlot, inventoryType, slotId, itemId) VALUES (@AccountId, @CharacterSlot, @InventoryType, @SlotId, @ItemId)");
        private static readonly MySqlCommand DeleteInvItemCommand = new MySqlCommand("DELETE FROM character_inventory WHERE accountId = @AccountId AND characterSlot = @CharacterSlot AND inventoryType = @InventoryType AND slotId = @SlotId");
        private static readonly MySqlCommand GetInvItemsCommand = new MySqlCommand("SELECT characterSlot, inventoryType, itemId, slotId FROM character_inventory WHERE accountId = @AccountId");
        private static readonly MySqlCommand MoveInvItemCommand = new MySqlCommand("UPDATE character_inventory SET characterSlot = @CharacterSlot, inventoryType = @InventoryType, slotId = @SlotId WHERE accountId = @AccountId AND itemId = @ItemId");

        public static void Initialize()
        {
            AddInvItemCommand.Connection = GameDatabaseAccess.CharConnection;
            AddInvItemCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            AddInvItemCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            AddInvItemCommand.Parameters.Add("@InventoryType", MySqlDbType.Int32);
            AddInvItemCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            AddInvItemCommand.Parameters.Add("@ItemId", MySqlDbType.UInt32);
            AddInvItemCommand.Prepare();

            DeleteInvItemCommand.Connection = GameDatabaseAccess.CharConnection;
            DeleteInvItemCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            DeleteInvItemCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            DeleteInvItemCommand.Parameters.Add("@InventoryType", MySqlDbType.Int32);
            DeleteInvItemCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            DeleteInvItemCommand.Prepare();

            GetInvItemsCommand.Connection = GameDatabaseAccess.CharConnection;
            GetInvItemsCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            GetInvItemsCommand.Prepare();

            MoveInvItemCommand.Connection = GameDatabaseAccess.CharConnection;
            MoveInvItemCommand.Parameters.Add("@AccountId", MySqlDbType.UInt32);
            MoveInvItemCommand.Parameters.Add("@CharacterSlot", MySqlDbType.UInt32);
            MoveInvItemCommand.Parameters.Add("@InventoryType", MySqlDbType.Int32);
            MoveInvItemCommand.Parameters.Add("@SlotId", MySqlDbType.Int32);
            MoveInvItemCommand.Parameters.Add("@ItemId", MySqlDbType.UInt32);
            MoveInvItemCommand.Prepare();
        }

        public static void AddInvItem(uint accountId, uint characterSlot, int inventoryType, int slotId, uint itemId)
        {
            
            lock (GameDatabaseAccess.CharLock)
            {
                AddInvItemCommand.Parameters["@AccountId"].Value = accountId;
                AddInvItemCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                AddInvItemCommand.Parameters["@InventoryType"].Value = inventoryType;
                AddInvItemCommand.Parameters["@SlotId"].Value = slotId;
                AddInvItemCommand.Parameters["@ItemId"].Value = itemId;
                AddInvItemCommand.ExecuteNonQuery();
            }
        }

        public static void DeleteInvItem(uint accountId, uint characterSlot, int inventoryType, int slotId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                DeleteInvItemCommand.Parameters["@AccountId"].Value = accountId;
                DeleteInvItemCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                DeleteInvItemCommand.Parameters["@InventoryType"].Value = inventoryType;
                DeleteInvItemCommand.Parameters["@SlotId"].Value = slotId;
                DeleteInvItemCommand.ExecuteNonQuery();
            }
        }

        public static List<CharacterInventoryEntry> GetItems(uint accountId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetInvItemsCommand.Parameters["@AccountId"].Value = accountId;
                var invItemData = new List<CharacterInventoryEntry>();
                using (var reader = GetInvItemsCommand.ExecuteReader())
                    while (reader.Read())
                        invItemData.Add(CharacterInventoryEntry.Read(reader));

                return invItemData;
            }
        }

        public static void MoveInvItem(uint accountId, uint characterSlot, int inventoryType, int slotId, uint itemId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                MoveInvItemCommand.Parameters["@AccountId"].Value = accountId;
                MoveInvItemCommand.Parameters["@CharacterSlot"].Value = characterSlot;
                MoveInvItemCommand.Parameters["@InventoryType"].Value = inventoryType;
                MoveInvItemCommand.Parameters["@SlotId"].Value = slotId;
                MoveInvItemCommand.Parameters["@ItemId"].Value = itemId;
                MoveInvItemCommand.ExecuteNonQuery();
            }
        }
    }
}
