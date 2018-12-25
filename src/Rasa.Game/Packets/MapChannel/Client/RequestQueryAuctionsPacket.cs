namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestQueryAuctionsPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestQueryAuctions;

        public uint EntityId { get; set; }      // g_auctioneerId
        public uint MinLevel { get; set; }      // minLevel
        public uint MaxLevel { get; set; }      // maxLevel
        public uint QualityId { get; set; }     // qualityId
        public uint CategoryId { get; set; }    // categoryId

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = (uint)pr.ReadLong();
            MinLevel = pr.ReadUInt();
            MaxLevel = pr.ReadUInt();
            QualityId = pr.ReadUInt();
            CategoryId = pr.ReadUInt();
        }
    }
}
