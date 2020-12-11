namespace Rasa.Configuration
{
    public class DatabaseConnectionConfiguration
    {
        public string Host { get; set; }
        public uint Port { get; set; }
        public string Database { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public int TimeoutInMilliseconds { get; set; }
    }
}