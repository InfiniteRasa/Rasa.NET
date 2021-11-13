using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;

    public class ClanInventoryTable
    {
        private static readonly MySqlCommand AddInvItemCommand = new MySqlCommand("INSERT INTO clan_inventory (clanid, slotId, itemId) VALUES (@ClanId, @SlotId, @ItemId)");
        private static readonly MySqlCommand DeleteInvItemCommand = new MySqlCommand("DELETE FROM clan_inventory WHERE clanid = @ClanId AND slotId = @SlotId");
        private static readonly MySqlCommand GetInvItemsCommand = new MySqlCommand("SELECT itemId, slotId FROM clan_inventory WHERE clanid = @ClanId");
        private static readonly MySqlCommand MoveInvItemCommand = new MySqlCommand("UPDATE clan_inventory SET slotId = @SlotId WHERE clanId = @ClanId AND itemId = @ItemId");

        public static void Initialize()
        {
            AddInvItemCommand.Connection = GameDatabaseAccess.CharConnection;
            AddInvItemCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            AddInvItemCommand.Parameters.Add("@SlotId", MySqlDbType.UInt32);
            AddInvItemCommand.Parameters.Add("@ItemId", MySqlDbType.Int32);
            AddInvItemCommand.Prepare();

            DeleteInvItemCommand.Connection = GameDatabaseAccess.CharConnection;
            DeleteInvItemCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            DeleteInvItemCommand.Parameters.Add("@SlotId", MySqlDbType.UInt32);
            DeleteInvItemCommand.Prepare();

            GetInvItemsCommand.Connection = GameDatabaseAccess.CharConnection;
            GetInvItemsCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            GetInvItemsCommand.Prepare();

            MoveInvItemCommand.Connection = GameDatabaseAccess.CharConnection;
            MoveInvItemCommand.Parameters.Add("@ClanId", MySqlDbType.UInt32);
            MoveInvItemCommand.Parameters.Add("@SlotId", MySqlDbType.UInt32);
            MoveInvItemCommand.Parameters.Add("@ItemId", MySqlDbType.UInt32);
            MoveInvItemCommand.Prepare();
        }

        public static void AddInvItem(uint clanId, uint slotId, uint itemId)
        {
         
            lock (GameDatabaseAccess.CharLock)
            {
                AddInvItemCommand.Parameters["@ClanId"].Value = clanId;
                AddInvItemCommand.Parameters["@SlotId"].Value = slotId;
                AddInvItemCommand.Parameters["@ItemId"].Value = itemId;
                AddInvItemCommand.ExecuteNonQuery();
            }
        }

        public static void DeleteInvItem(uint clanId, uint slotId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                DeleteInvItemCommand.Parameters["@ClanId"].Value = clanId;
                DeleteInvItemCommand.Parameters["@SlotId"].Value = slotId;
                DeleteInvItemCommand.ExecuteNonQuery();
            }
        }

        public static List<ClanInventoryEntry> GetItems(uint clanId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetInvItemsCommand.Parameters["@ClanId"].Value = clanId;
                var invItemData = new List<ClanInventoryEntry>();
                using (var reader = GetInvItemsCommand.ExecuteReader())
                    while (reader.Read())
                        invItemData.Add(ClanInventoryEntry.Read(reader));

                return invItemData;
            }
        }

        public static void MoveInvItem(uint clanId, uint slotId, uint itemId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                MoveInvItemCommand.Parameters["@ClanId"].Value = clanId;
                MoveInvItemCommand.Parameters["@SlotId"].Value = slotId;
                MoveInvItemCommand.Parameters["@ItemId"].Value = itemId;
                MoveInvItemCommand.ExecuteNonQuery();
            }
        }
    }
}
