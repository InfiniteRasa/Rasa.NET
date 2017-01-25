namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class CharacterClassPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterClass;

        public int CharacterClass { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(CharacterClass);
        }
    }
}
