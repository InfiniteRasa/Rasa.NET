using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Structures
{
    using Game;
    using Managers;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Structures.Char;

    internal sealed class MissionScenarioPlan : IDisposable
    {
        private readonly List<Action> _runtimeConvergence = new();
        private readonly List<Action> _publications = new();
        private readonly List<Action> _postCommit = new();
        private readonly List<MissionProgressPublicationPlan> _progressPlans = new();
        private readonly List<MissionFailurePublicationPlan> _failurePlans = new();
        private readonly List<MissionRewardGrant> _rewardGrants = new();
        private readonly Dictionary<uint, MissionLog> _committedMissions = new();
        private IReadOnlyDictionary<uint, uint> _flags;

        internal List<string> StepKeysToAdd { get; } = new();
        internal List<string> StepKeyPrefixesToRemove { get; } = new();
        internal List<string> ExactStepKeysToRemove { get; } = new();
        internal List<string> DurableKeyPrefixesToRemove { get; } = new();
        internal bool HasChanges =>
            _flags != null || StepKeysToAdd.Count > 0 ||
            StepKeyPrefixesToRemove.Count > 0 ||
            ExactStepKeysToRemove.Count > 0 ||
            DurableKeyPrefixesToRemove.Count > 0 ||
            _runtimeConvergence.Count > 0 ||
            _publications.Count > 0 ||
            _rewardGrants.Count > 0 ||
            _committedMissions.Count > 0 ||
            _progressPlans.Count > 0 ||
            _failurePlans.Count > 0 ||
            _postCommit.Count > 0;

        internal void AddRewardGrant(MissionRewardGrant grant)
        {
            if (grant != null)
                _rewardGrants.Add(grant);
        }

        internal void AddRuntimeConvergence(Action convergence)
        {
            if (convergence != null)
                _runtimeConvergence.Add(convergence);
        }

        internal void AddPublication(Action publication)
        {
            if (publication != null)
                _publications.Add(publication);
        }

        internal void AddPostCommit(Action action)
        {
            if (action != null)
                _postCommit.Add(action);
        }

        internal void AddProgressPlan(MissionProgressPublicationPlan publicationPlan)
        {
            if (publicationPlan.HasChanges)
            {
                _progressPlans.Add(publicationPlan);
                CaptureFlags(publicationPlan.FlagSnapshot);
            }
        }

        internal void AddFailurePlan(MissionFailurePublicationPlan failurePlan)
        {
            if (!ReferenceEquals(failurePlan, MissionFailurePublicationPlan.Empty))
            {
                _failurePlans.Add(failurePlan);
                CaptureFlags(failurePlan.FlagSnapshot);
            }
        }

        internal void CaptureFlags(IReadOnlyDictionary<uint, uint> flags)
        {
            if (flags != null)
                _flags = flags;
        }

        internal void CaptureMission(ICharUnitOfWork unit, CharacterMissionEntry mission,
            IReadOnlyDictionary<uint, CharacterMissionObjectiveEntry> objectives) =>
            TransactionValidation.BeforeValidation(unit, () =>
                _committedMissions[mission.MissionId] = MissionStatePublication.Capture(mission, objectives));

        internal void ApplyRuntime(Client client, ManifestationManager manifestationManager, MissionApplication missionManager)
        {
            if (!MissionStatePublication.Converge(client,
                _progressPlans.SelectMany(plan => plan.CommittedMissions.Values).Concat(_committedMissions.Values)))
                return;
            if (_flags != null)
            {
                client.FlagProjection.ApplyCommitted(client, _flags);
                missionManager.PublishCharacterFlags(client);
            }
            foreach (var rewardGrant in _rewardGrants)
                rewardGrant.ConvergeRuntime(client);
            foreach (var convergence in _runtimeConvergence)
                convergence();
            foreach (var failurePlan in _failurePlans)
                failurePlan.Publish(
                    client,
                    missionManager,
                    missionManager.TryExecuteScenario,
                    missionManager.TryExecuteFailureTransitionScenario,
                    convergeFlags: false,
                    convergeMission: false);
            foreach (var progressPlan in _progressPlans)
                progressPlan.Publish(client, convergeFlags: false, convergeMissions: false);
            foreach (var rewardGrant in _rewardGrants)
                rewardGrant.Publish(client, manifestationManager);
            foreach (var publication in _publications)
                publication();
            foreach (var action in _postCommit)
                action();
            missionManager.RefreshNpcConversationStatuses(client);
        }

        public void Dispose()
        {
            foreach (var rewardGrant in _rewardGrants)
                rewardGrant.Dispose();
        }
    }
}
