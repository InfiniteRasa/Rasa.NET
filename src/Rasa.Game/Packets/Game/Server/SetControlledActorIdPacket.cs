namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class SetControlledActorIdPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetControlledActorId;

        public uint EntityId { get; set; }

        public SetControlledActorIdPacket(uint entityId)
        {
            EntityId = entityId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(EntityId);
        }
    }
}
