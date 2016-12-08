namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class CharacterDeleteSuccessPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterDeleteSuccess;

        public int HasCharacters { get; set; }

        public CharacterDeleteSuccessPacket(bool hasCharacters)
        {
            HasCharacters = hasCharacters ? 1 : 0;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(HasCharacters);
        }
    }
}
