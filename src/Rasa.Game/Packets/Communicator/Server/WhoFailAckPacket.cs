namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    public class WhoFailAckPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WhoFailAck;

        public string Name { get; set; }
        public uint MessageId { get; set; }

        public WhoFailAckPacket(string name, uint msgId)
        {
            Name = name;
            MessageId = msgId;
        }
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUnicodeString(Name);
            pw.WriteUInt(MessageId);
        }
    }
}
