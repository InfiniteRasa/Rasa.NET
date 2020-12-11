using System;

using MySqlConnector;

namespace Rasa.Configuration
{

    public class ConnectionStringFactory
    {
        public string Create(DatabaseConnectionConfiguration configuration)
        {
            var timeoutInSeconds = (uint)TimeSpan.FromMilliseconds(configuration.TimeoutInMilliseconds).TotalSeconds;

            var builder = new MySqlConnectionStringBuilder
            {
                Server = configuration.Host, 
                Port = configuration.Port, 
                UserID = configuration.User, 
                Password = configuration.Password,
                Database = configuration.Database, 
                ConnectionTimeout = timeoutInSeconds, 
                DefaultCommandTimeout = timeoutInSeconds, 
                ConvertZeroDateTime = true,
                CharacterSet = "utf8"
            };

            return builder.ConnectionString;
        }
    }
}