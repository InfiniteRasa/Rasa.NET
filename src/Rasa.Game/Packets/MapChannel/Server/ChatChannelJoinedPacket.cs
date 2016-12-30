namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ChatChannelJoinedPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChatChannelJoined;

        public int ChannelId { get; set; }
        public int MapContextId { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(ChannelId);
            pw.WriteInt(MapContextId);
        }
    }
}
