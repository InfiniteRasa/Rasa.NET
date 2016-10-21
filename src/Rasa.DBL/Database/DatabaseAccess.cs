using MySql.Data.MySqlClient;

namespace Rasa.Database
{
    using Tables;

    public static class DatabaseAccess
    {
        public static MySqlConnection Connection { get; private set; }

        public static object Lock { get; } = new object();

        public static void Initialize(string connectionString)
        {
            Connection = new MySqlConnection(connectionString);
            Connection.Open();

            // TODO: load by reflection and call init for every class?
            AccountTable.Initialize();
        }
    }
}
