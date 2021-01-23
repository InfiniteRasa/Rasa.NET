namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestNPCConversePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestNPCConverse;

        public ActionId ActionId { get; set; }
        public int ActionArgId { get; set; }
        public ulong EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ActionId = (ActionId)pr.ReadInt();
            ActionArgId = pr.ReadInt();
            EntityId = pr.ReadULong();
        }
    }
}
