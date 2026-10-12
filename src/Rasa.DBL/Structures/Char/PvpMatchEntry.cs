using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    /// <summary>pvp_match.kind.</summary>
    public enum PvpMatchKind : byte
    {
        ClanFeud = 1,
        SquadWargame = 2,
        Battleground = 3,
        Duel = 4
    }

    /// <summary>pvp_match.outcome.</summary>
    public enum PvpMatchOutcome : byte
    {
        /// <summary>A clan feud that has not ended.</summary>
        Running = 0,
        Won = 1,
        Tied = 2,
        Cancelled = 3
    }

    /// <summary>
    /// The record of one PvP match between two sides (the game server's PvpRecords): a clan
    /// feud, a squad wargame, a battleground's match, or a duel. Who fought it, when, how it ended, who
    /// won and what the two sides' scores were. The players on each side, with their own
    /// scores, are its pvp_match_player rows.
    ///
    /// A squad wargame, a battleground's match and a duel are written when they end. A clan feud runs
    /// for days, through restarts, so its row is written when it starts, with no ended_at, kept
    /// up on every kill, and closed when the feud ends.
    /// </summary>
    [Table(TableName)]
    public class PvpMatchEntry
    {
        public const string TableName = "pvp_match";

        [Key]
        [Column("id")]
        public uint Id { get; set; }

        /// <summary>1 a clan feud, 2 a squad wargame, 3 a battleground's match, 4 a duel (PvpMatchKind).</summary>
        [Column("kind")]
        [Required]
        public byte Kind { get; set; }

        /// <summary>The wargame id the clients knew it by. Ids are used again over time: the row's own id is the record's.</summary>
        [Column("wargame_id")]
        [Required]
        public uint WargameId { get; set; }

        /// <summary>The map it was fought on, and which copy of it; 0 for a clan feud, which is fought everywhere.</summary>
        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("instance_id")]
        [Required]
        public uint InstanceId { get; set; }

        /// <summary>UTC.</summary>
        [Column("started_at")]
        [Required]
        public DateTime StartedAt { get; set; }

        /// <summary>UTC; null while a clan feud is still running.</summary>
        [Column("ended_at")]
        public DateTime? EndedAt { get; set; }

        /// <summary>0 still running, 1 won by <see cref="WinnerSide"/>, 2 tied, 3 cancelled (PvpMatchOutcome).</summary>
        [Column("outcome")]
        [Required]
        public byte Outcome { get; set; }

        /// <summary>1 or 2 for the side that won; 0 when nobody did.</summary>
        [Column("winner_side")]
        [Required]
        public byte WinnerSide { get; set; }

        /// <summary>How it came to end: time, kills, points, distance, surrender, forfeit, disbanded, gm, cancelled.</summary>
        [Column("reason", TypeName = "varchar(32)")]
        [Required]
        public string Reason { get; set; } = "";

        /// <summary>Side 1: the challenging clan, the challenging squad's leader, Red Team, or the duel's challenger.</summary>
        [Column("side1_name", TypeName = "varchar(64)")]
        [Required]
        public string Side1Name { get; set; } = "";

        /// <summary>The clan of side 1 in a clan feud; 0 otherwise.</summary>
        [Column("side1_clan_id")]
        [Required]
        public uint Side1ClanId { get; set; }

        /// <summary>What the side is judged by: its kills, or in a battleground the control points it held at the end.</summary>
        [Column("side1_score")]
        [Required]
        public int Side1Score { get; set; }

        [Column("side1_kills")]
        [Required]
        public int Side1Kills { get; set; }

        /// <summary>Side 2: the challenged clan, the challenged squad's leader, Blue Team, or the one challenged to the duel.</summary>
        [Column("side2_name", TypeName = "varchar(64)")]
        [Required]
        public string Side2Name { get; set; } = "";

        [Column("side2_clan_id")]
        [Required]
        public uint Side2ClanId { get; set; }

        [Column("side2_score")]
        [Required]
        public int Side2Score { get; set; }

        [Column("side2_kills")]
        [Required]
        public int Side2Kills { get; set; }
    }
}
