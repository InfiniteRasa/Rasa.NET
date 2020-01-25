using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures
{
    public partial class ArmorTemplateEntry
    {
        public uint ItemTemplateId { get; set; }
        public int ArmorValue { get; set; }
    }
}
