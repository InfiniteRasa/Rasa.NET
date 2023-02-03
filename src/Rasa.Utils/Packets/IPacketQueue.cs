namespace Rasa.Packets;

public interface IPacketQueue
{
    void EnqueueIncoming(IBasePacket data);
    void EnqueueOutgoing(IBasePacket data);
    IBasePacket? PopIncoming();
    IBasePacket? PopOutgoing();
}
