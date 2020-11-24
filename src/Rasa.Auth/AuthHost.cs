using Microsoft.Extensions.Hosting;

namespace Rasa
{
    using Auth;
    using Hosting;

    public class AuthHost : RasaHost
    {
        public AuthHost(IHostApplicationLifetime hostApplicationLifetime) : base(new Server(hostApplicationLifetime))
        {
        }
    }
}