using System.Collections.Generic;
using System;
using JetBrains.Annotations;

namespace Rasa.Repositories.Char.CharacterMissionDeadline
{
    using Structures.Char;

    public interface ICharacterMissionDeadlineRepository
    {
        IReadOnlyList<CharacterMissionDeadlineEntry> Get(uint characterId);
        [CanBeNull]
        CharacterMissionDeadlineEntry Get(uint characterId, uint missionId);
        void Add(CharacterMissionDeadlineEntry entry);
        void SetState(uint characterId, uint missionId, CharacterMissionDeadlineState state);
        void AddOrUpdate(uint characterId, uint missionId, DateTime dueAtUtc, CharacterMissionDeadlineState state);
    }
}
