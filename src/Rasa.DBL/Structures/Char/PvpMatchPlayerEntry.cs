using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// One player of a recorded PvP match (pvp_match): the side they were on and what they did
    /// in it. One row per match and character. The names are as they were at the time.
    /// </summary>
    [Table(TableName)]
    [Index(nameof(CharacterId), Name = "pvp_match_player_index_character_id")]
    public class PvpMatchPlayerEntry
    {
        public const string TableName = "pvp_match_player";

        /// <summary>pvp_match.id.</summary>
        [Column("match_id")]
        [Required]
        public uint MatchId { get; set; }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        /// <summary>1 or 2: the match's side1 or side2.</summary>
        [Column("side")]
        [Required]
        public byte Side { get; set; }

        /// <summary>The character's first name.</summary>
        [Column("name", TypeName = "varchar(64)")]
        [Required]
        public string Name { get; set; } = "";

        /// <summary>The account's family name, which is what the game shows of a player.</summary>
        [Column("family_name", TypeName = "varchar(64)")]
        [Required]
        public string FamilyName { get; set; } = "";

        /// <summary>The clan they were in; 0 for none.</summary>
        [Column("clan_id")]
        [Required]
        public uint ClanId { get; set; }

        [Column("kills")]
        [Required]
        public int Kills { get; set; }

        [Column("deaths")]
        [Required]
        public int Deaths { get; set; }

        /// <summary>Damage dealt to the other side; a battleground's match only.</summary>
        [Column("damage")]
        [Required]
        public int Damage { get; set; }

        /// <summary>Healing done to their own side; a battleground's match only.</summary>
        [Column("healing")]
        [Required]
        public int Healing { get; set; }

        /// <summary>Control points captured; a battleground's match only.</summary>
        [Column("captures")]
        [Required]
        public int Captures { get; set; }

        /// <summary>Prestige the match gave them; a battleground's match only.</summary>
        [Column("prestige")]
        [Required]
        public int Prestige { get; set; }

        /// <summary>Still on their side when it ended: false for one who left the match, the squad or the clan before.</summary>
        [Column("present_at_end")]
        [Required]
        public bool PresentAtEnd { get; set; }
    }
}
