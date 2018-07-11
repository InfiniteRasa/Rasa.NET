namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class CharacterClassPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterClass;

        public uint CharacterClass { get; set; }

        public CharacterClassPacket(uint characterClass)
        {
            CharacterClass = characterClass;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(CharacterClass);
        }
    }
}
