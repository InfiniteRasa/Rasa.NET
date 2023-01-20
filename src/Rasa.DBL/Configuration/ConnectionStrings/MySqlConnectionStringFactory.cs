namespace Rasa.Configuration.ConnectionStrings
{
    using System;
    using MySqlConnector;

    public class MySqlConnectionStringFactory : IConnectionStringFactory
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