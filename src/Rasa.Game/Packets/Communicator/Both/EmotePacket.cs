namespace Rasa.Packets.Communicator.Both
{
    using Data;
    using Memory;

    public class EmotePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Emote;

        public string Emote { get; set; }
        private string Name;
        
        public EmotePacket()
        {
        }

        public EmotePacket(string name, string emote)
        {
            Name = name;
            Emote = emote;
        }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Emote = pr.ReadUnicodeString();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUnicodeString(Name);
            pw.WriteUnicodeString(Emote);
        }
    }
}
