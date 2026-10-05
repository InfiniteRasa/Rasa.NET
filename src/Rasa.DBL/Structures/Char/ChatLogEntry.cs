using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.Char
{
    /// <summary>What kind of chat a line on the chat log (chat_log) is.</summary>
    public enum ChatLogKind : byte
    {
        /// <summary>Said to those nearby (/s, RadialChat).</summary>
        Say = 1,

        /// <summary>Shouted (/y).</summary>
        Shout = 2,

        /// <summary>An emote in the player's own words (/em).</summary>
        Emote = 3,

        /// <summary>A whisper, or a reply to one (/w, /r).</summary>
        Whisper = 4,

        /// <summary>To the squad (/p).</summary>
        Squad = 5,

        /// <summary>To the clan (/c).</summary>
        Clan = 6,

        /// <summary>To the clan's leaders (/cl).</summary>
        ClanLeaders = 7,

        /// <summary>On a numbered channel: General, LFG, a map's General, Trade or Defense, a team's.</summary>
        Channel = 8
    }

    /// <summary>What came of a line on the chat log.</summary>
    public enum ChatLogResult : byte
    {
        /// <summary>It was passed on: to as many others as the row's heard_by says, which may be none.</summary>
        Delivered = 1,

        /// <summary>The account is silenced by a game master; nobody was sent it.</summary>
        Muted = 2,

        /// <summary>A whisper to a name nobody in the game has; nobody was sent it.</summary>
        NoTarget = 3,

        /// <summary>A whisper to a player who has the sender on their ignore list; they were not sent it.</summary>
        Ignored = 4,

        /// <summary>
        /// The sender is not one who may say it there - in no squad, not in that clan or on that
        /// channel, below the rank the clan's leaders' channel needs; nobody was sent it.
        /// </summary>
        NotMember = 5,

        /// <summary>Longer than a chat line may be; nobody was sent it, and the row has its beginning.</summary>
        TooLong = 6
    }

    /// <summary>
    /// One line of chat on the chat log: who said it, when, where their character stood, what
    /// kind of chat it was and to whom, the line itself, and what came of it. Rows are only
    /// ever added.
    /// </summary>
    [Table(TableName)]
    [Index(nameof(AccountId), Name = "chat_log_index_account_id")]
    [Index(nameof(CreatedAt), Name = "chat_log_index_created_at")]
    [Index(nameof(TargetAccountId), Name = "chat_log_index_target_account_id")]
    public class ChatLogEntry
    {
        public const string TableName = "chat_log";

        public const int MaxNameLength = 64;
        public const int MaxTargetLength = 96;
        public const int MaxTextLength = 512;

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>When it was said, UTC.</summary>
        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>A <see cref="ChatLogKind"/>.</summary>
        [Column("kind")]
        [Required]
        public byte Kind { get; set; }

        /// <summary>A <see cref="ChatLogResult"/>.</summary>
        [Column("result")]
        [Required]
        public byte Result { get; set; }

        /// <summary>The account that said it.</summary>
        [Column("account_id")]
        [Required]
        public uint AccountId { get; set; }

        /// <summary>The account's level at the time: above 0 is a game master's.</summary>
        [Column("account_level")]
        [Required]
        public byte AccountLevel { get; set; }

        /// <summary>The character they were on.</summary>
        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        /// <summary>The character's first name at the time.</summary>
        [Column("name", TypeName = "varchar(64)")]
        [Required]
        public string Name { get; set; } = "";

        /// <summary>The account's family name at the time, which is the name chat shows.</summary>
        [Column("family_name", TypeName = "varchar(64)")]
        [Required]
        public string FamilyName { get; set; } = "";

        /// <summary>Where the character was: the map, its instance and the position.</summary>
        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("instance_id")]
        [Required]
        public uint InstanceId { get; set; }

        [Column("coord_x", TypeName = "double")]
        [Required]
        public double CoordX { get; set; }

        [Column("coord_y", TypeName = "double")]
        [Required]
        public double CoordY { get; set; }

        [Column("coord_z", TypeName = "double")]
        [Required]
        public double CoordZ { get; set; }

        /// <summary>
        /// The group it was said to: the squad's id for squad chat, the clan's for clan and clan
        /// leaders' chat, the chat channel's (1 General, 3 LFG, 4 a map's General, 6 Trade,
        /// 7 Defense, 10000008 Team) for channel chat; 0 for the rest.
        /// </summary>
        [Column("group_id")]
        [Required]
        public uint GroupId { get; set; }

        /// <summary>The account a whisper was to, when it is somebody in the game; 0 otherwise.</summary>
        [Column("target_account_id")]
        [Required]
        public uint TargetAccountId { get; set; }

        /// <summary>The character a whisper was to; 0 otherwise.</summary>
        [Column("target_character_id")]
        [Required]
        public uint TargetCharacterId { get; set; }

        /// <summary>
        /// To whom, in words: a whisper's recipient by family name (as typed, when nobody has
        /// it), a channel by name ("General", "Trade", "Team 1"), a clan by name; empty for the
        /// rest.
        /// </summary>
        [Column("target", TypeName = "varchar(96)")]
        [Required]
        public string Target { get; set; } = "";

        /// <summary>How many players other than the sender it was sent to.</summary>
        [Column("heard_by")]
        [Required]
        public uint HeardBy { get; set; }

        /// <summary>The line as it was said; one too long to be said is cut at 512 characters.</summary>
        [Column("text", TypeName = "varchar(512)")]
        [Required]
        public string Text { get; set; } = "";
    }
}
