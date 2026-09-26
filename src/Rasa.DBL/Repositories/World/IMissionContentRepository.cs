using System.Collections.Generic;

namespace Rasa.Repositories.World
{
    using Structures.World;

    public interface IMissionContentRepository
    {
        List<MissionContentDefinitionEntry> GetDefinitions();
        List<MissionRepeatPolicyEntry> GetRepeatPolicies() => new();
        List<MissionChannelPolicyEntry> GetChannelPolicies() => new();
        List<MissionPrerequisiteEntry> GetPrerequisites();
        List<MissionObjectiveDefinitionEntry> GetObjectives();
        List<MissionObjectiveTransitionEntry> GetTransitions();
        List<MissionTriggerEntry> GetTriggers();
        List<MissionActionEntry> GetActions();
        List<MissionRewardDefinitionEntry> GetRewards();
        List<MissionRewardItemEntry> GetRewardItems();
        List<MissionIndicatorEntry> GetIndicators();
        List<MissionAreaEntry> GetAreas();
        List<MissionSpawnGroupEntry> GetSpawnGroups();
        List<MissionSpawnEntry> GetSpawns();
        List<MissionScenarioEntry> GetScenarios();
        List<MissionScenarioStepEntry> GetScenarioSteps();
        List<MissionEvidenceEntry> GetEvidence();
    }
}
