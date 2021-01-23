using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class UpdateAttributesPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateAttributes;

        public Dictionary<Attributes, ActorAttributes> AttributeDataList { get; set; }
        public ulong WhoId { get; set; }

        public UpdateAttributesPacket(Dictionary<Attributes, ActorAttributes> attributesDataList, ulong whoId)
        {
            AttributeDataList = attributesDataList;
            WhoId = whoId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteList(AttributeDataList.Count);
            foreach (var entry in AttributeDataList)
            {
                var attribute = entry.Value;
                pw.WriteTuple(5);
                pw.WriteInt((int)attribute.AttributeId);
                pw.WriteInt(attribute.Current);
                pw.WriteInt(attribute.NormalMax);
                pw.WriteInt(attribute.CurrentMax);
                pw.WriteInt(attribute.RefreshAmount);
            }
            pw.WriteULong(WhoId);
        }
    }
}