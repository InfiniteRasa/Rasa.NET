using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class ArmorClassEntry
    {
        public uint ClassId { get; set; }
        public int MinDamageAbsorbed { get; set; }
        public int MaxDamageAbsorbed { get; set; }
        public int RegenRate { get; set; }

        public static ArmorClassEntry Read(MySqlDataReader reader)
        {
            return new ArmorClassEntry
            {
                ClassId = reader.GetUInt32("classId"),
                MinDamageAbsorbed = reader.GetInt32("minDamageAbsorbed"),
                MaxDamageAbsorbed = reader.GetInt32("maxDamageAbsorbed"),
                RegenRate = reader.GetInt32("regenRate")
            };
        }
    }
}
