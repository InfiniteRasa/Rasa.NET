using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class VendorItemsEntry
    {
        public uint DbId { get; set; }
        public int ItemTemplateId { get; set; }

        public static VendorItemsEntry Read(MySqlDataReader reader)
        {
            return new VendorItemsEntry
            {
                DbId = reader.GetUInt32("creatureDbId"),
                ItemTemplateId = reader.GetInt32("itemTemplateId")
            };
        }
    }
}