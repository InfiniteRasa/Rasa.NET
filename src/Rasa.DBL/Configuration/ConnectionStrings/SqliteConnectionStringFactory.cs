namespace Rasa.Configuration.ConnectionStrings
{
    using Microsoft.Data.Sqlite;

    public class SqliteConnectionStringFactory : IConnectionStringFactory
    {
        public string Create(DatabaseConnectionConfiguration configuration)
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = $"{configuration.Database}.db"
            };

            return builder.ConnectionString;
        }
    }
}