using System.Collections.Generic;

namespace Rasa.Structures
{
    public class Vendor
    {
        public uint VendorPackageId { get; set; }
        public List<uint> VendorItems = new List<uint>();
        public List<uint> BuyBackItems = new List<uint>();

        public Vendor(uint vendorPackageId)
        {
            VendorPackageId = vendorPackageId;
        }
    }
}
