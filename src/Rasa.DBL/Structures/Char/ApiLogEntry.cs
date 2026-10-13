using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// One request the REST API was sent (the game server's Api.ApiAudit): when, from which
    /// address, what was asked for, what the answer's status was, the body of a POST - with
    /// passwords, codes and keys in it written over - and the start of the answer. Rows are
    /// only ever added.
    /// </summary>
    [Table(TableName)]
    [Index(nameof(CreatedAt), Name = "api_log_index_created_at")]
    [Index(nameof(Address), Name = "api_log_index_address")]
    public class ApiLogEntry
    {
        public const string TableName = "api_log";

        public const int MaxAddressLength = 64;
        public const int MaxMethodLength = 16;
        public const int MaxPathLength = 256;
        public const int MaxQueryLength = 512;

        /// <summary>The most of a body kept, in characters: what a text column holds whatever they are.</summary>
        public const int MaxBodyLength = 16000;

        /// <summary>The most of an answer kept, in characters.</summary>
        public const int MaxResponseLength = 2000;

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>When it was answered, UTC.</summary>
        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>The address it came from.</summary>
        [Column("address", TypeName = "varchar(64)")]
        [Required]
        public string Address { get; set; } = "";

        /// <summary>GET, HEAD, POST, OPTIONS; empty for a request that could not be read.</summary>
        [Column("method", TypeName = "varchar(16)")]
        [Required]
        public string Method { get; set; } = "";

        /// <summary>The path asked for, "/kickuser"; empty for a request that could not be read.</summary>
        [Column("path", TypeName = "varchar(256)")]
        [Required]
        public string Path { get; set; } = "";

        /// <summary>What followed the question mark; empty for nothing.</summary>
        [Column("query", TypeName = "varchar(512)")]
        [Required]
        public string Query { get; set; } = "";

        /// <summary>The status of the answer.</summary>
        [Column("status")]
        [Required]
        public int Status { get; set; }

        /// <summary>The body of a POST as it was sent, its secrets written over; empty for none.</summary>
        [Column("body", TypeName = "text")]
        [Required]
        public string Body { get; set; } = "";

        /// <summary>How long the body was, in characters, before it was cut to <see cref="MaxBodyLength"/>.</summary>
        [Column("body_length")]
        [Required]
        public int BodyLength { get; set; }

        /// <summary>The answer's JSON, up to <see cref="MaxResponseLength"/> characters.</summary>
        [Column("response", TypeName = "text")]
        [Required]
        public string Response { get; set; } = "";
    }
}
