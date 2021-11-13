namespace Rasa.Packets.Clan.Server
{
    using Data;
    using Memory;

    public class ClanDisbandedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ClanDisbanded;
        
        public uint ClanId { get; set; }

        public ClanDisbandedPacket(uint clanId)
        {
            ClanId = clanId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(ClanId);
        }
    }
}
