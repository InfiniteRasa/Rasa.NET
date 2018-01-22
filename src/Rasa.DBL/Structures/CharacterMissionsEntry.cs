
using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CharacterMissionsEntry
    {
        public int Characterid { get; set; }
        public int MissionId { get; set; }
        public short MissionState { get; set; }

        public static CharacterMissionsEntry Read(MySqlDataReader reader)
        {
            return new CharacterMissionsEntry
            {
                Characterid = reader.GetInt32("characterid"),
                MissionId = reader.GetInt32("missionId"),
                MissionState = reader.GetInt16("missionState")
            };
        }
    }
}
