using System;

namespace Rasa.Structures.Char
{
    public static class CharacterFlagIds
    {
        public const uint ServerFlagStart = 0x80000000;
        public const uint BootcampComplete = ServerFlagStart + 1;

        public static bool IsMissionFlag(uint flagId) => flagId > 0 && flagId < ServerFlagStart;

        public static uint FromQualification(byte qualification) => qualification switch
        {
            (byte)CharacterQualificationKey.BootcampComplete => BootcampComplete,
            _ => throw new ArgumentOutOfRangeException(nameof(qualification), "Unknown historical qualification.")
        };
    }

    // IDs retained for frozen World migrations and existing serialized scene intents.
    public enum CharacterQualificationKey : byte
    {
        BootcampComplete = 1
    }
}
