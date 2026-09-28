using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    using Game;
    using Managers;
    using Repositories.Char;
    using Structures.Char;
    using Structures.Missions;

    internal sealed class MissionActionContext
    {
        internal MissionActionContext(
            Client client,
            MissionApplication missionManager,
            ManifestationManager manifestationManager,
            ICharUnitOfWork unitOfWork,
            Mission missionDefinition,
            CharacterMissionEntry durableMission,
            IReadOnlyDictionary<uint, CharacterMissionObjectiveEntry> durableObjectives,
            MissionScenarioDefinition scenario,
            IReadOnlyDictionary<uint, MissionRewardDefinition> rewardPackages,
            IReadOnlyDictionary<string, MissionScenarioStepState> completedSteps,
            IReadOnlyList<MissionScenarioStepState> scheduledSteps,
            DateTime utcNow,
            MapChannel mapChannel,
            MissionScenarioPlan plan)
        {
            Client = client;
            MissionApplication = missionManager;
            ManifestationManager = manifestationManager;
            UnitOfWork = unitOfWork;
            MissionDefinition = missionDefinition;
            DurableMission = durableMission;
            DurableObjectives = durableObjectives;
            Scenario = scenario;
            RewardPackages = rewardPackages;
            CompletedSteps = completedSteps;
            ScheduledSteps = scheduledSteps;
            UtcNow = utcNow;
            MapChannel = mapChannel;
            Plan = plan;
        }

        internal Client Client { get; }
        internal MissionApplication MissionApplication { get; }
        internal ManifestationManager ManifestationManager { get; }
        internal ICharUnitOfWork UnitOfWork { get; }
        internal Mission MissionDefinition { get; }
        internal CharacterMissionEntry DurableMission { get; }
        internal IReadOnlyDictionary<uint, CharacterMissionObjectiveEntry> DurableObjectives { get; }
        internal MissionScenarioDefinition Scenario { get; }
        internal IReadOnlyDictionary<uint, MissionRewardDefinition> RewardPackages { get; }
        internal IReadOnlyDictionary<string, MissionScenarioStepState> CompletedSteps { get; }
        internal IReadOnlyList<MissionScenarioStepState> ScheduledSteps { get; }
        internal DateTime UtcNow { get; }
        internal MapChannel MapChannel { get; }
        internal MissionScenarioPlan Plan { get; }
    }
}
