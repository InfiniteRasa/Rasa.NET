namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class DisplayClanLeaderInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.DisplayClanLeaderInfo;

        public string ClanName { get; set; }
        public string Name { get; set; }
        public ClanMemberData Data { get; set; }

        public DisplayClanLeaderInfoPacket(string clanName, string name, ClanMemberData data)
        {
            ClanName = clanName;
            Name = name;
            Data = data;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteString(ClanName);
            pw.WriteString(Name);
            pw.WriteStruct(Data);
        }
    }
}
