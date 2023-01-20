using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class IgnoredEntry
    {
        public const string TableName = "ignored";

        public IgnoredEntry()
        {
        }

        public IgnoredEntry(uint accountId, uint ignoredaccountId)
        {
            accountId = AccountId;
            IgnoredAccountId = ignoredaccountId;
        }

        [Key]
        [Column("account_id")]
        [Required]
        public uint AccountId { get; set; }

        [Column("ignored_account_id")]
        [Required]
        public uint IgnoredAccountId { get; set; }
    }
}
