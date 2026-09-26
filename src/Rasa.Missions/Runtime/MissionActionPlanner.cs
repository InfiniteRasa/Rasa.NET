using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Data;
using Rasa.Structures;
using Rasa.Structures.Missions;
using Rasa.Structures.World;

namespace Rasa.Missions.Runtime
{
    public sealed record MissionActionDecision(
        IReadOnlyDictionary<uint, MissionObjectiveState> States,
        IReadOnlyList<uint> Revealed,
        IReadOnlyList<uint> Activated,
        IReadOnlyList<MissionActionDefinition> Effects);

    public static class MissionActionPlanner
    {
        public static MissionActionDecision Decide(uint currentObjectiveId,
            MissionObjectiveExecutableTransition transition,
            IReadOnlyDictionary<uint, MissionObjectiveState> current)
        {
            var states = new Dictionary<uint, MissionObjectiveState>();
            var revealed = new List<uint>();
            var activated = new List<uint>();
            var effects = new List<MissionActionDefinition>();
            foreach (var action in transition?.Actions ?? Array.Empty<MissionActionDefinition>())
            {
                if (action.Kind == MissionActionKind.CompleteObjective)
                {
                    if (action.TargetObjectiveId.HasValue && action.TargetObjectiveId != currentObjectiveId)
                        throw new MissionRuleException("A transition can complete only its current objective.");
                    continue;
                }
                if (action.Kind is not (MissionActionKind.RevealObjective or MissionActionKind.ActivateObjective))
                {
                    effects.Add(action);
                    continue;
                }
                if (!action.TargetObjectiveId.HasValue || !current.TryGetValue(action.TargetObjectiveId.Value, out var initial))
                    throw new MissionRuleException("The target objective is absent from the assignment.");
                var id = action.TargetObjectiveId.Value;
                var state = states.GetValueOrDefault(id, initial);
                if (action.Kind == MissionActionKind.RevealObjective)
                {
                    if (state == MissionObjectiveState.Inactive)
                    {
                        state = MissionObjectiveState.NotAssigned;
                        revealed.Add(id);
                    }
                    states[id] = state;
                }
                else if (state is MissionObjectiveState.Inactive or MissionObjectiveState.NotAssigned)
                {
                    states[id] = MissionObjectiveState.Incomplete;
                    activated.Add(id);
                }
                else if (state is not (MissionObjectiveState.Incomplete or MissionObjectiveState.Completed or MissionObjectiveState.Failed))
                    throw new MissionRuleException("The target objective has an unsupported state.");
            }
            return new MissionActionDecision(states, revealed.Distinct().ToArray(), activated.Distinct().ToArray(), effects);
        }
    }
}
