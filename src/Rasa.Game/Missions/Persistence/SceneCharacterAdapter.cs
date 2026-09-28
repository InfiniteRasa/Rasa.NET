using System;
using System.Linq;
using Rasa.Game.Missions.Integration;

namespace Rasa.Game.Missions.Persistence
{
    using Data;
    using Managers;
    using global::Rasa.Missions.Scenes;
    using Repositories.Char;
    using Structures;
    using Structures.Char;
    using Structures.World;

    internal sealed class SceneCharacterAdapter
    {
        private readonly MissionApplication _missions;
        private readonly ManifestationManager _manifestations;
        internal SceneCharacterAdapter(MissionApplication missions, ManifestationManager manifestations)
        { _missions = missions; _manifestations = manifestations; }

        internal void Apply(Client client, SceneRun run, CharacterIntent intent,
            ICharUnitOfWork unit, MissionScenarioPlan publication)
        {
            if (client?.Player?.Id != run.OwnerCharacterId)
                throw new GameplayRejectionException($"Run {run.Id} has no authoritative character adapter.");
            switch (intent)
            {
                case OfferRadioMissionIntent offer:
                    var sourceScene = unit.CharacterMissions.Runtime.Scene(run.Id);
                    var sourceParticipant = unit.CharacterMissions.Runtime.Participants(run.Id)
                        .SingleOrDefault(entry => entry.CharacterId == client.Player.Id);
                    var source = new global::Rasa.Missions.Definitions.MissionOfferSourceIdentity(
                        global::Rasa.Missions.Definitions.MissionOfferSourceKind.Scene,
                        run.ScriptKey, run.Id, run.Generation, sourceScene?.AssignmentId,
                        sourceParticipant?.AssignmentGeneration ?? 0);
                    var publishOffer = _missions.Offers.PlanOffer(client, offer.MissionId, source, unit, offer.ForceDialog);
                    if (publishOffer != null)
                        publication.AddPublication(publishOffer);
                    break;
                case GrantRewardIntent reward:
                    if (!_missions.GetRewardPackages(reward.MissionId).TryGetValue(reward.RewardId, out var definition))
                        throw new GameplayRejectionException($"Unknown reward package {reward.MissionId}:{reward.RewardId}.");
                    var character = unit.Characters.Get(client.Player.Id);
                    if (character.AccountId != client.AccountEntry?.Id ||
                        !_manifestations.ValidateProgressionForClient(client))
                        throw new GameplayRejectionException("Scene reward character ownership/progression changed.");
                    var grant = definition.CreateScenarioGrant(_missions.BeforeRewardItemPublication);
                    publication.AddRewardGrant(grant);
                    grant.PlanAndSave(client, character, unit, _manifestations);
                    publication.AddProgressPlan(_missions.PlanProgress(client, grant.CreateItemAcquisitionEvents(), unit));
                    break;
                case ObjectiveIntent objective:
                    var action = objective.State switch
                    {
                        MissionObjectiveState.NotAssigned => MissionScenarioStepKind.RevealObjective,
                        MissionObjectiveState.Incomplete => MissionScenarioStepKind.ActivateObjective,
                        MissionObjectiveState.Completed => MissionScenarioStepKind.CompleteObjective,
                        MissionObjectiveState.Failed => MissionScenarioStepKind.FailObjective,
                        _ => throw new GameplayRejectionException("Unsupported authored objective state.")
                    };
                    if (!_missions.TryPlanScenarioObjectiveAction(client, objective.MissionId, objective.ObjectiveId,
                        action, unit, publication))
                        throw new GameplayRejectionException($"Cannot apply scene objective {objective.MissionId}:{objective.ObjectiveId}.");
                    break;
                case GrantAbilityIntent ability:
                    var learned = unit.CharacterSkills.GetCharacterSkills(client.Player.Id)
                        .SingleOrDefault(skill => skill.SkillId == ability.SkillId);
                    if (learned != null && learned.SkillLevel >= ability.Level)
                        break;
                    unit.CharacterSkills.AddOrUpdate(client.Player.Id, ability.SkillId, (int)ability.AbilityId, ability.Level);
                    if (ability.Slot.HasValue)
                        unit.CharacterAbilityDrawers.AddOrUpdate(client.Player.Id, ability.Slot.Value,
                            (int)ability.AbilityId, ability.Level);
                    publication.AddRuntimeConvergence(() =>
                    {
                        client.Player.Skills[(SkillId)ability.SkillId] =
                            new SkillsData((SkillId)ability.SkillId, (int)ability.AbilityId, ability.Level);
                        if (ability.Slot.HasValue)
                            client.Player.Abilities[ability.Slot.Value] =
                                new AbilityDrawerData(ability.Slot.Value, (int)ability.AbilityId, ability.Level);
                    });
                    publication.AddPublication(() =>
                    {
                        client.CallMethod(client.Player.EntityId,
                            new Packets.MapChannel.Server.SkillsPacket(client.Player.Skills));
                        if (ability.Slot.HasValue)
                            client.CallMethod(client.Player.EntityId,
                                new Packets.MapChannel.Server.AbilityDrawerPacket(client.Player.Abilities));
                    });
                    break;
                case SetQualificationIntent qualification:
                    if (!Enum.IsDefined(typeof(CharacterQualificationKey), qualification.Qualification))
                        throw new GameplayRejectionException("Unknown scene qualification.");
                    SetFlag(client, CharacterFlagIds.FromQualification(qualification.Qualification),
                        qualification.Present ? 1U : null, unit, publication);
                    break;
                case SetCharacterFlagIntent flag:
                    SetFlag(client, flag.FlagId, flag.Value, unit, publication);
                    break;
                case SetEntitlementIntent entitlement:
                    unit.GameAccounts.UpdateCanSkipBootcamp(client.AccountEntry.Id, entitlement.Enabled);
                    MissionRequirementService.ExpectEntitlement(unit, client.Player.Id, entitlement.Enabled);
                    publication.AddRuntimeConvergence(client.ReloadGameAccountEntry);
                    break;
                case IssueMissionItemIntent:
                case ConsumeMissionItemIntent:
                case RemoveMissionItemsIntent:
                    var missionId = intent switch
                    {
                        IssueMissionItemIntent issue => issue.MissionId,
                        ConsumeMissionItemIntent consume => consume.MissionId,
                        RemoveMissionItemsIntent remove => remove.MissionId,
                        _ => 0U
                    };
                    var scene = unit.CharacterMissions.Runtime.Scene(run.Id);
                    var participant = unit.CharacterMissions.Runtime.Participants(run.Id)
                        .SingleOrDefault(entry => entry.CharacterId == client.Player.Id);
                    if (run.MissionId != missionId || scene?.MissionId != missionId ||
                        scene.OwnerCharacterId != client.Player.Id || scene.Generation != run.Generation ||
                        participant == null || !participant.Active || participant.AssignmentId != scene.AssignmentId)
                        throw new GameplayRejectionException("Scene item intent has no exact assignment participant.");
                    var itemPlan = new MissionItemPlanner(client, unit, _missions);
                    itemPlan.Apply(intent, participant.AssignmentId, participant.AssignmentGeneration);
                    publication.AddRuntimeConvergence(() => itemPlan.Publish(client));
                    break;
                case MissionDeadlineIntent deadline:
                    var existingDeadline = unit.CharacterMissionDeadlines.Get(client.Player.Id, deadline.MissionId);
                    if (deadline.Kind == DeadlineIntentKind.Start)
                        unit.CharacterMissionDeadlines.AddOrUpdate(client.Player.Id, deadline.MissionId,
                            DateTime.UtcNow.AddMilliseconds(deadline.Milliseconds), CharacterMissionDeadlineState.Active);
                    else if (existingDeadline?.State == CharacterMissionDeadlineState.Active)
                        unit.CharacterMissionDeadlines.SetState(client.Player.Id, deadline.MissionId,
                            deadline.Kind == DeadlineIntentKind.Satisfy
                                ? CharacterMissionDeadlineState.Satisfied : CharacterMissionDeadlineState.Cancelled);
                    if (deadline.Kind != DeadlineIntentKind.Start)
                        _missions.Scenes.EndDeadline(unit, run, deadline.MissionId);
                    publication.AddPublication(() => _missions.PublishMissionStatus(client, deadline.MissionId,
                        $"scene {run.Id} deadline {deadline.Kind}"));
                    break;
                default:
                    throw new GameplayRejectionException($"Unsupported character intent {intent.GetType().Name}.");
            }
        }

        private static void SetFlag(Client client, uint flagId, uint? value,
            ICharUnitOfWork unit, MissionScenarioPlan publication)
        {
            if (value.HasValue)
                unit.CharacterFlags.Set(client.Player.Id, flagId, value.Value);
            else
                unit.CharacterFlags.Remove(client.Player.Id, flagId);
            MissionRequirementService.ExpectFlag(unit, client.Player.Id, flagId, value);
            publication.CaptureFlags(unit.CharacterFlags.Get(client.Player.Id));
            var startingExperienceCompleted = flagId == CharacterFlagIds.BootcampComplete
                ? MissionRequirementFactsAdapter.HasCompletedStartingExperience(unit, client.Player.Id)
                : (bool?)null;
            if (startingExperienceCompleted.HasValue)
                publication.AddRuntimeConvergence(() =>
                    client.Player.StartingExperienceCompleted = startingExperienceCompleted.Value);
        }
    }
}
