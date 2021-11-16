namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    public class ChatChannelJoinedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChatChannelJoined;

        public uint ChannelId { get; set; }
        public ulong MapEntityId { get; set; }
        public uint MapContextId { get; set; }
        
        public ChatChannelJoinedPacket(uint channelId, uint mapContextId = 0, ulong mapEntityId = 0)
        {
            ChannelId = channelId;
            MapEntityId = mapEntityId;
            MapContextId = mapContextId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(ChannelId);

            if (MapEntityId != 0)
                pw.WriteULong(MapEntityId);
            else
                pw.WriteNoneStruct();

            if (MapContextId != 0)
                pw.WriteUInt(MapContextId);
            else
                pw.WriteNoneStruct();
        }
    }
}
