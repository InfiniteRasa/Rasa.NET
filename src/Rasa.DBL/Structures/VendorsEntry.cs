using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class VendorsEntry
    {
        public uint CreatureDbId { get; set; }
        public int PackageId { get; set; }

        public static VendorsEntry Read(MySqlDataReader reader)
        {
            return new VendorsEntry
            {
                CreatureDbId = reader.GetUInt32("creatureDbId"),
                PackageId = reader.GetInt32("packageId")
            };
        }
    }
}
