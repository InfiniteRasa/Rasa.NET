using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.Char
{
    /// <summary>How a command on the audit log (gm_command_log) reached the server.</summary>
    public enum GmCommandSource : byte
    {
        /// <summary>A dot command typed in chat (.giveitem).</summary>
        Chat = 1,

        /// <summary>A slash command the client passes on as PrivilegedCommand (/gotomap), or one of its GM pickers.</summary>
        Slash = 2,

        /// <summary>A GM request the client has a message of its own for: /gotomob, /changefirstname, /changelastname, the GM mission window, a line-of-sight report.</summary>
        Request = 3,

        /// <summary>Typed at the game server's console.</summary>
        Console = 4
    }

    /// <summary>What came of a command on the audit log.</summary>
    public enum GmCommandResult : byte
    {
        /// <summary>The account had the level, and the command was run.</summary>
        Executed = 1,

        /// <summary>The account did not have the level the command needs; nothing was done.</summary>
        Denied = 2,

        /// <summary>No such command; nothing was done.</summary>
        Unknown = 3,

        /// <summary>The command was run and threw.</summary>
        Failed = 4
    }

    /// <summary>
    /// One command on the game master audit log: who entered it, when, from where, where their
    /// character stood, what they had selected, the command as it was entered, and what came of
    /// it. Rows are only ever added; the one change made to a row is its result, when a command
    /// that was run throws.
    /// </summary>
    [Table(TableName)]
    [Index(nameof(AccountId), Name = "gm_command_log_index_account_id")]
    [Index(nameof(CreatedAt), Name = "gm_command_log_index_created_at")]
    [Index(nameof(TargetCharacterId), Name = "gm_command_log_index_target_character_id")]
    public class GmCommandLogEntry
    {
        public const string TableName = "gm_command_log";

        public const int MaxCommandLength = 64;
        public const int MaxTextLength = 512;
        public const int MaxTargetLength = 96;

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>When it was entered, UTC.</summary>
        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>A <see cref="GmCommandSource"/>.</summary>
        [Column("source")]
        [Required]
        public byte Source { get; set; }

        /// <summary>A <see cref="GmCommandResult"/>.</summary>
        [Column("result")]
        [Required]
        public byte Result { get; set; }

        /// <summary>The account that entered it; 0 for the console.</summary>
        [Column("account_id")]
        [Required]
        public uint AccountId { get; set; }

        /// <summary>The account's level at the time.</summary>
        [Column("account_level")]
        [Required]
        public byte AccountLevel { get; set; }

        /// <summary>The level the command needs; 0 for a command that does not exist, and for the console.</summary>
        [Column("required_level")]
        [Required]
        public byte RequiredLevel { get; set; }

        /// <summary>The character they were on; 0 for the console.</summary>
        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        /// <summary>The character's first name at the time.</summary>
        [Column("name", TypeName = "varchar(64)")]
        [Required]
        public string Name { get; set; } = "";

        /// <summary>The account's family name at the time, which is what the game shows of a player.</summary>
        [Column("family_name", TypeName = "varchar(64)")]
        [Required]
        public string FamilyName { get; set; } = "";

        /// <summary>The address the client was connected from; empty for the console.</summary>
        [Column("address", TypeName = "varchar(64)")]
        [Required]
        public string Address { get; set; } = "";

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

        /// <summary>The character they had selected, when it was a player's; 0 otherwise.</summary>
        [Column("target_character_id")]
        [Required]
        public uint TargetCharacterId { get; set; }

        /// <summary>What they had selected: "player Family", "creature 1234 Name"; empty for nothing.</summary>
        [Column("target", TypeName = "varchar(96)")]
        [Required]
        public string Target { get; set; } = "";

        /// <summary>The command's name as the server knows it: ".giveitem", "/gotomap", "gm".</summary>
        [Column("command", TypeName = "varchar(64)")]
        [Required]
        public string Command { get; set; } = "";

        /// <summary>The whole line as it was entered, arguments and all; cut at 512 characters.</summary>
        [Column("text", TypeName = "varchar(512)")]
        [Required]
        public string Text { get; set; } = "";
    }
}
