using System.Linq;
using System.Reflection;

using MySql.Data.MySqlClient;

namespace Rasa.Database
{
    public class GameDatabaseAccess
    {
        public static WorldContext WorldConnection { get; private set; }
        public static MySqlConnection CharConnection { get; private set; }

        public static object WorldLock { get; } = new object();
        public static object CharLock { get; } = new object();

        public static void Initialize(WorldContext worldContext, string charConnectionString)
        {
            WorldConnection = worldContext;

            CharConnection = new MySqlConnection(charConnectionString);
            CharConnection.Open();

            foreach (var type in typeof(AuthDatabaseAccess).GetTypeInfo().Assembly.GetTypes().Where(c => (c.Namespace == "Rasa.Database.Tables.World" || c.Namespace == "Rasa.Database.Tables.Character") && !c.IsNested && c.GetTypeInfo().IsClass))
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
