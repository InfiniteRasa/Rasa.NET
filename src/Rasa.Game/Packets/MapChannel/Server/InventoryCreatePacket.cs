namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class InventoryCreatePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.InventoryCreate;

        public int InventoryType { get; set; }
        public int ListOfItems { get; set; }    // ToDo, rewrite to list later
        public int InventorySize { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(InventoryType);
            pw.WriteList(0);    // ToDo
            pw.WriteInt(InventorySize);
        }
    }
}
