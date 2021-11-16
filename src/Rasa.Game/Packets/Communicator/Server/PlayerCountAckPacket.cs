namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    public class PlayerCountAckPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PlayerCountAck;

        public uint MsgId { get; set; }
        public uint Count { get; set; }

        public PlayerCountAckPacket(uint msgId, uint count)
        {
            MsgId = msgId;
            Count = count;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(MsgId);
            pw.WriteUInt(Count);
        }
    }
}
