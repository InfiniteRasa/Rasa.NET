using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CreaturesEntry
    {
        public int DbId { get; set; }
        public int ClassId { get; set; }
        public int Faction { get; set; }
        public int Level { get; set; }
        public int MaxHitPoints { get; set; }
        public int NameId { get; set; }

        public static CreaturesEntry Read(MySqlDataReader reader)
        {
            return new CreaturesEntry
            {
                DbId = reader.GetInt32("dbId"),
                ClassId = reader.GetInt32("classId"),
                Faction = reader.GetInt32("faction"),
                Level = reader.GetInt32("level"),                
                MaxHitPoints = reader.GetInt32("maxHitPoints"),
                NameId = reader.GetInt32("nameId")
            };
        }
    }
}