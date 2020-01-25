using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class ItemTemplateResistanceEntry
    {
        public uint ItemTemplateId { get; set; }
        public short ResistanceType { get; set; }
        public int ResistanceValue { get; set; }
    }
}
