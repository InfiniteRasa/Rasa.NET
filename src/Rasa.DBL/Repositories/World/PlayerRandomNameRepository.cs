using System;
using System.Collections.Concurrent;
using System.Linq;

using JetBrains.Annotations;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public class PlayerRandomNameRepository : IPlayerRandomNameRepository
    {
        private readonly WorldContext _worldContext;

        /// <summary>
        /// The names each (type, gender) request can draw from, read from the table the first time
        /// that pair is asked for and kept for the life of the process. The table is seed data and
        /// does not change at runtime. Keyed by the raw gender byte the client sent, so at most 2 x
        /// 256 entries can ever exist.
        /// </summary>
        private static readonly ConcurrentDictionary<(byte Type, byte Gender), string[]> Pools = new();

        public PlayerRandomNameRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public string GetFirstName(Gender gender)
        {
            var randomEntry = GetRandomNameEntry(gender, NameType.First);

            if (randomEntry == null || string.IsNullOrEmpty(randomEntry.Name))
            {
                return gender == Gender.Female
                    ? "Rachel"
                    : "Richard";
            }

            return randomEntry.Name;
        }

        public string GetLastName()
        {
            var randomEntry = GetRandomNameEntry(Gender.Neutral, NameType.Last);
            return randomEntry?.Name ?? "Garriott";
        }

        [CanBeNull]
        private RandomNameEntry GetRandomNameEntry(Gender gender, NameType nameType)
        {
            // This used to read every matching row and sort them all by a fresh Guid on each call,
            // on the world's only thread - about 2,600 rows per request, and the request can be
            // sent as fast as a connection can send anything. Now the rows are read once per
            // (type, gender) and one is picked by index.
            var pool = Pools.GetOrAdd(((byte)nameType, (byte)gender), key =>
                _worldContext.CreateNoTrackingQuery(_worldContext.RandomNameEntries)
                    .Where(e => e.Type == key.Type)
                    .Where(e => e.Gender == key.Gender || e.Gender == (byte)Gender.Neutral)
                    .Select(e => e.Name)
                    .ToArray());

            if (pool.Length == 0)
                return null;

            return new RandomNameEntry { Name = pool[Random.Shared.Next(pool.Length)] };
        }

        public enum NameType : byte
        {
            First = 0,
            Last = 1
        }
    }
}