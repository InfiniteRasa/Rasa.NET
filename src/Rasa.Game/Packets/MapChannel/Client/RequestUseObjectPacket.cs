namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestUseObjectPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestUseObject;

        public int ActionId { get; set; }
        public int ActionArgId { get; set; }
        public long EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            //Logger.WriteLog(LogType.Debug, $"RequestUseObject:\n {pr.ToString()}");
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
