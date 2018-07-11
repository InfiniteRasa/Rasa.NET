using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    using Structures;
  
    public class ItemsTable
    {
        private static readonly MySqlCommand CraftItemCommand = new MySqlCommand("INSERT INTO items (itemTemplateId, stackSize, currentHitPoints, crafterName, color) VALUES (@ItemTemplateId, @StackSize, @CurrentHitPoints, @CrafterName, @Color)");
        private static readonly MySqlCommand CreateItemCommand = new MySqlCommand("INSERT INTO items (itemTemplateId, stackSize, currentHitPoints, color) VALUES (@ItemTemplateId, @StackSize, @CurrentHitPoints, @Color)");
        private static readonly MySqlCommand DeleteItemCommand = new MySqlCommand("DELETE FROM items WHERE itemId = @ItemId");
        private static readonly MySqlCommand GetItemCommand = new MySqlCommand("SELECT itemTemplateId, stackSize, currentHitPoints, color, ammoCount, crafterName FROM items WHERE itemId = @ItemId");
        private static readonly MySqlCommand UpdateItemCurrentAmmoCommand = new MySqlCommand("UPDATE items SET ammoCount = @AmmoCount WHERE itemId = @ItemId");
        private static readonly MySqlCommand UpdateItemCurrentHitPointsCommand = new MySqlCommand("UPDATE items SET currentHitPoints = @CurrentHitPoints WHERE itemId = @ItemId");
        private static readonly MySqlCommand UpdateItemStackSizeCommand = new MySqlCommand("UPDATE items SET stackSize = @StackSize WHERE itemId = @ItemId");

        public static void Initialize()
        {
            CraftItemCommand.Connection = GameDatabaseAccess.CharConnection;
            CraftItemCommand.Parameters.Add("@ItemTemplateId", MySqlDbType.UInt32);
            CraftItemCommand.Parameters.Add("@StackSize", MySqlDbType.Int32);
            CraftItemCommand.Parameters.Add("@CurrentHitPoints", MySqlDbType.Int32);
            CraftItemCommand.Parameters.Add("@CrafterName", MySqlDbType.VarChar);
            CraftItemCommand.Parameters.Add("@Color", MySqlDbType.UInt32);
            CraftItemCommand.Prepare();

            CreateItemCommand.Connection = GameDatabaseAccess.CharConnection;
            CreateItemCommand.Parameters.Add("@ItemTemplateId", MySqlDbType.UInt32);
            CreateItemCommand.Parameters.Add("@StackSize", MySqlDbType.Int32);
            CreateItemCommand.Parameters.Add("@CurrentHitPoints", MySqlDbType.Int32);
            CreateItemCommand.Parameters.Add("@Color", MySqlDbType.UInt32);
            CreateItemCommand.Prepare();

            DeleteItemCommand.Connection = GameDatabaseAccess.CharConnection;
            DeleteItemCommand.Parameters.Add("@ItemId", MySqlDbType.UInt32);
            DeleteItemCommand.Prepare();

            GetItemCommand.Connection = GameDatabaseAccess.CharConnection;
            GetItemCommand.Parameters.Add("@ItemId", MySqlDbType.UInt32);
            GetItemCommand.Prepare();

            UpdateItemCurrentAmmoCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateItemCurrentAmmoCommand.Parameters.Add("@ItemId", MySqlDbType.UInt32);
            UpdateItemCurrentAmmoCommand.Parameters.Add("@AmmoCount", MySqlDbType.Int32);
            UpdateItemCurrentAmmoCommand.Prepare();

            UpdateItemCurrentHitPointsCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateItemCurrentHitPointsCommand.Parameters.Add("@ItemId", MySqlDbType.UInt32);
            UpdateItemCurrentHitPointsCommand.Parameters.Add("@CurrentHitPoints", MySqlDbType.Int32);
            UpdateItemCurrentHitPointsCommand.Prepare();

            UpdateItemStackSizeCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateItemStackSizeCommand.Parameters.Add("@ItemId", MySqlDbType.UInt32);
            UpdateItemStackSizeCommand.Parameters.Add("@StackSize", MySqlDbType.Int32);
            UpdateItemStackSizeCommand.Prepare();
        }

        public static uint CraftItem(uint itemTemplateId, int stackSize, int currentHitPoints, string crafterName, uint color)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                CraftItemCommand.Parameters["@ItemTemplateId"].Value = itemTemplateId;
                CraftItemCommand.Parameters["@StackSize"].Value = stackSize;
                CraftItemCommand.Parameters["@CurrentHitPoints"].Value = currentHitPoints;
                CraftItemCommand.Parameters["@CrafterName"].Value = crafterName;
                CraftItemCommand.Parameters["@Color"].Value = color;
                CraftItemCommand.ExecuteNonQuery();
                return (uint)CraftItemCommand.LastInsertedId;
            }
        }

        public static uint CreateItem(uint itemTemplateId, int stackSize, int currentHitPoints, uint color)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                CreateItemCommand.Parameters["@ItemTemplateId"].Value = itemTemplateId;
                CreateItemCommand.Parameters["@StackSize"].Value = stackSize;
                CreateItemCommand.Parameters["@CurrentHitPoints"].Value = currentHitPoints;
                CreateItemCommand.Parameters["@Color"].Value = color;
                CreateItemCommand.ExecuteNonQuery();

                return (uint)CreateItemCommand.LastInsertedId;
            }
        }

        public static void DeleteItem(uint itemId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                DeleteItemCommand.Parameters["@ItemId"].Value = itemId;
                DeleteItemCommand.ExecuteNonQuery();
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

        public static void UpdateCurrentHitPoints(uint itemId, int currentHitPoints)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateItemCurrentHitPointsCommand.Parameters["@ItemId"].Value = itemId;
                UpdateItemCurrentHitPointsCommand.Parameters["@CurrentHitPoints"].Value = currentHitPoints;
                UpdateItemCurrentHitPointsCommand.ExecuteNonQuery();
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
