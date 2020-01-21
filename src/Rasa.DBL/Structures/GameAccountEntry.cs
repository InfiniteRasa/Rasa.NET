using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures
{
    public partial class GameAccountEntry
    {
        public uint Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public byte Level { get; set; }
        public byte SelectedSlot { get; set; }
        public string FamilyName { get; set; }
        public bool CanSkipBootcamp { get; set; }
        public string LastIP { get; set; }
        public DateTime LastLogin { get; set; }
        [NotMapped]
        public byte CharacterCount { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<CharacterEntry> Character { get; set; }

        public GameAccountEntry()
        {
            Character = new HashSet<CharacterEntry>();
        }
    }
}
