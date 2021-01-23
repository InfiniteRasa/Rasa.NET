namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ChannelChatPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChannelChat;

        public string Name { get; set; }
        public int ChannelId { get; set; }
        public ulong MapEntityId { get; set; }
        public uint MapContextId { get; set; }
        public string Message { get; set; }

        public ChannelChatPacket(string name, int channelId, ulong mapEntityId, uint mapContextId, string message)
        {
            Name = name;
            ChannelId = channelId;
            MapEntityId = mapEntityId;
            MapContextId = mapContextId;
            Message = message;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteString(Name);
            pw.WriteTuple(3);
            pw.WriteInt(ChannelId);
            pw.WriteULong(MapEntityId);
            pw.WriteUInt(MapContextId);
            pw.WriteString(Message);
        }
    }
}
