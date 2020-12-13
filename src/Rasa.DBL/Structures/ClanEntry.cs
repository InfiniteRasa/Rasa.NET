using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures
{
    using System;
    using Interfaces;

    [Table(ClanEntry.TableName)]
    public class ClanEntry : IHasId
    {
        public const string TableName = "clan";

        [Key]
        [Column("id")]
        public uint Id { get; set; }

        [Column("name", TypeName = "varchar(100)")]
        [Required]
        public string Name { get; set; }

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }
    }

    public class ClanMemberEntry
    {
        [Key]
        [Column("clan_id")]
        public uint ClanId { get; set; }

        [Key]
        [Column("clan_id")]
        public uint CharacterId { get; set; }
    }
}
