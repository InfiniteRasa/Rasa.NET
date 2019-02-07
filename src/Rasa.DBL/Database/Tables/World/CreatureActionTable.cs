using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Rasa.Database.Tables.World
{
    using Structures;

    public class CreatureActionTable
    {
        private static readonly MySqlCommand GetCreatureActionsCommand = new MySqlCommand("SELECT * FROM creature_action");

        public static void Initialize()
        {

            GetCreatureActionsCommand.Connection = GameDatabaseAccess.WorldConnection;
            GetCreatureActionsCommand.Prepare();
        }

        public static List<CreatureActionEntry> GetCreatureActions()
        {
            lock (GameDatabaseAccess.WorldLock)
            {
                var creatureActions = new List<CreatureActionEntry>();

                using (var reader = GetCreatureActionsCommand.ExecuteReader())
                    while (reader.Read())
                        creatureActions.Add(CreatureActionEntry.Read(reader));

                return creatureActions;
            }
        }
    }
}
