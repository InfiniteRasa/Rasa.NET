namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class SetTrackingTargetPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetTrackingTarget;

        public ulong EntityId { get; set; }

        public SetTrackingTargetPacket()
        {
        }

        public SetTrackingTargetPacket(ulong entityId)
        {
            EntityId = entityId;
        }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadULong();
        }
    }
}
