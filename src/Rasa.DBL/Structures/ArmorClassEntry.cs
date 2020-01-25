using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class ArmorClassEntry
    {
        public uint ClassId { get; set; }
        public int MinDamageAbsorbed { get; set; }
        public int MaxDamageAbsorbed { get; set; }
        public int RegenRate { get; set; }
    }
}
