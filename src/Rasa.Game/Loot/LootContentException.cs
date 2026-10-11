using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Loot
{
    internal sealed class LootContentException : Exception
    {
        public IReadOnlyList<string> Diagnostics { get; }

        internal LootContentException(IEnumerable<string> diagnostics)
            : this(diagnostics.ToArray())
        {
        }

        private LootContentException(string[] diagnostics)
            : base("Ordinary loot content validation failed: " + string.Join("; ", diagnostics))
        {
            Diagnostics = Array.AsReadOnly(diagnostics);
        }
    }
}
