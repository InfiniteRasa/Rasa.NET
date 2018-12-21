using MySql.Data.MySqlClient;

namespace Rasa.Structures
{
    public class NpcMissionEntry
    {
        public uint MissionId { get; set; }
        public int Command { get; set; }
        public int Var1 { get; set; }
        public int Var2 { get; set; }
        public int Var3 { get; set; }

        public static NpcMissionEntry Read(MySqlDataReader reader)
        {
            return new NpcMissionEntry
            {
                MissionId = reader.GetUInt32("missionId"),
                Command = reader.GetInt32("command"),
                Var1 = reader.GetInt32("var1"),
                Var2 = reader.GetInt32("var2"),
                Var3 = reader.GetInt32("var3")
            };
        }
    }
}
