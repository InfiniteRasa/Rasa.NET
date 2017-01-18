using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class ItemTemplateEntry
    {
        public int ItemTemplateId { get; set; }
        public int ClassId { get; set; }
        public int QualityId { get; set; }
        public int ItemType { get; set; }
        public bool HasSellableFlag { get; set; }
        public bool NotTradeableFlag { get; set; }
        public bool HasCharacterUniqueFlag { get; set; }
        public bool HasAccountUniqueFlag { get; set; }
        public bool HasBoEFlag { get; set; }
        public bool BoundToCharacterFlag { get; set; }
        public bool NotPlaceableInLockBoxFlag { get; set; }
        public int InventoryCategory { get; set; }
        public int BuyPrice { get; set; }
        public int SellPrice { get; set; }
        public int ReqLevel { get; set; }
        public int StackSize { get; set; }

        public static ItemTemplateEntry Read(MySqlDataReader reader)
        {
            if (!reader.Read())
                return null;

            return new ItemTemplateEntry
            {
                ItemTemplateId = reader.GetInt32("itemTemplateId"),
                ClassId = reader.GetInt32("classId"),
                QualityId = reader.GetInt32("qualityId"),
                ItemType = reader.GetInt32("itemType"),
                HasSellableFlag = reader.GetBoolean("hasSellableFlag"),
                NotTradeableFlag = reader.GetBoolean("notTradeableFlag"),
                HasCharacterUniqueFlag = reader.GetBoolean("hasCharacterUniqueFlag"),
                HasAccountUniqueFlag = reader.GetBoolean("hasAccountUniqueFlag"),
                HasBoEFlag = reader.GetBoolean("hasBoEFlag"),
                BoundToCharacterFlag = reader.GetBoolean("boundToCharacterFlag"),
                NotPlaceableInLockBoxFlag = reader.GetBoolean("notPlaceableInLockBoxFlag"),
                InventoryCategory = reader.GetInt32("inventoryCategory"),
                BuyPrice = reader.GetInt32("buyPrice"),
                SellPrice = reader.GetInt32("sellPrice"),
                ReqLevel = reader.GetInt32("reqLevel"),
                StackSize = reader.GetInt32("stackSize")
            };
        }
    }
}
