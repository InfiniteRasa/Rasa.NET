using System;
using System.Linq;

using JetBrains.Annotations;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public class PlayerRandomNameRepository : IPlayerRandomNameRepository
    {
        private readonly WorldContext _worldContext;

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
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.RandomNameEntries);

            // This is not the fastest implementation, as the random evaluation is done on client side to be DBMS agnostic.
            // Execution is still so fast that no wait time occurs in the client.
            // An alternative but more complex way would be to load count per name type (first/male, first/female, last),
            // get a random between 0 and the count and call "query.Skip(random).FirstOrDefault();"
            // This is possible, as the random names provided by the database don't change (at runtime).
            var randomEntry = query
                .Where(e => e.Type == (byte)nameType)
                .Where(e => e.Gender == (byte)gender || e.Gender == (byte)Gender.Neutral)
                .AsEnumerable()
                .OrderBy(e => Guid.NewGuid())
                .FirstOrDefault();

            return randomEntry;
        }

        public enum NameType : byte
        {
            First = 0,
            Last = 1
        }
    }
}