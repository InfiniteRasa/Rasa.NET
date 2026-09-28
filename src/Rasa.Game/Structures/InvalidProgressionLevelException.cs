using System;

namespace Rasa.Structures
{
    public sealed class InvalidProgressionLevelException : InvalidOperationException
    {
        public InvalidProgressionLevelException(uint characterId, int level)
            : base($"Character {characterId} has invalid progression level {level}.")
        {
        }
    }
}
