namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ChannelChatPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChannelChat;

        public string Name { get; set; }
        public int Target { get; set; }
        public uint MapEntityId { get; set; }
        public int MapContextId { get; set; }
        public string Message { get; set; }
        
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteString(Name);
            pw.WriteTuple(3);
            pw.WriteInt(Target);
            pw.WriteInt((int)MapEntityId);
            pw.WriteInt(MapContextId);
            pw.WriteString(Message);
        }
    }
}
