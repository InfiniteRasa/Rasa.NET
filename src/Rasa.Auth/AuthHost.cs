using Microsoft.Extensions.Hosting;

namespace Rasa
{
    using Hosting;

    public class AuthHost : RasaHost
    {
        public AuthHost(IRasaServer server, IHostApplicationLifetime lifetime) : base(server, lifetime)
        {
        }
    }
}