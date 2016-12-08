namespace Rasa.Config
{
    public class SocketAsyncConfig
    {
        public int BufferSize { get; set; }
        public int MaxClients { get; set; }
        public int ConcurrentOperationsByClient { get; set; }
    }
}
