namespace Rasa.Structures
{
    public class VendorItem
    {
        public uint ItemTemplateId { get; set; }

        public VendorItem(VendorItemsEntry item)
        {
            ItemTemplateId = item.ItemTemplateId;
        }
    }
}
