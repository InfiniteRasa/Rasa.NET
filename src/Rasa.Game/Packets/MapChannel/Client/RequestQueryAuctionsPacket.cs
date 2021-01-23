namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestQueryAuctionsPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestQueryAuctions;

        public ulong EntityId { get; set; }
        public uint MinLevel { get; set; }
        public uint MaxLevel { get; set; }
        public uint QualityId { get; set; }
        public uint CategoryId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadULong();
            MinLevel = pr.ReadUInt();
            MaxLevel = pr.ReadUInt();
            QualityId = pr.ReadUInt();
            CategoryId = pr.ReadUInt();
        }
    }
}
