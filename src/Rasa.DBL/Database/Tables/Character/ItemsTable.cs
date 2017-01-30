using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;
  
    public class ItemsTable
    {
        private static readonly MySqlCommand CraftItemCommand = new MySqlCommand("INSERT INTO items (entityClassId, stackSize, crafterName) VALUES (@EntityClassId, @StackSize, @CrafterName)");
        private static readonly MySqlCommand CreateItemCommand = new MySqlCommand("INSERT INTO items (entityClassId, stackSize, color) VALUES (@EntityClassId, @StackSize, @Color)");
        private static readonly MySqlCommand GetItemCommand = new MySqlCommand("SELECT entityClassId, stackSize, color, ammoCount, crafterName FROM items WHERE itemId = @ItemId");
        private static readonly MySqlCommand UpdateItemCurrentAmmoCommand = new MySqlCommand("UPDATE items SET ammoCount = @AmmoCount WHERE itemId = @ItemId");
        private static readonly MySqlCommand UpdateItemStackSizeCommand = new MySqlCommand("UPDATE items SET stackSize = @StackSize WHERE itemId = @ItemId");

        public static void Initialize()
        {
            CraftItemCommand.Connection = GameDatabaseAccess.CharConnection;
            CraftItemCommand.Parameters.Add("@EntityClassId", MySqlDbType.Int32);
            CraftItemCommand.Parameters.Add("@StackSize", MySqlDbType.Int32);
            CraftItemCommand.Parameters.Add("@CrafterName", MySqlDbType.VarChar);
            CraftItemCommand.Prepare();

            CreateItemCommand.Connection = GameDatabaseAccess.CharConnection;
            CreateItemCommand.Parameters.Add("@EntityClassId", MySqlDbType.Int32);
            CreateItemCommand.Parameters.Add("@StackSize", MySqlDbType.Int32);
            CreateItemCommand.Parameters.Add("@Color", MySqlDbType.Int32);
            CreateItemCommand.Prepare();

            GetItemCommand.Connection = GameDatabaseAccess.CharConnection;
            GetItemCommand.Parameters.Add("@ItemId", MySqlDbType.UInt32);
            GetItemCommand.Prepare();

            UpdateItemCurrentAmmoCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateItemCurrentAmmoCommand.Parameters.Add("@ItemId", MySqlDbType.UInt32);
            UpdateItemCurrentAmmoCommand.Parameters.Add("@AmmoCount", MySqlDbType.Int32);
            UpdateItemCurrentAmmoCommand.Prepare();

            UpdateItemStackSizeCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateItemStackSizeCommand.Parameters.Add("@ItemId", MySqlDbType.UInt32);
            UpdateItemStackSizeCommand.Parameters.Add("@StackSize", MySqlDbType.Int32);
            UpdateItemStackSizeCommand.Prepare();
        }

        public static uint CraftItem(int entityClassId, int stackSize, string crafterName)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                CraftItemCommand.Parameters["@EntityClassId"].Value = entityClassId;
                CraftItemCommand.Parameters["@StackSize"].Value = stackSize;
                CraftItemCommand.Parameters["@CrafterName"].Value = crafterName;
                CraftItemCommand.ExecuteNonQuery();
                return (uint)CraftItemCommand.LastInsertedId;
            }
        }

        public static uint CreateItem(int entityClassId, int stackSize, int color)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                CreateItemCommand.Parameters["@EntityClassId"].Value = entityClassId;
                CreateItemCommand.Parameters["@StackSize"].Value = stackSize;
                CreateItemCommand.Parameters["@Color"].Value = color;
                CreateItemCommand.ExecuteNonQuery();

                return (uint)CreateItemCommand.LastInsertedId;
            }
        }

        public static ItemsEntry GetItem(uint itemId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetItemCommand.Parameters["@ItemId"].Value = itemId;
                using (var reader = GetItemCommand.ExecuteReader())
                    return ItemsEntry.Read(reader);
            }
        }

        public static void UpdateItemCurrentAmmo(uint itemId, int ammoCount)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateItemCurrentAmmoCommand.Parameters["@ItemId"].Value = itemId;
                UpdateItemCurrentAmmoCommand.Parameters["@AmmoCount"].Value = ammoCount;
                UpdateItemCurrentAmmoCommand.ExecuteNonQuery();
            }
        }

        public static void UpdateItemStackSize(uint itemId, int stackSize)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateItemStackSizeCommand.Parameters["@ItemId"].Value = itemId;
                UpdateItemStackSizeCommand.Parameters["@StackSize"].Value = stackSize;
                UpdateItemStackSizeCommand.ExecuteNonQuery();
            }
        }
    }
}
