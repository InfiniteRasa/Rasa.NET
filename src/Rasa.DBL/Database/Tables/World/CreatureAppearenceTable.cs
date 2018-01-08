using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class CreatureAppearenceTable
    {
        private static readonly MySqlCommand GetCreatureAppearenceCommand = new MySqlCommand("SELECT * FROM creature_appearence WHERE dbId = @CreatureDbId");

        public static void Initialize()
        {
            GetCreatureAppearenceCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetCreatureAppearenceCommand.Parameters.Add("@CreatureDbId", MySqlDbType.UInt32);
            GetCreatureAppearenceCommand.Prepare();
        }

        public static CreatureAppearenceEntry GetCreatureAppearence(uint creatureDbId)
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                GetCreatureAppearenceCommand.Parameters["@CreatureDbId"].Value = creatureDbId;

                using (var reader = GetCreatureAppearenceCommand.ExecuteReader())
                    if (reader.Read())
                        return CreatureAppearenceEntry.Read(reader);

                return null;
            }
        }
    }
}
