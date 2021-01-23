namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class DestroyPhysicalEntityPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DestroyPhysicalEntity;

        public ulong EntityId { get; set; }

        public DestroyPhysicalEntityPacket(ulong entityId)
        {
            EntityId = entityId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteULong(EntityId);
        }
    }
}
