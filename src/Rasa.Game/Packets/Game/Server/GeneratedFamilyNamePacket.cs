namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class GeneratedFamilyNamePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GeneratedFamilyName;

        public string Name { get; set; }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUnicodeString(Name);
        }
    }
}
