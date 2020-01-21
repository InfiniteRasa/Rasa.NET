using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class CharacterEntry
    {
        public uint Id { get; set; }
        public uint AccountId { get; set; }
        public byte Slot { get; set; }
        public string Name { get; set; }
        public byte Race { get; set; }
        public uint Class { get; set; }
        public byte Gender { get; set; }
        public double Scale { get; set; }
        public uint Experience { get; set; }
        public byte Level { get; set; }
        public int Body { get; set; }
        public int Mind { get; set; }
        public int Spirit { get; set; }
        public uint CloneCredits { get; set; }
        public uint MapContextId { get; set; }
        public float CoordX { get; set; }
        public float CoordY { get; set; }
        public float CoordZ { get; set; }
        public float Orientation { get; set; }
        public int Credits { get; set; }
        public int Prestige { get; set; }
        public byte ActiveWeapon { get; set; }
        public uint NumLogins { get; set; }
        public DateTime? LastLogin { get; set; }
        public uint TotalTimePlayed { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual GameAccountEntry Account { get; set; }
        public virtual ClanMemberEntry ClanMember { get; set; }
        public virtual ICollection<CharacterAppearanceEntry> CharacterAppearance { get; set; }

        public CharacterEntry()
        {
            CharacterAppearance = new HashSet<CharacterAppearanceEntry>();
        }

    }
}
