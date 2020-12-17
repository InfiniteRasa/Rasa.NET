using System;
using System.Linq;

namespace Rasa.Configuration
{
    public class DatabaseConfiguration
    {
        public string Provider { get; set; }

        public DatabaseConnectionConfiguration Auth { get; set; }

        public DatabaseConnectionConfiguration Char { get; set; }

        public DatabaseProvider GetDatabaseProvider()
        {
            return ConvertDatabaseProvider(Provider);
        }

        public static DatabaseProvider ConvertDatabaseProvider(string databaseProviderStr)
        {
            if (Enum.TryParse(databaseProviderStr, true, out DatabaseProvider databaseProvider))
            {
                return databaseProvider;
            }
            var validValues = Enum.GetValues(typeof(DatabaseProvider))
                .Cast<DatabaseProvider>()
                .Select(v => v.ToString());
            throw new ArgumentOutOfRangeException(nameof(databaseProviderStr),
                $"The value '{databaseProviderStr}' is no valid database provider. Valid values are: {string.Join(", ", validValues)}");
        }
    }
}