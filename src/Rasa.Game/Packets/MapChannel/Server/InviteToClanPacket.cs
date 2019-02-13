namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class InviteToClanPacket : ServerPythonPacket
    {
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
            pw.WriteTuple(5);
            pw.WriteString(Data.InviterFamilyName);
            pw.WriteUInt(Data.ClanId);
            pw.WriteString(Data.ClanName);
            pw.WriteBool(Data.IsPvP);
            pw.WriteUInt(Data.InvitedCharacterId); 
        }
    }
}
