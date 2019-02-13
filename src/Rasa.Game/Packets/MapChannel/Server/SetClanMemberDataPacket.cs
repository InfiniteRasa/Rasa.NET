namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class SetClanMemberDataPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.SetClanMemberData;

        public string Name { get; set; }
        public ClanMemberData Data { get; set; }

        public SetClanMemberDataPacket(string name, ClanMemberData data)
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
