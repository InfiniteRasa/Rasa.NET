using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories.Char.CharacterMissionProgress
{
    using Context.Char;
    using Structures.Char;

    public class CharacterMissionProgressRepository : ICharacterMissionProgressRepository
    {
        private readonly CharContext _charContext;

        public CharacterMissionProgressRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public CharacterMissionProgressSnapshot Get(uint characterId) =>
            CreateSnapshot(characterId, null);

        public CharacterMissionProgressSnapshot Get(uint characterId, uint missionId) =>
            CreateSnapshot(characterId, missionId);

        public CharacterMissionObjectiveEntry Get(
            uint characterId,
            uint missionId,
            uint objectiveId) =>
            _charContext.CharacterMissionObjectiveEntries
                .Include(entry => entry.Counters)
                .Include(entry => entry.ItemCounters)
                .SingleOrDefault(entry =>
                    entry.CharacterId == characterId &&
                    entry.MissionId == missionId &&
                    entry.ObjectiveId == objectiveId);

        public IReadOnlyDictionary<uint, CharacterMissionObjectiveEntry> GetTracked(
            uint characterId,
            uint missionId) =>
            _charContext.CharacterMissionObjectiveEntries
                .Include(entry => entry.Counters)
                .Include(entry => entry.ItemCounters)
                .AsSingleQuery()
                .Where(entry =>
                    entry.CharacterId == characterId &&
                    entry.MissionId == missionId)
                .ToDictionary(entry => entry.ObjectiveId);

        public void AddObjectives(IEnumerable<CharacterMissionObjectiveEntry> objectives)
        {
            _charContext.CharacterMissionObjectiveEntries.AddRange(objectives);
            _charContext.SaveChanges();
        }

        public void SetObjectiveState(
            uint characterId,
            uint missionId,
            uint objectiveId,
            byte expectedState,
            byte state)
        {
            var objective = RequireObjective(characterId, missionId, objectiveId);
            RequireExpectedValue(
                objective.ObjectiveState,
                expectedState,
                "Mission objective state is stale.");
            objective.ObjectiveState = state;
            _charContext.SaveChanges();
        }

        public void SetCounter(
            uint characterId,
            uint missionId,
            uint objectiveId,
            uint counterId,
            uint expectedValue,
            uint value)
        {
            var counter = _charContext.CharacterMissionObjectiveCounterEntries.SingleOrDefault(entry =>
                entry.CharacterId == characterId &&
                entry.MissionId == missionId &&
                entry.ObjectiveId == objectiveId &&
                entry.CounterId == counterId);
            if (counter == null)
                throw new InvalidOperationException("Mission objective counter does not exist.");
            RequireExpectedValue(
                counter.CounterValue,
                expectedValue,
                "Mission objective counter is stale.");
            counter.CounterValue = value;
            _charContext.SaveChanges();
        }

        public void SetItemCounter(
            uint characterId,
            uint missionId,
            uint objectiveId,
            uint itemClassId,
            uint expectedValue,
            uint value)
        {
            var counter = _charContext.CharacterMissionObjectiveItemCounterEntries.SingleOrDefault(entry =>
                entry.CharacterId == characterId &&
                entry.MissionId == missionId &&
                entry.ObjectiveId == objectiveId &&
                entry.ItemClassId == itemClassId);
            if (counter == null)
                throw new InvalidOperationException("Mission objective item counter does not exist.");
            RequireExpectedValue(
                counter.CounterValue,
                expectedValue,
                "Mission objective item counter is stale.");
            counter.CounterValue = value;
            _charContext.SaveChanges();
        }

        public void Remove(uint characterId, uint missionId)
        {
            var objectives = _charContext.CharacterMissionObjectiveEntries.Where(entry =>
                entry.CharacterId == characterId && entry.MissionId == missionId);
            _charContext.CharacterMissionObjectiveEntries.RemoveRange(objectives);
            _charContext.SaveChanges();
        }

        public void Remove(uint characterId, uint missionId, uint objectiveId)
        {
            _charContext.CharacterMissionObjectiveEntries.Remove(
                RequireObjective(characterId, missionId, objectiveId));
            _charContext.SaveChanges();
        }

        private CharacterMissionObjectiveEntry RequireObjective(
            uint characterId,
            uint missionId,
            uint objectiveId) =>
            Get(characterId, missionId, objectiveId) ??
            throw new InvalidOperationException("Mission objective does not exist.");

        private static void RequireExpectedValue<T>(
            T currentValue,
            T expectedValue,
            string message)
        {
            if (!EqualityComparer<T>.Default.Equals(currentValue, expectedValue))
                throw new DbUpdateConcurrencyException(message);
        }

        private CharacterMissionProgressSnapshot CreateSnapshot(uint characterId, uint? missionId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterMissionObjectiveEntries)
                .Where(entry => entry.CharacterId == characterId);
            if (missionId.HasValue)
                query = query.Where(entry => entry.MissionId == missionId.Value);

            var objectives = query
                .Include(entry => entry.Counters)
                .Include(entry => entry.ItemCounters)
                .ToArray();
            var missions = objectives
                .GroupBy(entry => entry.MissionId)
                .ToDictionary(
                    group => group.Key,
                    group => new CharacterMissionProgress(
                        group.Key,
                        group.ToDictionary(
                            entry => entry.ObjectiveId,
                            entry => new CharacterMissionObjectiveProgress(
                                entry.ObjectiveId,
                                entry.ObjectiveState,
                                entry.Counters.ToDictionary(
                                    counter => counter.CounterId,
                                    counter => counter.CounterValue),
                                entry.ItemCounters.ToDictionary(
                                    counter => counter.ItemClassId,
                                    counter => counter.CounterValue)))));
            return new CharacterMissionProgressSnapshot(missions);
        }
    }
}
