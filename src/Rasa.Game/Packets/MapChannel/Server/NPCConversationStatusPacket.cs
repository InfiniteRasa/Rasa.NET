using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class NPCConversationStatusPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.NPCConversationStatus;

        public ConversationStatus ConvoStatusId { get; set; }
        public List<int> Data { get; set; }

        public NPCConversationStatusPacket(ConversationStatus convoStatusId, List<int> data)
        {
            ConvoStatusId = convoStatusId;
            Data = data;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt((int)ConvoStatusId);
            pw.WriteList(Data.Count);
            foreach (var data in Data)
                pw.WriteInt(data);
        }
    }
}
