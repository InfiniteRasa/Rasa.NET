using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
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

        public List<ClanMemberEntry> Members { get; set; }
    }
}
