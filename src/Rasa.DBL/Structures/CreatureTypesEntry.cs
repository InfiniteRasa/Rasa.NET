using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public partial class CreatureTypesEntry
    {
        public uint DbId { get; set; }
        public int ClassId { get; set; }
        public int IsNpc { get; set; }
        public int IsVendor { get; set; }
        public int IsHarvestable { get; set; }
    }
}