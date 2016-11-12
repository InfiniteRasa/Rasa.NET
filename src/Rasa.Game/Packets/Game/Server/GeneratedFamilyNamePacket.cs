namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class GeneratedFamilyNamePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GeneratedFamilyName;

        public string Name { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Name = pr.ReadUnicodeString();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUnicodeString(Name);
        }
    }
}
