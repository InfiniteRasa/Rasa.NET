namespace Rasa.Packets.Communicator.Both
{
    using Data;
    using Memory;

    public class ChannelChatPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChannelChat;

        public string Name { get; set; }
        public uint ChannelId { get; set; }
        public ulong MapEntityId { get; set; }
        public uint MapContextId { get; set; }
        public string Message { get; set; }

        public ChannelChatPacket()
        {
        }

        public ChannelChatPacket(string name, uint channelId, ulong mapEntityId, uint mapContextId, string message)
        {
            Name = name;
            ChannelId = channelId;
            MapEntityId = mapEntityId;
            MapContextId = mapContextId;
            Message = message;
        }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ChannelId = pr.ReadUInt();

            if (pr.PeekType() == PythonType.Long)
                MapEntityId = pr.ReadULong();
            else
                pr.ReadNoneStruct();

            if (pr.PeekType() == PythonType.Int)
                MapContextId = pr.ReadUInt();
            else
                pr.ReadNoneStruct();

            Message = pr.ReadUnicodeString();
        }
        
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteString(Name);
            pw.WriteTuple(3);
            pw.WriteUInt(ChannelId);
            pw.WriteULong(MapEntityId);
            pw.WriteUInt(MapContextId);
            pw.WriteUnicodeString(Message);
        }
    }
}
