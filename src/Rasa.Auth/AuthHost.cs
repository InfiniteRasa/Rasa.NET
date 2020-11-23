namespace Rasa
{
    using Auth;
    using Hosting;

    public class AuthHost : RasaHost
    {
        public AuthHost() : base(new Server())
        {
        }
    }
}