using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CreaturesEntry
    {
        public uint DbId { get; set; }
        public int ClassId { get; set; }
        public int Faction { get; set; }
        public int Level { get; set; }
        public int MaxHitPoints { get; set; }
        public int NameId { get; set; }
        public float RunSpeed { get; set; }
        public float WalkSpeed { get; set; }
        public uint Action1 { get; set; }
        public uint Action2 { get; set; }
        public uint Action3 { get; set; }
        public uint Action4 { get; set; }
        public uint Action5 { get; set; }
        public uint Action6 { get; set; }
        public uint Action7 { get; set; }
        public uint Action8 { get; set; }

        public static CreaturesEntry Read(MySqlDataReader reader)
        {
            return new CreaturesEntry
            {
                DbId = reader.GetUInt32("dbId"),
                ClassId = reader.GetInt32("classId"),
                Faction = reader.GetInt32("faction"),
                Level = reader.GetInt32("level"),                
                MaxHitPoints = reader.GetInt32("maxHitPoints"),
                NameId = reader.GetInt32("nameId"),
                RunSpeed = reader.GetFloat("run_speed"),
                WalkSpeed = reader.GetFloat("walk_speed"),
                Action1 = reader.GetUInt32("action1"),
                Action2 = reader.GetUInt32("action2"),
                Action3 = reader.GetUInt32("action3"),
                Action4 = reader.GetUInt32("action4"),
                Action5 = reader.GetUInt32("action5"),
                Action6 = reader.GetUInt32("action6"),
                Action7 = reader.GetUInt32("action7"),
                Action8 = reader.GetUInt32("action8")
            };
        }
    }
}