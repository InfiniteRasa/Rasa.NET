using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.Char
{
    /// <summary>What became of an item that was wagered in a recorded PvP match.</summary>
    public enum PvpWagerResult : byte
    {
        /// <summary>Still its owner's: their side did not lose, or the match is of a kind that takes nothing.</summary>
        Kept = 0,

        /// <summary>Forfeit, and put in the winning clan's lockbox.</summary>
        ClanLockbox = 1,

        /// <summary>Forfeit, and sent to the pick-up box of a member of the winning clan, its lockbox being full.</summary>
        PickUpBox = 2,

        /// <summary>Forfeit, but it could not be moved - no room in the lockbox or the pick-up box - and stayed with its owner.</summary>
        NotMoved = 3
    }

    /// <summary>
    /// An item that was wagered by a player of a recorded PvP match (pvp_match) when the match
    /// ended, and what became of it. One row per match and character: a character has one wager
    /// slot. A character of a match with no row had nothing wagered.
    /// </summary>
    [Table(TableName)]
    [Index(nameof(CharacterId), Name = "pvp_match_wager_index_character_id")]
    [Index(nameof(ItemId), Name = "pvp_match_wager_index_item_id")]
    public class PvpMatchWagerEntry
    {
        public const string TableName = "pvp_match_wager";

        /// <summary>pvp_match.id.</summary>
        [Column("match_id")]
        [Required]
        public uint MatchId { get; set; }

        /// <summary>Who had it wagered.</summary>
        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        /// <summary>1 or 2: the match's side1 or side2.</summary>
        [Column("side")]
        [Required]
        public byte Side { get; set; }

        /// <summary>items.item_id: the one item.</summary>
        [Column("item_id")]
        [Required]
        public uint ItemId { get; set; }

        /// <summary>What kind of item it is: its template; 0 when the item's row could not be read.</summary>
        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }

        /// <summary>The template's quality, which sets the wager's prestige bonus: 3 Uncommon, 4 Rare, 5 Epic; 0 when unknown.</summary>
        [Column("quality_id")]
        [Required]
        public int QualityId { get; set; }

        [Column("stack_size")]
        [Required]
        public uint StackSize { get; set; }

        /// <summary>A <see cref="PvpWagerResult"/>.</summary>
        [Column("result")]
        [Required]
        public byte Result { get; set; }

        /// <summary>The clan the item was forfeit to; 0 for one that was kept.</summary>
        [Column("recipient_clan_id")]
        [Required]
        public uint RecipientClanId { get; set; }

        /// <summary>The character whose pick-up box it went to; 0 unless <see cref="Result"/> is PickUpBox.</summary>
        [Column("recipient_character_id")]
        [Required]
        public uint RecipientCharacterId { get; set; }
    }
}
