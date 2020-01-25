using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class ItemTemplateSkillRequirementEntry
    {
        public uint ItemTemplateId { get; set; }
        public short SkillId { get; set; }
        public short SkillLevel { get; set; }
    }
}
