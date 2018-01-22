using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class VendorsEntry
    {
        public int CreatureDbId { get; set; }
        public int PackageId { get; set; }

        public static VendorsEntry Read(MySqlDataReader reader)
        {
            return new VendorsEntry
            {
                CreatureDbId = reader.GetInt32("creatureDbId"),
                PackageId = reader.GetInt32("packageId")
            };
        }
    }
}
