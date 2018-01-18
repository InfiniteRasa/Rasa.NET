namespace Rasa.Structures
{
    public class VendorItem
    {
        public int ItemTemplateId { get; set; }

        public VendorItem(VendorItemsEntry item)
        {
            ItemTemplateId = item.ItemTemplateId;
        }
    }
}
