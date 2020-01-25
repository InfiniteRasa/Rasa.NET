using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class CreaturesEntry
    {
        public uint DbId { get; set; }
        public string Comment { get; set; }
        public int ClassId { get; set; }
        public int Faction { get; set; }
        public int Level { get; set; }
        public int MaxHitPoints { get; set; }
        public int NameId { get; set; }
        public uint RunSpeed { get; set; }
        public uint WalkSpeed { get; set; }
        public uint Action1 { get; set; }
        public uint Action2 { get; set; }
        public uint Action3 { get; set; }
        public uint Action4 { get; set; }
        public uint Action5 { get; set; }
        public uint Action6 { get; set; }
        public uint Action7 { get; set; }
        public uint Action8 { get; set; }
    }
}
