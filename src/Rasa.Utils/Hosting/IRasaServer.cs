namespace Rasa.Hosting
{
    public interface IRasaServer
    {
        string ServerType { get; }

        bool Start();

        bool Running { get; }
    }
}