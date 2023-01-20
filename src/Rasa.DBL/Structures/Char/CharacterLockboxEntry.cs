using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CharacterLockboxEntry
    {
        public const string TableName = "character_lockbox";

        public CharacterLockboxEntry()
        {
        }

        public CharacterLockboxEntry(uint accountId, int credits, int purashedTabs)
        {
            AccountId = accountId;
            Credits = credits;
            PurashedTabs = purashedTabs;
        }

        [Key]
        [Column("account_id")]
        [Required]
        public uint AccountId { get; set; }

        [Column("credits")]
        [Required]
        public int Credits { get; set; }

        [Column("purashed_tabs")]
        [Required]
        public int PurashedTabs { get; set; }
    }
}
