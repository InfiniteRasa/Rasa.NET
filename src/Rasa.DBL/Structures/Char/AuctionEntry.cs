using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// One item listed at an auction house. The item itself stays in the items table and keeps
    /// a character_inventory row of type AuctionInventory, exactly as it would in a lockbox, so
    /// its entity is created on the client at login like any other item the character owns.
    /// This row carries only what the auction adds: who is selling, for how much, and since when.
    ///
    /// Duration is stored as the number of hours the seller chose rather than an expiry date,
    /// because the client asks for the hours remaining and the arithmetic is clearer from the
    /// two ends the seller actually picked.
    /// </summary>
    [Table(TableName)]
    [Index(nameof(SellerId), Name = "auction_index_seller_id")]
    public class AuctionEntry
    {
        public const string TableName = "auction";

        public AuctionEntry()
        {
        }

        public AuctionEntry(uint itemId, uint sellerId, string sellerName, uint price, uint deposit, uint durationHours)
        {
            ItemId = itemId;
            SellerId = sellerId;
            SellerName = sellerName;
            Price = price;
            Deposit = deposit;
            DurationHours = durationHours;
            CreatedAt = DateTime.UtcNow;
        }

        [Key]
        [Column("item_id")]
        [Required]
        public uint ItemId { get; set; }

        [Column("seller_id")]
        [Required]
        public uint SellerId { get; set; }

        [Column("seller_name", TypeName = "varchar(64)")]
        [Required]
        public string SellerName { get; set; }

        /// <summary>Buyout price in credits. There is no bidding yet, so this is the only price.</summary>
        [Column("price")]
        [Required]
        public uint Price { get; set; }

        /// <summary>What the seller paid to list it. Not refunded when the auction is cancelled.</summary>
        [Column("deposit")]
        [Required]
        public uint Deposit { get; set; }

        /// <summary>12, 24, 48 or 72 - the four durations the client offers.</summary>
        [Column("duration_hours")]
        [Required]
        public uint DurationHours { get; set; }

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Hours left before the auction expires, floored at zero. The client shows this
        /// verbatim and renders nothing when it is null, so it is never negative.
        /// </summary>
        public uint RemainingHours(DateTime now)
        {
            var elapsed = (now - CreatedAt).TotalHours;

            if (elapsed <= 0)
                return DurationHours;

            if (elapsed >= DurationHours)
                return 0;

            return DurationHours - (uint)elapsed;
        }
    }
}
