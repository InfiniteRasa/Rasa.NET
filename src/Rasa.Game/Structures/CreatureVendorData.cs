using System.Collections.Generic;

namespace Rasa.Structures
{
    public class CreatureVendorData
    {
        public int VendorPackageId { get; set; }
        public List<uint> SellItemList = new List<uint>();
        // list of sold items
        public List<VendorItem> SoldItemList { get; set; }
    }
}
