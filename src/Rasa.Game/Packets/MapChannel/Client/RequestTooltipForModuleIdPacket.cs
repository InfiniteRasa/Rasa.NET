namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestTooltipForModuleIdPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestTooltipForModuleId;

        public int ModuleId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ModuleId = pr.ReadInt();
        }
    }
}
