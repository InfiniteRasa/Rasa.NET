namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ChatChannelJoinedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChatChannelJoined;

        public uint ChannelId { get; set; }
        public uint MapContextId { get; set; }

        public ChatChannelJoinedPacket(uint channelId, uint mapContextId)
        {
            ChannelId = channelId;
            MapContextId = mapContextId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(ChannelId);
            pw.WriteUInt(MapContextId);
        }
    }
}
