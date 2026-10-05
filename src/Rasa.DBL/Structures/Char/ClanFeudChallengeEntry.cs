using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// A clan's challenge to another to feud, not yet answered (the game server's ClanFeuds).
    /// Written when it is made, removed when it is answered, revoked or a clan disbands, and read
    /// back when the server starts. The id is the wargame id the feud will have if it is accepted.
    /// </summary>
    [Table(TableName)]
    public class ClanFeudChallengeEntry
    {
        public const string TableName = "clan_feud_challenge";

        [Key]
        [Column("wargame_id")]
        public uint WargameId { get; set; }

        [Column("challenger_clan_id")]
        [Required]
        public uint ChallengerClanId { get; set; }

        [Column("target_clan_id")]
        [Required]
        public uint TargetClanId { get; set; }

        /// <summary>The character who made the challenge.</summary>
        [Column("challenger_character_id")]
        [Required]
        public uint ChallengerCharacterId { get; set; }
    }
}
