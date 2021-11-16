namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    public class ChatChannelLeftPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChatChannelLeft;

        public uint ChannelId { get; set; }
        public uint MapContextId { get; set; }

        public ChatChannelLeftPacket(uint channelId)
        {
            ChannelId = channelId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(ChannelId);
            pw.WriteNoneStruct();       // mapEntityId      => not used by game client
            pw.WriteNoneStruct();       // gameContextId    => not used by game client
        }
    }
}
