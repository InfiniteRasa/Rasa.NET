using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures
{
    using Interfaces;
    using JetBrains.Annotations;

    [Table(GameAccountEntry.TableName)]
    public class GameAccountEntry : IHasId
    {
        public const string TableName = "account";

        [Key]
        [Column("id")]
        public uint Id { get; set; }

        [Column("email", TypeName = "varchar(255)")]
        [Required]
        public string Email { get; set; }

        [Column("name", TypeName = "varchar(64)")]
        [Required]
        public string Name { get; set; }

        [Column("level")]
        [Required]
        public byte Level { get; set; }

        [Column("family_name", TypeName = "varchar(64)")]
        [Required]
        public string FamilyName { get; set; }

        [Column("selected_slot")]
        [Required]
        public byte SelectedSlot { get; set; }

        [Column("can_skip_bootcamp", TypeName = "bit")]
        [Required]
        public bool CanSkipBootcamp { get; set; }

        [Column("last_ip", TypeName = "varchar(15)")]
        [Required]
        public string LastIp { get; set; }

        [Column("last_login")]
        [Required]
        public DateTime LastLogin { get; set; }

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }

        public List<CharacterEntry> Characters { get; set; }

        public IDictionary<byte, CharacterEntry> GetCharactersWithSlot()
        {
            return Characters.ToDictionary(c => c.Slot, c => c);
        }

        [CanBeNull]
        public CharacterEntry GetCharacterBySlot(byte slot)
        {
            return Characters.FirstOrDefault(e => e.Slot == slot);
        }
    }
}
