namespace Rasa.Database
{
    public static class AuthDatabaseAccess
    {
        public static AuthContext Connection { get; private set; }

        public static object Lock { get; } = new object();

        public static void Initialize(AuthContext connection)
        {
            Connection = connection;
        }
    }
}
