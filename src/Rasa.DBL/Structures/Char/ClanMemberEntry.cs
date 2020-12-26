using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.Char
{
    [Table(ClanMemberEntry.TableName)]
    [Index(nameof(ClanMemberEntry.CharacterId), IsUnique = true, Name = "clan_member_index_character")]
    public class ClanMemberEntry
    {
        public const string TableName = "clan_member";

        [Column("clan_id")]
        [Required]
        public uint ClanId { get; set; }

        public ClanEntry Clan { get; set; }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        public CharacterEntry Character { get; set; }

        [Column("rank")]
        [Required]
        public byte Rank { get; set; }
    }
}