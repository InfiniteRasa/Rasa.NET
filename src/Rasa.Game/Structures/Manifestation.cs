using System.Collections.Generic;

namespace Rasa.Structures
{
    using Repositories.Char.Character;
    using Structures.Char;

    public class Manifestation : Actor, ICharacterChange
    {
        public uint Gender { get; set; }
        public uint Id => (uint)EntityGUID.Counter;
        public bool IsRunning { get; set; }
        public uint MapContextId { get; set; }
        public List<CharacterAppearanceEntry> AppearanceData { get; set; }
    }
}
