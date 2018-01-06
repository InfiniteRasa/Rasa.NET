namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestNPCConversePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestNPCConverse;

        public int ActionId { get; set; }
        public int ActionArgId { get; set; }
        public long EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ActionId = pr.ReadInt();
            ActionArgId = pr.ReadInt();
            EntityId = pr.ReadLong();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
