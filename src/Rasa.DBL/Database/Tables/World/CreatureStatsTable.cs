using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class CreatureStatsTable
    {
        private static readonly MySqlCommand GetCreatureStatsCommand = new MySqlCommand("SELECT * FROM creature_stats WHERE creatureDbId = @CreatureDbId");

        public static void Initialize()
        {
            GetCreatureStatsCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetCreatureStatsCommand.Parameters.Add("@CreatureDbId", MySqlDbType.Int32);
            GetCreatureStatsCommand.Prepare();
        }

        public static CreatureStatsEntry GetCreatureStats(int creatureDbId)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetCreatureStatsCommand.Parameters["@CreatureDbId"].Value = creatureDbId;

                using (var reader = GetCreatureStatsCommand.ExecuteReader())
                    if (reader.Read())
                        return CreatureStatsEntry.Read(reader);

                return null;
            }
        }
    }
}