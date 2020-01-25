using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class EntityClassEntry
    {
        public uint ClassId { get; set; }
        public string ClassName { get; set; }
        public int MeshId { get; set; }
        public short ClassCollisionRole { get; set; }
        public bool TargetFlag { get; set; }
        public string Augmentations { get; set; }
    }
}
