using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class SpawnPoolEntry
    {
        public int DbId { get; set; }
        public short Mode { get; set; }
        public short AnimType { get; set; }
        public int RespawnTime { get; set; }
        public double PosX { get; set; }
        public double PosY { get; set; }
        public double PosZ { get; set; }
        public double Rotation { get; set; }
        public int ContextId { get; set; }
        public int CreatureId1 { get; set; }
        public short CreatureMinCount1 { get; set; }
        public short CreatureMaxCount1 { get; set; }
        public int CreatureId2 { get; set; }
        public short CreatureMinCount2 { get; set; }
        public short CreatureMaxCount2 { get; set; }
        public int CreatureId3 { get; set; }
        public short CreatureMinCount3 { get; set; }
        public short CreatureMaxCount3 { get; set; }
        public int CreatureId4 { get; set; }
        public short CreatureMinCount4 { get; set; }
        public short CreatureMaxCount4 { get; set; }
        public int CreatureId5 { get; set; }
        public short CreatureMinCount5 { get; set; }
        public short CreatureMaxCount5 { get; set; }
        public int CreatureId6 { get; set; }
        public short CreatureMinCount6 { get; set; }
        public short CreatureMaxCount6 { get; set; }

        public static SpawnPoolEntry Read(MySqlDataReader reader)
        {
            return new SpawnPoolEntry
            {
                DbId = reader.GetInt32("dbId"),
                Mode = reader.GetInt16("mode"),
                AnimType = reader.GetInt16("animType"),
                RespawnTime = reader.GetInt32("respawnTime"),
                PosX = reader.GetDouble("posX"),
                PosY = reader.GetDouble("posY"),
                PosZ = reader.GetDouble("posZ"),
                Rotation = reader.GetDouble("rotation"),
                ContextId = reader.GetInt32("contextId"),
                CreatureId1 = reader.GetInt32("creatureId1"),
                CreatureMinCount1 = reader.GetInt16("creatureMinCount1"),
                CreatureMaxCount1 = reader.GetInt16("creatureMaxCount1"),
                CreatureId2 = reader.GetInt32("creatureId2"),
                CreatureMinCount2 = reader.GetInt16("creatureMinCount2"),
                CreatureMaxCount2 = reader.GetInt16("creatureMaxCount2"),
                CreatureId3 = reader.GetInt32("creatureId3"),
                CreatureMinCount3 = reader.GetInt16("creatureMinCount3"),
                CreatureMaxCount3 = reader.GetInt16("creatureMaxCount3"),
                CreatureId4 = reader.GetInt32("creatureId4"),
                CreatureMinCount4 = reader.GetInt16("creatureMinCount4"),
                CreatureMaxCount4 = reader.GetInt16("creatureMaxCount4"),
                CreatureId5 = reader.GetInt32("creatureId5"),
                CreatureMinCount5 = reader.GetInt16("creatureMinCount5"),
                CreatureMaxCount5 = reader.GetInt16("creatureMaxCount5"),
                CreatureId6 = reader.GetInt32("creatureId6"),
                CreatureMinCount6 = reader.GetInt16("creatureMinCount6"),
                CreatureMaxCount6 = reader.GetInt16("creatureMaxCount6"),
            };
        }
    }
}
