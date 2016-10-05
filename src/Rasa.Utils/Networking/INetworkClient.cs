namespace Rasa.Networking
{
    using Packets;

    public interface INetworkClient
    {
        void HandlePacket(IBasePacket packet);
        void SendPacket(IBasePacket packet);
    }
}
