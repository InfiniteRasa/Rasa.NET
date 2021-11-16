namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    public class AddIgnoreAckPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AddIgnoreAck;

        public string FamilyName { get; set; }
        public bool Success { get; set; }

        public AddIgnoreAckPacket(string familyName, bool success)
        {
            FamilyName = familyName;
            Success = success;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUnicodeString(FamilyName);
            pw.WriteBool(Success);
        }
    }
}
