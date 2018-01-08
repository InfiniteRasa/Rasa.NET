namespace Rasa.Structures
{
    public class VendorItem
    {
        public uint ItemTemplateId { get; set; }
        public uint Stacksize { get; set; }
        public int Sequence { get; set; }
        public Item itemInstance; // physical entity that is used to display the item
    }
}
