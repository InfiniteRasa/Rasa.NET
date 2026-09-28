using System.Collections.Generic;
using JetBrains.Annotations;

namespace Rasa.Repositories.Char.CharacterMission
{
    using Structures.Char;

    public interface ICharacterMissionRepository
    {
        MissionRuntime.MissionRuntimeRepository Runtime { get; }
        IReadOnlyList<CharacterMissionEntry> Get(uint characterId);
        List<CharacterMissionEntry> Get(
            uint accountId,
            uint characterSlot);
        int Count(uint characterId);
        [CanBeNull]
        CharacterMissionEntry GetByCharacterAndMission(uint characterId, uint missionId);
        void Add(CharacterMissionEntry entry);
        void SetCompletable(uint characterId, uint missionId, bool value);
        void SetState(uint characterId, uint missionId, uint state);
        void Remove(uint characterId, uint missionId);
        void RemoveAll(uint characterId);
    }
}
