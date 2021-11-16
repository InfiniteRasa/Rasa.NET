namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    public class SystemMessagePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SystemMessage;

        public string TextMessage { get; set; }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteString(TextMessage);
        }
    }
}
