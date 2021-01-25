using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;
    using Repositories.Char.Character;
    using Structures.Char;

    public class Manifestation : Actor, ICharacterChange
    {
        public uint Gender { get; set; }
        public uint Id => (uint)EntityGUID.Counter;

        public bool IsRunning { get; set; }

        public bool IsCrouching { get; set; }

        public void SetIsCrouching(Posture posture)
        {
            switch (posture)
            {
                case Posture.Crouching:
                    IsCrouching = true;
                    break;
                case Posture.Standing:
                    IsCrouching = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(posture), posture, null);
            }
        }

        public Posture GetPosture()
        {
            return IsCrouching
                ? Posture.Crouching
                : Posture.Standing;
        }

        public uint MapContextId { get; set; }
        public List<CharacterAppearanceEntry> AppearanceData { get; set; }
    }
}
