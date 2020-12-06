using Microsoft.Extensions.Hosting;

namespace Rasa
{
    using Game;
    using Hosting;

    public class GameHost : RasaHost
    {
        public GameHost(IHostApplicationLifetime hostApplicationLifetime) : base(new Server(hostApplicationLifetime))
        {
        }
    }
}