using System.Linq;

namespace Rasa.Repositories.Char.CharacterLockbox
{
    using Context.Char;
    using Structures.Char;

    public class CharacterLockboxRepository : ICharacterLockboxRepository
    {
        private readonly CharContext _charContext;

        public CharacterLockboxRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public void Add(uint id)
        {
            _charContext.Add(new CharacterLockboxEntry(id, 0, 1));
            _charContext.SaveChanges();
        }

        public CharacterLockboxEntry Get(uint accountId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterLockboxEntries);
            var lockboxInfo = query.FirstOrDefault(e => e.AccountId == accountId);

            return lockboxInfo;
        }

        public void UpdateCredits(uint accountId, int credits)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterLockboxEntries);
            var entry = query.FirstOrDefault(e => e.AccountId == accountId);

            entry.Credits = credits;
            _charContext.CharacterLockboxEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdatePurashedTabs(uint accountId, int purashedTabs)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterLockboxEntries);
            var entry = query.FirstOrDefault(e => e.AccountId == accountId);

            entry.PurashedTabs = purashedTabs;
            _charContext.CharacterLockboxEntries.Update(entry);
            _charContext.SaveChanges();
        }
    }
}
