using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class AttributeInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AttributeInfo;
        
        public Dictionary<Attributes, ActorAttributes> ActorAttributes { get; set; }
        
        public AttributeInfoPacket(Dictionary<Attributes, ActorAttributes> actorAttributes)
        {
            ActorAttributes = actorAttributes;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(ActorAttributes.Count);
            foreach (var entry in ActorAttributes)
            {
                var attribute = entry.Value;
                pw.WriteInt((int)attribute.AttributeId);
                pw.WriteTuple(5);
                pw.WriteInt(attribute.Current);
                pw.WriteInt(attribute.CurrentMax);
                pw.WriteInt(attribute.NormalMax);
                pw.WriteInt(attribute.RefreshAmount);
                pw.WriteInt(attribute.RefreshPeriod);
            }
        }
    }
}
