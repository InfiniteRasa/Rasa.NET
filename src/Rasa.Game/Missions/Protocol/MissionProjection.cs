using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Structures
{
    using Data;

    internal static class MissionProjection
    {
        internal static MissionInfo CreateInfo(
            this Mission mission,
            MissionState state,
            bool completeable,
            IReadOnlyDictionary<uint, MissionObjectiveLog> objectiveLogs = null,
            Func<uint, uint?> objectiveTimeRemaining = null)
        {
            if (!mission.IsOperational)
                throw new InvalidOperationException("Mission definition is not operational.");

            objectiveLogs ??= new Dictionary<uint, MissionObjectiveLog>();
            var objectives = new List<MissionObjective>();
            foreach (var definition in mission.Objectives.Values.OrderBy(objective => objective.Ordinal.Value))
            {
                if (!objectiveLogs.TryGetValue(definition.ObjectiveId, out var log) ||
                    log.State == MissionObjectiveState.Inactive)
                    continue;
                var objective = definition.CreateRuntime(log.State, log.Counters, log.ItemCounters);
                objective.TimeRemaining = objectiveTimeRemaining?.Invoke(definition.ObjectiveId);
                objectives.Add(objective);
            }

            return new MissionInfo
            {
                MissionState = state,
                Completeable = state == MissionState.Active && completeable,
                MissionConstantData = new MissionConstantData
                {
                    Level = mission.Level.Value,
                    GroupType = mission.GroupType.Value,
                    CategoryId = mission.CategoryId.Value,
                    Shareable = mission.Shareable.Value,
                    RadioCompletable = mission.RadioCompletable.Value
                },
                ObjectivesList = objectives
            };
        }
    }
}
