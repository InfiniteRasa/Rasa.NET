namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class SystemMessagePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SystemMessage;

        public string TextMessage { get; set; }
        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteString(TextMessage);
        }
    }
}
