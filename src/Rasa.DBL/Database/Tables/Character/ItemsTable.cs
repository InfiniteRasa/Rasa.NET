using System.Linq;

namespace Rasa.Database.Tables.Character
{
    using Structures;
  
    public class ItemsTable
    {
        public static uint CraftItem(uint itemTemplateId, uint stackSize, int currentHitPoints, string crafterName, uint color)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var item = new ItemsEntry
                {
                    ItemTemplateId = itemTemplateId,
                    StackSize = stackSize,
                    CurrentHitPoints = currentHitPoints,
                    CrafterName = crafterName,
                    Color = color
                };
                GameDatabaseAccess.CharConnection.Items.Add(item);
                GameDatabaseAccess.CharConnection.SaveChanges();

                return item.ItemId;
            }
        }

        public static uint CreateItem(uint itemTemplateId, uint stackSize, int currentHitPoints, uint color)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var item = new ItemsEntry
                {
                    ItemTemplateId = itemTemplateId,
                    StackSize = stackSize,
                    CurrentHitPoints = currentHitPoints,
                    Color = color
                };
                GameDatabaseAccess.CharConnection.Items.Add(item);
                GameDatabaseAccess.CharConnection.SaveChanges();

                return item.ItemId;
            }
        }

        public static void DeleteItem(uint itemId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var item = GameDatabaseAccess.CharConnection.Items.First(it => it.ItemId == itemId);
                GameDatabaseAccess.CharConnection.Remove(item);
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static ItemsEntry GetItem(uint itemId)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                return GameDatabaseAccess.CharConnection.Items.First(item => item.ItemId == itemId);
            }
        }

        public static void UpdateItemCurrentAmmo(uint itemId, uint ammoCount)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var item = GetItem(itemId);
                item.AmmoCount = ammoCount;
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static void UpdateCurrentHitPoints(uint itemId, int currentHitPoints)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var item = GetItem(itemId);
                item.CurrentHitPoints = currentHitPoints;
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }

        public static void UpdateItemStackSize(uint itemId, uint stackSize)
        {
            lock (GameDatabaseAccess.CharLock)
            {
                var item = GetItem(itemId);
                item.StackSize = stackSize;
                GameDatabaseAccess.CharConnection.SaveChanges();
            }
        }
    }
}
