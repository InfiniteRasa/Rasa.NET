namespace Rasa.Packets
{
    using Networking;

    public interface IPacketQueue
    {
        void EnqueueIncoming(INetworkClient client, IBasePacket data);

        void EnqueueOutgoing(INetworkClient client, IBasePacket data);
    }
}
