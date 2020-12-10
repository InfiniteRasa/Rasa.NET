using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures
{
    using System;
    using System.ComponentModel;

    [Table(AuthAccountEntry.TableName)]
    [Index(nameof(AuthAccountEntry.Email), IsUnique = true, Name = "email_UNIQUE")]
    [Index(nameof(AuthAccountEntry.Username), IsUnique = true, Name = "email_UNIQUE")]
    public class AuthAccountEntry
    {
        public const string TableName = "account";

        [Key]
        [Column("id", TypeName = "int(11)")]
        public uint Id { get; set; }

        [Column("email", TypeName = "varchar(255)")]
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Column("username", TypeName = "varchar(64)")]
        [Required]
        public string Username { get; set; }

        [Column("password", TypeName = "varchar(64)")]
        [Required]
        public string Password { get; set; }

        [Column("salt", TypeName = "varchar(40)")]
        [Required]
        public string Salt { get; set; }

        [Column("level", TypeName = "tinyint(3)")]
        [Required]
        [DefaultValue(0)]
        public byte Level { get; set; }

        [Column("last_ip", TypeName = "varchar(45)")]
        [Required]
        [DefaultValue("0.0.0.0")]
        public string LastIp { get; set; }

        [Column("last_server_id", TypeName = "tinyint(3)")]
        public byte LastServerId { get; set; }

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        [Column("join_date")]
        [Required]
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

        [Column("locked")]
        public bool Locked { get; set; }
        
        [Column("validated")]
        public bool Validated { get; set; }

        [Column("validation_token", TypeName = "varchar(40)")]
        public string ValidationToken { get; set; }
    }
}
