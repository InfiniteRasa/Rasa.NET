using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Structures;

namespace Rasa.Missions.Definitions
{
    public sealed class MissionObjectiveAggregation
    {
        public IReadOnlyList<uint> ChildObjectiveIds { get; }
        public uint TargetCount { get; }
        public uint? CounterId { get; }

        public MissionObjectiveAggregation(
            IReadOnlyList<uint> childObjectiveIds,
            uint targetCount,
            uint? counterId = null)
        {
            if (childObjectiveIds == null || childObjectiveIds.Count == 0 ||
                childObjectiveIds.Contains(0U) ||
                childObjectiveIds.Distinct().Count() != childObjectiveIds.Count)
                throw new ArgumentException("Aggregates require distinct non-zero child objectives.", nameof(childObjectiveIds));
            if (targetCount == 0 || targetCount > childObjectiveIds.Count)
                throw new ArgumentOutOfRangeException(nameof(targetCount));
            ChildObjectiveIds = Array.AsReadOnly(childObjectiveIds.OrderBy(id => id).ToArray());
            TargetCount = targetCount;
            CounterId = counterId;
        }

        internal static IEnumerable<string> Errors(IReadOnlyDictionary<uint, MissionObjectiveDefinition> objectives)
        {
            foreach (var objective in objectives.Values.Where(value => value.Aggregation != null))
                if (objective.Aggregation.ChildObjectiveIds.Any(id =>
                    id == objective.ObjectiveId || !objectives.ContainsKey(id)))
                    yield return $"objective {objective.ObjectiveId} aggregate must reference other objectives in this mission";

            var visiting = new HashSet<uint>();
            var visited = new HashSet<uint>();
            foreach (var id in objectives.Keys)
                if (HasCycle(id))
                {
                    yield return "objective aggregates contain a dependency cycle";
                    yield break;
                }

            bool HasCycle(uint id)
            {
                if (visited.Contains(id) || !objectives.TryGetValue(id, out var objective))
                    return false;
                if (!visiting.Add(id))
                    return true;
                if (objective.Aggregation != null && objective.Aggregation.ChildObjectiveIds.Any(HasCycle))
                    return true;
                visiting.Remove(id);
                visited.Add(id);
                return false;
            }
        }
    }
}
