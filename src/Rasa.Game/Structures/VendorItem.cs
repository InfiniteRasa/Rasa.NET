namespace Rasa.Structures
{
    public class VendorItem
    {
        public uint ItemTemplateId { get; set; }
        public uint Stacksize { get; set; }
        public int Sequence { get; set; }
        public Item ItemInstance { get; set; } // physical entity that is used to display the item

        public VendorItem(uint itemTemplateId, uint stackSize, int sequence, Item itemInstance)
        {
            ItemTemplateId = itemTemplateId;
            Stacksize = stackSize;
            Sequence = sequence;
            ItemInstance = itemInstance;
        }
    }
}
