using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Rasa.Config;

public static class Configuration
{
    public delegate void OnLoadDelegate();

    private static IChangeToken? Token { get; set; }
    private static IConfiguration? Config { get; set; }

    public static OnLoadDelegate? OnLoad { get; set; }
    public static OnLoadDelegate? OnReLoad { get; set; }

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
