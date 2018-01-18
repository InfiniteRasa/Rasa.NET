using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class VendorsEntry
    {
        public uint DbId { get; set; }
        public int PackageId { get; set; }

        public static VendorsEntry Read(MySqlDataReader reader)
        {
            return new VendorsEntry
            {
                DbId = reader.GetUInt32("creatureDbId"),
                PackageId = reader.GetInt32("packageId")
            };
        }
    }
}