namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ClanMembersRosterBeginPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.ClanMembersRosterBegin;

        public uint ClanId { get; set; }

        public ClanMembersRosterBeginPacket(uint clanId)
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
