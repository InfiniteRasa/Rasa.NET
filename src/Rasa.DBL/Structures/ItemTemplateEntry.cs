using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class ItemTemplateEntry
    {
        public int ItemTemplateId { get; set; }
        public int QualityId { get; set; }
        public bool HasSellableFlag { get; set; }
        public bool NotTradableFlag { get; set; }
        public bool HasCharacterUniqueFlag { get; set; }
        public bool HasAccountUniqueFlag { get; set; }
        public bool HasBoEFlag { get; set; }
        public bool BoundToCharacterFlag { get; set; }
        public bool NotPlaceableInLockBoxFlag { get; set; }
        public int InventoryCategory { get; set; }
        public int BuyPrice { get; set; }
        public int SellPrice { get; set; }

        public static ItemTemplateEntry Read(MySqlDataReader reader)
        {
            return new ItemTemplateEntry
            {
                ItemTemplateId = reader.GetInt32("itemTemplateId"),
                QualityId = reader.GetInt32("qualityId"),
                HasSellableFlag = reader.GetBoolean("hasSellableFlag"),
                NotTradableFlag = reader.GetBoolean("notTradableFlag"),
                HasCharacterUniqueFlag = reader.GetBoolean("hasCharacterUniqueFlag"),
                HasAccountUniqueFlag = reader.GetBoolean("hasAccountUniqueFlag"),
                HasBoEFlag = reader.GetBoolean("hasBoEFlag"),
                BoundToCharacterFlag = reader.GetBoolean("boundToCharacterFlag"),
                NotPlaceableInLockBoxFlag = reader.GetBoolean("notPlaceableInLockBoxFlag"),
                InventoryCategory = reader.GetInt32("inventoryCategory"),
                BuyPrice = reader.GetInt32("buyPrice"),
                SellPrice = reader.GetInt32("sellPrice")
            };
        }
    }
}
