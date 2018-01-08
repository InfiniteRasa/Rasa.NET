using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CreatureStatsEntry
    {
        public int Body { get; set; }
        public int Mind { get; set; }
        public int Spirit { get; set; }
        public int Health { get; set; }
        public int Armor { get; set; }

        public static CreatureStatsEntry Read(MySqlDataReader reader)
        {
            return new CreatureStatsEntry
            {
                Body = reader.GetInt32("body"),
                Mind = reader.GetInt32("mind"),
                Spirit = reader.GetInt32("spirit"),
                Health = reader.GetInt32("health"),
                Armor = reader.GetInt32("armor"),
            };
        }
    }
}
