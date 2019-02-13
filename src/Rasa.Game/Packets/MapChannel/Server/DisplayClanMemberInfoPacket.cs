namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class DisplayClanMemberInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.DisplayClanMemberInfo;

        public string ClanName { get; set; }
        public string Name { get; set; }
        public ClanMemberData Data { get; set; }

        public DisplayClanMemberInfoPacket(string clanName, string name, ClanMemberData data)
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
            pw.WriteTuple(11);
            pw.WriteBool(Data.IsOnline);
            pw.WriteUInt(Data.ContextId);
            pw.WriteUInt(Data.Level);
            pw.WriteString(Data.FamilyName);
            pw.WriteUInt(Data.UserId);
            pw.WriteUInt(Data.ClanId);
            pw.WriteString(Data.Rank);
            pw.WriteString(Data.Note);
            pw.WriteBool(Data.IsAfk);
            pw.WriteUInt(Data.CharacterId);
            pw.WriteString(Data.Map);
        }
    }
}
