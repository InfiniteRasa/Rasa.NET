
using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class CharacterMissionsEntry
    {
        public int CharacterSlot { get; set; }
        public int MissionId { get; set; }
        public short MissionState { get; set; }

        public static CharacterMissionsEntry Read(MySqlDataReader reader)
        {
            return new CharacterMissionsEntry
            {
                CharacterSlot = reader.GetInt32("characterSlot"),
                MissionId = reader.GetInt32("missionId"),
                MissionState = reader.GetInt16("missionState")
            };
        }
    }
}
