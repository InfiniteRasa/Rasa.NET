using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CreaturesEntry
    {
        public uint DbId { get; set; }
        public uint CreatureType { get; set; }
        public int Faction { get; set; }
        public int Level { get; set; }
        public int MaxHitPoints { get; set; }
        public int NameId { get; set; }

        public static CreaturesEntry Read(MySqlDataReader reader)
        {
            return new CreaturesEntry
            {
                DbId = reader.GetUInt32("dbId"),
                CreatureType = reader.GetUInt32("creatureType"),
                Faction = reader.GetInt32("faction"),
                Level = reader.GetInt32("level"),                
                MaxHitPoints = reader.GetInt32("maxHitPoints"),
                NameId = reader.GetInt32("nameId")
            };
        }
    }
}