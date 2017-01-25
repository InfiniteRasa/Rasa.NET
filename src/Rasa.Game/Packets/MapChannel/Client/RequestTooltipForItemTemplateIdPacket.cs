using System;
namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestTooltipForItemTemplateIdPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestTooltipForItemTemplateId;

        public int ItemTemplateId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ItemTemplateId = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
