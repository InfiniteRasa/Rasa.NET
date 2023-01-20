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

        [Column("is_pvp")]
        [Required]
        public bool IsPvP { get; set; }

        [Column("rank_title_0", TypeName = "varchar(64)")]
        [Required]
        public string RankTitle0 { get; set; }

        [Column("rank_title_1", TypeName = "varchar(64)")]
        [Required]
        public string RankTitle1 { get; set; }

        [Column("rank_title_2", TypeName = "varchar(64)")]
        [Required]
        public string RankTitle2 { get; set; }

        [Column("rank_title_3", TypeName = "varchar(64)")]
        [Required]
        public string RankTitle3 { get; set; }

        [Column("credits")]
        [Required]
        public uint Credits { get; set; }

        [Column("prestige")]
        [Required]
        public uint Prestige { get; set; }

        [Column("purashed_tabs")]
        [Required]
        public uint PurashedTabs { get; set; }
		
        public List<ClanMemberEntry> Members { get; set; }
    }
}
