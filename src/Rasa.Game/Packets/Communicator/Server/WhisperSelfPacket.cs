namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    public class WhisperSelfPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WhisperSelf;
        
        public string Message { get; set; }

        public WhisperSelfPacket(string message)
        {
            Message = message;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUnicodeString(Message);
        }
    }
}
