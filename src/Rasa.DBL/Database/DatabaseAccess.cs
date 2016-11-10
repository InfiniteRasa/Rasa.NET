using System.Linq;
using System.Reflection;

using MySql.Data.MySqlClient;

namespace Rasa.Database
{
    public static class DatabaseAccess
    {
        public static MySqlConnection Connection { get; private set; }

        public static object Lock { get; } = new object();

        public static void Initialize(string connectionString)
        {
            Connection = new MySqlConnection(connectionString);
            Connection.Open();

            foreach (var type in typeof(DatabaseAccess).GetTypeInfo().Assembly.GetTypes().Where(c => c.Namespace == "Rasa.Database.Tables"))
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
