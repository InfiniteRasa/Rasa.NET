namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class SetTargetIdPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetTargetId;

        public ulong EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadULong();
        }
    }
}
