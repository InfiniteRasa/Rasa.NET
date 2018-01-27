namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestTakeItemFromHomeInventoryPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestTakeItemFromHomeInventory;

        public int SrcSlot { get; set; }
        public int DestSlot { get; set; }
        public int Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"RequestTakeItemFromHomeInventory:\n{pr.ToString()}");
            pr.ReadTuple();
            SrcSlot = pr.ReadInt();
            DestSlot = pr.ReadInt();
            Quantity = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
