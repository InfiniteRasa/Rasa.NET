using JetBrains.Annotations;

namespace Rasa.Repositories.Char.CharacterStartingExperience
{
    using Structures.Char;

    public interface ICharacterStartingExperienceRepository
    {
        [CanBeNull]
        CharacterStartingExperienceEntry Get(uint characterId);
        CharacterStartingExperienceState? ReadState(uint characterId) =>
            throw new System.NotSupportedException("This repository cannot read authoritative starting-experience state.");
        void Add(CharacterStartingExperienceEntry entry);
        void SetState(uint characterId, CharacterStartingExperienceState state);
        bool TrySetState(
            uint characterId,
            CharacterStartingExperienceState expectedState,
            CharacterStartingExperienceState state);
    }
}
