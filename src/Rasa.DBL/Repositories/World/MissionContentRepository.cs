using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public class MissionContentRepository : IMigratedMissionContentRepository
    {
        private readonly WorldContext _worldContext;

        public MissionContentRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<MissionContentDefinitionEntry> GetEnabledDefinitions() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionContentDefinitionEntries)
                .Where(entry => entry.Enabled).ToList();
        public List<MissionSceneBindingEntry> GetSceneBindings() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.Set<MissionSceneBindingEntry>()).ToList();
        public List<MissionExperienceBindingEntry> GetExperiences() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.Set<MissionExperienceBindingEntry>())
                .Where(entry => entry.Enabled).ToList();
        public List<MissionContentDefinitionEntry> GetDefinitions() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionContentDefinitionEntries)
                .ToList();

        public List<MissionRepeatPolicyEntry> GetRepeatPolicies() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.Set<MissionRepeatPolicyEntry>()).ToList();
        public List<MissionChannelPolicyEntry> GetChannelPolicies() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.Set<MissionChannelPolicyEntry>()).ToList();

        public List<MissionPrerequisiteEntry> GetPrerequisites() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionPrerequisiteEntries)
                .ToList();

        public List<MissionObjectiveDefinitionEntry> GetObjectives() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionObjectiveDefinitionEntries)
                .ToList();

        public List<MissionObjectiveTransitionEntry> GetTransitions() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionObjectiveTransitionEntries)
                .ToList();

        public List<MissionTriggerEntry> GetTriggers() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionTriggerEntries)
                .ToList();

        public List<MissionActionEntry> GetActions() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionActionEntries)
                .ToList();

        public List<MissionRewardDefinitionEntry> GetRewards() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionRewardDefinitionEntries)
                .ToList();

        public List<MissionRewardItemEntry> GetRewardItems() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionRewardItemEntries)
                .ToList();

        public List<MissionIndicatorEntry> GetIndicators() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionIndicatorEntries)
                .ToList();

        public List<MissionAreaEntry> GetAreas() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionAreaEntries)
                .ToList();

        public List<MissionSpawnGroupEntry> GetSpawnGroups() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionSpawnGroupEntries)
                .ToList();

        public List<MissionSpawnEntry> GetSpawns() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionSpawnEntries)
                .ToList();

        public List<MissionScenarioEntry> GetScenarios() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionScenarioEntries)
                .ToList();

        public List<MissionScenarioStepEntry> GetScenarioSteps() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionScenarioStepEntries)
                .ToList();

        public List<MissionEvidenceEntry> GetEvidence() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.MissionEvidenceEntries)
                .ToList();
    }
}
