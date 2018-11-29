namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class PersonalInventory_MoveItemPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PersonalInventory_MoveItem;

        public int SrcSlot { get; set; }
        public int DestSlot { get; set; }
        public int Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            SrcSlot = pr.ReadInt();
            DestSlot = pr.ReadInt();
            if( pr.PeekType() == PythonType.Int)
                Quantity = pr.ReadInt();
            else
                Quantity = (int)pr.ReadLong();
        }
    }
}
