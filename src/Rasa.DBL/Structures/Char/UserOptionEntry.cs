using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class UserOptionEntry
    {
        public const string TableName = "user_option";

        public UserOptionEntry()
        {
        }

        public UserOptionEntry(uint accountId, uint optionId, string value)
        {
            AccountId = accountId;
            OptionId = optionId;
            Value = value;
        }
        
        [Column("account_id")]
        [Required]
        public uint AccountId { get; set; }

        [Column("option_id")]
        [Required]
        public uint OptionId { get; set; }

        [Column("value", TypeName = "varchar(50)")]
        [Required]
        public string Value { get; set; }
    }
}
