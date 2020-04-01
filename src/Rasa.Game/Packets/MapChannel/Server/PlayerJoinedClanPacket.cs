namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class PlayerJoinedClanPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.PlayerJoinedClan;

        public string Name { get; set; }
        public ClanMemberData Data { get; set; }

        public PlayerJoinedClanPacket(string name, ClanMemberData data)
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
