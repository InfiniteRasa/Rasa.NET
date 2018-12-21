using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class NPCPackagesEntry
    {
        public uint CreatureDbId { get; set; }
        public uint NpcPackageId { get; set; }

        public static NPCPackagesEntry Read(MySqlDataReader reader)
        {
            return new NPCPackagesEntry
            {
                CreatureDbId = reader.GetUInt32("creatureDbId"),
                NpcPackageId = reader.GetUInt32("packageId")
            };
        }
    }
}
