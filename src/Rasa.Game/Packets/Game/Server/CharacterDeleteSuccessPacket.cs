namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class CharacterDeleteSuccessPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.CharacterDeleteSuccess;

        public bool HasCharacters { get; }

        public CharacterDeleteSuccessPacket(bool hasCharacters)
        {
            HasCharacters = hasCharacters;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(HasCharacters);
        }
    }
}
