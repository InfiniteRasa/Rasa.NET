using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class CustomizationChoicesPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CustomizationChoices;

        public uint EntityId { get; set; }
        public Dictionary<int, int> Choices { get; set; }

        public CustomizationChoicesPacket(uint entityId, Dictionary<int, int> choices)
        {
            EntityId = entityId;
            Choices = choices;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(EntityId);
            pw.WriteList(Choices.Count);
            foreach (var choice in Choices)
            {
                pw.WriteTuple(2);
                pw.WriteInt(choice.Key);
                pw.WriteInt(choice.Value);
            }
        }
    }
}
