using System;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestTooltipForItemTemplateIdPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestTooltipForItemTemplateId;

        public uint ItemTemplateId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            if (pr.PeekType() == PythonType.Int)
                ItemTemplateId = pr.ReadUInt();
            else if (pr.PeekType() == PythonType.Long)
                ItemTemplateId = (uint)pr.ReadLong();
            else
               throw new Exception($"RequestTooltipForItemTemplateId:\n unsuported PythonType = {pr.PeekType()}");
        }
    }
}
