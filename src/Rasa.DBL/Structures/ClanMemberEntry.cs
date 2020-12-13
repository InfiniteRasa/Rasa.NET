using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures
{
    [Table(ClanMemberEntry.TableName)]
    public class ClanMemberEntry
    {
        public const string TableName = "clan_member";

        [Key]
        [Column("clan_id")]
        public uint ClanId { get; set; }

        [ForeignKey(nameof(ClanId))]
        public ClanEntry Clan { get; set; }

        [Key]
        [Column("character_id")]
        public uint CharacterId { get; set; }

        [ForeignKey(nameof(CharacterId))]
        public CharacterEntry Character { get; set; }
    }
}