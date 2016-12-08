namespace Rasa.Packets
{
    using Networking;

    public class QueuedPacket
    {
        public INetworkClient Client { get; set; }
        public IBasePacket Packet { get; set; }
    }
}
