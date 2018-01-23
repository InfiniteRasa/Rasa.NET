using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class NpcMissionsTable
    {
        private static readonly MySqlCommand GetNpcMissionsCommand = new MySqlCommand("SELECT * FROM npc_missions");

        public static void Initialize()
        {
            GetNpcMissionsCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetNpcMissionsCommand.Prepare();
        }

        public static List<NpcMissionEntry> GetNpcMissions()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var missions = new List<NpcMissionEntry>();
                using (var reader = GetNpcMissionsCommand.ExecuteReader())
                    while (reader.Read())
                        missions.Add(NpcMissionEntry.Read(reader));

                return missions;
            }
        }
    }
}
