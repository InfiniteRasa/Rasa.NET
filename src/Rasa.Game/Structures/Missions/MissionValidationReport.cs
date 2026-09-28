using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Rasa.Structures.Missions
{
    using Structures.World;

    internal sealed class MissionValidationReport
    {
        private static readonly IReadOnlyDictionary<string, int> CodeOrder =
            new ReadOnlyDictionary<string, int>(
                new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["missing-client-text"] = 1,
                    ["invalid-counter-text-binding"] = 2,
                    ["missing-objective-id"] = 3,
                    ["duplicate-objective-id"] = 4,
                    ["duplicate-mission-id"] = 5,
                    ["missing-target"] = 6,
                    ["transition-cycle"] = 7,
                    ["invalid-objective-graph"] = 8,
                    ["invalid-progress-event"] = 9,
                    ["invalid-trigger-shape"] = 10,
                    ["multiple-executable-transition-paths"] = 11,
                    ["unsupported-progress-transition-actions"] = 12,
                    ["unsupported-trigger"] = 13,
                    ["missing-related-objective"] = 14,
                    ["unsupported-action"] = 15,
                    ["missing-npc-package"] = 16,
                    ["missing-item-template"] = 17,
                    ["missing-entity-class"] = 18,
                    ["missing-creature"] = 19,
                    ["missing-map-context"] = 20,
                    ["missing-area"] = 21,
                    ["missing-spawn-group"] = 22,
                    ["missing-spawn"] = 23,
                    ["missing-scenario"] = 24,
                    ["missing-scenario-step"] = 25,
                    ["missing-indicator"] = 26,
                    ["missing-ambient-conversation-binding"] = 27,
                    ["missing-player-flag-binding"] = 28,
                    ["missing-reward-reference"] = 29,
                    ["ambiguous-reward-reference"] = 30,
                    ["missing-reward"] = 31,
                    ["invalid-radius"] = 32,
                    ["invalid-quantity"] = 33,
                    ["invalid-delay"] = 34,
                    ["invalid-reward-selection"] = 35,
                    ["unsupported-scenario-step"] = 36,
                    ["invalid-scenario-step-shape"] = 37,
                    ["invalid-tutorial"] = 38,
                    ["invalid-qualification"] = 39,
                    ["cross-revision-reference"] = 40,
                    ["required-chain-inactive"] = 41
                });

        private readonly HashSet<uint> _requiredMissionIds;

        public IReadOnlyList<MissionValidationDiagnostic> Diagnostics { get; }
        public bool BlocksReadiness { get; }

        public MissionValidationReport(
            IEnumerable<MissionValidationDiagnostic> diagnostics,
            IEnumerable<uint> requiredMissionIds)
        {
            _requiredMissionIds = new HashSet<uint>(
                requiredMissionIds ?? Array.Empty<uint>());
            Diagnostics = Array.AsReadOnly(
                (diagnostics ?? Array.Empty<MissionValidationDiagnostic>())
                .OrderBy(diagnostic => diagnostic.MissionId ?? 0U)
                .ThenBy(diagnostic => diagnostic.ContentRevision, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.ScenarioId ?? 0U)
                .ThenBy(diagnostic => diagnostic.StepId ?? 0U)
                .ThenBy(diagnostic => diagnostic.ObjectiveId ?? 0U)
                .ThenBy(diagnostic => diagnostic.TransitionId ?? 0U)
                .ThenBy(diagnostic => diagnostic.TriggerId ?? 0U)
                .ThenBy(diagnostic => diagnostic.ActionId ?? 0U)
                .ThenBy(diagnostic => CodeOrder.TryGetValue(diagnostic.Code, out var order)
                    ? order
                    : int.MaxValue)
                .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
                .ToArray());
            BlocksReadiness = Diagnostics.Any(diagnostic =>
                diagnostic.MissionId.HasValue &&
                _requiredMissionIds.Contains(diagnostic.MissionId.Value));
        }

        public bool HasErrorsForMission(uint missionId) =>
            Diagnostics.Any(diagnostic => diagnostic.MissionId == missionId);
    }
}
