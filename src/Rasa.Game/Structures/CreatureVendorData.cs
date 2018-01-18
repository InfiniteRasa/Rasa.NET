using System.Collections.Generic;

namespace Rasa.Structures
{
    public class CreatureVendorData
    {
        public int VendorPackageId { get; set; }
        public List<int> SellItemList = new List<int>();
        // list of sold items
        public List<VendorItem> SoldItemList { get; set; }
    }
}
