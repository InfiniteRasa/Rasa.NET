namespace Rasa.Packets.Protocol
{
    public interface ISubtypedPacket<T> : IClientMessage
    {
        T Subtype { get; set; }
    }
}
