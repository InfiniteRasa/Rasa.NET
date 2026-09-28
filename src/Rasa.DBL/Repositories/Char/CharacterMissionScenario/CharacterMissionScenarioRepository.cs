using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterMissionScenario
{
    using Context.Char;
    using Structures.Char;

    public class CharacterMissionScenarioRepository : ICharacterMissionScenarioRepository
    {
        private readonly CharContext _charContext;

        public CharacterMissionScenarioRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public IReadOnlyList<CharacterMissionScenarioStepEntry> Get(uint characterId, uint missionId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterMissionScenarioStepEntries);
            return query
                .Where(entry => entry.CharacterId == characterId && entry.MissionId == missionId)
                .OrderBy(entry => entry.StepKey)
                .ToList();
        }

        public bool HasStep(uint characterId, uint missionId, string stepKey) =>
            _charContext.CharacterMissionScenarioStepEntries.Any(entry =>
                entry.CharacterId == characterId &&
                entry.MissionId == missionId &&
                entry.StepKey == stepKey);

        public void Add(CharacterMissionScenarioStepEntry entry)
        {
            _charContext.CharacterMissionScenarioStepEntries.Add(entry);
            _charContext.SaveChanges();
        }

        public void Remove(uint characterId, uint missionId, string stepKey)
        {
            var entry = _charContext.CharacterMissionScenarioStepEntries.SingleOrDefault(candidate =>
                candidate.CharacterId == characterId &&
                candidate.MissionId == missionId &&
                candidate.StepKey == stepKey);
            if (entry == null)
                return;

            _charContext.CharacterMissionScenarioStepEntries.Remove(entry);
            _charContext.SaveChanges();
        }

        public void RemoveByPrefix(uint characterId, uint missionId, string stepKeyPrefix)
        {
            if (string.IsNullOrWhiteSpace(stepKeyPrefix))
                return;

            var entries = _charContext.CharacterMissionScenarioStepEntries.Where(candidate =>
                    candidate.CharacterId == characterId &&
                    candidate.MissionId == missionId &&
                    candidate.StepKey.StartsWith(stepKeyPrefix))
                .ToArray();
            if (entries.Length == 0)
                return;

            _charContext.CharacterMissionScenarioStepEntries.RemoveRange(entries);
            _charContext.SaveChanges();
        }
    }
}
