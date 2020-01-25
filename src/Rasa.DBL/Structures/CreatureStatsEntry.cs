using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class CreatureStatsEntry
    {
        public int CreatureDbId { get; set; }
        public int Body { get; set; }
        public int Mind { get; set; }
        public int Spirit { get; set; }
        public int Health { get; set; }
        public int Armor { get; set; }
    }
}
