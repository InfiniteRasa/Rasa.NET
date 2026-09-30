using System.Collections.Generic;

namespace Rasa.Structures
{
    public class Vendor
    {
        public uint VendorPackageId { get; set; }
        public List<uint> VendorItems = new List<uint>();
        public List<uint> BuyBackItems = new List<uint>();

        /// <summary>
        /// What every item at this counter costs (vendor_price), or null to sell each at its
        /// template's buy price.
        /// </summary>
        public int? ItemPrice { get; set; }

        public Vendor(uint vendorPackageId)
        {
            VendorPackageId = vendorPackageId;
        }

        /// <summary>
        /// The price of one of <paramref name="item"/> here: what the counter shows (Vend) and
        /// what a purchase charges, so the two cannot disagree.
        /// </summary>
        public int PriceOf(Item item)
        {
            return ItemPrice ?? item.ItemTemplate.BuyPrice;
        }
    }
}
