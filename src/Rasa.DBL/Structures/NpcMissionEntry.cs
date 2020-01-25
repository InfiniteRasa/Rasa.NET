using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures
{
    public partial class NpcMissionEntry
    {
        public uint MissionId { get; set; }
        public int Command { get; set; }
        public int Var1 { get; set; }
        public int Var2 { get; set; }
        public int Var3 { get; set; }
        public string Comment { get; set; }
    }
}
