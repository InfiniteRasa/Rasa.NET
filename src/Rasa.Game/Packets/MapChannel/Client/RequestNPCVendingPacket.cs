namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestNPCVendingPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestNPCVending;

        public ulong EntityId { get; set; }
        public int ChosenVendorPackage { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadULong();
            ChosenVendorPackage = pr.ReadInt();
        }
    }
}
