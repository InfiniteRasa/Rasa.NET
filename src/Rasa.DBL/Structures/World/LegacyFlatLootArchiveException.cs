using System;

namespace Rasa.Structures.World
{
    public sealed class LegacyFlatLootArchiveException : InvalidOperationException
    {
        public LegacyFlatLootArchiveException(string message) : base(message)
        {
        }
    }
}
