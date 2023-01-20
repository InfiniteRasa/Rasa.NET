using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CensorWordsEntry
    {
        public const string TableName = "censor_words";

        public CensorWordsEntry()
        {
        }

        public CensorWordsEntry(uint id, string word)
        {
            Id = id;
            Word = word;
        }

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("word", TypeName = "varchar(45)")]
        [Required]
        public string Word { get; set; }
    }
}
