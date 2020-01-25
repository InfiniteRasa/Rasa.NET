using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class ItemTemplateRequirementsEntry
    {
        public uint ItemTemplateId { get; set; }
        public short ReqType { get; set; }
        public short ReqValue { get; set; }
    }
}
