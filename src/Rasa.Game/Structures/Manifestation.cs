using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;
    using Structures.Char;

    public class Manifestation : Actor
    {
        public uint Gender { get; set; }
        public List<CharacterAppearanceEntry> AppearanceData { get; set; }
    }
}
