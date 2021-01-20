using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;
    using Rasa.Managers;
    using Structures.Char;

    public class Manifestation : Actor
    {
        public uint Gender { get; set; }
        public List<CharacterAppearanceEntry> AppearanceData { get; set; }

        public Manifestation(IEntityManager entityManager)
            : base(entityManager)
        {

        }
    }
}
