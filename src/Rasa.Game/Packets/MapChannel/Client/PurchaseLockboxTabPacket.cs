namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class PurchaseLockboxTabPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PurchaseLockboxTab;

        public uint TabId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            TabId = pr.ReadUInt();
        }
    }
}
