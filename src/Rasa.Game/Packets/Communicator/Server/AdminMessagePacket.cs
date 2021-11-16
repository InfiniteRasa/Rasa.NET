namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    public class AdminMessagePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AdminMessage;

        public string Message { get; set; }
        public uint FilterId { get; set; }

        public AdminMessagePacket(string msg, uint filterId)
        {
            Message = msg;
            FilterId = filterId;
        }
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUnicodeString(Message);
            pw.WriteUInt(FilterId);
        }
    }
}
