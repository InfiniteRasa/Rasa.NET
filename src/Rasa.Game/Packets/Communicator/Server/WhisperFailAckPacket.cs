namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    public class WhisperFailAckPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WhisperFailAck;

        public string FamilyName { get; set; }
        public string Message { get; set; }

        public WhisperFailAckPacket(string familyName, string message)
        {
            FamilyName = familyName;
            Message = message;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUnicodeString(FamilyName);
            pw.WriteUnicodeString(Message);
        }
    }
}
