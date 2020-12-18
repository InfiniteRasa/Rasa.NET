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
            var randomEntry = query
                .Where(e => e.Type == (byte)nameType)
                .Where(e => e.Gender == (byte)gender || e.Gender == (byte)Gender.Neutral)
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