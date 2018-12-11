namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class SetTrackingTargetPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetTrackingTarget;

        public uint EntityId { get; set; }

        public SetTrackingTargetPacket(uint entityId)
        {
            EntityId = entityId;
        }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = (uint)pr.ReadLong();
        }
    }
}
