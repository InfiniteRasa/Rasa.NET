using System.Collections.Generic;

namespace Rasa.Structures
{
    using Structures.Char;

    public class Manifestation : Actor
    {
        public uint Gender { get; set; }
        public bool IsRunning { get; set; }
        public uint MapContextId { get; set; }
        public List<CharacterAppearanceEntry> AppearanceData { get; set; }
    }
}
