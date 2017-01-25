namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestEquipArmorPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestEquipArmor;

        public int SrcSlot { get; set; }        // Source Slot
        public int SrcInventory { get; set; }   // Source Inventory
        public int DestSlot { get; set; }       // Destination Slot

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadInt();
            SrcInventory = pr.ReadInt();
            DestSlot = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
