using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public partial class TeleporterEntry
    {
        public uint Id { get; set; }
        public uint EntityClassId { get; set; }
        public uint Type { get; set; }
        public string Description { get; set; }
        public float CoordX { get; set; }
        public float CoordY { get; set; }
        public float CoordZ { get; set; }
        public float Orientation { get; set; }
        public uint MapContextId { get; set; }
    }
}
