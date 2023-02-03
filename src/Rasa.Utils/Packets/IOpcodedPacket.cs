namespace Rasa.Packets;

public interface IOpcodedPacket<out T> : IBasePacket
{
    T Opcode { get; }
}