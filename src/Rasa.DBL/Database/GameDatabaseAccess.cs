using System.Linq;
using System.Reflection;

using MySql.Data.MySqlClient;

namespace Rasa.Database
{
    public class GameDatabaseAccess
    {
        public static MySqlConnection WorldConnection { get; private set; }

        public static object WorldLock { get; } = new object();

        public static void Initialize(string worldConnectionString, string charConnectionString)
        {
            WorldConnection = new MySqlConnection(worldConnectionString);
            WorldConnection.Open();

            foreach (var type in typeof(GameDatabaseAccess).GetTypeInfo().Assembly.GetTypes().Where(c => (c.Namespace == "Rasa.Database.Tables.World" || c.Namespace == "Rasa.Database.Tables.Character") && !c.IsNested && c.GetTypeInfo().IsClass))
            {
                var method = type.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    Logger.WriteLog(LogType.Error, $"Table class {type.FullName} has no public static void Initialize()!");
                    continue;
                }

                method.Invoke(null, null);
            }
        }
    }
}
