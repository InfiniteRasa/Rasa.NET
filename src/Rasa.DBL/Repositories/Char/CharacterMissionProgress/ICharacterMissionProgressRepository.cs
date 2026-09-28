using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Rasa.Repositories.Char.CharacterMissionProgress
{
    using Structures.Char;

    public interface ICharacterMissionProgressRepository
    {
        CharacterMissionProgressSnapshot Get(uint characterId);
        CharacterMissionProgressSnapshot Get(uint characterId, uint missionId);
        CharacterMissionObjectiveEntry Get(uint characterId, uint missionId, uint objectiveId);
        IReadOnlyDictionary<uint, CharacterMissionObjectiveEntry> GetTracked(
            uint characterId,
            uint missionId);
        void AddObjectives(IEnumerable<CharacterMissionObjectiveEntry> objectives);
        void SetObjectiveState(
            uint characterId,
            uint missionId,
            uint objectiveId,
            byte expectedState,
            byte state);
        void SetCounter(
            uint characterId,
            uint missionId,
            uint objectiveId,
            uint counterId,
            uint expectedValue,
            uint value);
        void SetItemCounter(
            uint characterId,
            uint missionId,
            uint objectiveId,
            uint itemClassId,
            uint expectedValue,
            uint value);
        void Remove(uint characterId, uint missionId);
        void Remove(uint characterId, uint missionId, uint objectiveId);
    }

    public sealed class CharacterMissionProgressSnapshot
    {
        public IReadOnlyDictionary<uint, CharacterMissionProgress> Missions { get; }

        public CharacterMissionProgressSnapshot(
            IReadOnlyDictionary<uint, CharacterMissionProgress> missions)
        {
            Missions = new ReadOnlyDictionary<uint, CharacterMissionProgress>(
                new Dictionary<uint, CharacterMissionProgress>(
                    missions ?? new Dictionary<uint, CharacterMissionProgress>()));
        }

        public static CharacterMissionProgressSnapshot Empty { get; } =
            new(new Dictionary<uint, CharacterMissionProgress>());
    }

    public sealed class CharacterMissionProgress
    {
        public uint MissionId { get; }
        public IReadOnlyDictionary<uint, CharacterMissionObjectiveProgress> Objectives { get; }

        public CharacterMissionProgress(
            uint missionId,
            IReadOnlyDictionary<uint, CharacterMissionObjectiveProgress> objectives)
        {
            MissionId = missionId;
            Objectives = new ReadOnlyDictionary<uint, CharacterMissionObjectiveProgress>(
                new Dictionary<uint, CharacterMissionObjectiveProgress>(
                    objectives ?? new Dictionary<uint, CharacterMissionObjectiveProgress>()));
        }
    }

    public sealed class CharacterMissionObjectiveProgress
    {
        public uint ObjectiveId { get; }
        public byte State { get; }
        public IReadOnlyDictionary<uint, uint> Counters { get; }
        public IReadOnlyDictionary<uint, uint> ItemCounters { get; }

        public CharacterMissionObjectiveProgress(
            uint objectiveId,
            byte state,
            IReadOnlyDictionary<uint, uint> counters,
            IReadOnlyDictionary<uint, uint> itemCounters)
        {
            ObjectiveId = objectiveId;
            State = state;
            Counters = new ReadOnlyDictionary<uint, uint>(
                new Dictionary<uint, uint>(counters ?? new Dictionary<uint, uint>()));
            ItemCounters = new ReadOnlyDictionary<uint, uint>(
                new Dictionary<uint, uint>(itemCounters ?? new Dictionary<uint, uint>()));
        }
    }
}
