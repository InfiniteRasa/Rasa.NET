namespace Rasa.Packets.Clan.Server
{
    using Data;
    using Memory;

    public class ClanMembersRosterEndPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.ClanMembersRosterEnd;

        public uint ClanId { get; }

        public ClanMembersRosterEndPacket(uint clanId)
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
