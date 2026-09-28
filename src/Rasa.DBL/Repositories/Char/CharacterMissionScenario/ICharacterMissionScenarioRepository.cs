using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterMissionScenario
{
    using Structures.Char;

    public interface ICharacterMissionScenarioRepository
    {
        IReadOnlyList<CharacterMissionScenarioStepEntry> Get(uint characterId, uint missionId);
        bool HasStep(uint characterId, uint missionId, string stepKey);
        void Add(CharacterMissionScenarioStepEntry entry);
        void Remove(uint characterId, uint missionId, string stepKey);
        void RemoveByPrefix(uint characterId, uint missionId, string stepKeyPrefix);
    }
}
