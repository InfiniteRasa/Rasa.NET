using System;

using Microsoft.Extensions.DependencyInjection;

namespace Rasa.Config
{
    using Auth;
    using Hosting;
    using Services.Passwords;

    /// <summary>
    /// The password hash settings in force: the auth server's current PasswordHashConfig, read
    /// at every hash so a reload of appsettings.json applies to the next login. The server is
    /// looked up when first needed rather than injected, as the server itself is what builds
    /// the repositories that ask.
    /// </summary>
    public class AuthPasswordHashSettings : IPasswordHashSettings
    {
        private readonly IServiceProvider _services;

        public AuthPasswordHashSettings(IServiceProvider services)
        {
            _services = services;
        }

        private PasswordHashConfig Current =>
            (_services.GetService<IRasaServer>() as Server)?.Config?.PasswordHashConfig ?? new PasswordHashConfig();

        public string Pepper => Current.Pepper ?? string.Empty;

        public int Iterations => Current.Iterations;
    }
}
