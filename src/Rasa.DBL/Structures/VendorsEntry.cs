using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class VendorsEntry
    {
        public uint CreatureDbId { get; set; }
        public uint PackageId { get; set; }

        public static VendorsEntry Read(MySqlDataReader reader)
        {
            return new VendorsEntry
            {
                CreatureDbId = reader.GetUInt32("creatureDbId"),
                PackageId = reader.GetUInt32("packageId")
            };
        }
    }
}
