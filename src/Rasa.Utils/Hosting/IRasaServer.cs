namespace Rasa.Hosting
{
    using System.Threading;

    public interface IRasaServer
    {
        string ServerType { get; }

        bool Start(CancellationTokenSource stopToken);

        bool Running { get; }
    }
}