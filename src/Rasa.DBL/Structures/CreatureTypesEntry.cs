using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CreatureTypesEntry
    {
        public uint DbId { get; set; }
        public int ClassId { get; set; }
        public int IsNpc { get; set; }
        public int IsVendor { get; set; }
        public int IsHarvestable { get; set; }

        public static CreatureTypesEntry Read(MySqlDataReader reader)
        {
            return new CreatureTypesEntry
            {
                DbId = reader.GetUInt32("dbId"),
                ClassId = reader.GetInt32("classId"),
                IsNpc = reader.GetInt32("isNpc"),
                IsVendor = reader.GetInt32("isVendor"),
                IsHarvestable = reader.GetInt32("isHarvestable")
            };
        }
    }
}