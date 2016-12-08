using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Rasa.Config
{
    public static class Configuration
    {
        public delegate void OnLoadDelegate();

        public static IConfiguration Config { get; private set; }
        public static IChangeToken Token { get; private set; }
        public static OnLoadDelegate OnLoad;
        public static OnLoadDelegate OnReLoad;

        public static void Load()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile("appsettings.env.json", true, true);

            Config = builder.Build();
            Token = Config.GetReloadToken();
            Token.RegisterChangeCallback(OnChange, null);

            OnLoad?.Invoke();
        }

        public static void OnChange(object state)
        {
            OnReLoad?.Invoke();
        }

        public static void Bind(object obj)
        {
            Config.Bind(obj);
        }
    }
}
