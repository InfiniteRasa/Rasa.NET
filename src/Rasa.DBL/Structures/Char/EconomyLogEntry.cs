using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.Char
{
    /// <summary>What moved an item or a sum on the economy log (economy_log).</summary>
    public enum EconomyLogKind : byte
    {
        /// <summary>A completed trade between two players: items and credits each way.</summary>
        Trade = 1,

        /// <summary>An item put up at the auction house, and the deposit paid for it.</summary>
        AuctionListed = 2,

        /// <summary>An auction bought out: the item and the price to the buyer, the price to the seller.</summary>
        AuctionSold = 3,

        /// <summary>A listing taken back down by its seller: the item back in their pack. The deposit is not returned.</summary>
        AuctionCancelled = 4,

        /// <summary>A listing that ran out: the item back in its seller's inbox.</summary>
        AuctionExpired = 5,

        /// <summary>A clan feud won: each item wagered on the losing side, off its owner and to the winners - the lockbox (no character) or the member it was mailed to.</summary>
        Wager = 6,

        /// <summary>Credits or prestige paid into or taken out of the clan bank.</summary>
        ClanBank = 7,

        /// <summary>Credits paid into or taken out of the account's lockbox, which every character of the account shares.</summary>
        LockboxCredits = 8,

        /// <summary>A lockbox tab bought.</summary>
        LockboxTab = 9,

        /// <summary>A clan's creation fee.</summary>
        ClanCreation = 10,

        /// <summary>Bought from a vendor.</summary>
        VendorBuy = 11,

        /// <summary>Sold to a vendor.</summary>
        VendorSell = 12,

        /// <summary>Bought back from a vendor's buyback list.</summary>
        VendorBuyback = 13,

        /// <summary>A repair paid for at a vendor; the sum's row names the item.</summary>
        VendorRepair = 14,

        /// <summary>A mission's credit and prestige reward.</summary>
        MissionReward = 15,

        /// <summary>Credits or prestige taken from a corpse, the looter's and their squad's shares.</summary>
        Loot = 16,

        /// <summary>Prestige for a player killed in PvP - the killer's, and what was stolen from the victim - or for a clan feud won.</summary>
        PvpPrestige = 17,

        /// <summary>Transfer credit slips turned into credits.</summary>
        TransferCredit = 18,

        /// <summary>A crafting station's energy cost.</summary>
        Crafting = 19
    }

    /// <summary>
    /// One movement on the economy log: an item, or a sum of credits or prestige, coming to or
    /// going from one character. One event - a trade, a sale - is several rows with one
    /// transfer id: each item that changed hands and each sum, a row on each side for what
    /// passed between two players. Rows are only ever added.
    ///
    /// For an item, quantity is the stack, positive where it came to the character and negative
    /// where it left them, and item id is the items row, which is how an item is followed from
    /// one owner to the next. For a sum, currency is 1 credits or 2 prestige, amount is the
    /// change, signed the same way, and balance is what the character held afterwards. Other
    /// character is the player on the other side; 0 is an NPC, the auction house or the world.
    /// </summary>
    [Table(TableName)]
    [Index(nameof(CharacterId), Name = "economy_log_index_character_id")]
    [Index(nameof(CreatedAt), Name = "economy_log_index_created_at")]
    [Index(nameof(ItemId), Name = "economy_log_index_item_id")]
    [Index(nameof(TransferId), Name = "economy_log_index_transfer_id")]
    public class EconomyLogEntry
    {
        public const string TableName = "economy_log";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>When it happened, UTC.</summary>
        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>The rows of one event share it.</summary>
        [Column("transfer_id", TypeName = "varchar(32)")]
        [Required]
        public string TransferId { get; set; } = "";

        /// <summary>An <see cref="EconomyLogKind"/>.</summary>
        [Column("kind")]
        [Required]
        public byte Kind { get; set; }

        /// <summary>
        /// What it was done through, by kind: the item (auction, transfer credit), the vendor's
        /// package (vendor buy, sell, buyback), the mission, the clan (clan bank, creation, the
        /// feud's winners for a wager or a feud won), the lockbox tab, the corpse's class (loot),
        /// the recipe (crafting); 0 for a trade, the lockbox's credits, a repair and a PvP kill.
        /// </summary>
        [Column("reference_id")]
        [Required]
        public uint ReferenceId { get; set; }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        /// <summary>The character's account; 0 where it was not to hand (a seller who is offline).</summary>
        [Column("account_id")]
        [Required]
        public uint AccountId { get; set; }

        /// <summary>The player on the other side; 0 for none.</summary>
        [Column("other_character_id")]
        [Required]
        public uint OtherCharacterId { get; set; }

        /// <summary>The items row; 0 for a sum.</summary>
        [Column("item_id")]
        [Required]
        public uint ItemId { get; set; }

        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }

        /// <summary>The stack: positive to the character, negative from them.</summary>
        [Column("quantity")]
        [Required]
        public int Quantity { get; set; }

        /// <summary>1 credits, 2 prestige; 0 for an item.</summary>
        [Column("currency")]
        [Required]
        public byte Currency { get; set; }

        /// <summary>The sum: positive to the character, negative from them.</summary>
        [Column("amount")]
        [Required]
        public long Amount { get; set; }

        /// <summary>What the character held of that currency afterwards.</summary>
        [Column("balance")]
        [Required]
        public long Balance { get; set; }

        /// <summary>Where the character was; 0 when they were not in the world.</summary>
        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }
    }
}
