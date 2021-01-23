namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestGesturePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestGesture;

        public uint GestureId { get; set; }
        public ulong EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            GestureId = pr.ReadUInt();
            EntityId = pr.ReadULong();
        }
    }
}
