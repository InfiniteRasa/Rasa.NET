namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestUseObjectPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestUseObject;

        public ActionId ActionId { get; set; }
        public uint ActionArgId { get; set; }
        public ulong EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ActionId = (ActionId)pr.ReadInt();
            ActionArgId = pr.ReadUInt();
            EntityId = pr.ReadULong();
        }
    }
}
