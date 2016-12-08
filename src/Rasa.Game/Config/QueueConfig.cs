namespace Rasa.Config
{
    public class QueueConfig
    {
        public string PublicKey { get; set; }
        public string Prime { get; set; }
        public string Generator { get; set; }
        public int Port { get; set; }
        public int Backlog { get; set; }
        public int UpdateInterval { get; set; }
    }
}
