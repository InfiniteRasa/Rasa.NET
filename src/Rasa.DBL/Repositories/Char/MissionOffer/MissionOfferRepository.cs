using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Rasa.Context.Char;
using Rasa.Structures.Char;

namespace Rasa.Repositories.Char.MissionOffer
{
    public sealed class MissionOfferRepository
    {
        private readonly CharContext _context;
        public MissionOfferRepository(CharContext context) => _context = context;
        public CharacterMissionOfferEntry Get(uint characterId, uint missionId) =>
            _context.Set<CharacterMissionOfferEntry>().Find(characterId, missionId);
        public CharacterMissionOfferEntry Read(uint characterId, uint missionId) =>
            _context.Set<CharacterMissionOfferEntry>().AsNoTracking()
                .SingleOrDefault(entry => entry.CharacterId == characterId && entry.MissionId == missionId);
        public IReadOnlyList<CharacterMissionOfferEntry> ForCharacter(uint characterId)
        {
            var offers = _context.Set<CharacterMissionOfferEntry>();
            offers.Where(entry => entry.CharacterId == characterId).Load();
            return offers.Local.Where(entry => entry.CharacterId == characterId).ToArray();
        }
        public bool IsOwnedBy(uint characterId, uint accountId) =>
            _context.CharacterEntries.AsNoTracking().Any(entry => entry.Id == characterId && entry.AccountId == accountId);
        public void Add(CharacterMissionOfferEntry entry) => _context.Add(entry);
        public void Remove(CharacterMissionOfferEntry entry) => _context.Remove(entry);
    }
}
