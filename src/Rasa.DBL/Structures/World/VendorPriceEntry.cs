using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// A vendor whose whole stock sells at one price, in place of each item template's buy_price.
    /// The price the counter shows (Vend) and the price a purchase is charged are both this.
    /// Vendors with no row sell at buy_price as before.
    /// </summary>
    [Table(TableName)]
    public class VendorPriceEntry : IHasId
    {
        public const string TableName = "vendor_price";

        /// <summary>The vendor's creature row, as in vendor.id.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>Credits for one of any item this vendor sells.</summary>
        [Column("item_price")]
        [Required]
        public int ItemPrice { get; set; }
    }
}
