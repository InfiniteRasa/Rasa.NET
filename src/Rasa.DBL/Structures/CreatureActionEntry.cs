using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class CreatureActionEntry
    {
        public uint Id { get; set; }
        public string Description { get; set; }
        public uint ActionId { get; set; }
        public uint ActionArgId { get; set; }
        public float RangeMin { get; set; }
        public float RangeMax { get; set; }
        public uint Cooldown { get; set; }
        public uint WindupTime { get; set; }
        public uint MinDamage { get; set; }
        public uint MaxDamage { get; set; }
    }
}
