using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Rasa.Missions.Definitions
{
    public sealed class MissionHistoryAggregation
    {
        public IReadOnlyList<IReadOnlyList<uint>> Groups { get; }
        public uint? CounterId { get; }
        [JsonIgnore] public uint TargetCount => (uint)Groups.Count;

        public MissionHistoryAggregation(IReadOnlyList<IReadOnlyList<uint>> groups, uint? counterId = null)
        {
            if (groups == null || groups.Count == 0)
                throw new ArgumentException("History aggregates require at least one mission group.", nameof(groups));
            var frozen = new List<IReadOnlyList<uint>>();
            foreach (var group in groups)
            {
                if (group == null || group.Count == 0 || group.Contains(0U) ||
                    group.Distinct().Count() != group.Count)
                    throw new ArgumentException("History groups require distinct non-zero mission IDs.", nameof(groups));
                var members = group.OrderBy(id => id).ToArray();
                if (frozen.Any(existing => existing.SequenceEqual(members)))
                    throw new ArgumentException("History aggregates cannot repeat a mission group.", nameof(groups));
                frozen.Add(Array.AsReadOnly(members));
            }
            Groups = Array.AsReadOnly(frozen.ToArray());
            CounterId = counterId;
        }

        internal uint CountCompleted(IReadOnlySet<uint> completedMissions) =>
            (uint)Groups.Count(group => group.Any(completedMissions.Contains));
    }
}
