using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class StarterItemsEntry
    {
        public uint ClassId { get; set; }
        public uint ItemTemplateId { get; set; }
        public int? SlotId { get; set; }
        public string Comment { get; set; }
    }
}
