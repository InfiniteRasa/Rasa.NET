namespace Rasa.Structures
{
    using Game;

    public class AutoFireTimer
    {
        public Client Client { get; set; }
        public long RefireTime { get; set; }
        public long MaxAliveTime { get; set; }
        public long Delay { get; set; }

        public AutoFireTimer(Client client, long refireTime, long delay)
        {
            Client = client;
            RefireTime = refireTime;
            Delay = delay;
        }
    }
}
