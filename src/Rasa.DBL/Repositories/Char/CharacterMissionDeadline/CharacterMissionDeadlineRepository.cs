using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterMissionDeadline
{
    using Context.Char;
    using Structures.Char;

    public class CharacterMissionDeadlineRepository : ICharacterMissionDeadlineRepository
    {
        private readonly CharContext _charContext;

        public CharacterMissionDeadlineRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public IReadOnlyList<CharacterMissionDeadlineEntry> Get(uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterMissionDeadlineEntries);
            return query.Where(entry => entry.CharacterId == characterId).ToList();
        }

        public CharacterMissionDeadlineEntry Get(uint characterId, uint missionId) =>
            _charContext.CharacterMissionDeadlineEntries.SingleOrDefault(entry =>
                entry.CharacterId == characterId &&
                entry.MissionId == missionId);

        public void Add(CharacterMissionDeadlineEntry entry)
        {
            if (entry.DueAtUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Mission deadlines must be stored in UTC.", nameof(entry));

            _charContext.CharacterMissionDeadlineEntries.Add(entry);
            _charContext.SaveChanges();
        }

        public void SetState(uint characterId, uint missionId, CharacterMissionDeadlineState state)
        {
            var entry = Get(characterId, missionId);
            if (entry == null)
                throw new InvalidOperationException("Mission deadline does not exist.");

            entry.State = state;
            _charContext.SaveChanges();
        }

        public void AddOrUpdate(
            uint characterId,
            uint missionId,
            DateTime dueAtUtc,
            CharacterMissionDeadlineState state)
        {
            if (dueAtUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Mission deadlines must be stored in UTC.", nameof(dueAtUtc));

            var entry = Get(characterId, missionId);
            if (entry == null)
            {
                _charContext.CharacterMissionDeadlineEntries.Add(
                    new CharacterMissionDeadlineEntry(characterId, missionId, dueAtUtc, state));
            }
            else
            {
                entry.DueAtUtc = dueAtUtc;
                entry.State = state;
            }

            _charContext.SaveChanges();
        }
    }
}
