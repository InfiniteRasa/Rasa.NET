namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class DisplayClanMemberInfoHeaderPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.DisplayClanMemberInfoHeader;

        public string ClanName { get; set; }
        public uint ClanId { get; set; }

        public DisplayClanMemberInfoHeaderPacket(string clanName, uint clanId)
        {
            ClanName = clanName;
            ClanId = clanId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteString(ClanName);
            pw.WriteUInt(ClanId);
        }
    }
}
