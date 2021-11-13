namespace Rasa.Packets.Clan.Server
{
    using Data;
    using Memory;
    using Structures;

    public class InviteToClanPacket : ServerPythonPacket
    {
        public static readonly string NameKey = "ClanInviteData";

        public override GameOpcode Opcode => GameOpcode.InviteToClan;

        public string Name { get; set; }

        public ClanInviteData Data { get; set; }

        public InviteToClanPacket(string name, ClanInviteData data )
        {
            Name = name;
            Data = data;
        }
        
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteString(Name);
            pw.WriteStruct(Data);
        }
    }
}
