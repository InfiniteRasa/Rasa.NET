namespace Rasa.Packets.Social.Server
{
    using Data;
    using Memory;

    public class AddFriendAckPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AddFriendAck;

        public string FamilyName { get; set; }
        public bool Success { get; set; }

        public AddFriendAckPacket(string familyName, bool success)
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
