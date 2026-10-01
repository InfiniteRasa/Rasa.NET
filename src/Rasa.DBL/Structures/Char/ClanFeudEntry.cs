using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// A clan feud under way (the game server's ClanFeuds): the two clans, the score, and when it
    /// ends. Written when the feud starts and on every kill, removed when it ends, and read back
    /// when the server starts, so a feud lasts through a restart. The id is the feud's wargame id,
    /// the one the client tracks it by.
    /// </summary>
    [Table(TableName)]
    public class ClanFeudEntry
    {
        public const string TableName = "clan_feud";

        [Key]
        [Column("id")]
        public uint Id { get; set; }

        [Column("challenger_clan_id")]
        [Required]
        public uint ChallengerClanId { get; set; }

        [Column("target_clan_id")]
        [Required]
        public uint TargetClanId { get; set; }

        /// <summary>When the feud ends: Unix time in milliseconds, UTC. It runs on while the server is down.</summary>
        [Column("ends_at")]
        [Required]
        public long EndsAt { get; set; }

        [Column("challenger_kills")]
        [Required]
        public int ChallengerKills { get; set; }

        [Column("target_kills")]
        [Required]
        public int TargetKills { get; set; }
    }
}
