using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// One line of a clan lockbox's transaction history: who moved what, and when.
    ///
    /// The shape is the client's. <c>client/clanlockboxlog.py</c> unpacks each entry it is sent as
    /// <c>(transactionTypeId, characterId, characterName, userName, creditTypeId, amount,
    /// itemTemplateId, lootModuleIds, quantity, transactionTime)</c> and builds its own display
    /// line out of them, so every column here exists because something on that line needs it.
    ///
    /// The names are stored rather than looked up through the character id. A log is a record of
    /// what happened, and a member who has since left the clan, been renamed or deleted still has
    /// to read back as the person who did it.
    /// </summary>
    [Table(TableName)]
    [Index(nameof(ClanId), nameof(TransactionTime), Name = "clan_lockbox_log_index_clan_id_time")]
    public class ClanLockboxLogEntry
    {
        public const string TableName = "clan_lockbox_log";

        public ClanLockboxLogEntry()
        {
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public uint Id { get; set; }

        [Column("clan_id")]
        [Required]
        public uint ClanId { get; set; }

        /// <summary>inventorytransactiontype: 1 withdrawal, 2 deposit, 3 deletion, 4 tab purchase.</summary>
        [Column("transaction_type")]
        [Required]
        public byte TransactionType { get; set; }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        /// <summary>The character's given name; the client prints it followed by the family name.</summary>
        [Column("character_name")]
        [MaxLength(32)]
        public string CharacterName { get; set; }

        /// <summary>The family name. The client calls this userName.</summary>
        [Column("user_name")]
        [MaxLength(32)]
        public string UserName { get; set; }

        /// <summary>credittype: 1 credits, 2 prestige. 0 on an item row, which sends None.</summary>
        [Column("credit_type")]
        [Required]
        public byte CreditType { get; set; }

        [Column("amount")]
        [Required]
        public long Amount { get; set; }

        /// <summary>0 on a credit row. The client tests this first and reads the row as an item if it is set.</summary>
        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }

        [Column("quantity")]
        [Required]
        public uint Quantity { get; set; }

        /// <summary>Unix seconds: the client hands it straight to datetime.fromtimestamp.</summary>
        [Column("transaction_time")]
        [Required]
        public long TransactionTime { get; set; }

        public static ClanLockboxLogEntry ForCredits(uint clanId, byte transactionType, uint characterId,
            string characterName, string userName, byte creditType, long amount)
        {
            return new ClanLockboxLogEntry
            {
                ClanId = clanId,
                TransactionType = transactionType,
                CharacterId = characterId,
                CharacterName = characterName,
                UserName = userName,
                CreditType = creditType,
                Amount = amount,
                ItemTemplateId = 0,
                Quantity = 0,
                TransactionTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        public static ClanLockboxLogEntry ForItem(uint clanId, byte transactionType, uint characterId,
            string characterName, string userName, uint itemTemplateId, uint quantity)
        {
            return new ClanLockboxLogEntry
            {
                ClanId = clanId,
                TransactionType = transactionType,
                CharacterId = characterId,
                CharacterName = characterName,
                UserName = userName,
                CreditType = 0,
                Amount = 0,
                ItemTemplateId = itemTemplateId,
                Quantity = quantity,
                TransactionTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
    }
}
