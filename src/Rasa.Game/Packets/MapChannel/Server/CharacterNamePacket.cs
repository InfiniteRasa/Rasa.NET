namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class CharacterNamePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterName;

        public string CharacterName { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteString(CharacterName);
        }
    }
}
