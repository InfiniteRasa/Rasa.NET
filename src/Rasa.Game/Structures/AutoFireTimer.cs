namespace Rasa.Structures
{
    using Game;

    public class AutoFireTimer
    {
        public Client Client { get; set; }
        public int RefireTime { get; set; }
        public long Delay { get; set; }

        public AutoFireTimer(Client client, int refireTime, long delay)
        {
            Client = client;
            RefireTime = refireTime;
            Delay = delay;
        }
    }
}
