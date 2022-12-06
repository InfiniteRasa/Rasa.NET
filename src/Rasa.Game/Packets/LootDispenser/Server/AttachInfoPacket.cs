namespace Rasa.Packets.LootDispenser.Server
{
    using Data;
    using Memory;

    public class AttachInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AttachInfo;

        public ulong AttachedToEnityId { get; set; }

        public AttachInfoPacket(ulong attachedToEnityId)
        {
            AttachedToEnityId = attachedToEnityId;
        }
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteULong(AttachedToEnityId);
        }
    }
}
