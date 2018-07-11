namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class CharacterNamePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterName;

        public string CharacterName { get; set; }

        public CharacterNamePacket(string characterName)
        {
            CharacterName = characterName;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteString(CharacterName);
        }
    }
}
