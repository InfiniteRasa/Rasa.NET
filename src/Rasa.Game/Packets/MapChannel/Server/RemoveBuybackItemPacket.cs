namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class RemoveBuybackItemPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RemoveBuybackItem;

        public uint EntityId { get; set; }

        public RemoveBuybackItemPacket(uint entityId)
        {
            EntityId = entityId;
        }


        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(EntityId);
        }
    }
}
