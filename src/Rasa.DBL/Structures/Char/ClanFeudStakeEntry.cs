using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// A character who left one of the clans of a feud while it ran, with an item wagered (the
    /// game server's ClanFeuds.MemberRemoved): what is in their wager slot stays at stake for that
    /// feud as if they were still a member of the clan they left. Written when they leave or are
    /// kicked, removed with the feud, and read back when the server starts.
    /// </summary>
    [Table(TableName)]
    public class ClanFeudStakeEntry
    {
        public const string TableName = "clan_feud_stake";

        /// <summary>The feud's wargame id (clan_feud.id).</summary>
        [Column("feud_id")]
        [Required]
        public uint FeudId { get; set; }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        /// <summary>The clan they left: the side their wager stands with.</summary>
        [Column("clan_id")]
        [Required]
        public uint ClanId { get; set; }
    }
}
