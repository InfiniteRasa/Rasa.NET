namespace Rasa.Packets.Clan.Server
{
    using Data;
    using Memory;

    public class ClanCreatedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.ClanCreated;

        public uint ClanId { get; }

        public ClanCreatedPacket(uint clanId)
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
