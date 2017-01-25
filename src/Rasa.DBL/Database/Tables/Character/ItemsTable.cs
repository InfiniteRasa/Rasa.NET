using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.Character
{
    public class ItemsTable
    {
        private static readonly MySqlCommand CreateItemCommand = new MySqlCommand("INSERT INTO items (ownerId, ownerSlotId, entityId, stackSize) VALUES (@OwnerId, @OwnerSlotId, @EntityID, @StackSize)");
        private static readonly MySqlCommand GetItemCommand = new MySqlCommand("SELECT id, entityId, stackSize FROM items WHERE ownerId = @OwnerId AND ownerSlotId = @OwnerSlotId");
        private static readonly MySqlCommand UpdateItemCommand = new MySqlCommand("UPDATE items SET ownerId = @OwnerId, ownerSlotId = @OwnerSlotID, stackSize = @StackSize WHERE id = @Id");

        public static void Initialize()
        {
            CreateItemCommand.Connection = GameDatabaseAccess.CharConnection;
            CreateItemCommand.Parameters.Add("@OwnerId", MySqlDbType.UInt32);
            CreateItemCommand.Parameters.Add("@OwnerSlotId", MySqlDbType.Int32);
            CreateItemCommand.Parameters.Add("@EntityId", MySqlDbType.Int32);
            CreateItemCommand.Parameters.Add("@StackSize", MySqlDbType.Int32);
            CreateItemCommand.Prepare();

            GetItemCommand.Connection = GameDatabaseAccess.CharConnection;
            GetItemCommand.Parameters.Add("@OwnerId", MySqlDbType.UInt32);
            GetItemCommand.Parameters.Add("@OwnerSlotId", MySqlDbType.Int32);
            GetItemCommand.Prepare();

            UpdateItemCommand.Connection = GameDatabaseAccess.CharConnection;
            UpdateItemCommand.Parameters.Add("@Id", MySqlDbType.UInt32);
            UpdateItemCommand.Parameters.Add("@OwnerId", MySqlDbType.UInt32);
            UpdateItemCommand.Parameters.Add("@OwnerSlotId", MySqlDbType.Int32);
            UpdateItemCommand.Parameters.Add("@StackSize", MySqlDbType.Int32);
            UpdateItemCommand.Prepare();
        }

        public static uint CreateItem(uint ownerId, int ownerSlotId, int entityId, int stackSize)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                CreateItemCommand.Parameters["@OwnerId"].Value = ownerId;
                CreateItemCommand.Parameters["@OwnerSlotId"].Value = ownerSlotId;
                CreateItemCommand.Parameters["@EntityId"].Value = entityId;
                CreateItemCommand.Parameters["@stackSize"].Value = stackSize;
                CreateItemCommand.ExecuteNonQuery();

                return (uint)CreateItemCommand.LastInsertedId;
            }
        }

        public static List<uint> GetItem(uint ownerId, int ownerSlotId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                GetItemCommand.Parameters["@OwnerId"].Value = ownerId;
                GetItemCommand.Parameters["@OwnerSlotId"].Value = ownerSlotId;

                var itemData = new List<uint>();
                using (var reader = GetItemCommand.ExecuteReader())
                    if (reader.Read())
                        for (var i = 0; i < 3; i++)
                            itemData.Add((uint)reader[i].GetHashCode());

                return itemData;
            }
        }

        public static void UpdateItem(uint id, uint ownerId, int ownerSlotId, int stackSize)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                UpdateItemCommand.Parameters["@Id"].Value = id;
                UpdateItemCommand.Parameters["@OwnerId"].Value = ownerId;
                UpdateItemCommand.Parameters["@OwnerSlotId"].Value = ownerSlotId;
                UpdateItemCommand.Parameters["@StackSize"].Value = stackSize;
                UpdateItemCommand.ExecuteNonQuery();
            }
        }
    }
}
