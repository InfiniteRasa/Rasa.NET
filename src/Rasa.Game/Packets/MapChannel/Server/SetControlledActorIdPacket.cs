namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    public class SetControlledActorIdPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetControlledActorId;

        public ulong EntityId { get; set; }

        public SetControlledActorIdPacket(ulong entityId)
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
