namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class SetTrackingTargetPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetTrackingTarget;

        public uint EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = (uint)pr.ReadLong();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(EntityId);
        }
    }
}