using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class ClanEntry
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<ClanMemberEntry> ClanMember { get; set; }

        public ClanEntry()
        {
            ClanMember = new HashSet<ClanMemberEntry>();
        }
    }
}
