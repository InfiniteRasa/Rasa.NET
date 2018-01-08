using System.Collections.Generic;

namespace Rasa.Structures
{
    public class CreatureVendorData
    {
        public int VendorPackageId { get; set; }
        // list of sold items
        public List<VendorItem> SoldItemList { get; set; }
    }
}
