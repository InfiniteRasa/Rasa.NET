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
            if (pr.PeekType() == PythonType.Int)
                ItemTemplateId = pr.ReadInt();
            else if (pr.PeekType() == PythonType.Long)
                ItemTemplateId = (int)pr.ReadLong();
            else
               throw new Exception($"RequestTooltipForItemTemplateId:\n unsuported PythonType = {pr.PeekType()}");
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
