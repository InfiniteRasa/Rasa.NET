namespace Rasa.Packets.Clan.Server
{
    using Data;
    using Memory;

    public class ClanDeletedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.ClanDeleted;

        public uint ClanId { get; }

        public ClanDeletedPacket(uint clanId)
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
