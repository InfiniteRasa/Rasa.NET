namespace Rasa
{
    using Game;
    using Hosting;

    public class GameHost : RasaHost
    {
        public GameHost() : base(new Server())
        {
        }
    }
}