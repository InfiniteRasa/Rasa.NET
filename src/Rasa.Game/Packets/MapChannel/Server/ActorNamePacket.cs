namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ActorNamePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActorName;

        public string CharacterFamily { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteString(CharacterFamily);
        }
    }
}